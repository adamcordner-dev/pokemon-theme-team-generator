/**
 * build-synonyms.mjs
 *
 * Build a generated synonyms file using:
 * - Local dataset vocabulary (pokemon.json)
 * - enrichment-rules.json (detect + expand triggers)
 * - Datamuse API for synonym suggestions
 *
 * This runs locally at build-time. The deployed app just uses the JSON.
 */

import fs from "node:fs/promises";
import path from "node:path";

const ROOT = path.resolve(process.cwd());
const DATA_DIR = path.join(ROOT, "data");
const TOOLS_DIR = path.join(ROOT, "tools");
const CACHE_DIR = path.join(TOOLS_DIR, "cache");

const POKEMON_FILE = path.join(DATA_DIR, "pokemon.json");
const RULES_FILE = path.join(DATA_DIR, "enrichment-rules.json");

const OUT_FILE = path.join(DATA_DIR, "synonyms.generated.json");

const DATAMUSE = "https://api.datamuse.com/words";

const CONCURRENCY = 3;

const MAX_SYNONYMS_PER_WORD = 10; // keep small to avoid exploding expansions
const MAX_WORDS_TO_QUERY = 250;   // cap runtime / output size

// Words we generally don't want to expand to avoid noise
const BLOCKLIST = new Set([
  "pokemon", "pokémon", "poke", "team",
  "type", "form", "mega", "gmax",
  "male", "female",
  "generation", "gen",
]);

const GLOBAL_DENY = new Set([
  "upchuck", "barf", "vomit", "retch", "regurgitate", "disgorge", "regorge",
  "cad", "blackguard", "hombre", "guy", "cast", "click", "tag", "pawl", "andiron"
]);

// Words we will allow to fetch synonyms for using the more general "related words" (ml) endpoint instead of strict synonyms (rel_syn)
const USE_RELATED_ML = new Set([
  "fantasy", "magic", "magical", "horror", "spooky", "haunted", "myth", "mystic",
  "cute", "cool", "weird"
]);

async function main() {
  await fs.mkdir(CACHE_DIR, { recursive: true });

  const rules = await readJsonOptional(RULES_FILE) ?? { detect: {}, expand: {} };

  const seedWords = new Set();

  for (const key of Object.keys(rules.detect ?? {})) {
    seedWords.add(key);
  }
  for (const key of Object.keys(rules.expand ?? {})) {
    seedWords.add(key);
  }

  for (const words of Object.values(rules.detect ?? {})) {
    for (const w of words) {
      seedWords.add(String(w).toLowerCase());
    }
  }
  for (const words of Object.values(rules.expand ?? {})) {
    for (const w of words) {
      seedWords.add(String(w).toLowerCase());
    }
  }

  const pokemon = await readJson(POKEMON_FILE);
  for (const entry of pokemon.pokemon ?? []) {
    for (const tag of entry.tags?.curated ?? []) {
      seedWords.add(String(tag).toLowerCase());
    }
  }

  const seeds = [...seedWords]
    .map(normalizeWord)
    .filter(w => isAllowedSeed(w))
    .slice(0, MAX_WORDS_TO_QUERY);

  console.log(`Seeds to query: ${seeds.length}`);

  const generated = {};

  await runWithConcurrency(seeds, CONCURRENCY, async (word, idx) => {
    if ((idx + 1) % 25 === 0) {
      console.log(`... ${idx + 1}/${seeds.length}`);
    }

    const syns = await fetchSynonyms(word);

    if (syns.length > 0) {
      generated[word] = syns;
    }
  });

  const out = {
    version: 1,
    generatedAt: new Date().toISOString(),
    source: "datamuse",
    synonyms: generated,
  };

  await fs.writeFile(OUT_FILE, JSON.stringify(out, null, 2), "utf8");
  console.log(`Wrote: ${OUT_FILE}`);
}

function normalizeWord(w) {
  return String(w ?? "")
    .trim()
    .toLowerCase();
}

function isAllowedSeed(w) {
  if (!w) {
    return false;
  }
  if (BLOCKLIST.has(w)) {
    return false;
  }
  if (w.length < 3) {
    return false;
  }
  if (w.includes(" ")) {
    return false;
  }
  if (!/^[a-z-]+$/.test(w)) {
    return false;
  }
  return true;
}

async function fetchSynonyms(word) {
  const seedPos = await fetchSeedPos(word);

  const topic = topicForSeed(word);
  const isRelated = USE_RELATED_ML.has(word);
  const relParam = isRelated ? "ml" : "rel_syn";

  const url =
    `${DATAMUSE}?${relParam}=${encodeURIComponent(word)}&max=50&md=p` +
    (topic ? `&topics=${encodeURIComponent(topic)}` : "");

  const cachePath = cachePathForUrl(url);

  const cached = await tryReadJson(cachePath);
  if (cached) {
    return filterSynonyms(word, cached, seedPos);
  }

  const res = await fetch(url, {
    headers: { "User-Agent": "pokemon-theme-team-generator (local dev)" },
  });

  if (!res.ok) {
    console.warn(`Datamuse HTTP ${res.status} for ${word}`);
    return [];
  }

  const json = await res.json();
  await fs.writeFile(cachePath, JSON.stringify(json, null, 2), "utf8");

  return filterSynonyms(word, json, seedPos);
}

function filterSynonyms(seed, datamuseRows, seedPos) {
  const seedNorm = normalizeWord(seed);

  const out = [];
  for (const row of datamuseRows ?? []) {
    const w = normalizeWord(row?.word);
    if (!w) {
      continue;
    }
    if (w === seedNorm) {
      continue;
    }
    if (w.includes(" ")) {
      continue;
    }
    if (!/^[a-z-]+$/.test(w)) {
      continue;
    }
    if (w.length < 3) {
      continue;
    }
    if (BLOCKLIST.has(w)) {
      continue;
    }
    if (GLOBAL_DENY.has(w)) {
      continue;
    }

    const tags = Array.isArray(row?.tags) ? row.tags : [];
    const pos = getPos(tags);

    // Hard rule: keep nouns/adjectives only
    if (pos && pos !== "n" && pos !== "adj") {
      continue;
    }

    // Strong preference: if seedPos known, require same POS (or keep if unknown)
    if (seedPos && pos && pos !== seedPos) {
      continue;
    }

    out.push(w);

    if (out.length >= MAX_SYNONYMS_PER_WORD) {
      break;
    }
  }

  return [...new Set(out)];
}

function cachePathForUrl(url) {
  const raw = url.replace("https://", "");
  const safe = raw.replace(/[^a-zA-Z0-9._-]+/g, "__");
  return path.join(CACHE_DIR, `${safe}.json`);
}

async function readJson(filePath) {
  const raw = await fs.readFile(filePath, "utf8");
  return JSON.parse(raw);
}

async function readJsonOptional(filePath) {
  try {
    return await readJson(filePath);
  } catch {
    return null;
  }
}

async function tryReadJson(filePath) {
  try {
    const raw = await fs.readFile(filePath, "utf8");
    return JSON.parse(raw);
  } catch {
    return null;
  }
}

async function runWithConcurrency(items, limit, worker) {
  const queue = [...items];

  const runners = Array.from({ length: limit }, async () => {
    while (queue.length) {
      const item = queue.shift();
      const idx = items.length - queue.length - 1;
      await worker(item, idx);
    }
  });

  await Promise.all(runners);
}

async function fetchSeedPos(word) {
  const url = `${DATAMUSE}?sp=${encodeURIComponent(word)}&max=1&md=p`;
  const cachePath = cachePathForUrl(url);

  const cached = await tryReadJson(cachePath);
  const rows = cached ?? (await (await fetch(url)).json());

  const tags = Array.isArray(rows?.[0]?.tags) ? rows[0].tags : [];

  if (tags.includes("n")) {
    return "n";
  }
  if (tags.includes("adj")) {
    return "adj";
  }
  return null;
}

function getPos(tags) {
  if (!Array.isArray(tags)) {
    return null;
  }
  if (tags.includes("n")) {
    return "n";
  }
  if (tags.includes("adj")) {
    return "adj";
  }
  if (tags.includes("v")) {
    return "v";
  }
  return null;
}

function topicForSeed(seed) {
  // Very small, high-impact mapping, can be extended over time
  const s = normalizeWord(seed);

  if (["cat", "feline", "kitten", "dog", "canine", "puppy", "hound", "fox", "wolf"].includes(s)) {
    return "animals";
  }

  if (["spooky", "haunted", "ghost", "wraith", "specter", "spectre", "horror"].includes(s)) {
    return "horror";
  }

  return null;
}

await main();

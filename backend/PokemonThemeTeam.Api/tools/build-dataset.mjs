/**
 * build-dataset.mjs
 *
 * Build a local dataset file using:
 * - PokeAPI for comprehensive Pokémon data
 * - caches API responses to avoid hammering PokeAPI / speed up reruns
 *
 * This runs locally at build-time. The deployed app just uses the JSON.
 */

import fs from "node:fs/promises";
import path from "node:path";

const API = "https://pokeapi.co/api/v2";
const ROOT = path.resolve(process.cwd());
const CACHE_DIR = path.join(ROOT, "tools", "cache");
const OUT_FILE = path.join(ROOT, "data", "pokemon.json");
const RULES_FILE = path.join(ROOT, "data", "enrichment-rules.json");

const CONCURRENCY = 5;

const CURATION_RULES_FILE = path.join(ROOT, "data", "curation-rules.json");

const curationRules = await loadJsonOptional(CURATION_RULES_FILE) ?? { rules: [] };

async function main() {
  await fs.mkdir(CACHE_DIR, { recursive: true });

  const list = await getJson(`${API}/pokemon?limit=3000`);

  const names = list.results.map((r) => r.name);

  const pokemonMap = new Map();
  const speciesMap = new Map();
  const formMap = new Map();

  console.log(`Found ${names.length} pokemon entries (including forms). Fetching...`);

  await runWithConcurrency(names, CONCURRENCY, async (name, idx) => {
    if ((idx + 1) % 100 === 0) { 
      console.log(`... ${idx + 1}/${names.length}`);
    }

    const pokemon = await getJson(`${API}/pokemon/${name}`);
    pokemonMap.set(name, pokemon);

    const speciesName = pokemon.species?.name ?? name;
    if (!speciesMap.has(speciesName)) {
      const species = await getJson(`${API}/pokemon-species/${speciesName}`);
      speciesMap.set(speciesName, species);
    }

    try {
      const form = await getJson(`${API}/pokemon-form/${name}`);
      formMap.set(name, form);
    } catch {
      // Some edge cases may not have a form endpoint
    }
  });

  const evoChainMap = new Map();

  for (const species of speciesMap.values()) {
    const url = species.evolution_chain?.url;
    if (!url) {
      continue;
    }
    const chainId = extractId(url);

    if (!evoChainMap.has(chainId)) {
      const chain = await getJson(url);
      evoChainMap.set(chainId, chain);
    }
  }

  const evoInfoBySpecies = new Map();
  
  for (const [chainId, chain] of evoChainMap.entries()) {
    const roots = [];
    const leaves = [];

    function walk(node, depth) {
      const speciesName = node.species?.name;
      if (speciesName) {
        if (depth === 0) {
          roots.push(speciesName);
        }
        if (!node.evolves_to || node.evolves_to.length === 0) {
          leaves.push(speciesName);
        }
      }
      for (const child of node.evolves_to ?? []) {
        walk(child, depth + 1);
      }
    }

    walk(chain.chain, 0);

    for (const r of roots) {
      evoInfoBySpecies.set(r, {
        chainId,
        isUnevolved: true,
        isFullyEvolved: leaves.includes(r),
      });
    }
    for (const l of leaves) {
      const existing = evoInfoBySpecies.get(l);
      evoInfoBySpecies.set(l, {
        chainId,
        isUnevolved: existing?.isUnevolved ?? false,
        isFullyEvolved: true,
      });
    }
  }

  const enrichmentRules = await loadEnrichmentRules();

  const dataset = [];

  for (const [name, pokemon] of pokemonMap.entries()) {
    const speciesKey = pokemon.species?.name ?? name;
    const species = speciesMap.get(speciesKey);

    const isLegendary = !!species?.is_legendary;
    const isMythical = !!species?.is_mythical;
    const isBaby = !!species?.is_baby;

    const generation = parseGenerationNumber(species?.generation?.name);

    const evoInfo = evoInfoBySpecies.get(speciesKey) ?? {
      chainId: 0,
      isUnevolved: false,
      isFullyEvolved: false,
    };

    const types = (pokemon.types ?? [])
      .sort((a, b) => a.slot - b.slot)
      .map((t) => t.type.name);

    const officialArtwork =
      pokemon.sprites?.other?.["official-artwork"]?.front_default ?? null;

    const fallbackSprite =
      pokemon.sprites?.front_default ??
      pokemon.sprites?.other?.["home"]?.front_default ??
      null;

    const artUrl = officialArtwork ?? fallbackSprite ?? "";

    const form = formMap.get(name);
    const region = inferRegion(name);
    const formType = classifyFormType(name, form, region);
    const isMega = !!form?.is_mega || formType === "mega";
    const isGmax = name.endsWith("-gmax");

    const speciesEnName = getEnglishNameFromSpecies(species) ?? titleCase(speciesKey);
    const displayName = formatFormDisplayName(
      speciesEnName,
      name,
      formType,
      region,
      form
    );

    const color = species?.color?.name ?? null;
    const shape = species?.shape?.name ?? null;
    const habitat = species?.habitat?.name ?? null;
    const eggGroups = (species?.egg_groups ?? []).map((g) => normalizeEggGroup(g.name));
    const growthRate = species?.growth_rate?.name ?? null;
    const genusTokens = getEnglishGenusTokens(species);
    const speciesTokens = tokensFromName(speciesKey);
    const abilityNames = (pokemon.abilities ?? []).map((a) => a.ability.name);
    const abilityTokens = abilityNames.flatMap(tokensFromName);
    const formName = form?.form_name || "";
    const formTokens = tokensFromName(formName);
    const sizeTokens = sizeBuckets(pokemon.height, pokemon.weight);

    const derivedTags = [
      ...types.map((t) => `type:${t}`),
      generation ? `gen:${generation}` : null,
      evoInfo.isUnevolved ? "unevolved" : null,
      evoInfo.isFullyEvolved ? "fully-evolved" : null,
      isLegendary ? "legendary" : null,
      isMythical ? "mythical" : null,
      isBaby ? "baby" : null,
      formType !== "base" ? `form:${formType}` : null,

      color ? `color:${color}` : null,
      shape ? `shape:${shape}` : null,
      habitat ? `habitat:${habitat}` : null,
      growthRate ? `growthRate:${growthRate}` : null,
      ...eggGroups.map((g) => `eggGroup:${g}`),
    ].filter(Boolean);

    const textDerivedTokensAll = [
      ...abilityTokens,
      ...speciesTokens,
      ...formTokens,
      ...sizeTokens,
      ...genusTokens,
      ...(color ? [color] : []),
      ...(shape ? [shape] : []),
      ...(habitat ? [habitat] : []),
      ...eggGroups,
    ];

    const textDerivedTokensTrusted = [
      ...speciesTokens,
      ...genusTokens,
      ...(color ? [color] : []),
      ...(shape ? [shape] : []),
      ...(habitat ? [habitat] : []),
      ...eggGroups,
    ];

    const textAll = [...new Set(textDerivedTokensAll)];
    const trusted = [...new Set(textDerivedTokensTrusted)];

    const enrichedTrusted = applyEnrichmentRules(trusted, derivedTags, enrichmentRules);

    const finalTextDerived = [...new Set([...textAll, ...enrichedTrusted])];

    const curatedFromRules = applyCurationRules(
      derivedTags,
      finalTextDerived,
      curationRules
    );

    const curated = [...new Set([ ...curatedFromRules ])];

    dataset.push({
      key: name,
      dex: { national: pokemon.id, generation },
      names: { default: displayName },
      species: { key: speciesKey, id: species?.id ?? pokemon.id },

      form: {
        type: formType,
        isRegional: formType === "regional",
        region: region,
        isMega,
        megaOfSpeciesKey: isMega ? speciesKey : null,
        isGmax,
        gmaxOfSpeciesKey: isGmax ? speciesKey : null,
      },

      typing: {
        types,
        primary: types[0] ?? "",
        secondary: types[1] ?? null,
      },

      flags: { isLegendary, isMythical, isBaby },

      evolution: {
        chainId: evoInfo.chainId,
        isUnevolved: evoInfo.isUnevolved,
        isFullyEvolved: evoInfo.isFullyEvolved,
      },

      art: {
        preferred: artUrl,
        variants: {
          officialArtwork: officialArtwork,
          fallback: fallbackSprite,
        },
      },

      tags: {
        derived: derivedTags,
        textDerived: finalTextDerived,
        curated: curated,
      },

      search: {
        aliases: buildAliases(name, speciesKey),
      },
    });
  }

  const output = {
    version: 1,
    generatedAt: new Date().toISOString(),
    pokemon: dataset,
  };

  await fs.writeFile(OUT_FILE, JSON.stringify(output, null, 2), "utf8");
  console.log(`Wrote dataset: ${OUT_FILE}`);
}

function parseGenerationNumber(genName) {
  if (!genName) {
    return 0;
  }
  const roman = genName.replace("generation-", "").toUpperCase();
  return romanToInt(roman);
}

function romanToInt(roman) {
  const map = { I: 1, V: 5, X: 10, L: 50, C: 100, D: 500, M: 1000 };
  let total = 0;
  let prev = 0;
  for (let i = roman.length - 1; i >= 0; i--) {
    const val = map[roman[i]] ?? 0;
    if (val < prev) {
      total -= val;
    }
    else {
      total += val;
    }
    prev = val;
  }
  return total;
}

function titleCase(name) {
  return name
    .split("-")
    .map((p) => (p ? p[0].toUpperCase() + p.slice(1) : p))
    .join(" ");
}

function buildAliases(pokemonKey, speciesKey) {
  const aliases = new Set();
  aliases.add(pokemonKey.replaceAll("-", " "));
  aliases.add(speciesKey.replaceAll("-", " "));
  aliases.add(speciesKey);
  return [...aliases];
}

function extractId(url) {
  const m = url.match(/\/(\d+)\/?$/);
  return m ? Number(m[1]) : 0;
}

function inferRegion(name) {
  if (name.includes("-alola")) {
    return "alola";
  }
  if (name.includes("-galar")) {
    return "galar";
  }
  if (name.includes("-hisui")) {
    return "hisui";
  }
  if (name.includes("-paldea")) {
    return "paldea";
  }
  return null;
}

function classifyFormType(name, form, region) {
  if (name.endsWith("-gmax")) {
    return "gmax";
  }
  if (form?.is_mega) {
    return "mega";
  }

  if (region) {
    return "regional";
  }

  if (form?.is_default === true) {
    return "base";
  }

  if (name.includes("-") && !name.endsWith("-gmax")) {
    if (!region && !form?.is_mega) {
      return "other";
    }
  }

  return "base";
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

async function getJson(url) {
  const rawKey = url.replace("https://", "");
  const cacheKey = rawKey.replace(/[^a-zA-Z0-9._-]+/g, "__");
  const cachePath = path.join(CACHE_DIR, `${cacheKey}.json`);

  try {
    const cached = await fs.readFile(cachePath, "utf8");
    return JSON.parse(cached);
  } catch {
    // ignore cache miss
  }

  const res = await fetch(url, {
    headers: { "User-Agent": "pokemon-theme-team-generator (local dev)" },
  });

  if (!res.ok) {
    throw new Error(`HTTP ${res.status} for ${url}`);
  }

  const json = await res.json();
  await fs.writeFile(cachePath, JSON.stringify(json, null, 2), "utf8");
  return json;
}

function getEnglishNameFromSpecies(species) {
  const en = species?.names?.find((n) => n.language?.name === "en")?.name;
  return en ?? null;
}

function formatFormDisplayName(speciesEnName, pokemonKey, formType, region, form) {
  let base = speciesEnName ?? titleCase(pokemonKey);

  if (formType === "gmax") {
    return `Gigantamax ${base}`;
  }

  if (formType === "mega") {
    if (pokemonKey.endsWith("-mega-x")) {
      return `Mega ${base} X`;
    }
    if (pokemonKey.endsWith("-mega-y")) {
      return `Mega ${base} Y`;
    }
    if (pokemonKey.endsWith("-mega-z")) {
      return `Mega ${base} Z`;
    }
    return `Mega ${base}`;
  }

  if (formType === "regional" && region) {
    const regionalAdj = {
      alola: "Alolan",
      galar: "Galarian",
      hisui: "Hisuian",
      paldea: "Paldean",
    }[region] ?? `${region[0].toUpperCase()}${region.slice(1)}ian`;

    return `${regionalAdj} ${base}`;
  }

  const formName = form?.form_name ? titleCase(form.form_name.replaceAll("-", " ")) : "";
  if (formType === "other" && formName) {
    return `${base} (${formName})`;
  }

  return base;
}

function tokensFromName(text) {
  const ascii = String(text || "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "");

  return ascii
    .toLowerCase()
    .split(/[^a-z0-9]+/g)
    .filter(Boolean);
}


function getEnglishGenusTokens(species) {
  const genusEn = species?.genera?.find((g) => g.language?.name === "en")?.genus ?? null;
  if (!genusEn) {
    return [];
  }

  return tokensFromName(genusEn).filter((t) => t !== "pokemon");
}

function sizeBuckets(heightDecimetres, weightHectograms) {
  const heightM = (heightDecimetres ?? 0) / 10;
  const weightKg = (weightHectograms ?? 0) / 10;

  const buckets = [];

  if (heightM > 0) {
    if (heightM < 0.5) {
      buckets.push("tiny");
    }
    else if (heightM < 1.2) {
      buckets.push("small");
    }
    else if (heightM < 2.0) {
      buckets.push("medium");
    }
    else if (heightM < 3.5) {
      buckets.push("large");
    }
    else {
      buckets.push("giant");
    }
  }

  if (weightKg > 0) {
    if (weightKg < 10) {
      buckets.push("light");
    }
    else if (weightKg < 50) {
      buckets.push("heavy");
    }
    else if (weightKg < 150) {
      buckets.push("veryheavy");
    }
    else {
      buckets.push("massive");
    }
  }

  return buckets;
}

async function loadEnrichmentRules() {
  try {
    const raw = await fs.readFile(RULES_FILE, "utf8");
    return JSON.parse(raw);
  } catch {
    return { detect: {}, expand: {} };
  }
}

function applyEnrichmentRules(trustedTokens, derivedTags, rules) {
  const trustedSet = new Set(trustedTokens);
  const derivedSet = new Set(derivedTags);

  const all = new Set([...trustedSet, ...derivedSet]);

  for (const [tag, words] of Object.entries(rules.detect ?? {})) {
    if (words.some((w) => trustedSet.has(w))) {
      trustedSet.add(tag);
      all.add(tag);
    }
  }

  const MAX_PASSES = 2;
  for (let pass = 0; pass < MAX_PASSES; pass++) {
    let changed = false;

    for (const [trigger, addTokens] of Object.entries(rules.expand ?? {})) {
      if (!all.has(trigger)) {
        continue;
      }

      for (const t of addTokens) {
        if (!all.has(t)) {
          all.add(t);
          trustedSet.add(t);
          changed = true;
        }
      }
    }

    if (!changed) break;
  }

  return [...trustedSet];
}


function normalizeEggGroup(g) {
  return String(g || "").replace(/\d+$/g, "");
}

async function loadJsonOptional(filePath) {
  try {
    const raw = await fs.readFile(filePath, "utf8");
    return JSON.parse(raw);
  } catch {
    return null;
  }
}

function applyCurationRules(derivedTags, textTokens, rulesRoot) {
  const derivedSet = new Set((derivedTags ?? []).map((t) => String(t).toLowerCase()));
  const textSet = new Set((textTokens ?? []).map((t) => String(t).toLowerCase()));

  const out = new Set();

  const rules = rulesRoot?.rules;

  if (rules && typeof rules === "object") {
    for (const [triggerRaw, adds] of Object.entries(rules)) {
      const trigger = String(triggerRaw).toLowerCase();

      const triggerMatches = derivedSet.has(trigger) || textSet.has(trigger);
      if (!triggerMatches) {
        continue;
      }

      if (Array.isArray(adds)) {
        for (const add of adds) {
          out.add(String(add).toLowerCase());
        }
      }
    }

    return [...out];
  }

  return [...out];
}



await main();

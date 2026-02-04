import { useMemo, useState } from "react";
import "./App.css";

/** API response item */
type PokemonResult = {
  key: string;
  name: string;
  artUrl: string;
  types: string[];
  reasons: string[];
};

/** What the API returns */
type GenerateTeamResponse = {
  interpreted: {
    rawTokens: string[];
    expandedTokens: string[];
    unknownTokens: string[];
  };
  team: PokemonResult[];
};

/** Helper to avoid `any` and keep error handling safe */
function getErrorMessage(e: unknown): string {
  if (e instanceof Error) return e.message;
  if (typeof e === "string") return e;
  return "Something went wrong";
}

/**
 * Small helper to add/remove a number from an array (for multi-select checkboxes).
 * Keeps generation selection logic readable.
 */
function toggleNumber(list: number[], value: number): number[] {
  return list.includes(value) ? list.filter((x) => x !== value) : [...list, value];
}

export default function App() {
  // --- Core input ---
  const [themeText, setThemeText] = useState("spooky dog");

  // --- User-configurable parameters (Milestone 1 UI) ---
  const [teamSize, setTeamSize] = useState(6);

  // We send these exact strings to backend validation ("any" | "fullyEvolved" | "unevolved")
  const [evolutionStage, setEvolutionStage] = useState<"any" | "fullyEvolved" | "unevolved">(
    "any"
  );

  // Generations are a list so the user can choose any combination (e.g. 1,2,7)
  const [generations, setGenerations] = useState<number[]>([]);

  const [excludeLegendaries, setExcludeLegendaries] = useState(true);
  const [allowForms, setAllowForms] = useState(true);
  const [allowMega, setAllowMega] = useState(false);
  const [allowGmax, setAllowGmax] = useState(false);

  // --- UI state ---
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [data, setData] = useState<GenerateTeamResponse | null>(null);

  // Basic frontend validation to prevent pointless API calls
  const canSubmit = useMemo(() => {
    const textOk = themeText.trim().length > 0 && themeText.trim().length <= 200;
    const sizeOk = teamSize >= 1 && teamSize <= 6;
    return textOk && sizeOk;
  }, [themeText, teamSize]);

  const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string;

  if (!API_BASE_URL) {
    throw new Error("VITE_API_BASE_URL is not set. Add it to frontend/.env");
  }

  async function generate() {
    setError(null);
    setIsLoading(true);
    setData(null);

    // Client-side guard (backend also validates, but this improves UX)
    const trimmed = themeText.trim();
    if (!trimmed) {
      setError("Please enter a theme.");
      setIsLoading(false);
      return;
    }
    if (trimmed.length > 200) {
      setError("Theme must be 200 characters or fewer.");
      setIsLoading(false);
      return;
    }

    try {
      const res = await fetch(`${API_BASE_URL}/api/teams/generate`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },

        // Send the parameters exactly as the backend expects
        body: JSON.stringify({
          themeText: trimmed,
          teamSize,
          evolutionStage, // "any" | "fullyEvolved" | "unevolved"
          generations, // [] means "no filter"
          excludeLegendaries,
          allowForms,
          allowMega,
          allowGmax,
          allowSameSpeciesMultiple: false, // v2 UI
          allowSameFormDuplicates: false, // v2 UI
          includeTypes: [], // v2 UI
          excludeTypes: [], // v2 UI
        }),
      });

      if (!res.ok) {
        // Backend uses ProblemDetails; text is fine for now
        const text = await res.text();
        throw new Error(text || `Request failed with ${res.status}`);
      }

      const json = (await res.json()) as GenerateTeamResponse;
      setData(json);
    } catch (e: unknown) {
      setError(getErrorMessage(e));
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <div style={{ maxWidth: 1100, margin: "0 auto", padding: 24 }}>
      <h1>Pokémon Theme Team Generator</h1>
      <p style={{ marginTop: 0, opacity: 0.8 }}>
        Generate a team based on theme keywords. (Milestone 1: parameters + tag scoring)
      </p>

      {/* --- Input row --- */}
      <div style={{ display: "flex", gap: 12, alignItems: "end", flexWrap: "wrap" }}>
        <label style={{ display: "flex", flexDirection: "column", gap: 6, flex: "1 1 360px" }}>
          Theme
          <input
            value={themeText}
            onChange={(e) => setThemeText(e.target.value)}
            placeholder="e.g. spooky dog"
            maxLength={200}
          />
        </label>

        <button onClick={generate} disabled={!canSubmit || isLoading} style={{ height: 40 }}>
          {isLoading ? "Generating..." : "Generate"}
        </button>
      </div>

      {/* --- Controls + Results layout --- */}
      {/* --- Settings card (full width) --- */}
      <div
        style={{
          marginTop: 18,
          border: "1px solid rgba(255,255,255,0.15)",
          borderRadius: 12,
          padding: 12,
        }}
      >
        <h3 style={{ marginTop: 0, marginBottom: 12 }}>Settings</h3>

        {/* Use a grid so settings wrap nicely on smaller screens */}
        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fit, minmax(220px, 1fr))",
            gap: 12,
            alignItems: "start",
          }}
        >
          {/* Team size */}
          <label style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            Team size (1–6)
            <input
              type="number"
              min={1}
              max={6}
              value={teamSize}
              onChange={(e) => setTeamSize(Number(e.target.value))}
            />
          </label>

          {/* Evolution stage */}
          <label style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            Evolution stage
            <span style={{ fontSize: 12, opacity: 0.75 }}>
              Fully evolved = cannot evolve further; Unevolved = start of chain
            </span>
            <select
              value={evolutionStage}
              onChange={(e) => {
                const v = e.target.value;
                if (v === "any" || v === "fullyEvolved" || v === "unevolved") setEvolutionStage(v);
              }}
            >
              <option value="any">Any</option>
              <option value="fullyEvolved">Fully evolved only</option>
              <option value="unevolved">Unevolved only</option>
            </select>
          </label>

          {/* Toggles */}
          <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
            <div style={{ fontSize: 13, opacity: 0.85, marginBottom: 2 }}>Toggles</div>

            <label style={{ display: "flex", gap: 8, alignItems: "center" }}>
              <input
                type="checkbox"
                checked={excludeLegendaries}
                onChange={(e) => setExcludeLegendaries(e.target.checked)}
              />
              Exclude Legendary/Mythical
            </label>

            <label style={{ display: "flex", gap: 8, alignItems: "center" }}>
              <input
                type="checkbox"
                checked={allowForms}
                onChange={(e) => setAllowForms(e.target.checked)}
              />
              Allow regional/other forms
            </label>

            <label style={{ display: "flex", gap: 8, alignItems: "center" }}>
              <input type="checkbox" checked={allowMega} onChange={(e) => setAllowMega(e.target.checked)} />
              Allow Mega evolutions
            </label>

            <label style={{ display: "flex", gap: 8, alignItems: "center" }}>
              <input type="checkbox" checked={allowGmax} onChange={(e) => setAllowGmax(e.target.checked)} />
              Allow Gigantamax
            </label>
          </div>

          {/* Generations */}
          <div>
            <div style={{ marginBottom: 6 }}>Generations (optional)</div>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(3, 1fr)", gap: 6 }}>
              {Array.from({ length: 9 }, (_, i) => i + 1).map((gen) => (
                <label key={gen} style={{ display: "flex", gap: 6, alignItems: "center" }}>
                  <input
                    type="checkbox"
                    checked={generations.includes(gen)}
                    onChange={() => setGenerations((prev) => toggleNumber(prev, gen))}
                  />
                  Gen {gen}
                </label>
              ))}
            </div>
            <div style={{ fontSize: 12, opacity: 0.75, marginTop: 6 }}>
              Leave all unchecked to include any generation.
            </div>
          </div>
        </div>
      </div>

      {/* --- Errors / interpreted tokens --- */}
      {error && (
        <div style={{ marginTop: 12, padding: 12, border: "1px solid #f99", borderRadius: 12 }}>
          <strong>Error:</strong> {error}
        </div>
      )}

      {data && (
        <div
          style={{
            marginTop: 12,
            border: "1px solid rgba(255,255,255,0.15)",
            borderRadius: 12,
            padding: 12,
          }}
        >
          <div>
            <strong>Interpreted tokens:</strong> {data.interpreted.expandedTokens.join(", ")}
          </div>

          {data.interpreted.unknownTokens.length > 0 && (
            <div style={{ marginTop: 6, opacity: 0.85 }}>
              <strong>Unknown tokens:</strong> {data.interpreted.unknownTokens.join(", ")}
            </div>
          )}
        </div>
      )}

      {/* --- Results grid: 3 across x 2 down (responsive) --- */}
      {data && (
        <div
          style={{
            marginTop: 12,
            display: "grid",
            // Auto-fit columns with a minimum card width.
            // Desktop → 3+, Tablet → 2, Mobile → 1.
            gridTemplateColumns: "repeat(auto-fit, minmax(240px, 1fr))",
            gap: 12,
          }}
        >
          {data.team.map((p) => (
            <div
              key={p.key}
              style={{
                border: "1px solid rgba(255,255,255,0.15)",
                borderRadius: 12,
                padding: 12,
              }}
            >
              <img
                src={p.artUrl}
                alt={p.name}
                style={{ width: "100%", height: 210, objectFit: "contain" }}
              />
              <h3 style={{ margin: "8px 0 4px" }}>{p.name}</h3>
              <div style={{ fontSize: 13, opacity: 0.85 }}>
                <strong>Types:</strong> {p.types.join(" / ")}
              </div>
              <div style={{ fontSize: 13, opacity: 0.85, marginTop: 6 }}>
                <strong>Why:</strong> {p.reasons.join(", ")}
              </div>
            </div>
          ))}
        </div>
      )}

      {!data && !error && (
        <div style={{ opacity: 0.75, marginTop: 12 }}>
          Enter a theme and click <strong>Generate</strong>.
        </div>
      )}
    </div>
  );
}

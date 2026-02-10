// --- Enum int values (match backend C# enums) ---
export const EvolutionStageFilter = {
  Any: 0,
  FullyEvolved: 1,
  Unevolved: 2,
} as const;
type EvolutionStageFilter = (typeof EvolutionStageFilter)[keyof typeof EvolutionStageFilter];

import { useMemo, useRef, useState } from "react";
import "./App.css";

/**
 * --- Types (match backend ApiModels response) ---
 */

type PokemonResult = {
  key: string;
  name: string;
  artUrl: string;
  types: string[];
  reasons: string[];
};

type GenerateTeamResponse = {
  interpreted: {
    rawTokens: string[];
    expandedTokens: string[];
    unknownTokens: string[];
    groups: Record<string, string[]>;
  };
  team: PokemonResult[];
};

const darkControlBase: React.CSSProperties = {
  borderRadius: 10,
  border: "1px solid rgba(255,255,255,0.2)",
  background: "rgba(0,0,0,0.15)",
  color: "inherit",
};

const selectStyle: React.CSSProperties = {
  ...darkControlBase,
  height: 44,
  padding: "0 10px",
};

const optionStyle: React.CSSProperties = {
  background: "#111",
  color: "#fff",
};

const inputStyle: React.CSSProperties = {
  ...darkControlBase,
  height: 44,
  padding: "0 12px",
};

const buttonStyle: React.CSSProperties = {
  ...darkControlBase,
  height: 44,
  padding: "0 14px",
  cursor: "pointer",
};

const TYPE_COLORS: Record<string, string> = {
  normal: "#A8A77A",
  fire: "#EE8130",
  water: "#6390F0",
  electric: "#F7D02C",
  grass: "#7AC74C",
  ice: "#96D9D6",
  fighting: "#C22E28",
  poison: "#A33EA1",
  ground: "#E2BF65",
  flying: "#A98FF3",
  psychic: "#F95587",
  bug: "#A6B91A",
  rock: "#B6A136",
  ghost: "#735797",
  dragon: "#6F35FC",
  dark: "#705746",
  steel: "#B7B7CE",
  fairy: "#D685AD",
};

const ALL_GENS = [1, 2, 3, 4, 5, 6, 7, 8, 9] as const;

function titleCaseWord(w: string): string {
  return w.length ? w[0].toUpperCase() + w.slice(1).toLowerCase() : w;
}

function getErrorMessage(e: unknown): string {
  if (e instanceof Error) {
    return e.message;
  }
  if (typeof e === "string") {
    return e;
  }
  return "Something went wrong";
}

function sectionTitle(text: string) {
  return <div style={{ fontSize: 13, opacity: 0.85, marginBottom: 2 }}>{text}</div>;
}

export default function App() {
  const isDev = import.meta.env.DEV;
  const [debugEnabled, setDebugEnabled] = useState(false);

  const [themeText, setThemeText] = useState("");

  const [teamSize, setTeamSize] = useState(6);
  const [evolutionStage, setEvolutionStage] = useState<EvolutionStageFilter>(EvolutionStageFilter.Any);

  const [generations, setGenerations] = useState<number[]>([...ALL_GENS]);
  const [gensOpen, setGensOpen] = useState(false);
  const gensWrapRef = useRef<HTMLDivElement | null>(null);

  const [excludeLegendaries, setExcludeLegendaries] = useState(true);
  const [allowForms, setAllowForms] = useState(true);
  const [allowMega, setAllowMega] = useState(false);
  const [allowGmax, setAllowGmax] = useState(false);

  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [data, setData] = useState<GenerateTeamResponse | null>(null);

  const canSubmit = useMemo(() => {
    const textOk = themeText.trim().length > 0 && themeText.trim().length <= 200;
    const sizeOk = teamSize >= 1 && teamSize <= 6;
    return textOk && sizeOk;
  }, [themeText, teamSize]);

  const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string;

  if (!API_BASE_URL) {
    throw new Error("VITE_API_BASE_URL is not set. Add it to frontend/.env");
  }

  useMemo(() => {
    function onDocMouseDown(e: MouseEvent) {
      if (!gensOpen) {
        return;
      }
      const el = gensWrapRef.current;
      if (!el) {
        return;
      }
      if (e.target instanceof Node && !el.contains(e.target)) {
        setGensOpen(false);
      }
    }
    document.addEventListener("mousedown", onDocMouseDown);
    return () => document.removeEventListener("mousedown", onDocMouseDown);
  }, [gensOpen]);

  function generationsLabel(): string {
    const sorted = [...generations].sort((a, b) => a - b);
    if (sorted.length === ALL_GENS.length) {
      return "All generations";
    }
    if (sorted.length === 0) {
      return "All generations";
    }
    return sorted.map((g) => `Gen ${g}`).join(", ");
  }

  function toggleGen(g: number) {
    setGenerations((prev) => {
      const has = prev.includes(g);
      const next = has ? prev.filter((x) => x !== g) : [...prev, g];
      if (next.length === 0) {
        return [...ALL_GENS];
      }
      return next;
    });
  }

  async function generate() {
    setError(null);
    setIsLoading(true);
    setData(null);

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
        body: JSON.stringify({
          themeText: trimmed,
          teamSize,
          evolutionStage,
          generations,
          excludeLegendaries,
          allowForms,
          allowMega,
          allowGmax,
          allowSameSpeciesMultiple: false,
          allowSameFormDuplicates: false,
          includeTypes: [],
          excludeTypes: [],
          debug: isDev && debugEnabled,
        }),
      });

      if (!res.ok) {
        const text = await res.text();
        throw new Error(text || `Request failed with ${res.status}`);
      }

      const json = await res.json();
      console.log('API response:', json);
      setData(json as GenerateTeamResponse);
    } catch (e: unknown) {
      console.error('Error in generate:', e);
      setError(getErrorMessage(e));
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <div style={{ maxWidth: 1100, margin: "0 auto", padding: 16 }}>
      <h1 style={{ fontSize: 20, margin: "4px 0 2px" }}>Pokémon Theme Team Generator</h1>
      <p style={{ margin: "0 0 10px", opacity: 0.8, fontSize: 13 }}>
        Generate a team based on theme keywords.
      </p>

      {/* Input row */}
      <div style={{ display: "flex", gap: 12, alignItems: "stretch", flexWrap: "wrap" }}>
        <input
          value={themeText}
          onChange={(e) => setThemeText(e.target.value)}
          placeholder="Write a theme here..."
          maxLength={200}
          style={{ ...inputStyle, flex: "1 1 360px" }}
          aria-label="Theme"
        />

        <button onClick={generate} disabled={!canSubmit || isLoading} style={buttonStyle}>
          {isLoading ? "Generating..." : "Generate"}
        </button>
      </div>

      {/* Options card */}
      <div
        style={{
          marginTop: 14,
          border: "1px solid rgba(255,255,255,0.15)",
          borderRadius: 12,
          padding: 12,
        }}
      >
        <h3 style={{ marginTop: 0, marginBottom: 12 }}>Options</h3>

        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fit, minmax(220px, 1fr))",
            gap: 12,
            alignItems: "start",
          }}
        >
          {/* Team size dropdown */}
          <label style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            Team size
            <select value={teamSize} onChange={(e) => setTeamSize(Number(e.target.value))} style={selectStyle}>
              {[1, 2, 3, 4, 5, 6].map((n) => (
                <option key={n} value={n} style={optionStyle}>
                  {n}
                </option>
              ))}
            </select>
          </label>

          {/* Evolution stage */}
          <label style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            Evolution stage
            <select
              value={evolutionStage}
              onChange={e => setEvolutionStage(Number(e.target.value) as EvolutionStageFilter)}
              style={selectStyle}
            >
              <option value={EvolutionStageFilter.Any} style={optionStyle}>
                Any
              </option>
              <option value={EvolutionStageFilter.FullyEvolved} style={optionStyle}>
                Fully evolved only
              </option>
              <option value={EvolutionStageFilter.Unevolved} style={optionStyle}>
                Unevolved only
              </option>
            </select>
          </label>

          {/* Generations dropdown */}
          <div ref={gensWrapRef} style={{ display: "flex", flexDirection: "column", gap: 6, position: "relative" }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "baseline" }}>
              <div>Generations</div>
              <button
                type="button"
                onClick={() => setGenerations([...ALL_GENS])}
                style={{
                  background: "transparent",
                  color: "inherit",
                  border: "none",
                  textDecoration: "underline",
                  cursor: "pointer",
                  padding: 0,
                  opacity: 0.85,
                  fontSize: 12,
                }}
                title="Select all generations"
              >
                Select all
              </button>
            </div>

            <button
              type="button"
              onClick={() => setGensOpen((v) => !v)}
              style={{
                ...buttonStyle,
                textAlign: "left",
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
              }}
              aria-haspopup="listbox"
              aria-expanded={gensOpen}
              title="Click to choose generations"
            >
              <span style={{ opacity: 0.95 }}>{generationsLabel()}</span>
              <span style={{ opacity: 0.7 }}>▾</span>
            </button>

            {gensOpen && (
              <div
                style={{
                  position: "absolute",
                  top: 74,
                  left: 0,
                  right: 0,
                  zIndex: 50,
                  border: "1px solid rgba(255,255,255,0.2)",
                  borderRadius: 12,
                  background: "#111",
                  padding: 10,
                }}
              >
                <div style={{ display: "grid", gridTemplateColumns: "repeat(3, 1fr)", gap: 8 }}>
                  {ALL_GENS.map((g) => (
                    <label key={g} style={{ display: "flex", gap: 8, alignItems: "center", fontSize: 13 }}>
                      <input
                        type="checkbox"
                        checked={generations.includes(g)}
                        onChange={() => toggleGen(g)}
                      />
                      Gen {g}
                    </label>
                  ))}
                </div>

                <div style={{ marginTop: 10, display: "flex", justifyContent: "flex-end", gap: 8 }}>
                  <button type="button" onClick={() => setGensOpen(false)} style={{ ...buttonStyle, height: 36 }}>
                    Done
                  </button>
                </div>
              </div>
            )}
          </div>

          {/* Toggles */}
          <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
            {sectionTitle("Toggles")}

            <label style={{ display: "flex", gap: 8, alignItems: "center" }}>
              <input
                type="checkbox"
                checked={excludeLegendaries}
                onChange={(e) => setExcludeLegendaries(e.target.checked)}
              />
              Exclude Legendary/Mythical
            </label>

            <label style={{ display: "flex", gap: 8, alignItems: "center" }}>
              <input type="checkbox" checked={allowForms} onChange={(e) => setAllowForms(e.target.checked)} />
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

            {isDev && (
              <label style={{ display: "flex", gap: 8, alignItems: "center" }}>
                <input type="checkbox" checked={debugEnabled} onChange={(e) => setDebugEnabled(e.target.checked)} />
                Show debug (dev only)
              </label>
            )}
          </div>
        </div>
      </div>

      {/* Errors */}
      {error && (
        <div style={{ marginTop: 12, padding: 12, border: "1px solid #f99", borderRadius: 12 }}>
          <strong>Error:</strong> {error}
        </div>
      )}

      {/* Results grid */}
      {data && (
        <div className="resultsGrid" style={{ marginTop: 12 }} >
          {data.team.map((p) => (
            <div
              key={p.key}
              style={{
                border: "1px solid rgba(255,255,255,0.15)",
                borderRadius: 12,
                padding: 12,
              }}
            >
              <img src={p.artUrl} alt={p.name} style={{ width: "100%", height: 210, objectFit: "contain" }} />

              <h3 style={{ margin: "10px 0 6px" }}>{p.name}</h3>

              {/* Types */}
              <div style={{ display: "flex", justifyContent: "center" }}>
                <div style={{ display: "flex", gap: 8, flexWrap: "wrap", justifyContent: "center" }}>
                  {p.types.map((t) => {
                    const key = t.toLowerCase();
                    const bg = TYPE_COLORS[key] ?? "rgba(255,255,255,0.18)";
                    return (
                      <span
                        key={t}
                        style={{
                          background: bg,
                          color: "#111",
                          padding: "4px 10px",
                          borderRadius: 999,
                          fontSize: 12,
                          fontWeight: 700,
                        }}
                        title={`Type: ${titleCaseWord(t)}`}
                      >
                        {titleCaseWord(t)}
                      </span>
                    );
                  })}
                </div>
              </div>

              {/* Why? */}
              <div style={{ fontSize: 13, opacity: 0.92, marginTop: 10 }}>
                <strong>Why?</strong>

                <div style={{ marginTop: 6 }}>
                  {p.reasons.length > 0 ? p.reasons.join(", ") : "(no matching reasons)"}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Interpreted tokens */}
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
            <strong>Interpreted tokens:</strong>{" "}
            {Array.isArray(data.interpreted?.expandedTokens)
              ? data.interpreted.expandedTokens.join(", ")
              : <span style={{ color: '#f99' }}>(none)</span>}
          </div>

          {Array.isArray(data.interpreted?.unknownTokens) && data.interpreted.unknownTokens.length > 0 && (
            <div style={{ marginTop: 6, opacity: 0.85 }}>
              <strong>Unknown tokens:</strong>{" "}
              {data.interpreted.unknownTokens.join(", ")}
            </div>
          )}

          {/* Dev-only debug panel */}
          {isDev && debugEnabled && (
            <div
              style={{
                marginTop: 10,
                borderTop: "1px solid rgba(255,255,255,0.12)",
                paddingTop: 10,
                fontSize: 13,
                opacity: 0.9,
              }}
            >
              <div style={{ fontWeight: 600, marginBottom: 6 }}>Debug</div>

              <div>
                <strong>Raw tokens:</strong>{" "}
                {Array.isArray(data.interpreted?.rawTokens) && data.interpreted.rawTokens.length > 0
                  ? data.interpreted.rawTokens.join(", ")
                  : "(none)"}
              </div>

              <div style={{ marginTop: 6 }}>
                <strong>Expanded tokens:</strong>{" "}
                {Array.isArray(data.interpreted?.expandedTokens) && data.interpreted.expandedTokens.length > 0
                  ? data.interpreted.expandedTokens.join(", ")
                  : "(none)"}
              </div>
            </div>
          )}
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

# HUE GATES — Mechanic & Build Spec (give this to Cursor)

Working title. Single-player, offline, portrait mobile puzzle game for Android + iOS with IAP.

**Files that go with this document**
- `levels_1_25.json` — 25 ready-made levels, every one verified solvable (this is the game data).
- `level_tools.py` — reference engine + level generator + solver (Python). Port the engine part; the generator stays offline.

**Golden rules for Cursor**
1. Game logic is a **pure module with no engine/UI dependencies** (no Unity/DOM calls). Rendering calls the logic, never the other way round.
2. Do not change a rule in code without changing this document first.
3. Every logic function gets a unit test (section 12).
4. Levels are **data** (`levels_1_25.json`). The game never generates levels at runtime.

---

## 1. The game in one paragraph

A square grid holds colored arrows. Tap an arrow and it slides in the direction it points. It can leave the board only if **nothing is in its way** and the **gate on the edge it is heading for shows the arrow's own color**. Gates change color every time an arrow leaves (all gates move one step along the color wheel), so the order you clear arrows in matters. Clear the board to win. Every win reveals one piece of a hidden pixel-art picture.

## 2. Exact rules

1. Grid is `N × N`. Cells are addressed `(r, c)`; `r` = row from the top (0-based), `c` = column from the left (0-based).
2. A cell holds **zero or one** arrow. An arrow has a direction `U | D | L | R` and a color index `0..K-1`.
3. `K` is the number of colors in the level (1 to 5). `palette[]` in the level names the colors in **wheel order**.
4. Every border position has a **gate**: sides `T`, `B` (indexed by column) and `L`, `R` (indexed by row). That is `4N` gates.
5. The level stores each gate's **base color** (its color when `cleared = 0`).
6. `cleared` = number of arrows cleared so far. **Shown gate color = (baseColor + cleared) mod K.** So every valid clear shifts *all* gates one step along the wheel. `cleared` is the only game state besides which arrows remain (no separate "phase" variable).
7. An arrow's **path** = all cells from the cell next to it, in its direction, to the board edge. Its **exit gate** = the gate on that edge at the arrow's own column (for `U`/`D`) or row (for `L`/`R`).
8. Tapping an arrow resolves in this order:
   1. any other arrow on its path → **BLOCKED**
   2. else exit gate shown color ≠ arrow color → **DENIED**
   3. else → **CLEARED** (arrow leaves, `cleared += 1`)
9. **Win:** no arrows left.
10. **Hearts:** 3 per level (`lives` in the level data). BLOCKED and DENIED each cost 1 heart. At 0 hearts the level is **FAILED**.
11. **Stuck:** arrows remain but no arrow would resolve to CLEARED. This is checked at level load, after every CLEARED and after every Undo. Stuck costs nothing; the player must Undo or Restart.

Why "stuck" exists: clearing an arrow changes the gates, so a legal move now can make later moves impossible. This is the puzzle, and it is why Undo is valuable.

Levels 1-3 use `K = 1` (one color, so gates never visibly change): they teach tapping and blocking only. Color/gate changes start at level 4.

## 3. Right vs wrong — what happens, exactly

| Result | When | Board | Hearts | Gates | Feedback (suggested) |
|---|---|---|---|---|---|
| **CLEARED** (right) | path empty **and** gate color == arrow color | Arrow slides out along its line (250 ms ease-in) and is removed. `cleared += 1` | no change | **all gates shift +1 color** (180 ms cross-fade) | exit gate pulses white; "tick" sound whose pitch rises with combo; light haptic; score `+100 × combo`; combo `+1` (max ×5) |
| **BLOCKED** (wrong) | another arrow on the path | **No change.** Tapped arrow nudges 25% of a cell toward the blocker and springs back (240 ms); the nearest blocker pulses | **−1** | no shift | arrow shakes; heart cracks and fades; "thud"; medium haptic; combo resets to ×1 |
| **DENIED** (wrong) | path empty but gate color ≠ arrow color | **No change.** Arrow slides to the edge, the gate flashes dim/crossed, arrow slides back (440 ms total) | **−1** | no shift | "buzz"; medium haptic; combo resets |
| **STUCK** | arrows left, no CLEARED move exists | no change | no change | no change | banner "No moves left" with **Undo** and **Restart** buttons. Dismissed by Undo or Restart |
| **WIN** | 0 arrows left | — | stars = hearts left (3 / 2 / 1) | — | confetti; the next gallery piece reveals; progress bar fills; **Next** button |
| **FAIL** | hearts reach 0 | input locked | — | — | panel "Out of hearts": **Revive** (rewarded ad, +1 heart, once per level) or **Restart** |
| Tap on empty cell | — | ignored | — | — | nothing |

Notes
- BLOCKED is evaluated before DENIED. A blocked arrow never reports "wrong color".
- Wrong taps never change the board or `cleared`. Only CLEARED and Undo change `cleared`.
- **Undo** puts the last cleared arrow back at its cell, `cleared -= 1`, gates shift back one step. Hearts are **not** refunded.
- **Restart** reloads the level: all arrows back, `cleared = 0`, hearts = 3.
- While an arrow is animating it ignores taps; other arrows can still be tapped.

## 4. Level data format (`levels_1_25.json`)

```json
{
  "version": 1,
  "game": "Hue Gates",
  "levels": [
    {
      "id": 4,
      "size": 5,
      "colors": 2,
      "palette": ["coral", "sky"],
      "lives": 3,
      "gates": { "T": [0,1,0,1,1], "B": [..N..], "L": [..N..], "R": [..N..] },
      "arrows": [ { "r": 0, "c": 4, "dir": "D", "color": 1 }, "..." ],
      "solution": [0,1,2,3,4,5,6,7],
      "note": "Intro: 2 colors, gates change after each clear",
      "meta": { "seed": 400003, "arrows": 8, "initialValidMoves": 3,
                "initiallyBlockedArrows": 2, "randomPlayWinRate": 0.79 }
    }
  ]
}
```

- `gates.T[c]`, `gates.B[c]` are indexed by **column**; `gates.L[r]`, `gates.R[r]` by **row**. Values are base color indices `0..K-1`.
- `color` values index into `palette`.
- `arrows[]` is stored **in a valid clearing order**, so `solution` is always `[0,1,2,…]`. The array order has **no meaning to the player**. Never rely on it for drawing, tapping or hints.
- `meta.randomPlayWinRate` = how often a player who taps a random *valid* arrow every time wins. Lower = harder. Use it only for tuning.

Palette names → hex (from the art spec): `coral #FF6B6B`, `sun #FFC94D`, `mint #3DDC97`, `sky #4DA3FF`, `violet #9B6BFF`.

## 5. Engine pseudocode (port exactly; TypeScript-style)

```ts
type Dir = 'U' | 'D' | 'L' | 'R';
interface Arrow { r: number; c: number; dir: Dir; color: number }
interface Level {
  id: number; size: number; colors: number; palette: string[]; lives: number;
  gates: { T: number[]; B: number[]; L: number[]; R: number[] };
  arrows: Arrow[]; solution: number[];
}
type Outcome = 'CLEARED' | 'BLOCKED' | 'DENIED';

const DELTA = { U: [-1, 0], D: [1, 0], L: [0, -1], R: [0, 1] };

function exitGate(N: number, a: Arrow): { side: 'T'|'B'|'L'|'R'; idx: number } {
  if (a.dir === 'U') return { side: 'T', idx: a.c };
  if (a.dir === 'D') return { side: 'B', idx: a.c };
  if (a.dir === 'L') return { side: 'L', idx: a.r };
  return { side: 'R', idx: a.r };
}

function pathCells(N: number, a: Arrow): [number, number][] {
  const [dr, dc] = DELTA[a.dir]; const out = [];
  let r = a.r + dr, c = a.c + dc;
  while (r >= 0 && r < N && c >= 0 && c < N) { out.push([r, c]); r += dr; c += dc; }
  return out;
}

class GameState {
  remaining: boolean[];   // index = arrow index in level.arrows
  cleared = 0;            // number of arrows cleared
  hearts: number;
  history: number[] = []; // cleared arrow indices, for Undo
  constructor(public level: Level) {
    this.remaining = level.arrows.map(() => true);
    this.hearts = level.lives;
  }
}

function gateColor(level: Level, a: Arrow, cleared: number): number {
  const g = exitGate(level.size, a);
  return (level.gates[g.side][g.idx] + cleared) % level.colors;
}

function arrowAt(s: GameState, r: number, c: number): number {   // -1 if none
  return s.level.arrows.findIndex((a, i) => s.remaining[i] && a.r === r && a.c === c);
}

function resolveTap(s: GameState, i: number): Outcome {          // pure, no mutation
  const a = s.level.arrows[i];
  for (const [r, c] of pathCells(s.level.size, a)) if (arrowAt(s, r, c) !== -1) return 'BLOCKED';
  if (gateColor(s.level, a, s.cleared) !== a.color) return 'DENIED';
  return 'CLEARED';
}

function applyTap(s: GameState, i: number): Outcome {
  const o = resolveTap(s, i);
  if (o === 'CLEARED') { s.remaining[i] = false; s.cleared++; s.history.push(i); }
  else s.hearts--;
  return o;
}

function undo(s: GameState): boolean {
  if (!s.history.length) return false;
  s.remaining[s.history.pop()!] = true; s.cleared--; return true;
}

const validMoves = (s: GameState) =>
  s.level.arrows.map((_, i) => i).filter(i => s.remaining[i] && resolveTap(s, i) === 'CLEARED');
const isWon    = (s: GameState) => s.remaining.every(r => !r);
const isStuck  = (s: GameState) => !isWon(s) && validMoves(s).length === 0;
const isFailed = (s: GameState) => s.hearts <= 0;
```

**Hint solver (needed in the game):** depth-first search over `remaining` bitmasks.

```
solve(remainingMask): returns list of arrow indices or null
  if remainingMask == 0 return []
  if remainingMask in deadSet return null
  for i in validMoves(remainingMask):        // cleared = M - popcount(remainingMask)
      rest = solve(remainingMask without i)
      if rest != null return [i] + rest
  deadSet.add(remainingMask); return null
```
Cap at ~200,000 nodes. Hint = first move of the found solution. If none is found, highlight any valid move; if there are none, show the STUCK banner.

## 6. Grid sizes and the 25-level plan

**Starts at 4×4. Max is 8×8.**

| Grid | Levels | Why |
|---|---|---|
| 4×4 | 1-3 | tutorial, one color |
| 5×5 | 4-8 | introduces the gate color change (2 colors, then 3) |
| 6×6 | 9-15 | 3 then 4 colors |
| 7×7 | 16-21 | 4 then 5 colors |
| 8×8 | 22-25 | hardest set, 5 colors |

Phone sizing (360 dp wide, 8 dp margin each side): 4×4 ≈ 86 dp cells, 5×5 ≈ 69, 6×6 ≈ 57, 7×7 ≈ 49, 8×8 ≈ 43 dp. 43 dp is slightly under the usual 48 dp touch target, so the **whole cell is the tap area** and 8×8 is the hard cap. Do not go above 8×8.

How many levels? **25 are shipped in the JSON, all verified.** More are produced offline with `level_tools.py` (add rows to `PLAN` in the build script, run it, ship the JSON) — there is no limit, and the game never needs to generate levels itself.

### Level-by-level table (levels 1-25)

| Level | Grid | Colors | Arrows | Start: free / blocked | Random-play win rate | What is new |
|---|---|---|---|---|---|---|
| 1 | 4×4 | 1 | 5 | 4 / 1 | 100% | Tutorial: tap an arrow, it leaves |
| 2 | 4×4 | 1 | 6 | 3 / 3 | 100% | Tutorial: a blocked arrow costs a heart |
| 3 | 4×4 | 1 | 8 | 2 / 6 | 100% | Tutorial: clear blockers first (chains) |
| 4 | 5×5 | 2 | 8 | 3 / 2 | 79% | Intro: 2 colors, gates change after each clear |
| 5 | 5×5 | 2 | 9 | 2 / 4 | 93% | Breather |
| 6 | 5×5 | 2 | 10 | 3 / 5 | 62% |  |
| 7 | 5×5 | 2 | 11 | 4 / 5 | 67% |  |
| 8 | 5×5 | 3 | 10 | 2 / 5 | 62% | Intro: 3 colors |
| 9 | 6×6 | 3 | 12 | 3 / 5 | 72% | Intro: 6x6 board |
| 10 | 6×6 | 3 | 12 | 3 / 6 | 79% | Breather |
| 11 | 6×6 | 3 | 14 | 3 / 7 | 66% |  |
| 12 | 6×6 | 3 | 15 | 2 / 8 | 28% |  |
| 13 | 6×6 | 4 | 14 | 2 / 9 | 31% | Intro: 4 colors |
| 14 | 6×6 | 4 | 15 | 2 / 7 | 50% |  |
| 15 | 6×6 | 4 | 14 | 3 / 5 | 53% | Breather |
| 16 | 7×7 | 4 | 17 | 3 / 10 | 38% | Intro: 7x7 board |
| 17 | 7×7 | 4 | 19 | 3 / 12 | 36% |  |
| 18 | 7×7 | 4 | 20 | 2 / 12 | 35% |  |
| 19 | 7×7 | 5 | 18 | 3 / 8 | 16% | Intro: 5 colors |
| 20 | 7×7 | 5 | 18 | 3 / 7 | 45% | Breather (end of picture 1) |
| 21 | 7×7 | 5 | 21 | 2 / 12 | 20% |  |
| 22 | 8×8 | 5 | 22 | 4 / 13 | 9% | Intro: 8x8 board |
| 23 | 8×8 | 5 | 24 | 4 / 11 | 16% |  |
| 24 | 8×8 | 5 | 25 | 3 / 16 | 4% |  |
| 25 | 8×8 | 5 | 26 | 2 / 15 | 17% | Finale of this set |

*Random-play win rate = chance that tapping a random currently-valid arrow every time clears the board. A thinking player does far better; use it only to compare levels. "Free" = arrows that can be cleared at the start; "blocked" = arrows with another arrow in their path at the start.*

Pattern: every 5th level (5, 10, 15, 20) is a breather with a higher win rate; level 25 is the finale of this set. Beyond 25, repeat: size 8×8, 5 colors, 22-30 arrows, random-play win rate 3%-20%, a breather every 5th level.

## 7. Game flow (state machine)

```
Home ──Play──► Level(playing) ──WIN──► Result ──Next──► Level(playing)
                  │  ▲                      │
                  │  └──Undo/Restart────────┤ (Result also shows gallery reveal)
                  ├──STUCK──► banner (Undo | Restart)
                  └──FAIL───► panel (Revive via rewarded ad | Restart)
Home ──Gallery──► picture grid ──► picture detail
Home ──Shop──► IAP list
```

Level start: hearts = `lives`, `cleared = 0`, run the STUCK check (levels are guaranteed not to start stuck).

## 8. Boosters, ads and IAP

| Item | Behavior |
|---|---|
| **Undo** | `undo()`; consumes 1 from inventory (start with 3). 0 left → offer rewarded ad (+1) or the Undo Pack |
| **Hint** | runs the solver, pulses the suggested arrow for 1.2 s; consumes 1 (start with 3). 0 left → rewarded ad (+1) or Hint Pack |
| **Restart** | free, always available |
| **Revive** | rewarded ad, +1 heart, once per level |

IAP products (prices are placeholders; set the real ones in the store consoles):

| Product | Type | Grants |
|---|---|---|
| `remove_ads` | non-consumable | disables interstitials (rewarded ads stay optional) |
| `hint_pack_10` | consumable | +10 hints |
| `undo_pack_10` | consumable | +10 undos |
| `picture_pack_1` | non-consumable | +20 extra pictures |
| `starter_bundle` | one-time | remove ads + 20 hints + 20 undos + picture pack 1 |

Ads: rewarded only for Revive / Undo / Hint; an interstitial every 3rd level from level 7 on, never mid-level, skipped if `remove_ads` is owned. Put store/ad SDKs behind interfaces (`IStore`, `IAds`) with fake implementations for the editor. IAP can only be truly tested on a build installed from the Play **internal testing** track with license testers.

## 9. Gallery meta

- A picture is a **10×10 pixel image** split into **20 reveal pieces of 5 pixels** each, in a fixed, shuffled order stored with the picture.
- Winning level `n` reveals piece `((n-1) mod 20) + 1` of picture `ceil(n/20)`. So **levels 1-20 complete picture 1** and levels 21-25 reveal the first 5 pieces of picture 2.
- Revisiting a completed level never re-reveals anything. Progress is derived from `highestLevelCompleted`.
- The Level-Complete screen animates the new piece: glow, then fade-in.

## 10. Save data (local JSON / PlayerPrefs)

```
currentLevel, highestLevelCompleted, stars{levelId:1..3},
hints, undos, ownedProducts[],
settings{ sound, music, haptics, assistGlow, colorblindSymbols }
```

## 11. Tutorial, assists, accessibility

| Level | On-screen tip |
|---|---|
| 1 | "Tap an arrow to send it off the board." Pulse a valid arrow. |
| 2 | "Arrows can't pass other arrows. Clear the blocker first." |
| 3 | "Work back from the arrows that are free." |
| 4 | "Gates change color after every clear. An arrow leaves only through a gate of its own color." |
| 8, 13, 19 | one-line note when a new color count appears |

- **Ready glow:** an arrow whose exit gate currently shows its own color gets a soft glow (it ignores blocking, so the player still has to check paths). Default **on for levels 1-15**, off from 16, always switchable in Settings.
- **Color-blind symbols:** each color has a small symbol on the arrow and gate (coral ●, sun ▲, mint ■, sky ◆, violet ★). Setting, off by default.
- Haptics and sound each have an on/off setting.

## 12. Tests (must pass before any UI work is accepted)

1. **Replay test:** for every level in the JSON, apply `solution` in order → every tap returns CLEARED and the level ends WON with 3 hearts.
2. **Solver test:** `solve()` from the initial state returns a full solution for every level.
3. **Level 1 vector:** all gates are color 0; clearing in order `0,1,2,3,4` gives CLEARED each time. Tapping arrow `2` first is BLOCKED (arrow `0` is in its path), hearts 3 → 2.
4. **Level 4 first-move vector** (`levels[3]`, arrows in array order, `cleared = 0`):

| arrow # | (r,c) dir color | result |
|---|---|---|
| 0 | (0,4) D 1 | CLEARED |
| 1 | (0,3) R 0 | BLOCKED |
| 2 | (4,1) D 1 | CLEARED |
| 3 | (2,1) L 0 | DENIED |
| 4 | (2,2) R 0 | CLEARED |
| 5 | (3,0) U 1 | DENIED |
| 6 | (3,3) L 1 | BLOCKED |
| 7 | (1,2) U 1 | DENIED |

5. **Undo test:** CLEARED then undo returns `remaining`, `cleared` and gate colors to the exact previous values; hearts are unchanged.
6. **Wrong-tap test:** BLOCKED and DENIED never change `remaining` or `cleared`; hearts drop by exactly 1.
7. **Stuck test:** build a state where no arrow is CLEARED and `isStuck` is true; Undo makes it false.
8. **Fail test:** three wrong taps → `isFailed` true; Revive gives 1 heart and play continues.

**Acceptance:** all tests green; levels 1-25 playable start to finish; the right/wrong feedback in section 3 is visible and audible; no hard-coded level data in code.

## 13. Prompt sequence for Cursor

Put this file, `levels_1_25.json` and `level_tools.py` in `/docs` and `/data`. Tell Cursor your target engine first (Unity C# on the BlockPath base, or TypeScript with Phaser/Capacitor), then paste these one at a time, running the tests after each.

1. "Read `docs/HUE_GATES_1_MECHANIC_SPEC.md` and `data/level_tools.py`. Implement the pure game-logic module from sections 2, 3 and 5, with no engine dependencies, plus unit tests for section 12 items 1-8."
2. "Load `levels_1_25.json` and draw the board with placeholder colored shapes: grid, arrows rotated to their direction, and border gate segments colored `(base + cleared) mod K`. Add a level selector for testing."
3. "Wire tap input to `applyTap` and implement the CLEARED / BLOCKED / DENIED animations and gate color shift exactly as in section 3. Placeholder sounds are fine."
4. "Implement hearts, the STUCK banner, the FAIL panel, WIN with stars, and the level flow from section 7."
5. "Implement Undo, Hint (port the solver from section 5) and Restart with inventories from section 8."
6. "Implement the gallery reveal from section 9 and the save data from section 10. Use placeholder 10×10 pictures."
7. "Add `IStore` and `IAds` interfaces with fake editor implementations, then the real Play Billing / AdMob integration behind them, using the product IDs in section 8."
8. "Add tutorial tips, ready glow, color-blind symbols, settings, haptics and sound toggles from section 11."

Cursor rules to add (`.cursor/rules`): keep logic pure; never edit level JSON by hand; every rule change goes into this spec first; run tests before finishing a task.

## 14. Out of scope for version 1

Lives that refill over time, multiplayer, accounts, cloud save, in-game currency, level editor, procedural levels at runtime.

## 15. Store-risk reminders

Launch on Google Play first. Before submitting to Apple, check the game against the App Store's 4.3 (spam / similar apps) guideline: keep the gate-color wheel, the picture-reveal meta and your own art and level data front and center in the metadata and screenshots.

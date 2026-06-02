# Solitaire — Undo/Redo Prototype

A minimal, playable **Klondike Solitaire (Draw‑1)** built in Unity as a developer case study. The focus feature is a clean **Undo/Redo** system; the rest of the game is scoped just far enough to make undo meaningful.

> Game loop: deal → drag cards / draw from stock → undo or redo any move → win.

## What was built

**Gameplay — Klondike, Draw‑1, unlimited recycling**

- Full deal: 7 tableau columns, a 24‑card stock, waste, and 4 foundations.
- All core moves: tableau↔tableau (including valid multi‑card sequences), play to foundation, draw 1 from stock, and recycle the waste back into the stock.
- Auto‑flip of newly exposed tableau cards, and win detection.

**Undo / Redo — the headline feature**

- Multi‑level undo *and* redo (the brief asked for one move back; this handles the whole history).
- Buttons enable/disable based on availability; starting a new game clears the history.

**Controls & presentation**

- Drag‑and‑drop cards, tap the stock to draw, on‑screen **Undo / Redo / New Game** buttons, and a win banner.
- 2D world‑space rendering with smooth move and flip animations.

## How it works (architecture)

The game is split into small systems behind interfaces, wired together with dependency injection (VContainer). `CoreScope` is the composition root and `CoreFlow` owns the lifecycle: load assets → initialize presentation → start game (a *New Game* is a full teardown + re‑init).

| System | Responsibility | Unity? |
|---|---|---|
| `GameSystem` | Board state, game loop, applies/reverts moves | No — pure C# |
| `UndoSubSystem` | Undo/redo history | No — pure C# |
| `InputSystem` | Pointer/tap abstraction | Yes |
| `GameAssetsSystem` | Async asset load/unload | Yes |
| `LayoutSystem` | Pile/card position math | Yes |
| `PresentationSystem` | Visuals only (views, sprites, animation) | Yes |

`GameSystem` composes its own `DealSubSystem` (shuffle + deal), `RulesSubSystem` (move validation + win check) and `UndoSubSystem`. The board is exposed as an immutable `BoardState` snapshot through events: the presentation layer reacts to `OnBoardChanged` and translates completed gestures back into `GameSystem` commands. Rules, layout math, asset loading, and history are deliberately kept *out* of the presentation layer.

**Undo design.** Rather than snapshotting the entire board, each action produces a small reversible `BoardChange` (kind, source, target, card count, and an auto‑flip flag). `UndoSubSystem` keeps an undo stack and a redo stack; a new move clears the redo stack. *Undo* pops a change and reverts it (including un‑flipping the card it revealed); *redo* replays it. This keeps memory tiny and the logic easy to reason about and test.

## Tech & tools

Unity (2D / orthographic) · **VContainer** (DI) · **UniTask** (async) · **LitMotion** (tweening) · **UniText Platinum** (renders rank/suit glyphs when no card sprites are supplied, so the prototype runs with zero art — a required paid Asset Store package; see *Running it*) · uGUI. The core logic is covered by **EditMode unit tests** — deal, rules, undo/redo round‑trips, and a seed‑scanning auto‑flip‑then‑undo test.

## What I'd improve with more time

- A real card‑art sprite set in place of the emoji‑glyph fallback, plus deal and win animations.
- Quality‑of‑life: double‑click/tap to auto‑send a card to its foundation, hints, score/timer.
- PlayMode/integration tests covering drag‑and‑drop and a full game.
- Persistence (resume an in‑progress game *with* its undo history), card‑view pooling, and an optional Draw‑3 mode.

## AI‑assisted workflow

This was built with a **spec‑driven, AI‑assisted** process:

1. **Requirements first.** I wrote detailed design docs in `AD.Docs/Requirements` (systems 00–08, each with `REQ‑` IDs and an interface contract) and `AD.Docs/EditorTasks` (the manual Unity steps), defining the architecture and interfaces *before* any code.
2. **Code generation.** I generated the C# from those specs using **Cursor's AI coding agent**, iterating system by system, with the **`unity-mcp`** Model Context Protocol server so the agent could talk to the Unity Editor directly.
3. **Manual Unity work (by hand).** Scenes, the orthographic camera, the `EventSystem`, the `CoreScope` object, and the `CardView` / `PileView` / `BoardView` prefabs + `Resources` were set up in the editor per the EditorTasks docs.
4. **Refactor & cleanup.** Follow‑up passes consolidated gameplay into the `Core` scene and centralized the lifecycle flow.

In short: **architecture, requirements, prefab/scene wiring, and review were human‑driven; most of the C# implementation was AI‑generated from the specs.**

## Running it

> **Required dependency — import this first.** The project depends on **UniText Platinum** (a paid Unity Asset Store package by Light Side LLC). It is **not** included in the repo, and without it the project **will not compile** — the `LightSide` / `UniText` / `UniTextWorld` references in the presentation layer produce many compilation errors. Get it here: [UniText Platinum on the Unity Asset Store](https://assetstore.unity.com/packages/tools/gui/unitext-platinum-357844).

1. Open the Unity project under `AD.Unity/`.
2. Import **UniText Platinum** (Package Manager ▸ *My Assets* ▸ Import). You can ignore the initial compile errors until this finishes; once it's imported the project compiles cleanly.
3. Load the `3.Core` scene and press **Play**.

Scene and prefab setup are documented in `AD.Docs/EditorTasks`.

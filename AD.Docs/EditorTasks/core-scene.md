# EditorTask: Core scene setup

> Manual editor work (per [unity-rules.md](../Architecture/unity-rules.md) §1). The agent writes only `.cs`; the scene, GameObjects and component wiring are created by a human in the Unity editor.

This wires the `3.Core` scene so the generated Solitaire code can run. All runtime code already exists under `AD.Unity/Assets/_Project/Develop/Solitaire/Runtime/`.

## 1. Open / create the Core scene

- Open the scene at build index `3.Core` (see `RuntimeConstants.Scenes.Core`). If it does not exist, create it and add it to **Build Settings** at the slot used by `"3.Core"`.

## 2. Camera

- Add a `Camera` (or reuse the scene's `Main Camera`):
  - **Projection:** Orthographic.
  - **Size:** ~5 (tune so all 13 piles from `RuntimeConstants.Layout` are visible; the field spans roughly X `[-6 .. +6]`, Y `[-3 .. +4]`).
  - **Position:** `(0, 0, -10)`, rotation `(0,0,0)`.
  - Tag it `MainCamera` (the presentation `HitTester` resolves `Camera.main`).

## 3. EventSystem (for uGUI buttons)

- Add a GameObject with an `EventSystem` + `StandaloneInputModule` (GameObject ▸ UI ▸ Event System). Required for the Undo / Redo / New Game buttons on `BoardView`.

## 4. CoreScope (VContainer composition root)

- Create an empty GameObject named `CoreScope`.
- Add the **`CoreScope`** component (`Appodeal.Solitaire.Runtime.Core.CoreScope`).
- Leave its settings default. `CoreScope.Configure` already registers every system and the `CoreFlow` / `InputSystem` entry points; nothing needs to be assigned in the inspector.

> `CoreScope` registers `LoadingService`, `SceneManager`, `IGameSystem`, `ILayoutSystem`, `IGameAssetsSystem`, `IPresentationSystem`, the `InputSystem` (`ITickable`) entry point and the `CoreFlow` entry point. `GameSystem` creates its `Deal`/`Rules`/`Undo` subsystems internally (no separate DI registration). `CoreFlow` then runs: `gameAssets.LoadAsync()` → `presentation.InitializeAsync()` → `game.StartNewGame()`.

## 5. Run order sanity

- Ensure no other `LifetimeScope` in the scene is a parent of `CoreScope` that would double-register the same services (a standalone `CoreScope` is fine).
- Press Play: the loader runs, the board is built from the prefabs/sprites (see [presentation-prefabs-and-art.md](presentation-prefabs-and-art.md)) and a new game is dealt.

## Checklist

- [ ] `3.Core` scene exists and is in Build Settings.
- [ ] Orthographic `MainCamera` positioned to show the field.
- [ ] `EventSystem` present.
- [ ] `CoreScope` GameObject with the `CoreScope` component.
- [ ] Prefabs and sprites from the companion EditorTask are in place.

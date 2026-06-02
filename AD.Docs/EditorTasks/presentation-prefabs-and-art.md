# EditorTask: Presentation prefabs and card art

> Manual editor work (per [unity-rules.md](../Architecture/unity-rules.md) §1). The agent writes only `.cs`; prefabs, sprites and inspector wiring are created by a human in the Unity editor.

The presentation runtime (`PresentationSystem`, `BoardModel`, `CardModel`, `CardView`, `PileView`, `BoardView`, `CardViewFactory`, `HitTester`) already exists. It loads everything by path through `AssetService` using the paths in `RuntimeConstants.Assets`. This task creates those assets and prefabs.

Rendering model (decided): **world-space 2D** — `SpriteRenderer` + `BoxCollider2D` cards/piles under an orthographic camera, with a screen-space uGUI overlay only for the buttons and win banner.

## 1. Resources layout (paths must match `RuntimeConstants.Assets`)

Everything is loaded via `UnityEngine.Resources`, so it must live under a `Resources/` folder. Use `Assets/_Project/Resources/`.

| Asset | Resources path | Constant |
|---|---|---|
| 52 card faces | `Cards/{Suit}_{Rank}` | `Assets.Cards.FacePathFormat = "Cards/{0}_{1}"` |
| Card back | `Cards/Back` | `Assets.Cards.BackPath` |
| Card prefab | `Prefabs/CardView` | `Assets.Prefabs.CardView` |
| Pile prefab | `Prefabs/PileView` | `Assets.Prefabs.PileView` |
| Board prefab | `Prefabs/BoardView` | `Assets.Prefabs.BoardView` |

`{Suit}` is the `Suit` enum name: `Clubs`, `Diamonds`, `Hearts`, `Spades`.
`{Rank}` is the `Rank` enum name: `Ace`, `Two`, `Three`, `Four`, `Five`, `Six`, `Seven`, `Eight`, `Nine`, `Ten`, `Jack`, `Queen`, `King`.

So the 52 face sprites are files like:
`Assets/_Project/Resources/Cards/Clubs_Ace.png`, `Clubs_Two.png`, … , `Spades_King.png`, plus `Assets/_Project/Resources/Cards/Back.png`.

- Import each as **Sprite (2D and UI)**.
- Size them so one card is about 1 world unit wide (tune Pixels Per Unit accordingly). The fan steps (`RuntimeConstants.Layout.Tableau.FaceUpFanY`/`FaceDownFanY`) assume a card roughly 1.4 units tall.

## 2. `CardView` prefab (`Prefabs/CardView`)

GameObject with:
- `SpriteRenderer` — the card image. Set a sorting layer; order is driven at runtime.
- `BoxCollider2D` — sized to the card; used for drag hit-testing.
- `CardView` component (`Appodeal.Solitaire.Runtime.Core.Presentation.CardView`):
  - assign **`_renderer`** = the `SpriteRenderer`,
  - assign **`_collider`** = the `BoxCollider2D`.

Save as `Assets/_Project/Resources/Prefabs/CardView.prefab`.

## 3. `PileView` prefab (`Prefabs/PileView`)

GameObject with:
- `BoxCollider2D` — the drop/tap hit area (about one card in size; tableau zones may be taller).
- `PileView` component (`Appodeal.Solitaire.Runtime.Core.Presentation.PileView`).
- Optional: a faint placeholder `SpriteRenderer` (empty-slot frame) for visual clarity. Not required by code.

Save as `Assets/_Project/Resources/Prefabs/PileView.prefab`.

## 4. `BoardView` prefab (`Prefabs/BoardView`)

Root GameObject with the `BoardView` component (`Appodeal.Solitaire.Runtime.Core.Presentation.BoardView`) and these children:

Board roots (plain transforms at the board origin):
- `PilesRoot` (Transform) — parent for the 13 pile slots.
- `CardsRoot` (Transform) — parent for card views.

Screen-space overlay (a `Canvas`, render mode *Screen Space - Overlay*, with `GraphicRaycaster`):
- `UndoButton` — `UnityEngine.UI.Button`, label via `LightSide.UniText` ("Undo").
- `RedoButton` — `Button` + `UniText` ("Redo").
- `NewGameButton` — `Button` + `UniText` ("New Game").
- `WinBanner` — a panel GameObject containing:
  - `WinLabel` — a `LightSide.UniText` (the win text; code sets it to "You win!").
  - `WinNewGameButton` — `Button` + `UniText` ("New Game"). (May reuse the same handler as `NewGameButton`.)

Assign on the `BoardView` component:
- `_pilesRoot` = `PilesRoot`, `_cardsRoot` = `CardsRoot`.
- `_undoButton`, `_redoButton`, `_newGameButton`.
- `_winBanner` = the `WinBanner` GameObject, `_winLabel` = `WinLabel`, `_winNewGameButton` = `WinNewGameButton`.

Notes:
- The code calls `BoardView.InitializeAsync` which adds the click listeners and hides the banner; you only assign references, no manual `onClick` wiring needed.
- `PilesRoot`/`CardsRoot` are positioned at the board origin; the `LayoutSystem` supplies local coordinates, so keep these roots at local `(0,0,0)`.

Save as `Assets/_Project/Resources/Prefabs/BoardView.prefab`.

## 5. Verify against the scene task

Complete [core-scene.md](core-scene.md) (orthographic camera framing X `[-6..+6]`, Y `[-3..+4]`; `EventSystem`; `CoreScope`). Press Play — `CoreFlow` loads assets, builds the board and deals a new game; cards drag-and-drop, the stock draws on tap, and Undo/Redo/New Game work.

## Checklist

- [ ] 52 face sprites + `Back` at `Resources/Cards/...` named `{Suit}_{Rank}`.
- [ ] `CardView` prefab with `SpriteRenderer` + `BoxCollider2D` wired into the `CardView` component.
- [ ] `PileView` prefab with `BoxCollider2D` + `PileView` component.
- [ ] `BoardView` prefab with roots, overlay Canvas, 3 buttons, win banner, all references assigned.
- [ ] All three prefabs under `Resources/Prefabs/` at the constant paths.

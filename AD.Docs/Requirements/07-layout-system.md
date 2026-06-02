# Layout System (LayoutSystem)

> **Type:** System
> **API class:** `LayoutSystem`
> **Interface:** `ILayoutSystem`
> **References:** [00-overview.md](00-overview.md), [08-presentation-system.md](08-presentation-system.md) §6, [constants.md](../Architecture/constants.md)

---

## 1. Purpose

`LayoutSystem` is the **pure layout math** of the playing field. It computes:

- the anchor positions of all piles (stock, waste, 4 foundations, 7 tableau columns);
- the offset of each card within a pile (a downward fan for tableau; a stack for the rest).

The system **holds no board state and knows nothing about views**: the input is a pile identifier and a card index, the output is coordinates. This is extracted out of `PresentationSystem` so that "where things should be" (geometry) is separated from "how to move them nicely" (animation).

Coordinates are returned in the local space of the board container (`BoardView`); `PresentationSystem` positions the views relative to it.

---

## 2. Responsibility boundaries

### In scope:

| Responsibility | Description |
|---|---|
| Pile positions | The anchor of each of the 13 piles (1 stock + 1 waste + 4 foundations + 7 columns) |
| Card offsets | A fan offset in tableau (different for face-up/face-down); a stack for stock/waste/foundations |
| Card size | The reference card size for the calculations |

### Out of scope:

| Responsibility | Owner |
|---|---|
| Creating/moving views, animation | `PresentationSystem` ([08-presentation-system.md](08-presentation-system.md)) |
| Board state, the card count in a pile | `GameSystem` (an index is passed, not the state) |
| Pointer hit-testing | `PresentationSystem` |
| Adaptation to different screens/orientations | Basics via constants; advanced adaptation is out of scope |

---

## 3. Interaction

| Counterpart | Direction | Mechanism |
|---|---|---|
| `PresentationSystem` | outward | `ILayoutSystem` via constructor |

```
PresentationSystem (when building/animating):
    anchor = layout.GetPilePosition(pileId)
    offset = layout.GetCardOffset(pileId.Kind, indexInPile, card.FaceUp)
    targetLocalPos = anchor + offset      // where to place/animate the CardView
```

---

## 4. Field geometry

```
[Stock] [Waste]        [Foundation0][Foundation1][Foundation2][Foundation3]   <- top row

[Tab0] [Tab1] [Tab2] [Tab3] [Tab4] [Tab5] [Tab6]                              <- tableau columns
  |      |  (cards fanned downward)
```

| Pile | Placement |
|---|---|
| `Stock` | Top row, left |
| `Waste` | Top row, next to the stock |
| `Foundation` 0..3 | Top row, right, stepping along X |
| `Tableau` 0..6 | Lower area, 7 columns stepping along X |

| Card offset | Rule |
|---|---|
| `Tableau`, face-up card | downward step `FaceUpFanY` |
| `Tableau`, face-down card | smaller downward step `FaceDownFanY` |
| `Stock`/`Waste`/`Foundation` | stacked (zero or a minimal offset) |

---

## 5. Requirements

### REQ-LAY-001: Pile positions

**Description:** `GetPilePosition(PileId)` returns a pile's anchor in board-local coordinates.

**Behavior:**
1. `Stock`: `(StockX, TopRowY)`.
2. `Waste`: `(WasteX, TopRowY)`.
3. `Foundation[i]`: `(FoundationStartX + i * PileStepX, TopRowY)`.
4. `Tableau[i]`: `(TableauStartX + i * PileStepX, TableauTopY)`.
5. All values come from `RuntimeConstants.Layout`.

---

### REQ-LAY-002: Card offset within a pile

**Description:** `GetCardOffset(PileKind, indexInPile, faceUp)` — the offset of the card at `indexInPile` relative to the pile anchor.

**Behavior:**
1. `Tableau`: a cumulative downward offset; for each card below, add `FaceUpFanY` or `FaceDownFanY` depending on whether it is face-up.
2. `Stock`/`Waste`/`Foundation`: offset `Vector3.zero` (or a minimal visual stack shift).
3. A pure function: the same input → the same output.

**Example:**
```csharp
public Vector3 GetCardOffset(PileKind kind, int indexInPile, bool faceUp)
{
    if (kind != PileKind.Tableau)
        return Vector3.zero;
    // the cumulative downward fan is computed by the caller over the column's cards,
    // or here from indexInPile assuming a uniform step:
    return new Vector3(0f, -indexInPile * Layout.Tableau.FaceUpFanY, 0f);
}
```

> For mixed face-up/face-down cards, the exact cumulative shift is computed over the column's actual cards; `LayoutSystem` provides the steps (`FaceUpFanY`/`FaceDownFanY`) and the summation is done deterministically (in the system or on the caller side — to be fixed at implementation time, with no board state inside `LayoutSystem`).

---

### REQ-LAY-003: Purity and determinism

**Description:** The system has no board state and no side effects.

**Behavior:**
1. Does not hold `BoardState`, does not depend on `GameSystem`.
2. The result depends only on the input arguments and the `RuntimeConstants.Layout` constants.

---

## 6. Internal structure

### File structure

```
Layout/
└── LayoutSystem.cs    // ILayoutSystem + LayoutSystem (all the math; no workers needed)
```

### Components

| Component | Type | Responsibility |
|---|---|---|
| `LayoutSystem` | API class | Compute pile positions and card offsets from `RuntimeConstants.Layout` |

---

## 7. Dependencies

### Packages

| Package | Purpose |
|---|---|
| UnityEngine (core) | `Vector3` / `Vector2` |

### Dependencies on other systems

None. Uses the `PileKind` enum and the `PileId` type (see [01-game-system.md](01-game-system.md) §4).

### Constants

`RuntimeConstants.Layout`: `TopRowY`, `TableauTopY`, `StockX`, `WasteX`, `FoundationStartX`, `TableauStartX`, `PileStepX`, `Tableau.FaceUpFanY`, `Tableau.FaceDownFanY`, `CardSize`.

---

## 8. Implementation checklist

- [ ] `ILayoutSystem` + `LayoutSystem`.
- [ ] `GetPilePosition` for stock/waste/4 foundations/7 columns (REQ-LAY-001).
- [ ] `GetCardOffset` with a tableau fan and a stack for the rest (REQ-LAY-002).
- [ ] Pure functions with no board state (REQ-LAY-003).
- [ ] All values in `RuntimeConstants.Layout`.

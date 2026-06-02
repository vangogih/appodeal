# Rules Subsystem (RulesSubSystem)

> **Type:** Subsystem of `GameSystem`
> **API class:** `RulesSubSystem`
> **Interface:** `IRulesSubSystem`
> **References:** [00-overview.md](00-overview.md), [01-game-system.md](01-game-system.md) §4, §5

---

## 1. Purpose

`RulesSubSystem` is the keeper of Klondike rules. It answers two questions:

1. **Is a move legal?** — `Validate(state, move)`.
2. **Has the game been won?** — `IsWin(state)`.

The subsystem **does not mutate** the board and knows nothing about rendering/input/history — it is a pure predicate over `BoardState`. Applying a move is `GameSystem`'s responsibility. It is accessible only to its parent system `GameSystem`.

---

## 2. Responsibility boundaries

### In scope:

| Responsibility | Description |
|---|---|
| Tableau move validation | Descending rank + alternating color; an empty column accepts only a King |
| Foundation move validation | Ace first, then ascending in one suit; one card only |
| Moved-sequence validation | A tableau group: all face-up and forming a valid descending alternating-color run |
| Source validation | Which pile, and how many cards, may be picked up |
| Win detection | All 4 foundations complete (52 cards in foundations) |

### Out of scope:

| Responsibility | Owner |
|---|---|
| Moving cards / mutating the board | `GameSystem` ([01-game-system.md](01-game-system.md)) |
| Stock draw and recycle logic | `GameSystem` (REQ-GAME-003) |
| Auto-flip | `GameSystem` (REQ-GAME-005) |
| Hints / move search / solver | Out of scope ([00-overview.md](00-overview.md) §9) |

---

## 3. Interaction

| Counterpart | Direction | Mechanism |
|---|---|---|
| `GameSystem` | called from above | Direct composition: `GameSystem` creates `new RulesSubSystem()` and calls `Validate`/`IsWin` |

The subsystem returns its result as the method return value (no Service Locator).

```csharp
public interface IRulesSubSystem
{
    bool Validate(BoardState state, Move move);
    bool IsWin(BoardState state);
}
```

---

## 4. Rules (Klondike, Draw-1)

### Move source (what may be picked up)

| Source `From.Kind` | Card count | Condition |
|---|---|---|
| `Stock` | — | Not allowed as a `TryMove` source (use `DrawStock` only) |
| `Waste` | 1 | Top card |
| `Foundation` | 1 | Top card (foundation → tableau is allowed) |
| `Tableau` | 1..N | The top `count` cards: all face-up and forming a valid run (see REQ-RULE-003) |

### Move target (where it may be placed)

| Target `To.Kind` | Condition |
|---|---|
| `Tableau` (empty) | The bottom card of the group is a `King` |
| `Tableau` (non-empty) | `moved.Rank == top.Rank − 1` and `moved.Color ≠ top.Color` |
| `Foundation` | `count == 1`; empty → `Ace`; otherwise `card.Suit == top.Suit` and `card.Rank == top.Rank + 1` |
| `Stock` / `Waste` | Not allowed as a `TryMove` target |

"moved" is the bottom (highest-position) card of the moved group, the one that will land on the target's `top`.

---

## 5. Requirements

### REQ-RULE-001: Tableau move

**Description:** Validate moving a card/group onto a tableau column.

**Behavior:**
1. Let `m` be the bottom card of the moved group, `t` the target column.
2. If `t` is empty — valid iff `m.Rank == King`.
3. Otherwise — valid if `m.Rank == top(t).Rank − 1` AND `m.Color != top(t).Color`.

---

### REQ-RULE-002: Foundation move

**Description:** Validate moving a single card onto a foundation.

**Behavior:**
1. If `move.Count != 1` — invalid.
2. Let `c` be the moved card, `f` the target foundation.
3. If `f` is empty — valid if `c.Rank == Ace`.
4. Otherwise — valid if `c.Suit == top(f).Suit` AND `c.Rank == top(f).Rank + 1`.

---

### REQ-RULE-003: Movable sequence from tableau

**Description:** Validate that the top `count` cards of the source column can be lifted as a group.

**Behavior:**
1. All `count` cards must be `FaceUp == true`.
2. They must form a **descending alternating-color run**: each next card (lower in the pile, closer to the top) is one rank smaller and the opposite color.
3. For `Waste`/`Foundation` sources, only `count == 1` is allowed.

**Example:**
```csharp
// red 7 -> black 6 -> red 5 : a valid group of 3 cards
bool IsMovableRun(IReadOnlyList<Card> run)
{
    for (int i = 0; i < run.Count - 1; i++)
        if (!run[i].FaceUp || run[i + 1].Rank != run[i].Rank - 1 || run[i + 1].Color == run[i].Color)
            return false;
    return run.Count == 0 || run[^1].FaceUp;
}
```

---

### REQ-RULE-004: Win condition

**Description:** Determine the end of the game.

**Behavior:**
1. A win occurs when all 4 foundations together hold 52 cards (each foundation full: Ace…King of one suit, King on top).
2. `IsWin` has no side effects.

---

### REQ-RULE-005: Full move validation

**Description:** `Validate(state, move)` combines the source and target checks.

**Behavior:**
1. Reject if `From.Kind == Stock`, or `To.Kind ∈ {Stock, Waste}`.
2. Check the source (REQ-RULE-003 and the table in §4): the pile holds `count` cards and they are allowed to be lifted.
3. Check the target: tableau (REQ-RULE-001) or foundation (REQ-RULE-002).
4. Return `true` only if both the source and the target are valid.

---

## 6. Internal structure

### File structure

```
Game/Rules/
├── RulesSubSystem.cs    // IRulesSubSystem + RulesSubSystem (business logic: what and when to validate)
├── MoveValidator.cs     // worker: the source/target validation algorithm
└── WinDetector.cs       // worker: the win-check algorithm
```

### Components

| Component | Type | Responsibility |
|---|---|---|
| `RulesSubSystem` | API class | Decides **what** to validate (source/target/win); combines the result |
| `MoveValidator` | Worker | The move-validation algorithm details (service logic) |
| `WinDetector` | Worker | The win-check algorithm details (service logic) |

---

## 7. Dependencies

### Packages

| Package | Purpose |
|---|---|
| — | Pure C# |

### Dependencies on other systems

None. Uses the domain types `Card`, `Move`, `BoardState` and the enums `Rank`/`CardColor`/`Suit`/`PileKind` (see [01-game-system.md](01-game-system.md) §4).

### Constants

`RuntimeConstants.Game`: `Board.FoundationCount = 4`, `Cards.DeckSize = 52`.

---

## 8. Implementation checklist

- [ ] `IRulesSubSystem` + `RulesSubSystem` (`Validate`, `IsWin`).
- [ ] `MoveValidator`: tableau (REQ-RULE-001), foundation (REQ-RULE-002), sequence (REQ-RULE-003), source/target (REQ-RULE-005).
- [ ] `WinDetector`: 52 cards in foundations (REQ-RULE-004).
- [ ] Pure predicates with no `BoardState` mutation.
- [ ] The subsystem is created by `GameSystem` directly and not exposed outward.

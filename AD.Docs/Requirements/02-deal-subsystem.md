# Deal Subsystem (DealSubSystem)

> **Type:** Subsystem of `GameSystem`
> **API class:** `DealSubSystem`
> **Interface:** `IDealSubSystem`
> **References:** [00-overview.md](00-overview.md), [01-game-system.md](01-game-system.md) §4, §7

---

## 1. Purpose

`DealSubSystem` prepares the **initial board state** for a Klondike game: it builds a standard 52-card deck, shuffles it, and lays it out by Klondike rules (7 tableau columns + stock).

The subsystem knows nothing about rendering, input, or move history. Its only job is to return a valid `BoardState` to start the game. It is accessible only to its parent system `GameSystem` (rules 1–2 in [architecture-rules.md](../Architecture/architecture-rules.md)).

---

## 2. Responsibility boundaries

### In scope:

| Responsibility | Description |
|---|---|
| Build the deck | A full 52-card deck (4 suits × 13 ranks) |
| Shuffle | Fisher–Yates algorithm; optional `seed` for reproducibility |
| Klondike deal | 7 columns sized 1..7 with the top card face-up; 24 cards to the stock; waste/foundations empty |

### Out of scope:

| Responsibility | Owner |
|---|---|
| Holding and mutating the board after start | `GameSystem` ([01-game-system.md](01-game-system.md)) |
| Move and win checks | `RulesSubSystem` ([03-rules-subsystem.md](03-rules-subsystem.md)) |
| Guaranteeing a winnable deal (solver) | Out of scope (see [00-overview.md](00-overview.md) §9) |
| Any rendering | `PresentationSystem` |

---

## 3. Interaction

| Counterpart | Direction | Mechanism |
|---|---|---|
| `GameSystem` | called from above | Direct composition: `GameSystem` creates `new DealSubSystem()` and calls `Deal(...)` |

The subsystem returns its result upward as the method return value — no Service Locator needed.

```
GameSystem.StartNewGame(seed)
    -> deal.Deal(seed) : BoardState
    -> set as current state
```

---

## 4. Deal algorithm

| Step | Action | Detail |
|---|---|---|
| 1 | Build the deck | 52 cards: for each `Suit` × each `Rank`, `FaceUp = false` |
| 2 | Shuffle | Fisher–Yates over `System.Random(seed)` or `Random()` without a seed |
| 3 | Lay out tableau | Column `i` (0..6) receives `i + 1` cards; the topmost is `FaceUp = true`, the rest face-down |
| 4 | Fill the stock | The remaining 24 cards go to the stock, all `FaceUp = false` |
| 5 | Clear waste and foundations | Waste empty; 4 foundations empty |

Tableau card count: 1+2+3+4+5+6+7 = 28; stock: 52 − 28 = 24.

---

## 5. Requirements

### REQ-DEAL-001: Build a standard deck

**Description:** Create a full deck of 52 unique cards.

**Behavior:**
1. For each `Suit` (4) and each `Rank` (13), create `Card{ suit, rank, FaceUp = false }`.
2. Result: 52 cards with no duplicates.

---

### REQ-DEAL-002: Shuffle (Fisher–Yates)

**Description:** Shuffle the deck with the Fisher–Yates algorithm.

**Input:** `int? seed` (optional).

**Behavior:**
1. If `seed` is provided — use `new System.Random(seed)` (reproducible deal); otherwise `new System.Random()`.
2. Classic back-to-front pass swapping the current element with a random one from the unprocessed prefix.

**Example:**
```csharp
for (int i = deck.Count - 1; i > 0; i--)
{
    int j = random.Next(i + 1);
    (deck[i], deck[j]) = (deck[j], deck[i]);
}
```

---

### REQ-DEAL-003: Klondike deal

**Description:** Lay out the shuffled deck into the initial `BoardState`.

**Behavior:**
1. 7 tableau columns: column `i` receives `i + 1` cards.
2. In each column, only the top card is `FaceUp = true`; the rest are face-down.
3. The remaining 24 cards go to the stock (face-down).
4. The waste and 4 foundations are empty.
5. Return the assembled `BoardState`.

**Example signature:**
```csharp
public interface IDealSubSystem
{
    BoardState Deal(int? seed = null);
}
```

---

## 6. Internal structure

### File structure

```
Game/Deal/
├── DealSubSystem.cs     // IDealSubSystem + DealSubSystem (deal business logic)
├── DeckShuffler.cs      // worker: Fisher–Yates
└── Dealer.cs            // worker: lay the deck out into a BoardState
```

### Components

| Component | Type | Responsibility |
|---|---|---|
| `DealSubSystem` | API class | Orchestration: build → shuffle → lay out; decides **what** and **when** (business logic) |
| `DeckShuffler` | Worker | The shuffle algorithm (service logic) |
| `Dealer` | Worker | Lay a card array out into a `BoardState` structure (service logic) |

`DeckShuffler` and `Dealer` are invisible outside `DealSubSystem`.

---

## 7. Dependencies

### Packages

| Package | Purpose |
|---|---|
| — | Pure C# (`System.Random`) |

### Dependencies on other systems

None. Uses the domain types `Card`, `BoardState` from `Runtime/Domain/` (see [01-game-system.md](01-game-system.md) §4).

### Constants

`RuntimeConstants.Game`: `Board.TableauColumns = 7`, `Cards.DeckSize = 52` (see [01-game-system.md](01-game-system.md) §8).

---

## 8. Implementation checklist

- [ ] `IDealSubSystem` + `DealSubSystem` with a `Deal(seed)` method.
- [ ] `DeckShuffler` — Fisher–Yates with an optional seed (REQ-DEAL-002).
- [ ] `Dealer` — lay out 7 columns (1..7, top face-up) + 24 to the stock (REQ-DEAL-003).
- [ ] Return a valid `BoardState` (28 in tableau, 24 in stock, waste/foundations empty).
- [ ] The subsystem is created by `GameSystem` directly and not exposed outward.

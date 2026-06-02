# Game System (GameSystem)

> **Type:** System
> **API class:** `GameSystem`
> **Interface:** `IGameSystem`
> **References:** [00-overview.md](00-overview.md), [02-deal-subsystem.md](02-deal-subsystem.md), [03-rules-subsystem.md](03-rules-subsystem.md), [04-undo-system.md](04-undo-system.md), [08-presentation-system.md](08-presentation-system.md)

---

## 1. Purpose

`GameSystem` is the head system of the gameplay. It is **pure logic with no Unity**: it owns the board state (`BoardState`), orchestrates the game loop, and holds all business logic of a Klondike (Draw-1) game.

Every game operation goes through it: starting a new game, moving cards, drawing from the stock, undoing/redoing a move, detecting a win. State is exposed **read-only** (snapshot + `OnBoardChanged` event); only the system itself may mutate the board.

`GameSystem` uses two subsystems (direct composition): `DealSubSystem` (deal) and `RulesSubSystem` (rules), and depends on the external `IUndoSystem` (move history).

---

## 2. Responsibility boundaries

### In scope:

| Responsibility | Description |
|---|---|
| Board state | Owns `BoardState` (stock, waste, 4 foundations, 7 columns); the single mutation point |
| Game loop | Start a game, apply moves, auto-flip, win check |
| Stock draw (Draw-1) | Move 1 card stock→waste; recycle waste→stock when the stock is empty |
| Applying moves | Move cards between piles after validation |
| Undo/Redo | Record `BoardChange` into `IUndoSystem`, revert and replay on the board state |
| Public contract | `IGameSystem`: commands, state snapshot, `OnBoardChanged`/`OnGameWon` events |

### Out of scope:

| Responsibility | Owner |
|---|---|
| Creating/shuffling/dealing the deck | `DealSubSystem` ([02-deal-subsystem.md](02-deal-subsystem.md)) |
| Move legality and win check (the algorithm) | `RulesSubSystem` ([03-rules-subsystem.md](03-rules-subsystem.md)) |
| Storing the history stacks | `UndoSystem` ([04-undo-system.md](04-undo-system.md)) |
| Any rendering, sprites, animation, input | `PresentationSystem`, `InputSystem` |
| Asset loading, screen-position math | `GameAssetsSystem`, `LayoutSystem` |

---

## 3. Interaction with other systems

| Counterpart | Direction | Mechanism | Why |
|---|---|---|---|
| `DealSubSystem` | down | direct composition (`new`) | obtain the initial `BoardState` |
| `RulesSubSystem` | down | direct composition (`new`) | validate a move, check for a win |
| `UndoSystem` | sideways | `IUndoSystem` via constructor | record/retrieve `BoardChange` |
| `PresentationSystem` | up (outward) | `IGameSystem` events | subscribe to `OnBoardChanged`/`OnGameWon`, invoke commands |

Move-application flow:

```
PresentationSystem -> IGameSystem.TryMove(from, to, count)
    GameSystem:
        if (!rules.Validate(State, move)) return false   // direct composition, bool
        var change = ApplyMove(move)                      // mutate board + auto-flip, build BoardChange
        undo.Record(change)                               // IUndoSystem
        OnBoardChanged?.Invoke(State)
        if (rules.IsWin(State)) OnGameWon?.Invoke()
```

> **Service Locator.** Per [architecture-rules.md](../Architecture/architecture-rules.md) §4, `GameSystem` may hold a local Service Locator for bottom-up calls. In the current minimal scope the workers (`MoveValidator`, `Dealer`, …) **do not** call `GameSystem` business logic: subsystems return their result upward and `GameSystem` performs the mutation itself (direct composition, the default mechanism). Therefore no requirement in this document needs a Service Locator.

---

## 4. Domain model

Passive data types — **no behavior** (beyond computed properties), no dependencies — live in the shared namespace `Appodeal.Solitaire.Runtime.Domain`. Enums used by several systems live at namespace level next to `RuntimeConstants` (per [constants.md](../Architecture/constants.md)). `UndoSystem`, `LayoutSystem`, and `PresentationSystem` reuse these contracts (read-only).

### Enums

```csharp
public enum Suit  { Clubs, Diamonds, Hearts, Spades }
public enum CardColor { Black, Red }
public enum Rank  { Ace = 1, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King }
public enum PileKind { Stock, Waste, Foundation, Tableau }
```

### Card and pile identifier

```csharp
public readonly struct Card
{
    public readonly Suit Suit;
    public readonly Rank Rank;
    public readonly bool FaceUp;

    public CardColor Color => Suit is Suit.Hearts or Suit.Diamonds ? CardColor.Red : CardColor.Black;
}

public readonly struct PileId
{
    public readonly PileKind Kind;
    public readonly int Index; // Stock/Waste: 0; Foundation: 0..3; Tableau: 0..6
}
```

### Board state

| Field | Type | Count | Top of pile |
|---|---|---|---|
| `Stock` | list of `Card` | 1 | last element |
| `Waste` | list of `Card` | 1 | last element |
| `Foundations` | list of `Card`[] | 4 | last element |
| `Tableau` | list of `Card`[] | 7 | last element |

```csharp
public sealed class BoardState
{
    public IReadOnlyList<Card> Stock { get; }
    public IReadOnlyList<Card> Waste { get; }
    public IReadOnlyList<Card> Foundations { get; } // 4 piles
    public IReadOnlyList<Card> Tableau { get; }     // 7 piles
    // read-only snapshot; the mutable version is private to GameSystem
}
```

### Move and reversible change

```csharp
public readonly struct Move            // a move request
{
    public readonly PileId From;
    public readonly PileId To;
    public readonly int Count;          // 1 for most moves; >1 for tableau sequences
}

public enum BoardChangeKind { Move, DrawStock, RecycleStock }

public sealed class BoardChange        // a reversible record for Undo/Redo
{
    public BoardChangeKind Kind;
    public PileId From;
    public PileId To;
    public int Count;
    public bool FlippedSourceTop;       // whether the source pile's top card was auto-flipped
}
```

`BoardChange` stores the **minimum required to revert**: kind, source/target, card count, and the auto-flip flag. For `RecycleStock`, `Count` is the number of cards moved. History storage details are in [04-undo-system.md](04-undo-system.md).

---

## 5. Gameplay rules (Klondike, Draw-1)

| Element | Behavior |
|---|---|
| Start | Deal by `DealSubSystem`; stock = 24 cards, 7 columns sized 1..7 with tops face-up; waste/foundations empty |
| Stock draw | Draw-1: the top stock card is flipped face-up and moved to the waste |
| Recycle | Stock empty → the entire waste is returned to the stock face-down, in reverse order (unlimited) |
| Tableau move | By `RulesSubSystem`: one rank lower and the opposite color; an empty column accepts only a King |
| Foundation move | By `RulesSubSystem`: Ace → … → King, one suit, one card at a time |
| Auto-flip | After the last face-up card leaves a column, its new top (face-down) card is flipped face-up |
| Win | All 4 foundations are full (King on top) — determined by `RulesSubSystem` |

---

## 6. Requirements

### REQ-GAME-001: Board state

**Description:** `GameSystem` owns the single mutable `BoardState` and exposes only a read-only snapshot of it.

**Behavior:**
1. Holds stock, waste, 4 foundations, 7 columns per §4.
2. The `State` property returns an immutable snapshot (or a read-only wrapper).
3. No external code can mutate the board outside the `IGameSystem` commands.

---

### REQ-GAME-002: Start a new game

**Description:** `StartNewGame(int? seed = null)` prepares a fresh deal.

**Behavior:**
1. Request an initial `BoardState` from `DealSubSystem` (with an optional `seed`).
2. Set it as the current state.
3. Clear history: `undo.Clear()`.
4. Raise `OnBoardChanged(State)`.

---

### REQ-GAME-003: Stock draw (Draw-1) and recycle

**Description:** `DrawStock()` implements the Draw-1 mechanic with unlimited recycling.

**Behavior:**
1. If the stock is not empty: take the top stock card, set `FaceUp = true`, push to the waste. Record `BoardChange{ Kind = DrawStock, Count = 1 }`.
2. If the stock is empty and the waste is not: move all waste cards back to the stock face-down, in reverse order. Record `BoardChange{ Kind = RecycleStock, Count = N }`.
3. If both stock and waste are empty: do nothing, return `false`.
4. On change — `undo.Record(change)` and `OnBoardChanged(State)`.

---

### REQ-GAME-004: Move cards

**Description:** `TryMove(PileId from, PileId to, int count)` performs a move after validation.

**Behavior:**
1. Build `Move{from, to, count}`.
2. Call `RulesSubSystem.Validate(State, move)`.
3. If invalid — return `false`, state unchanged.
4. If valid — move `count` cards from `from` to `to`, perform auto-flip (REQ-GAME-005), build a `BoardChange`.
5. `undo.Record(change)`, `OnBoardChanged(State)`, win check (REQ-GAME-006). Return `true`.

---

### REQ-GAME-005: Auto-flip a revealed card

**Description:** After cards leave a tableau column, its new top card flips face-up automatically.

**Behavior:**
1. If, after the move, the source tableau column's top card is face-down (`FaceUp == false`) — set `FaceUp = true`.
2. Record the fact in `BoardChange.FlippedSourceTop = true` (for correct undo).
3. Applies only to `PileKind.Tableau`.

---

### REQ-GAME-006: Win detection

**Description:** A win is checked after every successful change.

**Behavior:**
1. Call `RulesSubSystem.IsWin(State)`.
2. If true — raise `OnGameWon()` (once per game).

---

### REQ-GAME-007: Undo / Redo

**Description:** `Undo()` and `Redo()` revert and replay the last move on top of `IUndoSystem`.

**Behavior:**
1. `Undo()`: if `undo.CanUndo` — `change = undo.PopUndo()`, **revert** the change on the board (including returning the auto-flipped card to face-down when `FlippedSourceTop`), `OnBoardChanged(State)`, return `true`; otherwise `false`.
2. `Redo()`: if `undo.CanRedo` — `change = undo.PopRedo()`, **re-apply** the change, `OnBoardChanged(State)`, return `true`; otherwise `false`.
3. Any new move/draw (REQ-GAME-003/004) clears the redo stack — this is the `UndoSystem.Record` contract ([04-undo-system.md](04-undo-system.md), REQ-UNDO-002).
4. `CanUndo`/`CanRedo` proxy the `IUndoSystem` state.

> Reverting **state** is the responsibility of `GameSystem` (the board owner). `UndoSystem` stores only `BoardChange` records.

---

### REQ-GAME-008: Public API and events

**Description:** The system's external contract.

```csharp
public interface IGameSystem
{
    BoardState State { get; }
    bool CanUndo { get; }
    bool CanRedo { get; }

    event Action<BoardState> OnBoardChanged;
    event Action OnGameWon;

    void StartNewGame(int? seed = null);
    bool TryMove(PileId from, PileId to, int count);
    bool DrawStock();
    bool Undo();
    bool Redo();
}
```

**Behavior:** `OnBoardChanged` is raised on any board change; `OnGameWon` when the win condition is met. Subscribers must unsubscribe (see [event-subscriptions.md](../Architecture/event-subscriptions.md)).

---

## 7. Internal structure

### File structure

```
Game/
├── GameSystem.cs            // IGameSystem + GameSystem (business logic; may be partial)
├── Deal/
│   ├── DealSubSystem.cs     // IDealSubSystem + DealSubSystem
│   ├── DeckShuffler.cs      // worker
│   └── Dealer.cs            // worker
└── Rules/
    ├── RulesSubSystem.cs    // IRulesSubSystem + RulesSubSystem
    ├── MoveValidator.cs     // worker
    └── WinDetector.cs       // worker
```

Domain types live in shared `Runtime/Domain/` (`Card.cs`, `PileId.cs`, `BoardState.cs`, `BoardChange.cs`, `Move.cs`); enums next to `RuntimeConstants` (see [00-overview.md](00-overview.md) §8).

### Components

| Component | Type | Responsibility |
|---|---|---|
| `GameSystem` | API class | Board state, loop, draw/recycle, applying moves, auto-flip, Undo/Redo, events |
| `DealSubSystem` | Subsystem | Deal (see [02-deal-subsystem.md](02-deal-subsystem.md)) |
| `RulesSubSystem` | Subsystem | Validation and win (see [03-rules-subsystem.md](03-rules-subsystem.md)) |

Subsystems are created by `GameSystem` directly (`new`) and called directly; they are invisible outside (rules 1–3 in [architecture-rules.md](../Architecture/architecture-rules.md)).

---

## 8. Dependencies

### Packages

| Package | Purpose |
|---|---|
| — | Pure C#; no Unity dependencies |

### Dependencies on other systems

| Dependency | Mechanism |
|---|---|
| `IUndoSystem` | via constructor (registered in `CoreScope`) |

`DealSubSystem`, `RulesSubSystem` are internal subsystems (direct composition, not injection).

### Constants

`RuntimeConstants.Game`: `Board.TableauColumns = 7`, `Board.FoundationCount = 4`, `Cards.DeckSize = 52`, `Cards.RankCount = 13`, `Cards.SuitCount = 4`, `Stock.DrawCount = 1`.

---

## 9. Usage in CoreScope / CoreFlow

```csharp
// CoreScope.Configure
builder.Register<IUndoSystem, UndoSystem>(Lifetime.Singleton);
builder.Register<IGameSystem, GameSystem>(Lifetime.Singleton);
// GameSystem receives IUndoSystem via constructor;
// it creates DealSubSystem/RulesSubSystem internally.
```

```csharp
// CoreFlow.Start (after assets load and presentation init)
_game.StartNewGame();   // deal -> OnBoardChanged -> presentation builds the board
```

---

## 10. Implementation checklist

- [ ] `BoardState` and domain types in `Runtime/Domain/`; enums next to `RuntimeConstants`.
- [ ] `IGameSystem` + `GameSystem` with commands and events (REQ-GAME-008).
- [ ] `StartNewGame` via `DealSubSystem` + `undo.Clear()` (REQ-GAME-002).
- [ ] `DrawStock` Draw-1 + recycle (REQ-GAME-003).
- [ ] `TryMove` validated via `RulesSubSystem` (REQ-GAME-004).
- [ ] Auto-flip recorded in `BoardChange` (REQ-GAME-005).
- [ ] Win check and `OnGameWon` (REQ-GAME-006).
- [ ] `Undo`/`Redo` on top of `IUndoSystem` with state revert (REQ-GAME-007).
- [ ] Constants extracted to `RuntimeConstants.Game`.

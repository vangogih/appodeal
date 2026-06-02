# Move History System (UndoSystem)

> **Type:** System
> **API class:** `UndoSystem`
> **Interface:** `IUndoSystem`
> **References:** [00-overview.md](00-overview.md), [01-game-system.md](01-game-system.md) §4, §6 (REQ-GAME-007)

---

## 1. Purpose

`UndoSystem` stores the **move history** of a game as two stacks of reversible `BoardChange` records: an undo stack and a redo stack. It is **pure logic with no Unity**.

The system is intentionally "dumb": it only stores and hands out `BoardChange` records. **Reverting and replaying the board state itself is done by `GameSystem`** (the `BoardState` owner), because only it knows how to apply and reverse a change. This split avoids duplicating board logic and prevents circular dependencies.

It corresponds to the `UndoSystem` reserved in [domain-responsibility.md](../Architecture/domain-responsibility.md) (the "Move history" domain).

---

## 2. Responsibility boundaries

### In scope:

| Responsibility | Description |
|---|---|
| Undo stack | A LIFO store of applied `BoardChange` records |
| Redo stack | A LIFO store of undone `BoardChange` records |
| Record a move | `Record` pushes a change to undo and clears redo |
| Hand out for undo/redo | `PopUndo` / `PopRedo` move a record between stacks |
| Availability flags | `CanUndo` / `CanRedo` |
| Reset | `Clear` on starting a new game |

### Out of scope:

| Responsibility | Owner |
|---|---|
| Applying/reverting a change on the board | `GameSystem` (REQ-GAME-007) |
| Building a `BoardChange` | `GameSystem` (on move/draw) |
| Any rendering of Undo/Redo buttons | `PresentationSystem` |
| Persisting history between sessions | Out of scope ([00-overview.md](00-overview.md) §9) |

---

## 3. Interaction

| Counterpart | Direction | Mechanism |
|---|---|---|
| `GameSystem` | called from outside | `IUndoSystem` via the `GameSystem` constructor |

```
GameSystem.TryMove/DrawStock  -> undo.Record(change)         // a new move clears redo
GameSystem.Undo()             -> change = undo.PopUndo()     -> revert on the board
GameSystem.Redo()             -> change = undo.PopRedo()     -> replay on the board
GameSystem.StartNewGame()     -> undo.Clear()
```

`PresentationSystem` never talks to `UndoSystem` directly — the Undo/Redo buttons call `IGameSystem.Undo()/Redo()` (the domain's single-entry-point rule).

---

## 4. Contract

```csharp
public interface IUndoSystem
{
    bool CanUndo { get; }
    bool CanRedo { get; }

    void Record(BoardChange change);
    BoardChange PopUndo();   // call only when CanUndo
    BoardChange PopRedo();   // call only when CanRedo
    void Clear();
}
```

---

## 5. Requirements

### REQ-UNDO-001: Two `BoardChange` stacks

**Description:** The system holds an undo stack and a redo stack of `BoardChange` items.

**Behavior:**
1. Both stacks are initially empty.
2. `CanUndo == (undo.Count > 0)`, `CanRedo == (redo.Count > 0)`.

---

### REQ-UNDO-002: Record a move

**Description:** `Record(change)` captures an applied change.

**Behavior:**
1. Push `change` onto the undo stack.
2. **Clear** the redo stack (after a new move, replaying undone branches is impossible).

---

### REQ-UNDO-003: Undo

**Description:** `PopUndo()` hands out the last change to revert.

**Behavior:**
1. Precondition: `CanUndo == true`.
2. Pop the top `change` off the undo stack.
3. Push it onto the redo stack.
4. Return `change` (`GameSystem` reverts it).

---

### REQ-UNDO-004: Redo

**Description:** `PopRedo()` hands out the last undone change to replay.

**Behavior:**
1. Precondition: `CanRedo == true`.
2. Pop the top `change` off the redo stack.
3. Push it back onto the undo stack.
4. Return `change` (`GameSystem` re-applies it).

---

### REQ-UNDO-005: Reset history

**Description:** `Clear()` empties both histories.

**Behavior:**
1. Clear the undo stack and the redo stack.
2. Called by `GameSystem.StartNewGame` (REQ-GAME-002).

---

## 6. Internal structure

### File structure

```
Undo/
└── UndoSystem.cs    // IUndoSystem + UndoSystem (all logic; no workers needed)
```

### Components

| Component | Type | Responsibility |
|---|---|---|
| `UndoSystem` | API class | Manage the two `BoardChange` stacks and the availability flags |

No workers are required — the logic is trivial (two `Stack<BoardChange>`).

---

## 7. Dependencies

### Packages

| Package | Purpose |
|---|---|
| — | Pure C# (`System.Collections.Generic.Stack<>`) |

### Dependencies on other systems

None. Uses the domain type `BoardChange` from `Runtime/Domain/` (see [01-game-system.md](01-game-system.md) §4). The dependency graph is acyclic: `GameSystem → IUndoSystem`, both sides depend on `Domain`, which depends on nothing.

### Constants

`RuntimeConstants.Undo` — optional. In the minimal scope the history depth is **unlimited** (within a game). If needed later: `Undo.History.MaxDepth` discarding the oldest records (out of current scope).

---

## 8. Implementation checklist

- [ ] `IUndoSystem` + `UndoSystem` with two `Stack<BoardChange>`.
- [ ] `Record` pushes to undo and clears redo (REQ-UNDO-002).
- [ ] `PopUndo`/`PopRedo` move a record between stacks (REQ-UNDO-003/004).
- [ ] `CanUndo`/`CanRedo`, `Clear` (REQ-UNDO-001/005).
- [ ] Registered in `CoreScope` and injected into `GameSystem` via constructor.
- [ ] No `BoardState` mutation (storage of records only).

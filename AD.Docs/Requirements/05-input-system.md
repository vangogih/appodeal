# Input System (InputSystem)

> **Type:** System
> **API class:** `InputSystem`
> **Interface:** `IInputSystem`
> **References:** [00-overview.md](00-overview.md), [08-presentation-system.md](08-presentation-system.md) §5

---

## 1. Purpose

`InputSystem` is an input-device abstraction. It reads the pointer (mouse/touch) and publishes **semantic pointer events** in screen coordinates: press, move, release, and "tap".

The system **knows nothing about the domain or rendering**: not about cards, piles, or views. It deals only in screen coordinates and pointer phases. Who/what is under the pointer is determined by the consumer (`PresentationSystem`), because the view geometry belongs to it.

Input is polled per frame via `VContainer.Unity.ITickable` (no `MonoBehaviour`, in the spirit of [unity-rules.md](../Architecture/unity-rules.md) §2).

---

## 2. Responsibility boundaries

### In scope:

| Responsibility | Description |
|---|---|
| Read the pointer | Position and pressed/released state (mouse or the first touch) |
| Pointer phases | `OnPointerDown` / `OnPointerMove` / `OnPointerUp` events |
| Tap classification | A short press+release that does not exceed the drag threshold → `OnTap` |
| Normalization | Coordinates in a single screen space (screen pixels) |

### Out of scope:

| Responsibility | Owner |
|---|---|
| Hit-test: which card/pile is under the pointer | `PresentationSystem` (owns the view geometry) |
| Drag visuals | `PresentationSystem` |
| Building and validating a move | `GameSystem` / `RulesSubSystem` |
| Mapping an event to a game command | `PresentationSystem` (translation into `IGameSystem`) |
| Collider raycasts, camera references | Out of scope (hit-testing lives in presentation) |

---

## 3. Interaction

| Counterpart | Direction | Mechanism |
|---|---|---|
| `PresentationSystem` | outward | subscribes to `IInputSystem` events |

```
InputSystem.Tick() (every frame):
    reads the pointer -> raises OnPointerDown / OnPointerMove / OnPointerUp / OnTap

PresentationSystem:
    OnPointerDown -> hit-test the views, begin a visual drag
    OnPointerMove -> move the "lifted" card with the pointer
    OnPointerUp   -> hit-test the target pile -> IGameSystem.TryMove(...)
    OnTap         -> if the stock was hit -> IGameSystem.DrawStock()
```

---

## 4. Contract

```csharp
public interface IInputSystem
{
    event Action<Vector2> OnPointerDown;   // screen coordinates of the press
    event Action<Vector2> OnPointerMove;   // only while the pointer is held
    event Action<Vector2> OnPointerUp;     // coordinates of the release
    event Action<Vector2> OnTap;           // down+up without exceeding the drag threshold
}
```

`Vector2` is in screen coordinates (pixels). A subscriber must unsubscribe when releasing resources ([event-subscriptions.md](../Architecture/event-subscriptions.md)).

---

## 5. Requirements

### REQ-INP-001: Per-frame pointer polling

**Description:** The system polls the input device every frame.

**Behavior:**
1. Implements `ITickable.Tick()` (registered as an entry point in `CoreScope`).
2. Reads the pointer position and pressed state (`UnityEngine.Input`: mouse and/or the first touch `Input.GetTouch(0)`).
3. Normalizes coordinates to screen space (`Vector2`).

---

### REQ-INP-002: Pointer phases

**Description:** Emit press, move, and release events.

**Behavior:**
1. On the frame the press begins — `OnPointerDown(pos)`.
2. While the pointer is held and has moved since the previous frame — `OnPointerMove(pos)`.
3. On the frame of release — `OnPointerUp(pos)`.

---

### REQ-INP-003: Tap classification

**Description:** Distinguish a "tap" from a drag.

**Behavior:**
1. Remember the `OnPointerDown` position.
2. On release: if the total displacement from the press point **did not exceed** `RuntimeConstants.Input.DragThresholdPixels` — additionally raise `OnTap(pos)`.
3. Otherwise the event is treated as the end of a drag (only `OnPointerUp`).

---

### REQ-INP-004: Independence from domain and views

**Description:** The system contains no knowledge of the game or rendering.

**Behavior:**
1. No references to `IGameSystem`, domain types, views, or card colliders.
2. Only screen coordinates and pointer phases. The consumer performs the hit-test.

---

## 6. Internal structure

### File structure

```
Input/
├── InputSystem.cs     // IInputSystem + InputSystem (ITickable, gesture classification)
└── PointerReader.cs   // worker (optional): read UnityEngine.Input -> position/pressed
```

### Components

| Component | Type | Responsibility |
|---|---|---|
| `InputSystem` | API class | Poll on `Tick`, classify phases/tap, publish events |
| `PointerReader` | Worker (opt.) | Isolate reading of raw `UnityEngine.Input` (I/O service logic) |

No `MonoBehaviour` is used — per-frame behavior comes from `ITickable` (see [unity-rules.md](../Architecture/unity-rules.md) §2, §6).

---

## 7. Dependencies

### Packages

| Package | Purpose |
|---|---|
| `jp.hadashikick.vcontainer` | `ITickable` for per-frame polling |
| UnityEngine (input module, legacy) | `Input`, `Vector2` (the manifest has no Input System package) |

### Dependencies on other systems

None.

### Constants

`RuntimeConstants.Input`: `DragThresholdPixels` — the displacement threshold for distinguishing a tap from a drag.

---

## 8. Implementation checklist

- [ ] `IInputSystem` + `InputSystem : ITickable`, registered as an entry point in `CoreScope`.
- [ ] Read the pointer (mouse/touch) and normalize to screen coordinates (REQ-INP-001).
- [ ] `OnPointerDown`/`OnPointerMove`/`OnPointerUp` events (REQ-INP-002).
- [ ] `OnTap` classification by `DragThresholdPixels` (REQ-INP-003).
- [ ] Zero knowledge of the domain/views (REQ-INP-004).
- [ ] `RuntimeConstants.Input.DragThresholdPixels` constant.

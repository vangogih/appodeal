# Game Assets System (GameAssetsSystem)

> **Type:** System
> **API class:** `GameAssetsSystem`
> **Interface:** `IGameAssetsSystem`
> **References:** [00-overview.md](00-overview.md), [08-presentation-system.md](08-presentation-system.md) §7, [unity-rules.md](../Architecture/unity-rules.md) §5, [domain-responsibility.md](../Architecture/domain-responsibility.md)

---

## 1. Purpose

`GameAssetsSystem` is responsible for the **asynchronous loading and unloading** of a game's assets: card sprites (52 faces + back) and view prefabs (`CardView`, `PileView`, `BoardView`). Once loaded, the system acts as a **provider** of those assets for `PresentationSystem`.

This frees the presentation layer from loading: it only **uses** the already-loaded sprites/prefabs, while "where to get them and when to release them" is handled by a separate system.

Complex loading logic is placed in an `ILoadUnit` (`SolitaireAssetsLoadUnit`) and run through the infrastructure `LoadingService` — exactly like `FooLoadingUnit` in `BootstrapFlow`. The assets themselves are loaded through `AssetService` (the single point of asset access, see [domain-responsibility.md](../Architecture/domain-responsibility.md)).

---

## 2. Responsibility boundaries

### In scope:

| Responsibility | Description |
|---|---|
| Async loading | `LoadAsync` — preload sprites and prefabs via an `ILoadUnit` + `LoadingService` |
| Unloading | `UnloadAsync` — release references and unused assets |
| Sprite provider | `GetCardSprite(suit, rank)`, `GetCardBackSprite()` |
| Prefab provider | `CardViewPrefab`, `PileViewPrefab`, `BoardViewPrefab` (as `GameObject`) |

### Out of scope:

| Responsibility | Owner |
|---|---|
| Instantiating views from prefabs | `PresentationSystem` / `CardViewFactory` ([08-presentation-system.md](08-presentation-system.md)) |
| Assigning sprites to renderers | `PresentationSystem` (the views) |
| Direct `UnityEngine.Resources.Load` | Forbidden; go through `AssetService` ([domain-responsibility.md](../Architecture/domain-responsibility.md)) |
| Addressables / streaming / runtime-built atlases | Out of scope ([00-overview.md](00-overview.md) §9) |

---

## 3. Interaction

| Counterpart | Direction | Mechanism |
|---|---|---|
| `LoadingService` | down (infra) | `BeginLoading(SolitaireAssetsLoadUnit)` |
| `AssetService` | down (infra) | `AssetService.R.Load<T>(path)` inside the load unit |
| `PresentationSystem` | outward | `IGameAssetsSystem` via constructor |

```
CoreFlow.Start:
    await gameAssets.LoadAsync(ct)
        -> loadingService.BeginLoading(new SolitaireAssetsLoadUnit(assetService, cache))
            -> loads 52 sprites + back + 3 prefabs through AssetService
    ... then presentation.InitializeAsync() reads assets via GetCardSprite/...Prefab
```

---

## 4. Contract

```csharp
public interface IGameAssetsSystem
{
    UniTask LoadAsync(CancellationToken ct = default);
    UniTask UnloadAsync();

    Sprite GetCardSprite(Suit suit, Rank rank);
    Sprite GetCardBackSprite();

    GameObject CardViewPrefab { get; }
    GameObject PileViewPrefab { get; }
    GameObject BoardViewPrefab { get; }
}
```

Prefabs are handed out as `GameObject` (rather than view types) so that the assets system does not depend on `PresentationSystem` — the dependency graph stays acyclic.

---

## 5. Requirements

### REQ-AST-001: Async loading via a load unit

**Description:** `LoadAsync` preloads all assets needed for a game.

**Behavior:**
1. Create a `SolitaireAssetsLoadUnit` (implements `ILoadUnit`) and run it via `LoadingService.BeginLoading(unit)`.
2. Inside the unit, load through `AssetService`: 52 face sprites, 1 back sprite, the `CardView`/`PileView`/`BoardView` prefabs.
3. Store the loaded assets in the system's cache (e.g. `Dictionary<(Suit,Rank), Sprite>` + prefab fields).
4. The method is asynchronous (`UniTask`); split loading across frames if needed to avoid a hitch.
5. Honor the `CancellationToken`.

---

### REQ-AST-002: Asset provider

**Description:** Access to the loaded assets.

**Behavior:**
1. `GetCardSprite(suit, rank)` — the card face sprite from the cache.
2. `GetCardBackSprite()` — the back sprite.
3. `CardViewPrefab` / `PileViewPrefab` / `BoardViewPrefab` — the prefabs (`GameObject`).
4. Accessing them before `LoadAsync` completes is a programming error (may be logged via `Log`).

---

### REQ-AST-003: Unloading

**Description:** `UnloadAsync` releases assets when the `Core` scene ends.

**Behavior:**
1. Drop the cache references to sprites/prefabs.
2. Request unloading of unused assets (`Resources.UnloadUnusedAssets`) if needed.
3. If the load unit is an `IDisposableLoadUnit`, it ends up in `LoadingService.Disposables` and is released normally.

---

### REQ-AST-004: Single point of asset access

**Description:** Loading only through `AssetService`.

**Behavior:**
1. Inside the load unit, use `AssetService.R.Load<T>(path)`; direct `UnityEngine.Resources.Load` is forbidden ([domain-responsibility.md](../Architecture/domain-responsibility.md)).
2. Asset paths come from `RuntimeConstants.Assets`.

> Note: the current `AssetService.R.Load<T>` is synchronous; asynchrony is provided by wrapping it in an `ILoadUnit`/`UniTask`. A possible switch to `Resources.LoadAsync`/Addressables is out of the current scope.

---

## 6. Internal structure

### File structure

```
Assets/
├── GameAssetsSystem.cs          // IGameAssetsSystem + GameAssetsSystem (cache, provider, load orchestration)
└── SolitaireAssetsLoadUnit.cs   // worker/unit: ILoadUnit, loads sprites and prefabs via AssetService
```

### Components

| Component | Type | Responsibility |
|---|---|---|
| `GameAssetsSystem` | API class | Decides **what** to load/unload; the cache; the provider; running the load unit |
| `SolitaireAssetsLoadUnit` | Worker (`ILoadUnit`) | Asset-loading details via `AssetService` (I/O service logic) |

---

## 7. Dependencies

### Packages

| Package | Purpose |
|---|---|
| `com.cysharp.unitask` | `UniTask`, `CancellationToken` |
| UnityEngine (core) | `Sprite`, `GameObject` |

### Dependencies on other systems

| Dependency | Mechanism |
|---|---|
| `LoadingService` | via constructor (infrastructure, `Lifetime.Scoped`) |
| `AssetService` | the static access point `AssetService.R` |

Uses the domain enums `Suit`/`Rank` (see [01-game-system.md](01-game-system.md) §4). It does **not** depend on `PresentationSystem`.

### Constants

`RuntimeConstants.Assets`: paths/templates — e.g. `Cards.FacePathFormat` (`"Cards/{0}_{1}"`), `Cards.BackPath`, `Prefabs.CardView`, `Prefabs.PileView`, `Prefabs.BoardView`.

---

## 8. Implementation checklist

- [ ] `IGameAssetsSystem` + `GameAssetsSystem` with a sprite/prefab cache.
- [ ] `SolitaireAssetsLoadUnit : ILoadUnit`, run via `LoadingService` (REQ-AST-001).
- [ ] Provider `GetCardSprite`/`GetCardBackSprite`/`*Prefab` (REQ-AST-002).
- [ ] `UnloadAsync` releasing references (REQ-AST-003).
- [ ] Loading only through `AssetService`; paths in `RuntimeConstants.Assets` (REQ-AST-004).
- [ ] Registered in `CoreScope`; `CoreFlow` calls `await LoadAsync()` before initializing presentation.

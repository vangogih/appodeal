using System.Threading;
using Appodeal.Solitaire.Runtime.Core.Game;
using Appodeal.Solitaire.Runtime.Utilities;
using Appodeal.Solitaire.Runtime.Utilities.Logging;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Appodeal.Solitaire.Runtime.Core.Assets
{
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

    /// <summary>
    /// Loads and provides a game's assets (card sprites + view prefabs). Loading is delegated to a
    /// <see cref="SolitaireAssetsLoadUnit"/> run through the infrastructure <see cref="LoadingService"/>;
    /// the loaded assets live in a cache and are handed out to <c>PresentationSystem</c>. Prefabs are
    /// exposed as <see cref="GameObject"/> so this system does not depend on the presentation layer.
    /// </summary>
    public sealed class GameAssetsSystem : IGameAssetsSystem
    {
        private const string LogTag = "ASSETS";

        private readonly LoadingService _loadingService;
        private readonly SolitaireAssetCache _cache = new();

        private bool _loaded;

        public GameAssetsSystem(LoadingService loadingService)
        {
            _loadingService = loadingService;
        }

        public GameObject CardViewPrefab => _cache.CardViewPrefab;
        public GameObject PileViewPrefab => _cache.PileViewPrefab;
        public GameObject BoardViewPrefab => _cache.BoardViewPrefab;

        // REQ-AST-001
        public async UniTask LoadAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var unit = new SolitaireAssetsLoadUnit(_cache);
            await _loadingService.BeginLoading(unit);

            _loaded = true;
        }

        // REQ-AST-003
        public UniTask UnloadAsync()
        {
            _cache.Clear();
            _loaded = false;
            UnityEngine.Resources.UnloadUnusedAssets();
            return UniTask.CompletedTask;
        }

        // REQ-AST-002
        public Sprite GetCardSprite(Suit suit, Rank rank)
        {
            WarnIfNotLoaded();
            return _cache.Faces.TryGetValue((suit, rank), out var sprite) ? sprite : null;
        }

        public Sprite GetCardBackSprite()
        {
            WarnIfNotLoaded();
            return _cache.Back;
        }

        private void WarnIfNotLoaded()
        {
            if (!_loaded)
                Log.Loading.W(LogTag, "Accessing game assets before LoadAsync completed.");
        }
    }
}

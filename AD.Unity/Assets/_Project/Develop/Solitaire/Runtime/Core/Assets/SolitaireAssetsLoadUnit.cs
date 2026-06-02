using System.Collections.Generic;
using Appodeal.Solitaire.Runtime.Core.Game;
using Appodeal.Solitaire.Runtime.Utilities;
using Appodeal.Solitaire.Runtime.Utilities.Logging;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Appodeal.Solitaire.Runtime.Core.Assets
{
    /// <summary>
    /// Mutable cache shared between <see cref="GameAssetsSystem"/> (reads) and
    /// <see cref="SolitaireAssetsLoadUnit"/> (writes).
    /// </summary>
    internal sealed class SolitaireAssetCache
    {
        public readonly Dictionary<(Suit, Rank), Sprite> Faces = new();
        public Sprite Back;
        public GameObject CardViewPrefab;
        public GameObject PileViewPrefab;
        public GameObject BoardViewPrefab;

        public void Clear()
        {
            Faces.Clear();
            Back = null;
            CardViewPrefab = null;
            PileViewPrefab = null;
            BoardViewPrefab = null;
        }
    }

    /// <summary>
    /// Worker (<see cref="ILoadUnit"/>): loads all game assets (52 face sprites + back + 3 prefabs)
    /// through <see cref="AssetService"/> and stores them in the shared cache. Asset paths come from
    /// <see cref="RuntimeConstants.Game.Assets"/>. I/O service logic only.
    /// </summary>
    internal sealed class SolitaireAssetsLoadUnit : ILoadUnit
    {
        private const string LogTag = "ASSETS";

        private readonly SolitaireAssetCache _cache;

        public SolitaireAssetsLoadUnit(SolitaireAssetCache cache)
        {
            _cache = cache;
        }

        public async UniTask Load()
        {
            _cache.Clear();

            for (var suit = (Suit)0; (int)suit < RuntimeConstants.Game.Cards.SuitCount; suit++)
            {
                for (var rank = Rank.Ace; rank <= Rank.King; rank++)
                {
                    string path = string.Format(RuntimeConstants.Game.Assets.Cards.FacePathFormat, suit, rank);
                    var sprite = AssetService.R.Load<Sprite>(path);

                    if (sprite == null)
                        Log.Loading.W(LogTag, $"Missing card face sprite at '{path}'");

                    _cache.Faces[(suit, rank)] = sprite;
                }

                // Spread loading across frames to avoid a hitch on large sets.
                await UniTask.NextFrame();
            }

            _cache.Back = AssetService.R.Load<Sprite>(RuntimeConstants.Game.Assets.Cards.BackPath);
            if (_cache.Back == null)
                Log.Loading.W(LogTag, $"Missing card back sprite at '{RuntimeConstants.Game.Assets.Cards.BackPath}'");

            _cache.CardViewPrefab = LoadPrefab(RuntimeConstants.Game.Assets.Prefabs.CardView);
            _cache.PileViewPrefab = LoadPrefab(RuntimeConstants.Game.Assets.Prefabs.PileView);
            _cache.BoardViewPrefab = LoadPrefab(RuntimeConstants.Game.Assets.Prefabs.BoardView);
        }

        private static GameObject LoadPrefab(string path)
        {
            var prefab = AssetService.R.Load<GameObject>(path);
            if (prefab == null)
                Log.Loading.W(LogTag, $"Missing prefab at '{path}'");

            return prefab;
        }
    }
}

using System.Threading;
using Appodeal.Solitaire.Runtime.Assets;
using Appodeal.Solitaire.Runtime.Utilities.Logging;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Appodeal.Solitaire.Runtime.Presentation
{
    /// <summary>
    /// Factory (has a dependency, so registered logic rather than a static method): instantiates a
    /// <see cref="CardView"/> from the prefab provided by <see cref="IGameAssetsSystem"/> and runs its
    /// async initialization. Asynchronous by default per the Unity rules.
    /// </summary>
    public sealed class CardViewFactory
    {
        private const string LogTag = "PRESENTATION";

        private readonly IGameAssetsSystem _assets;

        public CardViewFactory(IGameAssetsSystem assets)
        {
            _assets = assets;
        }

        public async UniTask<CardView> CreateAsync(CardModel model, Transform parent, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var prefab = _assets.CardViewPrefab;
            if (prefab == null)
            {
                Log.Default.E(LogTag, "CardView prefab is not loaded; cannot create a card view.");
                return null;
            }

            var instance = Object.Instantiate(prefab, parent);
            var view = instance.GetComponent<CardView>();

            if (view == null)
            {
                Log.Default.E(LogTag, "CardView prefab has no CardView component.");
                Object.Destroy(instance);
                return null;
            }

            await view.InitializeAsync(model, ct);
            return view;
        }
    }
}

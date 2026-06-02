using System.Threading;
using Appodeal.Solitaire.Runtime.Assets;
using Appodeal.Solitaire.Runtime.Domain;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Appodeal.Solitaire.Runtime.Presentation
{
    /// <summary>
    /// Model of a single card: owns its <see cref="CardView"/> and depends only on the external
    /// <see cref="IGameAssetsSystem"/> (for the face/back sprites). Translates the card's
    /// <see cref="Card"/> data into the view's sprite, sorting and position. Manages the view lifecycle.
    /// </summary>
    public sealed class CardModel
    {
        private readonly IGameAssetsSystem _assets;

        private CardView _view;
        private Card _card;

        public CardModel(IGameAssetsSystem assets)
        {
            _assets = assets;
        }

        public CardView View => _view;

        public async UniTask InitializeAsync(CardViewFactory factory, Transform parent, CancellationToken ct = default)
        {
            _view = await factory.CreateAsync(this, parent, ct);
        }

        public void Apply(Card card, PileId pile, int indexInPile, Vector3 localPosition, int sortingOrder, bool animate)
        {
            _card = card;

            if (_view == null)
                return;

            _view.SetSlot(pile, indexInPile, card.FaceUp);
            _view.SetSortingOrder(sortingOrder);
            _view.SetSprite(ResolveSprite(card));
            _view.SetLocalPosition(localPosition, animate);
            _view.SetColliderEnabled(true);
        }

        public void Lift(Vector3 worldPosition, int sortingBoost)
        {
            if (_view == null)
                return;

            _view.SetSortingOrder(sortingBoost);
            _view.SetColliderEnabled(false);
            _view.SetWorldPosition(worldPosition);
        }

        public void MoveLiftedTo(Vector3 worldPosition)
        {
            if (_view != null)
                _view.SetWorldPosition(worldPosition);
        }

        private Sprite ResolveSprite(Card card)
        {
            return card.FaceUp ? _assets.GetCardSprite(card.Suit, card.Rank) : _assets.GetCardBackSprite();
        }

        public void Dispose()
        {
            if (_view != null)
            {
                Object.Destroy(_view.gameObject);
                _view = null;
            }
        }
    }
}

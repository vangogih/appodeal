using System.Threading;
using Appodeal.Solitaire.Runtime.Core.Assets;
using Appodeal.Solitaire.Runtime.Core.Domain;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Appodeal.Solitaire.Runtime.Core.Presentation
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
        private bool _hasCard;
        private bool _lastFaceUp;

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
            if (_view == null)
                return;

            _view.ResetScale();
            _view.SetSlot(pile, indexInPile, card.FaceUp);
            _view.SetSortingOrder(sortingOrder);

            bool faceChanged = _hasCard && _lastFaceUp != card.FaceUp;
            var sprite = ResolveSprite(card);

            // REQ-PRES-003.2: animate the flip when a card's face state changed.
            if (animate && faceChanged)
                _view.AnimateFlip(sprite);
            else
                _view.SetSprite(sprite);

            _view.SetLocalPosition(localPosition, animate);
            _view.SetColliderEnabled(true);

            _hasCard = true;
            _lastFaceUp = card.FaceUp;
        }

        public void Lift(Vector3 worldPosition, int sortingBoost)
        {
            if (_view == null)
                return;

            _view.SetSortingOrder(sortingBoost);
            _view.SetColliderEnabled(false);
            _view.SetLiftScale(RuntimeConstants.Game.Presentation.DragLiftScale);
            _view.SetWorldPosition(WithLiftZ(worldPosition));
        }

        public void MoveLiftedTo(Vector3 worldPosition)
        {
            if (_view != null)
                _view.SetWorldPosition(WithLiftZ(worldPosition));
        }

        private static Vector3 WithLiftZ(Vector3 worldPosition)
        {
            worldPosition.z = RuntimeConstants.Game.Presentation.DragLiftZ;
            return worldPosition;
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

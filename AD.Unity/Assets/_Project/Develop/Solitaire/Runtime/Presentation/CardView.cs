using System.Threading;
using Appodeal.Solitaire.Runtime.Domain;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace Appodeal.Solitaire.Runtime.Presentation
{
    /// <summary>
    /// View (display only) of a single card: a <see cref="SpriteRenderer"/> + <see cref="BoxCollider2D"/>.
    /// Holds no game logic; the owning <see cref="CardModel"/> drives sprite, sorting and position.
    /// Also stores the card's current pile/index so the presentation hit-test can build a move.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CardView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private BoxCollider2D _collider;

        private CardModel _model;
        private MotionHandle _moveHandle;

        public PileId Pile { get; private set; }
        public int IndexInPile { get; private set; }
        public bool FaceUp { get; private set; }
        public CardModel Model => _model;

        public UniTask InitializeAsync(CardModel model, CancellationToken ct = default)
        {
            _model = model;

            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();
            if (_collider == null)
                _collider = GetComponent<BoxCollider2D>();

            return UniTask.CompletedTask;
        }

        public void SetSlot(PileId pile, int indexInPile, bool faceUp)
        {
            Pile = pile;
            IndexInPile = indexInPile;
            FaceUp = faceUp;
        }

        public void SetSprite(Sprite sprite)
        {
            if (_renderer != null)
                _renderer.sprite = sprite;
        }

        public void SetSortingOrder(int order)
        {
            if (_renderer != null)
                _renderer.sortingOrder = order;
        }

        public void SetLocalPosition(Vector3 localPosition, bool animate)
        {
            if (_moveHandle.IsActive())
                _moveHandle.TryCancel();

            if (!animate)
            {
                transform.localPosition = localPosition;
                return;
            }

            _moveHandle = LMotion
                .Create(transform.localPosition, localPosition, RuntimeConstants.Presentation.MoveDuration)
                .WithEase(Ease.OutQuad)
                .BindToLocalPosition(transform)
                .AddTo(gameObject);
        }

        public void SetWorldPosition(Vector3 worldPosition)
        {
            if (_moveHandle.IsActive())
                _moveHandle.TryCancel();

            transform.position = worldPosition;
        }

        public void SetColliderEnabled(bool enabled)
        {
            if (_collider != null)
                _collider.enabled = enabled;
        }

        /// <summary>Factory method: no extra dependencies, so a static creator on the View itself.</summary>
        public static CardView Create(CardView prefab, Transform parent)
        {
            var view = Instantiate(prefab, parent);
            return view;
        }
    }
}

using Appodeal.Solitaire.Runtime.Core.Domain;
using Appodeal.Solitaire.Runtime.Core.Game;
using UnityEngine;

namespace Appodeal.Solitaire.Runtime.Core.Presentation
{
    /// <summary>
    /// View (display only) of a pile slot: a hit-test area (<see cref="BoxCollider2D"/>) plus its
    /// <see cref="PileId"/>. Used as the drop target during drag-and-drop.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class PileView : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D _collider;

        public PileId PileId { get; private set; }

        public void Setup(PileId pileId, Vector3 localPosition)
        {
            PileId = pileId;
            transform.localPosition = localPosition;
            name = $"Pile_{pileId.Kind}_{pileId.Index}";
            ConfigureHitArea(pileId.Kind);
        }

        // Tableau columns fan downward, so their drop zone must be tall enough to catch a release
        // anywhere along the column. Other piles are a single card-sized slot at the anchor.
        private void ConfigureHitArea(PileKind kind)
        {
            if (_collider == null)
                _collider = GetComponent<BoxCollider2D>();
            if (_collider == null)
                return;

            if (kind == PileKind.Tableau)
            {
                _collider.size = new Vector2(1.0f, 6.0f);
                _collider.offset = new Vector2(0f, -2.3f);
            }
            else
            {
                _collider.size = new Vector2(1.0f, 1.4f);
                _collider.offset = Vector2.zero;
            }
        }

        /// <summary>Factory method: no extra dependencies, so a static creator on the View itself.</summary>
        public static PileView Create(PileView prefab, Transform parent)
        {
            var view = Instantiate(prefab, parent);
            return view;
        }
    }
}

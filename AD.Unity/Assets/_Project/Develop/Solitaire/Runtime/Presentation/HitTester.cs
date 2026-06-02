using UnityEngine;

namespace Appodeal.Solitaire.Runtime.Presentation
{
    /// <summary>
    /// Worker: maps a screen position to the top <see cref="CardView"/> or a <see cref="PileView"/>
    /// using the camera and 2D physics. Owns the view-geometry hit-test (input stays domain-agnostic).
    /// </summary>
    public sealed class HitTester
    {
        private static readonly RaycastHit2D[] Buffer = new RaycastHit2D[32];

        private readonly Camera _camera;

        public HitTester(Camera camera)
        {
            _camera = camera != null ? camera : Camera.main;
        }

        public CardView TopCardAt(Vector2 screenPosition)
        {
            var world = ToWorld(screenPosition);
            int count = Physics2D.RaycastNonAlloc(world, Vector2.zero, Buffer);

            CardView top = null;
            int topOrder = int.MinValue;

            for (int i = 0; i < count; i++)
            {
                var card = Buffer[i].collider.GetComponent<CardView>();
                if (card == null)
                    continue;

                var renderer = card.GetComponentInChildren<SpriteRenderer>();
                int order = renderer != null ? renderer.sortingOrder : 0;

                if (order >= topOrder)
                {
                    topOrder = order;
                    top = card;
                }
            }

            return top;
        }

        public PileView PileAt(Vector2 screenPosition)
        {
            var world = ToWorld(screenPosition);
            int count = Physics2D.RaycastNonAlloc(world, Vector2.zero, Buffer);

            for (int i = 0; i < count; i++)
            {
                var pile = Buffer[i].collider.GetComponent<PileView>();
                if (pile != null)
                    return pile;
            }

            return null;
        }

        public Vector3 ToWorld(Vector2 screenPosition)
        {
            var cam = _camera != null ? _camera : Camera.main;
            if (cam == null)
                return new Vector3(screenPosition.x, screenPosition.y, 0f);

            var world = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -cam.transform.position.z));
            world.z = 0f;
            return world;
        }
    }
}

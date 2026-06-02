using UnityEngine;

namespace Appodeal.Solitaire.Runtime.Core.Input
{
    /// <summary>
    /// Worker: isolates reading of raw <see cref="UnityEngine.Input"/> (mouse or the first touch)
    /// into a normalized screen position + pressed flag. I/O service logic only.
    /// </summary>
    internal sealed class PointerReader
    {
        public readonly struct PointerState
        {
            public readonly Vector2 Position;
            public readonly bool Pressed;

            public PointerState(Vector2 position, bool pressed)
            {
                Position = position;
                Pressed = pressed;
            }
        }

        public PointerState Read()
        {
            if (UnityEngine.Input.touchCount > 0)
            {
                var touch = UnityEngine.Input.GetTouch(0);
                bool pressed = touch.phase is TouchPhase.Began or TouchPhase.Moved or TouchPhase.Stationary;
                return new PointerState(touch.position, pressed);
            }

            return new PointerState(UnityEngine.Input.mousePosition, UnityEngine.Input.GetMouseButton(0));
        }
    }
}

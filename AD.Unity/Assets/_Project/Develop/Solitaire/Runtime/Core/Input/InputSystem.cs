using System;
using UnityEngine;
using VContainer.Unity;

namespace Appodeal.Solitaire.Runtime.Core.Input
{
    public interface IInputSystem
    {
        event Action<Vector2> OnPointerDown; // screen coordinates of the press
        event Action<Vector2> OnPointerMove; // only while the pointer is held
        event Action<Vector2> OnPointerUp;   // coordinates of the release
        event Action<Vector2> OnTap;         // down+up without exceeding the drag threshold
    }

    /// <summary>
    /// Input-device abstraction. Polls the pointer every frame via <see cref="ITickable"/> and
    /// publishes semantic pointer events in screen coordinates. Knows nothing about the domain,
    /// cards, piles or views; hit-testing belongs to the consumer.
    /// </summary>
    public sealed class InputSystem : IInputSystem, ITickable
    {
        private readonly PointerReader _reader = new();

        private bool _isDown;
        private Vector2 _downPosition;
        private Vector2 _lastPosition;

        public event Action<Vector2> OnPointerDown;
        public event Action<Vector2> OnPointerMove;
        public event Action<Vector2> OnPointerUp;
        public event Action<Vector2> OnTap;

        // REQ-INP-001 / REQ-INP-002 / REQ-INP-003
        public void Tick()
        {
            var pointer = _reader.Read();
            var pos = pointer.Position;

            if (pointer.Pressed && !_isDown)
            {
                _isDown = true;
                _downPosition = pos;
                _lastPosition = pos;
                OnPointerDown?.Invoke(pos);
            }
            else if (pointer.Pressed && _isDown)
            {
                if (pos != _lastPosition)
                {
                    _lastPosition = pos;
                    OnPointerMove?.Invoke(pos);
                }
            }
            else if (!pointer.Pressed && _isDown)
            {
                _isDown = false;
                OnPointerUp?.Invoke(pos);

                float displacement = Vector2.Distance(_downPosition, pos);
                if (displacement <= RuntimeConstants.Game.Input.DragThresholdPixels)
                    OnTap?.Invoke(pos);
            }
        }
    }
}

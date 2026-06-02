using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LightSide;
using UnityEngine;
using UnityEngine.UI;

namespace Appodeal.Solitaire.Runtime.Core.Presentation
{
    /// <summary>
    /// View (display only) of the board container: holds the parent transforms for piles/cards and a
    /// screen-space uGUI overlay (Undo / Redo / New Game buttons and a win banner). Exposes button
    /// clicks as events; the owning <see cref="BoardModel"/> wires them to game commands.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] private Transform _pilesRoot;
        [SerializeField] private Transform _cardsRoot;

        [Header("Controls (uGUI overlay)")]
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _redoButton;
        [SerializeField] private Button _newGameButton;

        [Header("Win banner")]
        [SerializeField] private GameObject _winBanner;
        [SerializeField] private UniText _winLabel;
        [SerializeField] private Button _winNewGameButton;

        public event Action OnUndoClicked;
        public event Action OnRedoClicked;
        public event Action OnNewGameClicked;

        public Transform PilesRoot => _pilesRoot != null ? _pilesRoot : transform;
        public Transform CardsRoot => _cardsRoot != null ? _cardsRoot : transform;

        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            if (_undoButton != null) _undoButton.onClick.AddListener(() => OnUndoClicked?.Invoke());
            if (_redoButton != null) _redoButton.onClick.AddListener(() => OnRedoClicked?.Invoke());
            if (_newGameButton != null) _newGameButton.onClick.AddListener(() => OnNewGameClicked?.Invoke());
            if (_winNewGameButton != null) _winNewGameButton.onClick.AddListener(() => OnNewGameClicked?.Invoke());

            HideWin();
            return UniTask.CompletedTask;
        }

        public void SetUndoInteractable(bool value)
        {
            if (_undoButton != null) _undoButton.interactable = value;
        }

        public void SetRedoInteractable(bool value)
        {
            if (_redoButton != null) _redoButton.interactable = value;
        }

        public void ShowWin()
        {
            if (_winLabel != null) _winLabel.Text = "You win!";
            if (_winBanner != null) _winBanner.SetActive(true);
        }

        public void HideWin()
        {
            if (_winBanner != null) _winBanner.SetActive(false);
        }

        public void Teardown()
        {
            if (_undoButton != null) _undoButton.onClick.RemoveAllListeners();
            if (_redoButton != null) _redoButton.onClick.RemoveAllListeners();
            if (_newGameButton != null) _newGameButton.onClick.RemoveAllListeners();
            if (_winNewGameButton != null) _winNewGameButton.onClick.RemoveAllListeners();
        }
    }
}

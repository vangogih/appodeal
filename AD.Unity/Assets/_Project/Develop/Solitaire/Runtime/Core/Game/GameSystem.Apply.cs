using System.Collections.Generic;
using Appodeal.Solitaire.Runtime.Core.Domain;

namespace Appodeal.Solitaire.Runtime.Core.Game
{
    // Private board mutation, apply/revert of changes and snapshot building.
    // Service logic that backs the business decisions taken in GameSystem.cs.
    public sealed partial class GameSystem
    {
        private List<Card> GetMutablePile(PileId id)
        {
            return id.Kind switch
            {
                PileKind.Stock => _stock,
                PileKind.Waste => _waste,
                PileKind.Foundation => _foundations[id.Index],
                PileKind.Tableau => _tableau[id.Index],
                _ => null
            };
        }

        private void LoadFrom(BoardState state)
        {
            _stock.Clear();
            _stock.AddRange(state.Stock);

            _waste.Clear();
            _waste.AddRange(state.Waste);

            for (int i = 0; i < _foundations.Length; i++)
            {
                _foundations[i].Clear();
                _foundations[i].AddRange(state.Foundations[i]);
            }

            for (int i = 0; i < _tableau.Length; i++)
            {
                _tableau[i].Clear();
                _tableau[i].AddRange(state.Tableau[i]);
            }
        }

        // Move the top `count` cards of `from` onto `to`, preserving their order.
        private void MoveCards(PileId from, PileId to, int count)
        {
            var source = GetMutablePile(from);
            var target = GetMutablePile(to);

            int start = source.Count - count;
            for (int i = start; i < source.Count; i++)
                target.Add(source[i]);

            source.RemoveRange(start, count);
        }

        // REQ-GAME-005: flip the new top of a tableau column face-up. Returns whether a flip happened.
        private bool AutoFlip(PileId from)
        {
            if (from.Kind != PileKind.Tableau)
                return false;

            var column = _tableau[from.Index];
            if (column.Count == 0)
                return false;

            var top = column[^1];
            if (top.FaceUp)
                return false;

            column[^1] = top.AsFaceUp(true);
            return true;
        }

        private void ApplyDraw()
        {
            var card = _stock[^1];
            _stock.RemoveAt(_stock.Count - 1);
            _waste.Add(card.AsFaceUp(true));
        }

        private void RevertDraw()
        {
            var card = _waste[^1];
            _waste.RemoveAt(_waste.Count - 1);
            _stock.Add(card.AsFaceUp(false));
        }

        // Entire waste back to the stock, face-down, in reverse order.
        private void ApplyRecycle()
        {
            for (int i = _waste.Count - 1; i >= 0; i--)
                _stock.Add(_waste[i].AsFaceUp(false));

            _waste.Clear();
        }

        // Inverse of ApplyRecycle: entire stock back to the waste, face-up, in reverse order.
        private void RevertRecycle()
        {
            for (int i = _stock.Count - 1; i >= 0; i--)
                _waste.Add(_stock[i].AsFaceUp(true));

            _stock.Clear();
        }

        private void ApplyChange(BoardChange change)
        {
            switch (change.Kind)
            {
                case BoardChangeKind.Move:
                    MoveCards(change.From, change.To, change.Count);
                    if (change.FlippedSourceTop)
                        ForceFlipTop(change.From, faceUp: true);
                    break;
                case BoardChangeKind.DrawStock:
                    ApplyDraw();
                    break;
                case BoardChangeKind.RecycleStock:
                    ApplyRecycle();
                    break;
            }
        }

        private void RevertChange(BoardChange change)
        {
            switch (change.Kind)
            {
                case BoardChangeKind.Move:
                    // Undo the auto-flip first (the flipped card is currently on top of the source),
                    // then return the moved cards on top of it.
                    if (change.FlippedSourceTop)
                        ForceFlipTop(change.From, faceUp: false);
                    MoveCards(change.To, change.From, change.Count);
                    break;
                case BoardChangeKind.DrawStock:
                    RevertDraw();
                    break;
                case BoardChangeKind.RecycleStock:
                    RevertRecycle();
                    break;
            }
        }

        private void ForceFlipTop(PileId id, bool faceUp)
        {
            var pile = GetMutablePile(id);
            if (pile.Count == 0)
                return;

            pile[^1] = pile[^1].AsFaceUp(faceUp);
        }

        private BoardState BuildSnapshot()
        {
            var foundations = new IReadOnlyList<Card>[_foundations.Length];
            for (int i = 0; i < _foundations.Length; i++)
                foundations[i] = new List<Card>(_foundations[i]);

            var tableau = new IReadOnlyList<Card>[_tableau.Length];
            for (int i = 0; i < _tableau.Length; i++)
                tableau[i] = new List<Card>(_tableau[i]);

            return new BoardState(
                new List<Card>(_stock),
                new List<Card>(_waste),
                foundations,
                tableau);
        }
    }
}

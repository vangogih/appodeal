using Appodeal.Solitaire.Runtime;
using Appodeal.Solitaire.Runtime.Domain;
using Appodeal.Solitaire.Runtime.Game;
using Appodeal.Solitaire.Runtime.Game.Rules;
using Appodeal.Solitaire.Runtime.Undo;
using NUnit.Framework;

namespace Appodeal.Solitaire.Tests.EditMode
{
    public sealed class GameTests
    {
        private static GameSystem NewGameSystem() => new(new UndoSystem());

        [Test]
        public void StartNewGame_DealsClearsHistoryAndRaisesBoardChanged()
        {
            var game = NewGameSystem();
            int changedCount = 0;
            game.OnBoardChanged += _ => changedCount++;

            game.StartNewGame(seed: 10);

            Assert.AreEqual(1, changedCount);
            Assert.AreEqual(24, game.State.Stock.Count);
            Assert.AreEqual(0, game.State.Waste.Count);
            Assert.AreEqual(RuntimeConstants.Game.Board.TableauColumns, game.State.Tableau.Count);
            for (int i = 0; i < game.State.Tableau.Count; i++)
                Assert.AreEqual(i + 1, game.State.Tableau[i].Count);
            Assert.IsFalse(game.CanUndo);
            Assert.IsFalse(game.CanRedo);
        }

        [Test]
        public void DrawStock_MovesOneCardFaceUpToWaste()
        {
            var game = NewGameSystem();
            game.StartNewGame(seed: 5);

            bool drawn = game.DrawStock();

            Assert.IsTrue(drawn);
            Assert.AreEqual(23, game.State.Stock.Count);
            Assert.AreEqual(1, game.State.Waste.Count);
            Assert.IsTrue(game.State.Waste[^1].FaceUp);
            Assert.IsTrue(game.CanUndo);
        }

        [Test]
        public void DrawStock_RecyclesWasteWhenStockEmpty()
        {
            var game = NewGameSystem();
            game.StartNewGame(seed: 5);

            for (int i = 0; i < 24; i++)
                Assert.IsTrue(game.DrawStock());

            Assert.AreEqual(0, game.State.Stock.Count);
            Assert.AreEqual(24, game.State.Waste.Count);

            bool recycled = game.DrawStock();

            Assert.IsTrue(recycled);
            Assert.AreEqual(24, game.State.Stock.Count);
            Assert.AreEqual(0, game.State.Waste.Count);
            foreach (var card in game.State.Stock)
                Assert.IsFalse(card.FaceUp, "Recycled stock must be face-down");
        }

        [Test]
        public void Undo_Redo_DrawStock_RoundTripsBoardState()
        {
            var game = NewGameSystem();
            game.StartNewGame(seed: 5);
            string initial = BoardStateBuilder.Signature(game.State);

            game.DrawStock();
            string afterDraw = BoardStateBuilder.Signature(game.State);
            Assert.AreNotEqual(initial, afterDraw);

            Assert.IsTrue(game.Undo());
            Assert.AreEqual(initial, BoardStateBuilder.Signature(game.State));

            Assert.IsTrue(game.Redo());
            Assert.AreEqual(afterDraw, BoardStateBuilder.Signature(game.State));
        }

        [Test]
        public void TryMove_FromStock_ReturnsFalseAndLeavesStateUnchanged()
        {
            var game = NewGameSystem();
            game.StartNewGame(seed: 5);
            string before = BoardStateBuilder.Signature(game.State);

            bool moved = game.TryMove(PileId.Stock, PileId.Tableau(0), 1);

            Assert.IsFalse(moved);
            Assert.AreEqual(before, BoardStateBuilder.Signature(game.State));
        }

        [Test]
        public void TryMove_ValidTableauMove_AutoFlipsRevealedCard_AndUndoRestores()
        {
            var rules = new RulesSubSystem();

            for (int seed = 0; seed < 400; seed++)
            {
                var game = NewGameSystem();
                game.StartNewGame(seed);
                var state = game.State;

                if (!TryFindTableauMoveWithHiddenCard(rules, state, out int from, out int to))
                    continue;

                string before = BoardStateBuilder.Signature(state);
                bool moved = game.TryMove(PileId.Tableau(from), PileId.Tableau(to), 1);

                Assert.IsTrue(moved, $"Seed {seed}: expected the discovered move to be valid");
                Assert.IsTrue(game.State.Tableau[from][^1].FaceUp,
                    $"Seed {seed}: the revealed source top must be auto-flipped face-up");

                Assert.IsTrue(game.Undo());
                Assert.AreEqual(before, BoardStateBuilder.Signature(game.State),
                    $"Seed {seed}: undo must fully restore the board (including the re-hidden card)");
                return;
            }

            Assert.Inconclusive("No deal with a single-card tableau move over a hidden card was found.");
        }

        [Test]
        public void OnGameWon_DoesNotFireOnFreshDealOrAfterFirstDraw()
        {
            var game = NewGameSystem();
            bool won = false;
            game.OnGameWon += () => won = true;

            game.StartNewGame(seed: 3);
            Assert.IsFalse(won);

            game.DrawStock();
            Assert.IsFalse(won);
        }

        // Finds a column whose face-up top can legally move onto another column's top, where the
        // source column has a face-down card underneath (so the move triggers an auto-flip).
        private static bool TryFindTableauMoveWithHiddenCard(RulesSubSystem rules, BoardState state, out int from, out int to)
        {
            for (from = 0; from < state.Tableau.Count; from++)
            {
                var column = state.Tableau[from];
                if (column.Count < 2 || column[^2].FaceUp)
                    continue;

                for (to = 0; to < state.Tableau.Count; to++)
                {
                    if (to == from)
                        continue;

                    if (rules.Validate(state, new Move(PileId.Tableau(from), PileId.Tableau(to), 1)))
                        return true;
                }
            }

            from = -1;
            to = -1;
            return false;
        }
    }
}

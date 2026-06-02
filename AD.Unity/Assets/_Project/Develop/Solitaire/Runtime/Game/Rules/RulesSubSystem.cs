using Appodeal.Solitaire.Runtime.Domain;

namespace Appodeal.Solitaire.Runtime.Game.Rules
{
    public interface IRulesSubSystem
    {
        bool Validate(BoardState state, Move move);
        bool IsWin(BoardState state);
    }

    /// <summary>
    /// Subsystem of <c>GameSystem</c>: keeper of Klondike rules. Decides what to validate and
    /// combines the result; the algorithm details live in the <see cref="MoveValidator"/> and
    /// <see cref="WinDetector"/> workers. Pure predicate over <see cref="BoardState"/>.
    /// </summary>
    public sealed class RulesSubSystem : IRulesSubSystem
    {
        private readonly MoveValidator _moveValidator = new();
        private readonly WinDetector _winDetector = new();

        public bool Validate(BoardState state, Move move) => _moveValidator.Validate(state, move);

        public bool IsWin(BoardState state) => _winDetector.IsWin(state);
    }
}

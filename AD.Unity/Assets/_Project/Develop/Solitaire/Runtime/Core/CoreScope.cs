using Appodeal.Solitaire.Runtime.Core.Assets;
using Appodeal.Solitaire.Runtime.Core.Game;
using Appodeal.Solitaire.Runtime.Core.Input;
using Appodeal.Solitaire.Runtime.Core.Layout;
using Appodeal.Solitaire.Runtime.Core.Presentation;
using VContainer;
using VContainer.Unity;

namespace Appodeal.Solitaire.Runtime.Core
{
    public sealed class CoreScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // Game domain (pure logic). GameSystem creates its Deal/Rules/Undo subsystems internally.
            builder.Register<IGameSystem, GameSystem>(Lifetime.Singleton);

            // Unity-side systems.
            builder.Register<ILayoutSystem, LayoutSystem>(Lifetime.Singleton);
            builder.Register<IGameAssetsSystem, GameAssetsSystem>(Lifetime.Singleton);
            builder.Register<IPresentationSystem, PresentationSystem>(Lifetime.Singleton);

            // InputSystem is an ITickable entry point; AsImplementedInterfaces (inside
            // RegisterEntryPoint) also exposes it as IInputSystem for injection.
            builder.RegisterEntryPoint<InputSystem>(Lifetime.Singleton);

            builder.RegisterEntryPoint<CoreFlow>();
        }
    }
}

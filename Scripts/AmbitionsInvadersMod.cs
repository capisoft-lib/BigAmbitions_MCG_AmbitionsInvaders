using System;
using System.Threading.Tasks;
using BAModAPI;
using Capisoft.Lib.BaComputerGames;

[assembly: RegisterModClass(typeof(AmbitionsInvaders.AmbitionsInvadersMod))]

namespace AmbitionsInvaders
{
    [ModEntryOnCityLoad]
    public sealed class AmbitionsInvadersMod : IModBigAmbitions
    {
        // Keep the registered type loadable before Mono resolves the separate MCG Workshop assembly.
        private IDisposable _registration;
        public string[] RelativeAssetBundlePaths => Array.Empty<string>();

        public Task OnLoadAsync(ModContext context)
        {
            _registration?.Dispose();
            _registration = AmbitionsInvadersRegistration.Register(context);
            return Task.CompletedTask;
        }

        public Task OnUnloadAsync()
        {
            _registration?.Dispose();
            _registration = null;
            return Task.CompletedTask;
        }
    }

    internal static class AmbitionsInvadersRegistration
    {
        internal static IDisposable Register(ModContext context)
        {
            var definition = ComputerGameDefinition.Create<AmbitionsInvadersGame>(
                "capisoft:ambitions-invaders", "Ambitions Invaders", "Pilot your banknote and blast waves of rival tycoons.",
                version: "1.0.1", loader: new InvadersLoader(), descriptionKey: "invaders_description", ruleset: "invaders-standard-v1")
                .WithNativeRetroEffects(false);
            return ComputerGames.Register(context.ModId, context.ModRootPath, definition);
        }
    }
}

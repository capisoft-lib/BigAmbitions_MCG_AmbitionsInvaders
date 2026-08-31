using BAModAPI;
using Capisoft.Lib.BaComputerGames;

[assembly: RegisterModClass(typeof(AmbitionsInvaders.AmbitionsInvadersMod))]

namespace AmbitionsInvaders
{
    [ModEntryOnCityLoad]
    public sealed class AmbitionsInvadersMod : ComputerGameMod<AmbitionsInvadersGame>
    {
        protected override ComputerGameDefinition Definition => ComputerGameDefinition.Create<AmbitionsInvadersGame>(
            "capisoft:ambitions-invaders", "Ambitions Invaders", "Pilot your banknote and blast waves of rival tycoons.",
            version: "0.1.0", loader: new InvadersLoader(), descriptionKey: "invaders_description", ruleset: "invaders-standard-v1")
            .WithNativeRetroEffects(false);
    }
}

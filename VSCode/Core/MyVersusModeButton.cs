using System.Collections.Generic;
using Monocle;
using TowerFall;
using TFModFortRiseTournament.Tournament;
using MonocleEntity = Monocle.Entity;

namespace TFModFortRiseTournament
{
    /// <summary>
    /// Point d'entrée du mode tournoi (déclenché par le bouton TOURNOI du menu).
    /// </summary>
    internal class MyVersusModeButton
    {
        // Plus aucun hook nécessaire : les écrans de tournoi sont de vraies scènes.
        internal static void Load()
        {
        }

        internal static void Unload()
        {
        }

        /// <summary>
        /// Charge les joueurs du tournoi.
        ///
        /// On n'interdit plus l'entrée quand la liste est trop courte : l'écran de
        /// sélection permet désormais d'ajouter des joueurs au clavier virtuel (Y),
        /// et le refuser ici enfermerait l'utilisateur — un fichier absent ou
        /// incomplet ne laissait aucun moyen de le remplir depuis le jeu.
        /// La vérification du nombre reste faite au moment de lancer le tournoi.
        /// </summary>
        internal static List<string> TryGetTournamentPlayers()
        {
            var playerNames = TournamentPlayerManager.LoadPlayerNames() ?? new List<string>();

            if (playerNames.Count < TournamentPlayerManager.GetMinimumPlayerCount())
            {
                Logger.Info($"Only {playerNames.Count} player(s) in tournament_players.json "
                    + $"(minimum {TournamentPlayerManager.GetMinimumPlayerCount()}) - "
                    + "ouverture de l'ecran de selection pour en ajouter");
            }

            return playerNames;
        }

        /// <summary>
        /// Première page à afficher : reprise si une sauvegarde existe, sinon sélection
        /// des joueurs.
        /// </summary>
        internal static MonocleEntity CreateFirstTournamentPage()
        {
            // Le mode de jeu vient du versus normal et peut etre celui d'un mod
            // (Playtag...) : on repart du dernier survivant des l'entree dans le
            // tournoi, pour que les ecrans de reglages montrent deja le bon mode.
            TournamentMatchLauncher.ResetGameMode();

            if (TournamentSave.Exists())
            {
                var saved = TournamentSave.Load();
                if (saved != null)
                    return new TournamentResumeScene(saved);
            }

            var roster = TournamentPlayerManager.LoadPlayerNames();
            return new TournamentPlayerSelectionScene(roster);
        }

        /// <summary>
        /// Ouvre le tournoi directement (sans animation de transition du menu).
        /// </summary>
        internal static void OpenTournamentMode()
        {
            Sounds.ui_click.Play(160f, 1f);
            Engine.Instance.Scene = new TournamentScene(CreateFirstTournamentPage());
        }
    }
}

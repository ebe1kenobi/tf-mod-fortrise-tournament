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
        /// Charge les joueurs du tournoi. Retourne null (et joue un son d'erreur) s'il
        /// n'y en a pas assez.
        /// </summary>
        internal static List<string> TryGetTournamentPlayers()
        {
            var playerNames = TournamentPlayerManager.LoadPlayerNames();

            if (playerNames == null || playerNames.Count < TournamentPlayerManager.GetMinimumPlayerCount())
            {
                Logger.Info($"Not enough players in tournament_players.json (need at least {TournamentPlayerManager.GetMinimumPlayerCount()})");
                Sounds.ui_invalid.Play(160f, 1f);
                return null;
            }

            return playerNames;
        }

        /// <summary>
        /// Première page à afficher : reprise si une sauvegarde existe, sinon sélection
        /// des joueurs.
        /// </summary>
        internal static MonocleEntity CreateFirstTournamentPage()
        {
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
            // S'il n'y a pas de sauvegarde à reprendre, il faut assez de joueurs.
            if (!TournamentSave.Exists())
            {
                var players = TryGetTournamentPlayers();
                if (players == null)
                    return;
            }

            Sounds.ui_click.Play(160f, 1f);
            Engine.Instance.Scene = new TournamentScene(CreateFirstTournamentPage());
        }
    }
}

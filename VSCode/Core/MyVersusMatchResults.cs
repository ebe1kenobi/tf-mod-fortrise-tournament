using System;
using Monocle;
using TowerFall;
using TFModFortRiseTournament.Tournament;

namespace TFModFortRiseTournament
{
  /// <summary>
  /// Intercepte la fin de match versus. Quand un tournoi est actif ET que le match est
  /// terminé (il y a un gagnant), on court-circuite AVANT que le jeu ne construise son
  /// propre écran de résultats (VersusMatchResults), on enregistre le résultat dans le
  /// bracket, puis on affiche l'écran de résultats du tournoi.
  /// </summary>
  public static class MyVersusMatchResults
  {
    public static void Load()
    {
      On.TowerFall.Session.CreateResults += CreateResults_patch;
      Logger.Info("MyVersusMatchResults hooks initialized");
    }

    public static void Unload()
    {
      On.TowerFall.Session.CreateResults -= CreateResults_patch;
    }

    private static void CreateResults_patch(
      On.TowerFall.Session.orig_CreateResults orig,
      Session self)
    {
      bool tournamentActive = TournamentSession.Current != null && TournamentSession.Current.IsActive;

      // Pas de tournoi, ou fin de round sans gagnant (le match continue) :
      // comportement normal du jeu.
      if (!tournamentActive || self.GetWinner() == -1)
      {
        orig(self);
        return;
      }

      int winnerIndex = self.GetWinner();
      string winnerName = GetWinnerName(winnerIndex, self);

      Logger.Info($"Tournament match finished. Winner: Player {winnerIndex} ({winnerName})");

      // Enregistrer le résultat (fait avancer le bracket).
      TournamentSession.Current.RecordMatchResult(winnerName);

      // Basculer vers l'écran de résultats du tournoi (le changement de scène est
      // différé par Monocle : la frame courante du Level se termine proprement).
      Engine.Instance.Scene = new TournamentScene(new TournamentMatchResultsScene(winnerName));
    }

    private static string GetWinnerName(int winnerIndex, Session session)
    {
      for (int i = 0; i < 4; i++)
      {
        if (TFGame.Players[i] && session.GetScoreIndex(i) == winnerIndex)
        {
          if (CustomNameImport.GetPlayerName != null)
          {
            try
            {
              string customName = CustomNameImport.GetPlayerName(i);
              if (!string.IsNullOrEmpty(customName))
                return customName;
            }
            catch (Exception ex)
            {
              Logger.Info($"Error getting custom name for player {i}: {ex.Message}");
            }
          }

          return $"Player {i + 1}";
        }
      }

      return "Unknown";
    }
  }
}

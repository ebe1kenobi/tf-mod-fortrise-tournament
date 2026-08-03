using System;
using FortRise;
using HarmonyLib;
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
  public class MyVersusMatchResults : IHookable
  {
    public static void Load(IHarmony harmony)
    {
      // CreateResults est privee : patch par nom. Prefix rendant false pour
      // remplacer entierement l'ecran de resultats vanilla quand un tournoi est actif.
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Session), "CreateResults"),
          prefix: new HarmonyMethod(CreateResults_patch)
      );
      Logger.Info("MyVersusMatchResults hooks initialized");
    }

    private static bool CreateResults_patch(Session __instance)
    {
      Session self = __instance;

      bool tournamentActive = TournamentSession.Current != null && TournamentSession.Current.IsActive;

      // Pas de tournoi, ou fin de round sans gagnant (le match continue) :
      // comportement normal du jeu.
      if (!tournamentActive || self.GetWinner() == -1)
      {
        return true;
      }

      int winnerIndex = self.GetWinner();
      string winnerName = GetWinnerName(winnerIndex, self);

      Logger.Info($"Tournament match finished. Winner: Player {winnerIndex} ({winnerName})");

      // Enregistrer le résultat (fait avancer le bracket).
      TournamentSession.Current.RecordMatchResult(winnerName);

      // Basculer vers l'écran de résultats du tournoi (le changement de scène est
      // différé par Monocle : la frame courante du Level se termine proprement).
      Engine.Instance.Scene = new TournamentScene(new TournamentMatchResultsScene(winnerName));

      return false; // l'ecran de resultats vanilla est remplace
    }

    private static string GetWinnerName(int winnerIndex, Session session)
    {
      for (int i = 0; i < 4; i++)
      {
        if (TFGame.Players[i] && session.GetScoreIndex(i) == winnerIndex)
        {
          // CustomNameImport.GetPlayerName gere lui-meme le repli ("P1".."P8") ;
          // IsAvailable distingue un vrai nom custom d'un repli.
          if (CustomNameImport.IsAvailable)
          {
            string customName = CustomNameImport.GetPlayerName(i);
            if (!string.IsNullOrEmpty(customName))
              return customName;
          }

          return $"Player {i + 1}";
        }
      }

      return "Unknown";
    }
  }
}

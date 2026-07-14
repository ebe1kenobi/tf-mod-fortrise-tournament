using System;
using System.Collections.Generic;
using Monocle;
using TowerFall;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Gestionnaire singleton pour l'état actuel du tournoi FFA
  /// </summary>
  public class TournamentSession
  {
    private static TournamentSession current;

    /// <summary>
    /// Instance actuelle du tournoi (null si aucun tournoi actif)
    /// </summary>
    public static TournamentSession Current
    {
      get { return current; }
      private set { current = value; }
    }

    /// <summary>
    /// Données du tournoi en cours
    /// </summary>
    public TournamentData Data { get; private set; }

    /// <summary>
    /// Indique si un tournoi est actuellement actif
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Scène actuelle du tournoi (pour la navigation)
    /// </summary>
    public Scene CurrentScene { get; set; }

    private TournamentSession(TournamentData data)
    {
      Data = data;
      IsActive = true;
      Logger.Info("TournamentSession created");
    }

    /// <summary>
    /// Démarre un nouveau tournoi
    /// </summary>
    /// <param name="data">Données du tournoi configuré</param>
    public static void StartTournament(TournamentData data)
    {
      if (data == null)
      {
        Logger.Info("Cannot start tournament: data is null");
        return;
      }

      if (Current != null && Current.IsActive)
      {
        Logger.Info("Warning: Starting new tournament while one is already active");
        EndTournament();
      }

      Current = new TournamentSession(data);
      Logger.Info($"Tournament started with {data.GetPlayerCount()} players in {data.GetFormatDisplay()} format");
      Logger.Info($"Total matches: {data.TotalMatches}, Total rounds: {data.TotalRounds}");

      // Sauvegarde pour permettre la reprise plus tard.
      TournamentSave.Save(data);
    }

    /// <summary>
    /// Termine le tournoi en cours
    /// </summary>
    public static void EndTournament()
    {
      if (Current != null)
      {
        Logger.Info("Tournament ended");
        Current.IsActive = false;
        Current = null;
      }

      // On quitte le tournoi : plus rien à reprendre.
      TournamentSave.Delete();
    }

    /// <summary>
    /// Retourne le match actuellement en cours
    /// </summary>
    public TournamentMatch GetCurrentMatch()
    {
      if (Data == null)
        return null;

      return Data.GetCurrentMatch();
    }

    /// <summary>
    /// Enregistre le résultat d'un match et met à jour le bracket
    /// </summary>
    /// <param name="winner">Nom du gagnant</param>
    public void RecordMatchResult(string winner)
    {
      if (Data == null || string.IsNullOrEmpty(winner))
      {
        Logger.Info("Cannot record match result: invalid data");
        return;
      }

      var currentMatch = GetCurrentMatch();
      if (currentMatch == null)
      {
        Logger.Info("Cannot record match result: no current match");
        return;
      }

      Logger.Info($"Recording result for Match {currentMatch.MatchIndex}: Winner = {winner}");

      // Mettre à jour le bracket
      TournamentBracket.UpdateBracketAfterMatch(Data.Bracket, currentMatch.MatchIndex, winner);

      // Vérifier si le tournoi est terminé
      if (TournamentBracket.IsTournamentComplete(Data.Bracket))
      {
        Data.IsComplete = true;

        // Pour les formats basés sur les victoires (round-robin, KotH), le champion
        // est le premier du classement (avec départage). Pour l'élimination, c'est
        // le vainqueur de la finale.
        if (Data.Type == TournamentType.RoundRobin || Data.Type == TournamentType.KingOfTheHill)
        {
          var standings = GetStandings();
          Data.Champion = standings.Count > 0 ? standings[0].Key : null;
        }
        else
        {
          Data.Champion = TournamentBracket.GetChampion(Data.Bracket);
        }

        Logger.Info($"Tournament complete! Champion: {Data.Champion}");

        // Tournoi terminé : on peut effacer la sauvegarde.
        TournamentSave.Delete();
      }
      else
      {
        // Passer au match suivant
        NavigateToNextMatch();

        // Sauvegarder la progression après chaque match.
        TournamentSave.Save(Data);
      }
    }

    /// <summary>
    /// Navigue vers le prochain match à jouer
    /// </summary>
    public void NavigateToNextMatch()
    {
      if (Data == null)
        return;

      var nextMatch = TournamentBracket.GetNextMatch(Data.Bracket);
      
      if (nextMatch != null)
      {
        Data.CurrentMatchIndex = nextMatch.MatchIndex;
        Logger.Info($"Next match: {nextMatch.MatchIndex} - {nextMatch.GetRoundName()}");
        Logger.Info($"  Players: {nextMatch.GetPlayersDisplay()}");
      }
      else
      {
        Logger.Info("No more matches to play");
      }
    }

    /// <summary>
    /// Vérifie si le tournoi est terminé
    /// </summary>
    public bool IsTournamentComplete()
    {
      if (Data == null)
        return false;

      return Data.IsComplete;
    }

    /// <summary>
    /// Retourne le champion du tournoi
    /// </summary>
    public string GetChampion()
    {
      if (Data == null || !Data.IsComplete)
        return null;

      return Data.Champion;
    }

    /// <summary>
    /// Retourne le nombre de matchs complétés
    /// </summary>
    public int GetCompletedMatchCount()
    {
      if (Data == null)
        return 0;

      return Data.GetCompletedMatchCount();
    }

    /// <summary>
    /// Retourne le nombre total de matchs
    /// </summary>
    public int GetTotalMatchCount()
    {
      if (Data == null)
        return 0;

      return Data.TotalMatches;
    }

    /// <summary>
    /// Crée les MatchSettings pour le match en cours basé sur la configuration du tournoi
    /// </summary>
    public MatchSettings CreateMatchSettingsForCurrentMatch()
    {
      if (Data == null)
      {
        Logger.Info("Cannot create match settings: invalid data");
        return null;
      }

      var currentMatch = GetCurrentMatch();
      if (currentMatch == null)
      {
        Logger.Info("Cannot create match settings: no current match");
        return null;
      }

      Logger.Info($"TODO: Create match settings for Match {currentMatch.MatchIndex}");
      
      ConfigurePlayersForMatch(null, currentMatch);
      return null;
    }

    /// <summary>
    /// Configure les joueurs dans les MatchSettings selon le match FFA
    /// </summary>
    private void ConfigurePlayersForMatch(MatchSettings settings, TournamentMatch match)
    {
      for (int i = 0; i < TFGame.Players.Length; i++)
      {
        TFGame.Players[i] = false;
      }

      int playerIndex = 0;
      foreach (var playerName in match.Players)
      {
        if (playerIndex < TFGame.Players.Length && playerName != "TBD")
        {
          TFGame.Players[playerIndex] = true;
          playerIndex++;
        }
      }

      Logger.Info($"Configured {playerIndex} players for FFA match");
    }

    public static void Load()
    {
      Logger.Info("TournamentSession hooks loaded");
    }

    public static void Unload()
    {
      EndTournament();
      Logger.Info("TournamentSession hooks unloaded");
    }

    public string GetCurrentMatchDescription()
    {
      var match = GetCurrentMatch();
      if (match == null)
        return "No match";

      return $"{match.GetRoundName()} - {match.GetPlayersDisplay()}";
    }

    public float GetProgressPercentage()
    {
      if (Data == null || Data.TotalMatches == 0)
        return 0f;

      return (float)GetCompletedMatchCount() / (float)Data.TotalMatches * 100f;
    }

    /// <summary>
    /// Classement des joueurs par nombre de matchs gagnés (décroissant), avec départage :
    /// à égalité de victoires, on regarde la confrontation directe (victoires dans les
    /// matchs où tous les participants sont eux aussi à égalité), puis l'ordre alphabétique
    /// pour rester déterministe. Inclut tous les joueurs inscrits, même à 0 victoire.
    /// </summary>
    public List<KeyValuePair<string, int>> GetStandings()
    {
      var wins = new Dictionary<string, int>();

      if (Data != null && Data.SelectedPlayerNames != null)
      {
        foreach (var player in Data.SelectedPlayerNames)
        {
          if (!string.IsNullOrEmpty(player) && !wins.ContainsKey(player))
            wins[player] = 0;
        }
      }

      if (Data != null && Data.Bracket != null)
      {
        foreach (var match in Data.Bracket)
        {
          if (match.IsPlayed && !string.IsNullOrEmpty(match.Winner))
          {
            if (!wins.ContainsKey(match.Winner))
              wins[match.Winner] = 0;
            wins[match.Winner]++;
          }
        }
      }

      // Départage : confrontation directe parmi les joueurs à égalité de victoires.
      var headToHead = new Dictionary<string, int>();
      foreach (var kv in wins)
        headToHead[kv.Key] = 0;

      if (Data != null && Data.Bracket != null)
      {
        foreach (var match in Data.Bracket)
        {
          if (!match.IsPlayed || string.IsNullOrEmpty(match.Winner))
            continue;
          if (!wins.ContainsKey(match.Winner))
            continue;

          int winnerWins = wins[match.Winner];
          bool allTied = true;
          foreach (var pl in match.Players)
          {
            if (pl == "TBD" || !wins.ContainsKey(pl) || wins[pl] != winnerWins)
            {
              allTied = false;
              break;
            }
          }

          if (allTied)
            headToHead[match.Winner]++;
        }
      }

      var standings = new List<KeyValuePair<string, int>>(wins);
      standings.Sort((a, b) =>
      {
        if (a.Value != b.Value)
          return b.Value.CompareTo(a.Value);

        int ha = headToHead.ContainsKey(a.Key) ? headToHead[a.Key] : 0;
        int hb = headToHead.ContainsKey(b.Key) ? headToHead[b.Key] : 0;
        if (ha != hb)
          return hb.CompareTo(ha);

        return string.Compare(a.Key, b.Key, StringComparison.Ordinal);
      });
      return standings;
    }
  }
}

// Made with Bob

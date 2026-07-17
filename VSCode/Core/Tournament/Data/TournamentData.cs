using System.Collections.Generic;
using TowerFall;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Contient toutes les données d'un tournoi FFA
  /// </summary>
  public class TournamentData
  {
    /// <summary>
    /// Noms des joueurs participants au tournoi
    /// </summary>
    public List<string> SelectedPlayerNames { get; set; }

    /// <summary>
    /// Format des matchs (FFA 2, 3, 4 joueurs)
    /// </summary>
    public TournamentMatchFormat Format { get; set; }

    /// <summary>
    /// Type de tournoi (élimination directe ou round-robin)
    /// </summary>
    public TournamentType Type { get; set; }

    /// <summary>
    /// Configuration de base pour tous les matchs du tournoi (non sauvegardée : type
    /// TowerFall non sérialisable en JSON).
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    public MatchSettings BaseMatchSettings { get; set; }

    /// <summary>
    /// Mode de sélection de la map (voir TournamentMapMode : -2 manuel, -1 aléatoire,
    /// >= 0 index de tour fixe).
    /// </summary>
    public int MapMode { get; set; }

    /// <summary>
    /// Structure du bracket avec tous les matchs
    /// </summary>
    public List<TournamentMatch> Bracket { get; set; }

    /// <summary>
    /// Index du match actuellement en cours
    /// </summary>
    public int CurrentMatchIndex { get; set; }

    /// <summary>
    /// Nombre total de rounds dans le tournoi
    /// </summary>
    public int TotalRounds { get; set; }

    /// <summary>
    /// Nombre de rounds à gagner pour remporter un match (choisi dans les settings)
    /// </summary>
    public int RoundsToWin { get; set; }

    /// <summary>
    /// Nombre total de matchs dans le tournoi
    /// </summary>
    public int TotalMatches { get; set; }

    /// <summary>
    /// Nom du champion (null tant que le tournoi n'est pas terminé)
    /// </summary>
    public string Champion { get; set; }

    /// <summary>
    /// Indique si le tournoi est terminé
    /// </summary>
    public bool IsComplete { get; set; }

    public TournamentData()
    {
      SelectedPlayerNames = new List<string>();
      Bracket = new List<TournamentMatch>();
      CurrentMatchIndex = 0;
      TotalRounds = 0;
      TotalMatches = 0;
      RoundsToWin = 3;
      MapMode = TournamentMapMode.Manual;
      Champion = null;
      IsComplete = false;
      Format = TournamentMatchFormat.Ffa2;
      Type = TournamentType.Elimination;
    }

    /// <summary>
    /// Retourne le match actuellement en cours
    /// </summary>
    public TournamentMatch GetCurrentMatch()
    {
      if (CurrentMatchIndex >= 0 && CurrentMatchIndex < Bracket.Count)
        return Bracket[CurrentMatchIndex];
      
      return null;
    }

    /// <summary>
    /// Retourne le nombre de joueurs participants
    /// </summary>
    public int GetPlayerCount()
    {
      return SelectedPlayerNames?.Count ?? 0;
    }

    /// <summary>
    /// Retourne le nombre de matchs déjà joués
    /// </summary>
    public int GetCompletedMatchCount()
    {
      int count = 0;
      foreach (var match in Bracket)
      {
        if (match.IsPlayed)
          count++;
      }
      return count;
    }

    /// <summary>
    /// Vérifie si tous les matchs sont terminés
    /// </summary>
    public bool AreAllMatchesComplete()
    {
      foreach (var match in Bracket)
      {
        if (!match.IsPlayed)
          return false;
      }
      return true;
    }

    /// <summary>
    /// Retourne le prochain match à jouer
    /// </summary>
    public TournamentMatch GetNextUnplayedMatch()
    {
      foreach (var match in Bracket)
      {
        if (!match.IsPlayed)
        {
          bool hasTBD = false;
          foreach (var player in match.Players)
          {
            if (player == "?")
            {
              hasTBD = true;
              break;
            }
          }
          if (!hasTBD)
            return match;
        }
      }
      return null;
    }

    /// <summary>
    /// Retourne le nombre de joueurs par match selon le format
    /// </summary>
    public int GetPlayersPerMatch()
    {
      return TournamentBracket.GetPlayersPerMatch(Format);
    }

    /// <summary>
    /// Retourne une description du format pour l'affichage
    /// </summary>
    public string GetFormatDisplay()
    {
      int playersPerMatch = TournamentBracket.GetPlayersPerMatch(Format);
      return $"FFA {playersPerMatch}";
    }
  }
}

// Made with Bob

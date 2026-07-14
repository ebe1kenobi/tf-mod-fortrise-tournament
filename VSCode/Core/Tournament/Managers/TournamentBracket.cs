using System;
using System.Collections.Generic;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Gestionnaire pour générer et manipuler le bracket du tournoi (FFA)
  /// </summary>
  public static class TournamentBracket
  {
    /// <summary>
    /// Retourne le nombre de joueurs par match selon le format
    /// </summary>
    public static int GetPlayersPerMatch(TournamentMatchFormat format)
    {
      switch (format)
      {
        case TournamentMatchFormat.Ffa2:
          return 2;
        case TournamentMatchFormat.Ffa3:
          return 3;
        case TournamentMatchFormat.Ffa4:
          return 4;
        default:
          return 2;
      }
    }

    /// <summary>
    /// Génère un bracket complet pour le tournoi FFA
    /// </summary>
    /// <param name="playerNames">Liste des noms de joueurs</param>
    /// <param name="format">Format des matchs (FFA 2,3,4)</param>
    /// <returns>Liste des matchs du bracket</returns>
    public static List<TournamentMatch> GenerateBracket(List<string> playerNames, TournamentMatchFormat format, TournamentType type)
    {
      if (playerNames == null || playerNames.Count == 0)
      {
        Logger.Info("Cannot generate bracket: no players provided");
        return new List<TournamentMatch>();
      }

      if (type == TournamentType.RoundRobin)
        return GenerateRoundRobin(playerNames, format);

      if (type == TournamentType.KingOfTheHill)
        return GenerateKingOfTheHill(playerNames);

      return GenerateElimination(playerNames, format);
    }

    /// <summary>
    /// King of the Hill : une "échelle" linéaire. Match 0 = joueur 0 vs joueur 1 ;
    /// match i = vainqueur du match précédent vs challenger suivant. Toujours en 1 contre 1
    /// (le format FFA est ignoré). Le champion sera celui qui a le plus de victoires
    /// (plus long règne).
    /// </summary>
    private static List<TournamentMatch> GenerateKingOfTheHill(List<string> playerNames)
    {
      var players = new List<string>(playerNames);
      Shuffle(players);

      var matches = new List<TournamentMatch>();
      if (players.Count < 2)
        return matches;

      for (int i = 0; i < players.Count - 1; i++)
      {
        var match = new TournamentMatch
        {
          MatchIndex = i,
          BracketPosition = i,
          RoundLabel = $"DEFI {i + 1}",
          // Le vainqueur passe au défi suivant (sauf le dernier).
          NextMatchIndex = (i < players.Count - 2) ? i + 1 : -1
        };

        if (i == 0)
        {
          match.Players.Add(players[0]);
          match.Players.Add(players[1]);
        }
        else
        {
          match.Players.Add("TBD");          // le roi (vainqueur du défi précédent)
          match.Players.Add(players[i + 1]); // le challenger
        }

        matches.Add(match);
      }

      Logger.Info($"Generated King of the Hill with {matches.Count} matches");
      return matches;
    }

    /// <summary>
    /// Génère un bracket à élimination directe (quarts, demies, finale) avec gestion des byes.
    /// </summary>
    private static List<TournamentMatch> GenerateElimination(List<string> playerNames, TournamentMatchFormat format)
    {
      // Mélanger les joueurs pour un bracket plus équilibré
      var shuffledPlayers = new List<string>(playerNames);
      Shuffle(shuffledPlayers);

      int k = GetPlayersPerMatch(format);
      if (k < 2)
        k = 2;
      Logger.Info($"Generating FFA bracket for {shuffledPlayers.Count} players in {format} format");

      var matches = new List<TournamentMatch>();

      // Chaque "entrant" d'un round est soit un joueur concret, soit le futur gagnant
      // d'un match déjà créé (ProducerMatch >= 0 => son nom d'entrée est "TBD").
      var entrants = new List<Entrant>();
      foreach (var name in shuffledPlayers)
        entrants.Add(new Entrant(name, -1));

      // Construction round par round. Pour chaque round on vise la puissance de k
      // immédiatement >= au nombre d'entrants, et on répartit les joueurs en
      // "groups = puissance/k" groupes de taille la plus homogène possible. Les byes
      // (groupes de 1) sont ainsi placés dès le premier tour -> bracket équilibré :
      // tous les matchs du 1er tour sont à la même profondeur (colonne de gauche).
      while (entrants.Count > 1)
      {
        int total = entrants.Count;

        int p = k;
        while (p < total)
          p *= k;
        int groups = p / k;
        if (groups < 1)
          groups = 1;

        int baseSize = total / groups;
        int rem = total % groups;

        var nextEntrants = new List<Entrant>();
        int idx = 0;

        for (int g = 0; g < groups; g++)
        {
          // Répartition ENTRELACÉE des groupes plus grands (matchs) : ça évite que les
          // byes se retrouvent groupés et s'affrontent entre eux. Chaque bye affronte
          // ainsi un vainqueur au round suivant -> arbre propre.
          int size = baseSize + (((g + 1) * rem) / groups - (g * rem) / groups);

          // Groupe de 1 => BYE : le joueur passe directement au round suivant.
          if (size <= 1)
          {
            nextEntrants.Add(entrants[idx]);
            idx++;
            continue;
          }

          var match = new TournamentMatch
          {
            MatchIndex = matches.Count,
            BracketPosition = matches.Count
          };

          for (int j = 0; j < size; j++)
          {
            var entrant = entrants[idx];
            match.Players.Add(entrant.Name); // nom réel, ou "TBD" (gagnant d'un match)

            if (entrant.ProducerMatch >= 0)
              matches[entrant.ProducerMatch].NextMatchIndex = match.MatchIndex;

            idx++;
          }

          matches.Add(match);
          nextEntrants.Add(new Entrant("TBD", match.MatchIndex));
        }

        entrants = nextEntrants;
      }

      // Numéro de round basé sur la profondeur jusqu'à la finale (finale = 0).
      foreach (var match in matches)
      {
        int round = 0;
        int idx = match.NextMatchIndex;
        while (idx >= 0 && idx < matches.Count)
        {
          round++;
          idx = matches[idx].NextMatchIndex;
        }
        match.Round = round;
      }

      Logger.Info($"Generated FFA bracket with {matches.Count} matches total");
      return matches;
    }

    /// <summary>
    /// Entrant d'un round : soit un joueur concret, soit le gagnant (encore "TBD")
    /// d'un match déjà généré.
    /// </summary>
    private sealed class Entrant
    {
      public readonly string Name;
      public readonly int ProducerMatch;

      public Entrant(string name, int producerMatch)
      {
        Name = name;
        ProducerMatch = producerMatch;
      }
    }

    /// <summary>
    /// Génère un tournoi round-robin (poule) : toutes les combinaisons possibles de
    /// joueurs (de la taille du format FFA) jouent une fois. Le champion est celui qui
    /// remporte le plus de matchs.
    /// </summary>
    private static List<TournamentMatch> GenerateRoundRobin(List<string> playerNames, TournamentMatchFormat format)
    {
      var players = new List<string>(playerNames);
      Shuffle(players);

      int groupSize = GetPlayersPerMatch(format);
      if (groupSize < 2)
        groupSize = 2;
      if (groupSize > players.Count)
        groupSize = players.Count;

      Logger.Info($"Generating round-robin for {players.Count} players, groups of {groupSize}");

      var combos = new List<List<string>>();
      BuildCombinations(players, groupSize, 0, new List<string>(), combos);

      var matches = new List<TournamentMatch>();
      for (int i = 0; i < combos.Count; i++)
      {
        var match = new TournamentMatch
        {
          MatchIndex = i,
          BracketPosition = i,
          Round = 0,
          IsPoolMatch = true,
          NextMatchIndex = -1
        };
        match.Players.AddRange(combos[i]);
        matches.Add(match);
      }

      Logger.Info($"Generated round-robin with {matches.Count} matches total");
      return matches;
    }

    /// <summary>
    /// Construit récursivement toutes les combinaisons de <paramref name="k"/> éléments.
    /// </summary>
    private static void BuildCombinations(List<string> items, int k, int start, List<string> current, List<List<string>> result)
    {
      if (current.Count == k)
      {
        result.Add(new List<string>(current));
        return;
      }

      for (int i = start; i < items.Count; i++)
      {
        current.Add(items[i]);
        BuildCombinations(items, k, i + 1, current, result);
        current.RemoveAt(current.Count - 1);
      }
    }

    /// <summary>
    /// Retourne le joueur ayant remporté le plus de matchs (pour le round-robin).
    /// En cas d'égalité, retourne le premier trouvé.
    /// </summary>
    public static string GetChampionByWins(List<TournamentMatch> matches)
    {
      if (matches == null || matches.Count == 0)
        return null;

      var wins = new Dictionary<string, int>();
      foreach (var match in matches)
      {
        if (match.IsPlayed && !string.IsNullOrEmpty(match.Winner))
        {
          if (!wins.ContainsKey(match.Winner))
            wins[match.Winner] = 0;
          wins[match.Winner]++;
        }
      }

      string champion = null;
      int best = -1;
      foreach (var kvp in wins)
      {
        if (kvp.Value > best)
        {
          best = kvp.Value;
          champion = kvp.Key;
        }
      }

      return champion;
    }

    /// <summary>
    /// Nombre total de matchs prévus pour l'affichage (avant génération du bracket).
    /// </summary>
    public static int CountTotalMatches(int playerCount, TournamentMatchFormat format, TournamentType type)
    {
      if (type == TournamentType.RoundRobin)
      {
        int k = GetPlayersPerMatch(format);
        if (k < 2)
          k = 2;
        if (k > playerCount)
          k = playerCount;
        return Binomial(playerCount, k);
      }

      // Élimination : exact pour FFA-2, approximatif pour FFA-3/4.
      return playerCount > 0 ? playerCount - 1 : 0;
    }

    /// <summary>
    /// Coefficient binomial C(n, k).
    /// </summary>
    private static int Binomial(int n, int k)
    {
      if (k < 0 || k > n)
        return 0;
      if (k == 0 || k == n)
        return 1;

      k = Math.Min(k, n - k);
      long result = 1;
      for (int i = 0; i < k; i++)
      {
        result = result * (n - i) / (i + 1);
      }
      return (int)result;
    }

    /// <summary>
    /// Mélange une liste (Fisher-Yates shuffle)
    /// </summary>
    private static void Shuffle<T>(IList<T> list)
    {
      Random rng = new Random();
      int n = list.Count;
      while (n > 1)
      {
        n--;
        int k = rng.Next(n + 1);
        T value = list[k];
        list[k] = list[n];
        list[n] = value;
      }
    }

    /// <summary>
    /// Met à jour le bracket après qu'un match soit terminé
    /// </summary>
    public static void UpdateBracketAfterMatch(List<TournamentMatch> matches, int matchIndex, string winner)
    {
      if (matches == null || matchIndex < 0 || matchIndex >= matches.Count)
      {
        Logger.Info($"Cannot update bracket: invalid match index {matchIndex}");
        return;
      }

      var completedMatch = matches[matchIndex];
      completedMatch.Winner = winner;
      completedMatch.IsPlayed = true;

      Logger.Info($"Match {matchIndex} completed. Winner: {winner}");

      // Si ce n'est pas la finale, propager le gagnant au match suivant
      if (completedMatch.NextMatchIndex >= 0 && completedMatch.NextMatchIndex < matches.Count)
      {
        var nextMatch = matches[completedMatch.NextMatchIndex];
        
        // Remplacer le premier "TBD" par le gagnant
        for (int i = 0; i < nextMatch.Players.Count; i++)
        {
          if (nextMatch.Players[i] == "TBD")
          {
            nextMatch.Players[i] = winner;
            Logger.Info($"Winner advanced to Match {nextMatch.MatchIndex}");
            break;
          }
        }
      }
      else
      {
        Logger.Info($"Match {matchIndex} is the finale. Tournament complete!");
      }
    }

    /// <summary>
    /// Trouve le prochain match à jouer dans le bracket
    /// </summary>
    public static TournamentMatch GetNextMatch(List<TournamentMatch> matches)
    {
      if (matches == null)
        return null;

      // Parcourir les matchs dans l'ordre pour trouver le premier non joué avec des joueurs complets (pas de TBD sauf si c'est la finale et les TBD sont remplacés)
      foreach (var match in matches)
      {
        if (!match.IsPlayed && match.Players.Count > 0)
        {
          // Vérifier qu'il n'y a pas de TBD (sauf si c'est la finale et les TBD sont des vrais joueurs)
          bool hasTBD = false;
          foreach (var player in match.Players)
          {
            if (player == "TBD")
            {
              hasTBD = true;
              break;
            }
          }

          if (!hasTBD)
          {
            return match;
          }
        }
      }

      return null;
    }

    /// <summary>
    /// Vérifie si le tournoi est terminé (tous les matchs joués)
    /// </summary>
    public static bool IsTournamentComplete(List<TournamentMatch> matches)
    {
      if (matches == null || matches.Count == 0)
        return false;

      foreach (var match in matches)
      {
        if (!match.IsPlayed)
          return false;
      }

      return true;
    }

    /// <summary>
    /// Retourne le champion du tournoi (gagnant de la finale)
    /// </summary>
    public static string GetChampion(List<TournamentMatch> matches)
    {
      if (matches == null || matches.Count == 0)
        return null;

      // La finale est toujours le dernier match (Round 0)
      var finale = matches.Find(m => m.Round == 0);
      
      if (finale != null && finale.IsPlayed)
      {
        return finale.Winner;
      }

      return null;
    }

    /// <summary>
    /// Calcule le nombre total de rounds pour un nombre de joueurs et un format donné
    /// </summary>
    public static int GetTotalRounds(int playerCount, TournamentMatchFormat format)
    {
      int playersPerMatch = GetPlayersPerMatch(format);
      int rounds = 0;
      int remaining = playerCount;

      while (remaining > 1)
      {
        remaining = (int)Math.Ceiling((double)remaining / playersPerMatch);
        rounds++;
      }

      return rounds;
    }

    /// <summary>
    /// Calcule le nombre total de matchs pour un nombre de joueurs et un format donné
    /// </summary>
    public static int GetTotalMatches(int playerCount, TournamentMatchFormat format)
    {
      // Pour un tournoi à élimination directe en FFA, c'est toujours (nombre de joueurs - 1) matchs total !
      return playerCount - 1;
    }
  }
}

// Made with Bob

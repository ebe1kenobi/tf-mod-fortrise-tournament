using System.Collections.Generic;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Représente un match individuel dans le tournoi (FFA)
  /// </summary>
  public class TournamentMatch
  {
    /// <summary>
    /// Index unique du match dans le bracket
    /// </summary>
    public int MatchIndex { get; set; }

    /// <summary>
    /// Round du match (0 = finale, 1 = demi-finale, 2 = quart de finale...)
    /// </summary>
    public int Round { get; set; }

    /// <summary>
    /// Noms des joueurs du match FFA
    /// </summary>
    public List<string> Players { get; set; }

    /// <summary>
    /// Nom du gagnant (null si le match n'est pas encore joué)
    /// </summary>
    public string Winner { get; set; }

    /// <summary>
    /// Index du match suivant pour le gagnant (-1 si c'est la finale)
    /// </summary>
    public int NextMatchIndex { get; set; }

    /// <summary>
    /// Indique si le match a été joué
    /// </summary>
    public bool IsPlayed { get; set; }

    /// <summary>
    /// Position du match dans le bracket (pour l'affichage)
    /// </summary>
    public int BracketPosition { get; set; }

    /// <summary>
    /// Vrai si ce match fait partie d'un tournoi round-robin (poule) plutôt que
    /// d'un bracket à élimination directe.
    /// </summary>
    public bool IsPoolMatch { get; set; }

    /// <summary>
    /// Libellé de round personnalisé (ex: "DEFI 3" en King of the Hill). Si défini,
    /// il remplace le nom de round calculé (FINALE, DEMI-FINALE...).
    /// </summary>
    public string RoundLabel { get; set; }

    public TournamentMatch()
    {
      Players = new List<string>();
      Winner = null;
      IsPlayed = false;
      NextMatchIndex = -1;
      BracketPosition = 0;
      IsPoolMatch = false;
    }

    /// <summary>
    /// Retourne la liste des joueurs formatée pour l'affichage
    /// </summary>
    public string GetPlayersDisplay()
    {
      if (Players == null || Players.Count == 0)
        return "?";
      
      return string.Join(" vs ", Players);
    }

    /// <summary>
    /// Retourne le nom du round pour l'affichage
    /// </summary>
    public string GetRoundName()
    {
      if (!string.IsNullOrEmpty(RoundLabel))
        return RoundLabel;

      if (IsPoolMatch)
        return "POOL";

      switch (Round)
      {
        case 0:
          return "FINAL";
        case 1:
          return "SEMI-FINALE";
        case 2:
          return "QUARTERE FINAL";
        default:
          return $"ROUND {Round + 1}";
      }
    }

    /// <summary>
    /// Vérifie si un joueur est dans ce match
    /// </summary>
    public bool ContainsPlayer(string playerName)
    {
      if (Players == null || string.IsNullOrEmpty(playerName))
        return false;
      
      return Players.Contains(playerName);
    }
  }
}

// Made with Bob

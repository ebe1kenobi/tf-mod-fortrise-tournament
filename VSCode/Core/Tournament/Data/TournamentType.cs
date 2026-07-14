namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Type de déroulement du tournoi.
  /// </summary>
  public enum TournamentType
  {
    /// <summary>
    /// Élimination directe : bracket avec quarts, demies, finale.
    /// </summary>
    Elimination,

    /// <summary>
    /// Round-robin / poule : toutes les rencontres possibles sont jouées,
    /// le joueur avec le plus de victoires remporte le tournoi.
    /// </summary>
    RoundRobin,

    /// <summary>
    /// King of the Hill : le vainqueur reste, un nouveau challenger arrive à chaque
    /// match. Le champion est celui qui a enchaîné le plus de victoires (plus long règne).
    /// </summary>
    KingOfTheHill
  }

  /// <summary>
  /// Comment la map de chaque match du tournoi est choisie.
  /// Valeurs spéciales : -2 = manuel (écran de sélection), -1 = aléatoire.
  /// Valeur >= 0 = index d'une tour fixe (GameData.VersusTowers).
  /// </summary>
  public static class TournamentMapMode
  {
    public const int Manual = -2;
    public const int Random = -1;
  }
}

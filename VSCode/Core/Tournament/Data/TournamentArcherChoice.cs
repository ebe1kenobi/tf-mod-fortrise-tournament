namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Archer retenu par un joueur du tournoi, memorise d'un match a l'autre pour
  /// qu'il n'ait qu'a valider aux matchs suivants.
  ///
  /// AltSelect est stocke en int et non en ArcherData.ArcherTypes : TournamentData
  /// est serialise en JSON et on evite d'y faire entrer un type du jeu.
  /// </summary>
  public class TournamentArcherChoice
  {
    public int CharacterIndex { get; set; }
    public int AltSelect { get; set; }
  }
}

using System.Collections.Generic;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// D'ou viennent les noms proposes a la selection des joueurs.
  ///
  /// Deux sources coexistent, et le choix appartient a l'utilisateur :
  ///
  /// - le fichier tournament_players.json, que le mod gere depuis toujours et qui ne
  ///   contient que des noms ;
  /// - la liste des profils du mod Profiles, quand il est installe.
  ///
  /// Profiles est une dependance optionnelle : absent, trop ancien, ou simplement pas
  /// voulu, le tournoi fonctionne exactement comme avant. Le repli n'est jamais
  /// silencieux au point d'etre trompeur - l'ecran de selection affiche la source
  /// reellement utilisee, pas celle demandee.
  /// </summary>
  public static class TournamentRoster
  {
    public const string SourceJson = "JSON";
    public const string SourceProfiles = "PROFILES";

    // Lue une fois puis gardee en memoire : l'ecran de selection interroge la source
    // a chaque image pour son affichage, et relire le fichier a ce rythme n'apporte
    // rien - ce mod est seul a l'ecrire.
    private static string source;

    /// <summary>
    /// Source demandee, telle qu'elle est enregistree. Peut nommer Profiles alors que
    /// le mod n'est pas la : le choix survit a une desinstallation temporaire au lieu
    /// d'etre efface.
    /// </summary>
    public static string Source
    {
      get => source ??= TournamentPlayerManager.LoadSource();
      private set
      {
        source = value;
        TournamentPlayerManager.SaveSource(value);
      }
    }

    /// <summary>
    /// Source qui va reellement servir : la demandee, sauf si Profiles ne peut pas la
    /// fournir.
    /// </summary>
    public static string EffectiveSource =>
        Source == SourceProfiles && ProfilesImport.HasRoster ? SourceProfiles : SourceJson;

    /// <summary>
    /// Vrai quand basculer a un sens. Sans Profiles il n'y a qu'une source : proposer
    /// un choix a une seule issue ne ferait qu'egarer.
    /// </summary>
    public static bool CanChooseSource => ProfilesImport.HasRoster;

    /// <summary>
    /// Passe d'une source a l'autre et rend la nouvelle source effective.
    /// </summary>
    public static string ToggleSource()
    {
      if (!CanChooseSource)
        return EffectiveSource;

      Source = EffectiveSource == SourceProfiles ? SourceJson : SourceProfiles;
      Logger.Info($"Roster source: {EffectiveSource}");
      return EffectiveSource;
    }

    /// <summary>
    /// Noms disponibles pour composer un tournoi. Jamais null : une source vide rend
    /// une liste vide, que l'ecran de selection sait presenter.
    /// </summary>
    public static List<string> Load()
    {
      if (EffectiveSource == SourceProfiles)
        return ProfilesImport.GetProfileNames();

      return TournamentPlayerManager.LoadPlayerNames() ?? new List<string>();
    }

    /// <summary>
    /// Vrai quand l'utilisateur peut ajouter un nom depuis le jeu. Les profils se
    /// creent dans le menu de Profiles, avec un archer, des sons et des couleurs :
    /// en fabriquer un ici a partir d'un simple nom donnerait un profil vide, sans
    /// rien de ce qui en fait un.
    /// </summary>
    public static bool CanAddName => EffectiveSource == SourceJson;

    /// <summary>
    /// Comment nommer la source a l'ecran. Le repli est dit explicitement : demander
    /// Profiles et voir la liste du fichier JSON sans explication ferait passer une
    /// dependance absente pour une liste vide.
    /// </summary>
    public static string Label =>
        Source == SourceProfiles && !ProfilesImport.HasRoster
            ? "JSON (NO PROFILES)"
            : EffectiveSource;

    /// <summary>
    /// Ce qu'il faut afficher quand la source retenue ne donne aucun nom. Le message
    /// depend de la source : la marche a suivre n'est pas la meme. Tenu court, la
    /// colonne ou il s'affiche fait 120 pixels.
    /// </summary>
    public static string EmptyHint =>
        EffectiveSource == SourceProfiles
            ? "ADD IN PROFILES"
            : "Y: ADD NAME";
  }
}

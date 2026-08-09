namespace TFModFortRiseTournament;

/// <summary>
/// Copie de l'interface de roster publiee par le mod Profiles.
///
/// Demandee separement de <see cref="IProfilesModApi"/>, et avec une version
/// minimale : l'interop batit son proxy sur la forme des membres, declarer ici un
/// membre absent du Profiles installe ferait perdre toute l'API. En la tenant a part,
/// un Profiles anterieur continue de fournir les noms de joueurs et seul le roster
/// retombe sur le fichier JSON.
/// </summary>
public partial interface IProfilesRosterApi
{
    string[] GetProfileNames();
    bool AssignProfile(int playerIndex, string profileName);
    int GetProfileArcher(string profileName);
    bool IsProfileAlt(string profileName);
}

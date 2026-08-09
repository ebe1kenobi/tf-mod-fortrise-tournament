using System;

namespace TFModFortRiseTournament;

/// <summary>
/// Copie de l'interface publiee par le mod Profiles. En FortRise 5 l'interop
/// passe par une interface partagee (GetApi) et non plus par MonoMod.ModInterop.
/// </summary>
public partial interface IProfilesModApi
{
    void SetPlayerName(int playerIndex, String playerName);
    String GetPlayerName(int playerIndex);
}

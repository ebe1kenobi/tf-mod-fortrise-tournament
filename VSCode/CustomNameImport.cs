using System;

namespace TFModFortRiseTournament
{
  /// <summary>
  /// Acces aux noms de joueurs fournis par le mod CustomName.
  ///
  /// FortRise 4 passait par MonoMod.ModInterop ([ModImportName] + delegues statiques).
  /// CustomName n'exporte plus par ce biais en FortRise 5 : il publie une interface
  /// via GetApi(). Les delegues restaient donc null.
  ///
  /// L'API est optionnelle : IsAvailable permet aux appelants de distinguer un vrai
  /// nom custom d'un repli, et GetPlayerName retombe sur "P1".."P8" si absent.
  /// </summary>
  public static class CustomNameImport
  {
    internal static ICustomNameModApi Api;

    public static bool IsAvailable => Api != null;

    public static String GetPlayerName(int playerIndex)
    {
      if (Api != null)
      {
        try
        {
          string name = Api.GetPlayerName(playerIndex);
          if (!string.IsNullOrEmpty(name))
            return name;
        }
        catch (Exception ex)
        {
          Logger.Info($"[CustomName] GetPlayerName({playerIndex}) a echoue : {ex.Message}");
        }
      }

      return "P" + (playerIndex + 1);
    }

    public static void SetPlayerName(int playerIndex, String playerName)
    {
      if (Api == null)
        return;

      try
      {
        Api.SetPlayerName(playerIndex, playerName);
      }
      catch (Exception ex)
      {
        Logger.Info($"[CustomName] SetPlayerName({playerIndex}) a echoue : {ex.Message}");
      }
    }
  }
}

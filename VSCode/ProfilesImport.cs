using System;
using FortRise;
using System.Collections.Generic;

namespace TFModFortRiseTournament
{
  /// <summary>
  /// Acces aux noms de joueurs fournis par le mod Profiles.
  ///
  /// Profiles a repris ce role a CustomName : il publie une interface via
  /// GetApi(), que l'interop de FortRise proxifie sur la forme des membres.
  ///
  /// L'API est optionnelle : IsAvailable permet aux appelants de distinguer un vrai
  /// nom custom d'un repli, et GetPlayerName retombe sur "P1".."P8" si absent.
  /// </summary>
  public static class ProfilesImport
  {
    private static IModInterop interop;
    private static IProfilesModApi api;
    private static IProfilesRosterApi roster;

    /// <summary>
    /// Retient de quoi interroger les autres mods, sans rien demander tout de suite.
    ///
    /// On ne PEUT PAS resoudre dans le constructeur du module : ModuleManager n'inscrit
    /// un mod dans son annuaire qu'APRES avoir execute son constructeur. Au moment ou
    /// celui du tournoi tourne, Archer n'est donc pas joignable si l'ordre de
    /// chargement le place apres - et c'est ce qui arrivait : le journal disait
    /// "mod absent", la source PROFILES disparaissait de l'ecran, et Archer etait
    /// pourtant bien la.
    /// </summary>
    public static void Bind(IModInterop modInterop)
    {
      interop = modInterop;
    }

    /// <summary>
    /// Resolue au PREMIER BESOIN, c'est-a-dire a l'ouverture de l'ecran de selection,
    /// longtemps apres que tous les mods sont charges. Mise en cache seulement en cas
    /// de succes : un echec ne coute qu'une recherche dans un dictionnaire et ne doit
    /// pas condamner les suivants.
    /// </summary>
    internal static IProfilesModApi Api
    {
      get
      {
        if (api != null || interop == null)
        {
          return api;
        }

        try
        {
          api = interop.GetApi<IProfilesModApi>("Archer");
        }
        catch (Exception)
        {
          // Mod absent, ou installe sans exposer cette interface.
        }

        return api;
      }
    }

    /// <summary>
    /// Le roster, demande A PART : l'interop batit son proxy sur la forme des membres,
    /// donc un Archer qui ne le publierait pas rend simplement null, sans faire perdre
    /// les noms de joueurs.
    /// </summary>
    internal static IProfilesRosterApi Roster
    {
      get
      {
        if (roster != null || interop == null)
        {
          return roster;
        }

        try
        {
          roster = interop.GetApi<IProfilesRosterApi>("Archer");
        }
        catch (Exception)
        {
        }

        return roster;
      }
    }

    public static bool IsAvailable => Api != null;

    /// <summary>
    /// Vrai quand le Profiles installe sait publier sa liste de profils. Distinct de
    /// <see cref="IsAvailable"/> : une version anterieure fournit les noms de joueurs
    /// sans connaitre le roster.
    /// </summary>
    public static bool HasRoster => Roster != null;

    /// <summary>
    /// Noms des profils enregistres, ou une liste vide si Profiles est absent, trop
    /// ancien, ou qu'aucun profil n'existe. L'appelant n'a pas a distinguer ces cas :
    /// dans tous, il n'y a rien a proposer.
    /// </summary>
    public static List<string> GetProfileNames()
    {
      if (Roster == null)
        return new List<string>();

      try
      {
        string[] names = Roster.GetProfileNames();
        if (names == null)
          return new List<string>();

        var cleaned = new List<string>();
        foreach (var name in names)
        {
          if (!string.IsNullOrWhiteSpace(name))
            cleaned.Add(name.Trim().ToUpper());
        }

        return cleaned;
      }
      catch (Exception ex)
      {
        Logger.Info($"[Profiles] GetProfileNames a echoue : {ex.Message}");
        return new List<string>();
      }
    }

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
          Logger.Info($"[Profiles] GetPlayerName({playerIndex}) a echoue : {ex.Message}");
        }
      }

      return "P" + (playerIndex + 1);
    }

    /// <summary>
    /// Rattache un profil a un emplacement joueur. C'est ce rattachement qui fait
    /// suivre les couleurs, les sons et les images du profil en jeu ; poser le nom ne
    /// fait que poser le nom.
    ///
    /// Un nom qui ne correspond a aucun profil detache l'emplacement : sans cela, le
    /// joueur d'un match precedent garderait ses couleurs sur le suivant.
    /// </summary>
    public static bool AssignProfile(int playerIndex, string profileName)
    {
      if (Roster == null)
        return false;

      try
      {
        return Roster.AssignProfile(playerIndex, profileName);
      }
      catch (Exception ex)
      {
        Logger.Info($"[Profiles] AssignProfile({playerIndex}, {profileName}) a echoue : {ex.Message}");
        return false;
      }
    }

    /// <summary>
    /// Archer prefere d'un profil, ou -1 si le nom n'en designe aucun.
    /// </summary>
    public static int GetProfileArcher(string profileName)
    {
      if (Roster == null)
        return -1;

      try
      {
        return Roster.GetProfileArcher(profileName);
      }
      catch (Exception ex)
      {
        Logger.Info($"[Profiles] GetProfileArcher({profileName}) a echoue : {ex.Message}");
        return -1;
      }
    }

    public static bool IsProfileAlt(string profileName)
    {
      if (Roster == null)
        return false;

      try
      {
        return Roster.IsProfileAlt(profileName);
      }
      catch (Exception ex)
      {
        Logger.Info($"[Profiles] IsProfileAlt({profileName}) a echoue : {ex.Message}");
        return false;
      }
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
        Logger.Info($"[Profiles] SetPlayerName({playerIndex}) a echoue : {ex.Message}");
      }
    }
  }
}

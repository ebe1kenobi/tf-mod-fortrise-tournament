using System;
using System.Collections.Generic;
using FortRise;
using TowerFall;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Modes de jeu proposes par le tournoi : les trois modes versus du jeu, puis ceux
  /// enregistres par les mods (Respawn, PlayTag, Speed Run...). Meme composition que
  /// le bouton de mode de l'ecran versus.
  ///
  /// L'identifiant retenu est un nom : "LastManStanding" pour un mode du jeu, la cle
  /// du registre pour un mode de mod. On ne sauvegarde pas la valeur numerique de
  /// Modes : FortRise l'attribue a l'execution, elle depend des mods presents et
  /// changerait d'un lancement a l'autre.
  /// </summary>
  public static class TournamentGameModes
  {
    private static readonly Modes[] BuiltIn =
    {
      Modes.LastManStanding,
      Modes.HeadHunters,
      Modes.TeamDeathmatch,
    };

    public const string Default = "LastManStanding";

    /// <summary>Identifiants disponibles, dans l'ordre d'affichage.</summary>
    public static List<string> GetNames()
    {
      var names = new List<string>();

      foreach (Modes mode in BuiltIn)
        names.Add(mode.ToString());

      if (GameModeRegistry.VersusGameModes != null)
      {
        foreach (IVersusGameModeEntry entry in GameModeRegistry.VersusGameModes)
        {
          if (entry != null && !string.IsNullOrEmpty(entry.Name))
            names.Add(entry.Name);
        }
      }

      return names;
    }

    /// <summary>Libelle lisible : "LAST MAN STANDING" plutot que "LastManStanding".</summary>
    public static string GetDisplay(string name)
    {
      if (string.IsNullOrEmpty(name))
        return "?";

      var sb = new System.Text.StringBuilder(name.Length + 6);
      for (int i = 0; i < name.Length; i++)
      {
        if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
          sb.Append(' ');
        sb.Append(char.ToUpperInvariant(name[i]));
      }
      return sb.ToString();
    }

    /// <summary>
    /// Applique le mode aux reglages du versus, comme le fait le bouton de mode du
    /// menu : IsCustom a false remet la logique de round du jeu, a true celle du mod.
    /// </summary>
    public static void Apply(string name)
    {
      var settings = MainMenu.VersusMatchSettings;
      if (settings == null)
        return;

      if (string.IsNullOrEmpty(name))
        name = Default;

      // Mode ajoute par un mod.
      if (GameModeRegistry.RegistryVersusGameModes != null
          && GameModeRegistry.RegistryVersusGameModes.ContainsKey(name))
      {
        settings.IsCustom = true;
        settings.Mode = GameModeRegistry.GetGameModeModes(name);
        // CustomVersusModeName a un setter internal : on passe par le registre, qui
        // est la source que MatchSettings relit ensuite.
        SetCustomModeName(settings, name);
        return;
      }

      // Mode du jeu.
      Modes parsed;
      if (!Enum.TryParse(name, out parsed) || !Enum.IsDefined(typeof(Modes), parsed))
        parsed = Modes.LastManStanding;

      settings.IsCustom = false;
      settings.Mode = parsed;
    }

    /// <summary>
    /// CustomVersusModeName n'est pas assignable depuis un mod (setter internal),
    /// d'ou la reflexion. Sans lui, MatchSettings.CustomVersusGameMode resterait sur
    /// le mode precedent et le RoundLogic du mod ne serait pas celui attendu.
    /// </summary>
    private static void SetCustomModeName(MatchSettings settings, string name)
    {
      try
      {
        var prop = typeof(MatchSettings).GetProperty("CustomVersusModeName");
        if (prop != null && prop.GetSetMethod(true) != null)
          prop.GetSetMethod(true).Invoke(settings, new object[] { name });
      }
      catch (Exception ex)
      {
        Logger.Info($"[Tournament] mode custom '{name}' non applique : {ex.Message}");
      }
    }
  }
}

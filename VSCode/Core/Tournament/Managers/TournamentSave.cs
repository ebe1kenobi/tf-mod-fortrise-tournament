using System;
using System.IO;
using System.Text.Json;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Sauvegarde / reprise d'un tournoi en cours (fichier JSON).
  /// </summary>
  public static class TournamentSave
  {
    // FortRise 4 ecrivait dans .\Mods\tf-mod-fortrise-poto\ (dossier d'un AUTRE mod,
    // vraisemblablement un copier-coller). FortRise 5 vit hors du repertoire de
    // TowerFall : on utilise l'espace de sauvegarde de CE mod.
    private static string SavePath =>
      Path.Combine(TFModFortRiseTournamentModule.SavePath, "tournament_save.json");

    /// <summary>
    /// Indique si une sauvegarde de tournoi en cours existe.
    /// </summary>
    public static bool Exists()
    {
      try
      {
        return File.Exists(SavePath);
      }
      catch (Exception ex)
      {
        Logger.Info($"TournamentSave.Exists error: {ex.Message}");
        return false;
      }
    }

    /// <summary>
    /// Écrit l'état du tournoi sur disque.
    /// </summary>
    public static void Save(TournamentData data)
    {
      if (data == null)
        return;

      try
      {
        string json = JsonSerializer.Serialize(data);

        string directory = Path.GetDirectoryName(SavePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
          Directory.CreateDirectory(directory);

        File.WriteAllText(SavePath, json);
        Logger.Info("Tournament saved");
      }
      catch (Exception ex)
      {
        Logger.Info($"TournamentSave.Save error: {ex.Message}");
      }
    }

    /// <summary>
    /// Charge le tournoi sauvegardé, ou null si absent / invalide / déjà terminé.
    /// </summary>
    public static TournamentData Load()
    {
      try
      {
        if (!File.Exists(SavePath))
          return null;

        string json = File.ReadAllText(SavePath);
        var data = JsonSerializer.Deserialize<TournamentData>(json);

        if (data == null || data.IsComplete || data.Bracket == null || data.Bracket.Count == 0)
          return null;

        Logger.Info("Tournament loaded from save");
        return data;
      }
      catch (Exception ex)
      {
        Logger.Info($"TournamentSave.Load error: {ex.Message}");
        return null;
      }
    }

    /// <summary>
    /// Supprime la sauvegarde.
    /// </summary>
    public static void Delete()
    {
      try
      {
        if (File.Exists(SavePath))
          File.Delete(SavePath);
      }
      catch (Exception ex)
      {
        Logger.Info($"TournamentSave.Delete error: {ex.Message}");
      }
    }
  }
}

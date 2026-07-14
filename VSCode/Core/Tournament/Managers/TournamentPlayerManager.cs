using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Gestionnaire pour charger et sauvegarder la liste des joueurs depuis/vers le fichier JSON
  /// </summary>
  public static class TournamentPlayerManager
  {
    private static readonly string JsonPath = @"C:\Program Files (x86)\Steam\steamapps\common\TowerFall\tournament_players.json";

    /// <summary>
    /// Structure pour la désérialisation du JSON
    /// </summary>
    private class PlayerListData
    {
      public List<string> players { get; set; }
    }

    /// <summary>
    /// Initialise le gestionnaire et crée le fichier par défaut s'il n'existe pas
    /// </summary>
    public static void Initialize()
    {
      try
      {
        if (!File.Exists(JsonPath))
        {
          Logger.Info($"Tournament players file not found, creating default: {JsonPath}");
          CreateDefaultFile();
        }
        else
        {
          Logger.Info($"Tournament players file found: {JsonPath}");
        }
      }
      catch (Exception ex)
      {
        Logger.Info($"Error initializing TournamentPlayerManager: {ex.Message}");
      }
    }

    /// <summary>
    /// Charge la liste des noms de joueurs depuis le fichier JSON
    /// </summary>
    /// <returns>Liste des noms de joueurs, ou null en cas d'erreur</returns>
    public static List<string> LoadPlayerNames()
    {
      try
      {
        if (!File.Exists(JsonPath))
        {
          Logger.Info($"Tournament players file not found: {JsonPath}");
          CreateDefaultFile();
        }

        string jsonContent = File.ReadAllText(JsonPath);
        var data = JsonConvert.DeserializeObject<PlayerListData>(jsonContent);

        if (data?.players == null || data.players.Count == 0)
        {
          Logger.Info("No players found in JSON file");
          return new List<string>();
        }

        // Nettoyer les noms (trim, uppercase)
        var cleanedNames = new List<string>();
        foreach (var name in data.players)
        {
          if (!string.IsNullOrWhiteSpace(name))
          {
            cleanedNames.Add(name.Trim().ToUpper());
          }
        }

        Logger.Info($"Loaded {cleanedNames.Count} player names from {JsonPath}");
        return cleanedNames;
      }
      catch (Exception ex)
      {
        Logger.Info($"Error loading player names: {ex.Message}");
        return null;
      }
    }

    /// <summary>
    /// Crée un fichier JSON par défaut avec des noms de joueurs
    /// </summary>
    public static void CreateDefaultFile()
    {
      try
      {
        var defaultData = new PlayerListData
        {
          players = new List<string>
          {
            "DAVID",
            "ERIC",
            "LOUIS",
            "ALEXANDRE",
            "JULIEN",
            "MEHDI",
            "BENOIT",
            "PLAYER8"
          }
        };

        string jsonContent = JsonConvert.SerializeObject(defaultData, Formatting.Indented);
        
        // Créer le répertoire si nécessaire
        string directory = Path.GetDirectoryName(JsonPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
          Directory.CreateDirectory(directory);
        }

        File.WriteAllText(JsonPath, jsonContent);
        Logger.Info($"Created default tournament players file: {JsonPath}");
      }
      catch (Exception ex)
      {
        Logger.Info($"Error creating default file: {ex.Message}");
      }
    }

    /// <summary>
    /// Vérifie si le fichier JSON existe
    /// </summary>
    public static bool FileExists()
    {
      return File.Exists(JsonPath);
    }

    /// <summary>
    /// Retourne le chemin du fichier JSON
    /// </summary>
    public static string GetFilePath()
    {
      return JsonPath;
    }

    /// <summary>
    /// Valide qu'il y a suffisamment de joueurs pour un tournoi
    /// </summary>
    /// <param name="playerCount">Nombre de joueurs</param>
    /// <returns>True si le nombre est valide (4 ou 8)</returns>
    public static bool IsValidPlayerCount(int playerCount)
    {
      return playerCount == 4 || playerCount == 8;
    }

    /// <summary>
    /// Retourne le nombre minimum de joueurs requis
    /// </summary>
    public static int GetMinimumPlayerCount()
    {
      return 4;
    }

    /// <summary>
    /// Retourne le nombre maximum de joueurs supportés
    /// </summary>
    public static int GetMaximumPlayerCount()
    {
      return 16;
    }
  }
}

// Made with Bob

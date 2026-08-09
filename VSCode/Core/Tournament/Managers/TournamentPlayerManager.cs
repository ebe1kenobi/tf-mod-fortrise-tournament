using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Gestionnaire pour charger et sauvegarder la liste des joueurs depuis/vers le fichier JSON
  /// </summary>
  public static class TournamentPlayerManager
  {
    // FortRise 4 pointait en dur sur le repertoire d'installation de TowerFall.
    // FortRise 5 est independant du jeu : on utilise l'espace de sauvegarde du mod.
    private static string JsonPath =>
      Path.Combine(TFModFortRiseTournamentModule.SavePath, "tournament_players.json");

    /// <summary>
    /// Structure pour la désérialisation du JSON
    /// </summary>
    private class PlayerListData
    {
      public List<string> players { get; set; }

      /// <summary>
      /// Source du roster : "JSON" ou "PROFILES". Absente des fichiers ecrits par les
      /// versions anterieures, ce qui vaut "JSON" - le comportement qu'ils avaient.
      /// </summary>
      public string source { get; set; }
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

        var data = ReadData();

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
    /// Ajoute un nom au fichier et le rend disponible pour les tournois suivants.
    /// Le nom est normalisé comme au chargement (trim + majuscules) pour éviter
    /// les doublons qui ne diffèrent que par la casse ou une espace.
    /// </summary>
    /// <returns>Le nom retenu, ou null si vide ou déjà présent.</returns>
    public static string AddPlayerName(string rawName)
    {
      if (string.IsNullOrWhiteSpace(rawName))
        return null;

      string name = rawName.Trim().ToUpper();

      var names = LoadPlayerNames() ?? new List<string>();
      if (names.Contains(name))
      {
        Logger.Info($"Player name already present: {name}");
        return null;
      }

      names.Add(name);
      if (!SavePlayerNames(names))
        return null;

      Logger.Info($"Player name added: {name}");
      return name;
    }

    /// <summary>
    /// Réécrit le fichier des joueurs, en conservant la source choisie : elle vit
    /// dans le même fichier et une réécriture de la liste ne doit pas l'effacer.
    /// </summary>
    public static bool SavePlayerNames(List<string> names)
    {
      var data = ReadData() ?? new PlayerListData();
      data.players = names;
      return WriteData(data);
    }

    /// <summary>
    /// Source du roster enregistrée. "JSON" par défaut : c'est ce que faisaient les
    /// versions sans ce réglage, et un fichier de noms déjà rempli doit continuer de
    /// servir sans que rien n'ait à être reconfiguré.
    /// </summary>
    public static string LoadSource()
    {
      var data = ReadData();
      return string.IsNullOrWhiteSpace(data?.source)
          ? TournamentRoster.SourceJson
          : data.source.Trim().ToUpper();
    }

    public static bool SaveSource(string source)
    {
      var data = ReadData() ?? new PlayerListData();
      data.source = source;
      return WriteData(data);
    }

    private static PlayerListData ReadData()
    {
      try
      {
        if (!File.Exists(JsonPath))
          return null;

        return JsonSerializer.Deserialize<PlayerListData>(File.ReadAllText(JsonPath));
      }
      catch (Exception ex)
      {
        Logger.Info($"Error reading tournament players file: {ex.Message}");
        return null;
      }
    }

    private static bool WriteData(PlayerListData data)
    {
      try
      {
        string directory = Path.GetDirectoryName(JsonPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
          Directory.CreateDirectory(directory);

        string jsonContent = JsonSerializer.Serialize(
            data,
            new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(JsonPath, jsonContent);
        return true;
      }
      catch (Exception ex)
      {
        Logger.Info($"Error saving tournament players file: {ex.Message}");
        return false;
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

          }
        };

        string jsonContent = JsonSerializer.Serialize(defaultData, new JsonSerializerOptions
        {
          WriteIndented = true
        });
        
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

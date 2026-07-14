using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Écran de configuration du tournoi FFA
  /// </summary>
  public class TournamentSettingsScene : Entity
  {
    private readonly List<string> selectedPlayers;
    private TournamentType selectedType;
    private TournamentMatchFormat selectedFormat;
    private int selectedGoal; // Nombre de rounds pour gagner un match
    private int selectedMapIndex; // -2 manuel, -1 aléatoire, >=0 tour fixe
    private int selectedOption; // 0=Type, 1=Format, 2=Goal, 3=Map
    private const int TotalOptions = 4;

    public static bool IsOpen { get; private set; }
    public static TournamentSettingsScene Instance { get; private set; }

    public TournamentSettingsScene(List<string> players)
    {
      selectedPlayers = players ?? new List<string>();
      selectedType = TournamentType.Elimination;
      selectedFormat = TournamentMatchFormat.Ffa2;
      selectedGoal = 3; // Par défaut 3 rounds pour gagner
      selectedMapIndex = TournamentMapMode.Manual;
      selectedOption = 0;
      Position = new Vector2(160f, 120f);
      
      Logger.Info($"TournamentSettingsScene created with {selectedPlayers.Count} players");
    }

    public override void Added()
    {
      base.Added();
      Instance = this;
      IsOpen = true;
      Sounds.ui_pause.Play(160f);
      Logger.Info("TournamentSettingsScene opened");
    }

    public override void Removed()
    {
      base.Removed();
      Instance = null;
      IsOpen = false;
      Sounds.ui_unpause.Play(160f);
      MenuInput.Clear();
      Logger.Info("TournamentSettingsScene closed");
    }

    public override void Update()
    {
      base.Update();
      MenuInput.Update();

      // Navigation haut/bas entre les options
      if (MenuInput.Up && selectedOption > 0)
      {
        selectedOption--;
        Sounds.ui_move1.Play(160f, 1f);
        return;
      }

      if (MenuInput.Down && selectedOption < TotalOptions - 1)
      {
        selectedOption++;
        Sounds.ui_move1.Play(160f, 1f);
        return;
      }

      // Modification des valeurs avec gauche/droite
      if (MenuInput.Left)
      {
        AdjustOption(-1);
        Sounds.ui_click.Play(160f, 1f);
        return;
      }

      if (MenuInput.Right)
      {
        AdjustOption(1);
        Sounds.ui_click.Play(160f, 1f);
        return;
      }

      // Retour à la sélection des joueurs
      if (MenuInput.Back)
      {
        Logger.Info("Returning to player selection");
        var playerSelectionScene = new TournamentPlayerSelectionScene(GetAllAvailablePlayers());
        Scene.Add(playerSelectionScene);
        RemoveSelf();
        return;
      }

      // Confirmer et générer le bracket
      if (MenuInput.Start || MenuInput.Confirm)
      {
        if (ValidateSettings())
        {
          Logger.Info("Settings confirmed, generating tournament");
          Sounds.ui_click.Play(160f, 1f);
          GenerateTournament();
        }
        else
        {
          Sounds.ui_invalid.Play(160f, 1f);
        }
        return;
      }
    }

    private void AdjustOption(int direction)
    {
      switch (selectedOption)
      {
        case 0: // Type de tournoi
          AdjustType(direction);
          break;
        case 1: // Format
          AdjustFormat(direction);
          break;
        case 2: // Goal
          AdjustGoal(direction);
          break;
        case 3: // Map
          AdjustMap(direction);
          break;
      }
    }

    private void AdjustMap(int direction)
    {
      int towerCount = (GameData.VersusTowers != null) ? GameData.VersusTowers.Count : 0;
      int min = TournamentMapMode.Manual;      // -2
      int max = towerCount - 1;                 // dernière tour fixe
      if (max < TournamentMapMode.Random)       // pas de tour connue -> au moins Manuel/Aléatoire
        max = TournamentMapMode.Random;         // -1

      selectedMapIndex += direction;
      if (selectedMapIndex < min)
        selectedMapIndex = max;
      else if (selectedMapIndex > max)
        selectedMapIndex = min;

      Logger.Info($"Map mode changed to: {selectedMapIndex}");
    }

    private void AdjustType(int direction)
    {
      var values = System.Enum.GetValues(typeof(TournamentType));
      int currentIndex = (int)selectedType;
      int newIndex = currentIndex + direction;

      if (newIndex < 0)
        newIndex = values.Length - 1;
      else if (newIndex >= values.Length)
        newIndex = 0;

      selectedType = (TournamentType)values.GetValue(newIndex);
      Logger.Info($"Tournament type changed to: {selectedType}");
    }

    private void AdjustFormat(int direction)
    {
      var values = System.Enum.GetValues(typeof(TournamentMatchFormat));
      int currentIndex = (int)selectedFormat;
      int newIndex = currentIndex + direction;

      if (newIndex < 0)
        newIndex = values.Length - 1;
      else if (newIndex >= values.Length)
        newIndex = 0;

      selectedFormat = (TournamentMatchFormat)values.GetValue(newIndex);
      Logger.Info($"Format changed to: {selectedFormat}");
    }

    private void AdjustGoal(int direction)
    {
      selectedGoal += direction;
      
      if (selectedGoal < 1)
        selectedGoal = 1;
      else if (selectedGoal > 9)
        selectedGoal = 9;

      Logger.Info($"Goal changed to: {selectedGoal}");
    }

    private bool ValidateSettings()
    {
      // On accepte n'importe quel nombre de joueurs maintenant !
      return selectedPlayers.Count >= 2;
    }

    private void GenerateTournament()
    {
      // Générer le bracket
      var bracket = TournamentBracket.GenerateBracket(selectedPlayers, selectedFormat, selectedType);

      // Créer les données du tournoi
      var tournamentData = new TournamentData
      {
        SelectedPlayerNames = new List<string>(selectedPlayers),
        Type = selectedType,
        Format = selectedFormat,
        Bracket = bracket,
        CurrentMatchIndex = 0,
        TotalRounds = TournamentBracket.GetTotalRounds(selectedPlayers.Count, selectedFormat),
        // Nombre réel de matchs du bracket (tient compte des byes / formats FFA 3-4).
        TotalMatches = bracket.Count,
        RoundsToWin = selectedGoal,
        MapMode = selectedMapIndex
      };

      Logger.Info($"Tournament Type: {selectedType}, Players per match: {TournamentBracket.GetPlayersPerMatch(selectedFormat)}, Goal: {selectedGoal}");

      // Démarrer la session de tournoi
      TournamentSession.StartTournament(tournamentData);

      // Naviguer vers l'écran du bracket
      var bracketScene = new TournamentBracketScene();
      Scene.Add(bracketScene);
      RemoveSelf();
    }

    private List<string> GetAllAvailablePlayers()
    {
      // Recharger la liste complète des joueurs depuis le JSON
      return TournamentPlayerManager.LoadPlayerNames();
    }

    public override void Render()
    {
      // Fond semi-transparent

      // Titre
      Draw.OutlineTextCentered(
        TFGame.Font, 
        "TOURNOI - CONFIGURATION FFA", 
        new Vector2(160f, 20f), 
        Color.White, 
        1.5f
      );

      // Récapitulatif
      int totalMatches = TournamentBracket.CountTotalMatches(selectedPlayers.Count, selectedFormat, selectedType);
      string summary = $"{selectedPlayers.Count} JOUEURS - {totalMatches} MATCHS";
      Draw.TextCentered(
        TFGame.Font,
        summary,
        new Vector2(160f, 40f),
        Calc.HexToColor("5EFF5E")
      );

      // Options
      RenderOptions();

      // Instructions
      RenderInstructions();
    }

    private void RenderOptions()
    {
      float startY = 60f;
      float lineHeight = 26f;

      // Option 0: Type de tournoi
      RenderOption(
        0,
        "MODE DE TOURNOI:",
        GetTypeDisplay(),
        new Vector2(160f, startY)
      );

      // Option 1: Format
      RenderOption(
        1,
        "FORMAT FFA:",
        GetFormatDisplay(),
        new Vector2(160f, startY + lineHeight)
      );

      // Option 2: Goal
      RenderOption(
        2,
        "ROUNDS POUR GAGNER:",
        selectedGoal.ToString(),
        new Vector2(160f, startY + lineHeight * 2)
      );

      // Option 3: Map
      RenderOption(
        3,
        "MAP:",
        GetMapDisplay(),
        new Vector2(160f, startY + lineHeight * 3)
      );
    }

    private string GetTypeDisplay()
    {
      switch (selectedType)
      {
        case TournamentType.Elimination:
          return "ELIMINATION";
        case TournamentType.RoundRobin:
          return "ROUND-ROBIN";
        case TournamentType.KingOfTheHill:
          return "KING OF HILL";
        default:
          return "ELIMINATION";
      }
    }

    private string GetMapDisplay()
    {
      if (selectedMapIndex == TournamentMapMode.Manual)
        return "MANUEL";
      if (selectedMapIndex == TournamentMapMode.Random)
        return "ALEATOIRE";
      return "MAP " + (selectedMapIndex + 1);
    }

    private void RenderOption(int optionIndex, string label, string value, Vector2 position)
    {
      bool isSelected = optionIndex == selectedOption;
      Color labelColor = isSelected ? Calc.HexToColor("FFEC5E") : Color.White;
      Color valueColor = isSelected ? Calc.HexToColor("5EFF5E") : Color.Gray;

      if (isSelected)
      {
        Draw.Rect(position.X - 140f, position.Y - 5f, 280f, 20f, Calc.HexToColor("F87858") * 0.3f);
        Draw.Text(TFGame.Font, "< ", new Vector2(position.X - 130f, position.Y), labelColor);
        Draw.Text(TFGame.Font, " >", new Vector2(position.X + 120f, position.Y), labelColor);
      }

      Draw.Text(TFGame.Font, label, new Vector2(position.X - 130f, position.Y), labelColor);
      Draw.Text(TFGame.Font, value, new Vector2(position.X + 20f, position.Y), valueColor);
    }

    private string GetFormatDisplay()
    {
      switch (selectedFormat)
      {
        case TournamentMatchFormat.Ffa2:
          return "2 JOUEURS";
        case TournamentMatchFormat.Ffa3:
          return "3 JOUEURS";
        case TournamentMatchFormat.Ffa4:
          return "4 JOUEURS";
        default:
          return "2 JOUEURS";
      }
    }

    private void RenderInstructions()
    {
      float y = 180f;

      Draw.TextCentered(
        TFGame.Font, 
        "HAUT/BAS: OPTION", 
        new Vector2(160f, y), 
        Color.Gray
      );

      Draw.TextCentered(
        TFGame.Font, 
        "GAUCHE/DROITE: MODIFIER", 
        new Vector2(160f, y + 12f), 
        Color.Gray
      );

      Draw.OutlineTextCentered(
        TFGame.Font, 
        "START: COMMENCER LE TOURNOI", 
        new Vector2(160f, y + 30f), 
        Calc.HexToColor("5EFF5E"), 
        1f
      );

      Draw.TextCentered(
        TFGame.Font, 
        "SELECT: RETOUR", 
        new Vector2(160f, y + 42f), 
        Color.Gray
      );
    }
  }
}

// Made with Bob

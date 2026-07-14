using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Écran de sélection des joueurs pour le tournoi FFA
  /// </summary>
  public class TournamentPlayerSelectionScene : Entity
  {
    private readonly List<string> availablePlayers;
    private readonly List<string> selectedPlayers;
    private int selectedIndex;
    private int scrollOffset;
    private const int MaxVisiblePlayers = 8;
    private const int MinPlayers = 2;
    private const int MaxPlayers = 16;

    public static bool IsOpen { get; private set; }
    public static TournamentPlayerSelectionScene Instance { get; private set; }

    public TournamentPlayerSelectionScene(List<string> playerNames)
    {
      availablePlayers = playerNames ?? new List<string>();
      selectedPlayers = new List<string>();
      selectedIndex = 0;
      scrollOffset = 0;
      Position = new Vector2(160f, 120f);

      Logger.Info($"TournamentPlayerSelectionScene created with {availablePlayers.Count} available players");
    }

    public override void Added()
    {
      base.Added();
      Instance = this;
      IsOpen = true;
      Sounds.ui_pause.Play(160f);
      Logger.Info("TournamentPlayerSelectionScene opened");
    }

    public override void Removed()
    {
      base.Removed();
      Instance = null;
      IsOpen = false;
      MenuInput.Clear();
      Logger.Info("TournamentPlayerSelectionScene closed");
    }

    public override void Update()
    {
      base.Update();
      MenuInput.Update();

      if (availablePlayers.Count == 0)
      {
        Logger.Info("No players available, closing scene");
        TournamentScene.ExitToMainMenu();
        return;
      }

      if (MenuInput.Up && selectedIndex > 0)
      {
        selectedIndex--;
        AdjustScroll();
        Sounds.ui_move1.Play(160f, 1f);
        return;
      }

      if (MenuInput.Down && selectedIndex < availablePlayers.Count - 1)
      {
        selectedIndex++;
        AdjustScroll();
        Sounds.ui_move1.Play(160f, 1f);
        return;
      }

      if (MenuInput.Confirm)
      {
        string playerToAdd = availablePlayers[selectedIndex];

        if (!selectedPlayers.Contains(playerToAdd) && selectedPlayers.Count < MaxPlayers)
        {
          selectedPlayers.Add(playerToAdd);
          Sounds.ui_click.Play(160f, 1f);
          Logger.Info($"Added player: {playerToAdd} (Total: {selectedPlayers.Count})");
        }
        else if (selectedPlayers.Contains(playerToAdd))
        {
          Sounds.ui_invalid.Play(160f, 1f);
          Logger.Info($"Player {playerToAdd} already selected");
        }
        else
        {
          Sounds.ui_invalid.Play(160f, 1f);
          Logger.Info($"Maximum {MaxPlayers} players reached");
        }
        return;
      }

      if (MenuInput.Back && selectedPlayers.Count > 0)
      {
        string removedPlayer = selectedPlayers[selectedPlayers.Count - 1];
        selectedPlayers.RemoveAt(selectedPlayers.Count - 1);
        Sounds.ui_click.Play(160f, 1f);
        Logger.Info($"Removed player: {removedPlayer} (Total: {selectedPlayers.Count})");
        return;
      }

      if (MenuInput.Back && selectedPlayers.Count == 0)
      {
        Logger.Info("Cancelled player selection");
        TournamentScene.ExitToMainMenu();
        return;
      }

      if (MenuInput.Start)
      {
        if (selectedPlayers.Count >= MinPlayers && selectedPlayers.Count <= MaxPlayers)
        {
          Logger.Info($"Proceeding to settings with {selectedPlayers.Count} players");
          Sounds.ui_click.Play(160f, 1f);
          ProceedToSettings();
        }
        else
        {
          Sounds.ui_invalid.Play(160f, 1f);
          Logger.Info($"Invalid player count: {selectedPlayers.Count} (need {MinPlayers} to {MaxPlayers})");
        }
        return;
      }
    }

    private void AdjustScroll()
    {
      if (selectedIndex < scrollOffset)
      {
        scrollOffset = selectedIndex;
      }
      else if (selectedIndex >= scrollOffset + MaxVisiblePlayers)
      {
        scrollOffset = selectedIndex - MaxVisiblePlayers + 1;
      }
    }

    private void ProceedToSettings()
    {
      var settingsScene = new TournamentSettingsScene(selectedPlayers);
      Scene.Add(settingsScene);
      RemoveSelf();
    }

    public override void Render()
    {

      Draw.OutlineTextCentered(
        TFGame.Font,
        "TOURNOI FFA - SELECTION JOUEURS",
        new Vector2(160f, 20f),
        Color.White,
        1.5f
      );

      Draw.TextCentered(
        TFGame.Font,
        "HAUT/BAS: NAVIGUER  A: AJOUTER  B: RETIRER",
        new Vector2(160f, 35f),
        Color.Gray
      );

      Color counterColor = GetCounterColor();
      string counterText = $"{selectedPlayers.Count}/{MaxPlayers} JOUEURS";
      Draw.OutlineTextCentered(
        TFGame.Font,
        counterText,
        new Vector2(160f, 50f),
        counterColor,
        1f
      );

      RenderAvailablePlayers();
      RenderSelectedPlayers();
      RenderBottomInstructions();
    }

    private void RenderAvailablePlayers()
    {
      float startY = 70f;
      float lineHeight = 12f;

      Draw.OutlineTextCentered(
        TFGame.Font,
        "DISPONIBLES",
        new Vector2(80f, startY),
        Calc.HexToColor("5EFF5E"),
        1f
      );

      int endIndex = System.Math.Min(scrollOffset + MaxVisiblePlayers, availablePlayers.Count);

      for (int i = scrollOffset; i < endIndex; i++)
      {
        float y = startY + 15f + (i - scrollOffset) * lineHeight;
        bool isSelected = i == selectedIndex;
        bool isAlreadyAdded = selectedPlayers.Contains(availablePlayers[i]);

        Color textColor = isAlreadyAdded ? Color.Gray : Color.White;
        string prefix = isSelected ? "> " : "  ";

        if (isSelected)
        {
          Draw.Rect(20f, y - 2f, 120f, lineHeight, Calc.HexToColor("F87858") * 0.3f);
        }

        Draw.Text(
          TFGame.Font,
          TournamentText.Safe(prefix + availablePlayers[i]),
          new Vector2(25f, y),
          textColor
        );
      }

      if (scrollOffset > 0)
      {
        Draw.TextCentered(TFGame.Font, "^", new Vector2(80f, startY + 12f), Color.White);
      }
      if (endIndex < availablePlayers.Count)
      {
        Draw.TextCentered(TFGame.Font, "v", new Vector2(80f, startY + 15f + MaxVisiblePlayers * lineHeight), Color.White);
      }
    }

    private void RenderSelectedPlayers()
    {
      float startY = 70f;
      float lineHeight = 12f;

      Draw.OutlineTextCentered(
        TFGame.Font,
        "SELECTIONNES",
        new Vector2(240f, startY),
        Calc.HexToColor("FFEC5E"),
        1f
      );

      if (selectedPlayers.Count == 0)
      {
        Draw.TextCentered(
          TFGame.Font,
          "(vide)",
          new Vector2(240f, startY + 30f),
          Color.Gray
        );
      }
      else
      {
        for (int i = 0; i < selectedPlayers.Count; i++)
        {
          float y = startY + 15f + i * lineHeight;

          Draw.Text(
            TFGame.Font,
            TournamentText.Safe($"{i + 1}. {selectedPlayers[i]}"),
            new Vector2(180f, y),
            Color.White
          );
        }
      }
    }

    private void RenderBottomInstructions()
    {
      float y = 210f;

      if (selectedPlayers.Count >= MinPlayers)
      {
        Draw.OutlineTextCentered(
          TFGame.Font,
          "START: CONTINUER",
          new Vector2(160f, y),
          Calc.HexToColor("5EFF5E"),
          1f
        );
      }
      else
      {
        Draw.TextCentered(
          TFGame.Font,
          $"SELECTIONNEZ {MinPlayers} A {MaxPlayers} JOUEURS",
          new Vector2(160f, y),
          Color.Gray
        );
      }

      Draw.TextCentered(
        TFGame.Font,
        "SELECT: ANNULER",
        new Vector2(160f, y + 12f),
        Color.Gray
      );
    }

    private Color GetCounterColor()
    {
      if (selectedPlayers.Count >= MinPlayers)
      {
        return Calc.HexToColor("5EFF5E");
      }
      else if (selectedPlayers.Count > 0)
      {
        return Calc.HexToColor("FFEC5E");
      }
      else
      {
        return Color.Gray;
      }
    }
  }
}

// Made with Bob

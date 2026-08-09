using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using TowerFall;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Écran de sélection des joueurs pour le tournoi FFA
  /// </summary>
  public class TournamentPlayerSelectionScene : Entity
  {
    private List<string> availablePlayers;
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

      // Tant que le clavier virtuel est ouvert, il capte tout : sans cela les
      // memes touches piloteraient aussi cette liste en arriere-plan.
      if (TournamentNameKeyboard.IsOpen)
        return;

      MenuInput.Update();

      // Gauche/droite : changer de source. Place avant tout le reste, y compris
      // avant le cas de la liste vide - c'est justement quand une source ne donne
      // rien qu'il faut pouvoir passer a l'autre.
      if (TournamentRoster.CanChooseSource && (MenuInput.Left || MenuInput.Right))
      {
        SwitchSource();
        return;
      }

      // Y (RB au clavier via MenuAlt) : ajouter un joueur au fichier. Disponible
      // meme quand la liste est vide, sinon un fichier absent ou trop court
      // laisserait l'ecran sans aucune issue. Sans objet sur la source Profiles,
      // ou les noms viennent de profils crees dans leur propre menu.
      if (TournamentRoster.CanAddName && AddNamePressed())
      {
        OpenNameKeyboard();
        return;
      }

      if (availablePlayers.Count == 0)
      {
        // Plus de fermeture automatique : on laisse l'ecran ouvert pour permettre
        // la saisie des premiers noms. B reste la sortie.
        if (MenuInput.Back)
        {
          Logger.Info("No players available, leaving tournament");
          TournamentScene.ExitToMainMenu();
        }
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

    /// <summary>
    /// Detection de la touche Y. PlayerInput n'expose pas Y (seulement Confirm,
    /// Back, Alt, Alt2 et Start), on lit donc le bouton directement sur la manette.
    /// Au clavier, c'est la touche Y elle-meme ; MenuAlt (Tab / RB) marche aussi,
    /// pour rester coherent avec le reste des menus du jeu.
    /// </summary>
    private static bool AddNamePressed()
    {
      for (int i = 0; i < TFGame.PlayerInputs.Length; i++)
      {
        PlayerInput input = TFGame.PlayerInputs[i];
        if (input == null || !input.Attached) continue;

        var pad = input as XGamepadInput;
        if (pad != null && pad.XGamepad != null && pad.XGamepad.Pressed(Buttons.Y))
          return true;

        if (input.MenuAlt)
          return true;
      }

      return MInput.Keyboard != null && MInput.Keyboard.Pressed(Keys.Y);
    }

    /// <summary>
    /// Bascule entre le fichier de noms et les profils, et recharge la liste.
    ///
    /// Les joueurs deja retenus sont conserves : un tournoi se compose de noms, d'ou
    /// qu'ils viennent, et les perdre sur une touche pressee par curiosite serait la
    /// pire des reponses. Rien n'empeche donc de composer une grille en puisant dans
    /// les deux sources.
    /// </summary>
    private void SwitchSource()
    {
      TournamentRoster.ToggleSource();
      availablePlayers = TournamentRoster.Load();

      selectedIndex = 0;
      scrollOffset = 0;
      Sounds.ui_move1.Play(160f, 1f);
      Logger.Info($"Source: {TournamentRoster.EffectiveSource} ({availablePlayers.Count} noms)");
    }

    private void OpenNameKeyboard()
    {
      var keyboard = new TournamentNameKeyboard(
          availablePlayers,
          OnNameValidated,
          null);

      Scene.Add(keyboard);
    }

    /// <summary>
    /// Ecrit le nom dans tournament_players.json et l'ajoute a la liste affichee.
    /// Rend false si l'enregistrement echoue, pour que le clavier le signale au
    /// lieu de se fermer en laissant croire que c'est fait.
    /// </summary>
    private bool OnNameValidated(string name)
    {
      string added = TournamentPlayerManager.AddPlayerName(name);
      if (added == null)
        return false;

      availablePlayers.Add(added);
      selectedIndex = availablePlayers.Count - 1;
      AdjustScroll();
      Logger.Info($"Nouveau joueur ajoute au fichier : {added}");
      return true;
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
        "FFA TOURNAMENT - PLAYERS SELECTION",
        new Vector2(160f, 20f),
        Color.White,
        1.5f
      );

      Draw.TextCentered(
        TFGame.Font,
        TournamentRoster.CanAddName
            ? "UP/DOWN: NAVIGATE  A: ADD  B: REMOVE  Y: NEW NAME"
            : "UP/DOWN: NAVIGATE  A: ADD  B: REMOVE",
        new Vector2(160f, 35f),
        Color.Gray
      );

      Color counterColor = GetCounterColor();
      string counterText = $"{selectedPlayers.Count}/{MaxPlayers} PLAYERS";
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

      // La colonne porte le nom de sa source plutot qu'un "AVAILABLE" generique :
      // c'est le seul endroit ou l'on voit d'ou sortent ces noms, et le seul ou
      // l'on puisse en changer.
      Draw.OutlineTextCentered(
        TFGame.Font,
        TournamentRoster.CanChooseSource
            ? "< " + TournamentRoster.Label + " >"
            : TournamentRoster.Label,
        new Vector2(80f, startY),
        Calc.HexToColor("5EFF5E"),
        1f
      );

      // Liste vide : on oriente vers la creation plutot que de laisser un blanc. La
      // marche a suivre depend de la source, un profil ne se cree pas ici.
      if (availablePlayers.Count == 0)
      {
        Draw.TextCentered(TFGame.Font, "NO PLAYERS",
            new Vector2(80f, startY + 20f), Color.Gray);
        Draw.TextCentered(TFGame.Font, TournamentText.Safe(TournamentRoster.EmptyHint),
            new Vector2(80f, startY + 34f), Calc.HexToColor("FFEC5E"));
        return;
      }

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
        "SELECTED",
        new Vector2(240f, startY),
        Calc.HexToColor("FFEC5E"),
        1f
      );

      if (selectedPlayers.Count == 0)
      {
        Draw.TextCentered(
          TFGame.Font,
          "(EMPTY)",
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
          "START: CONTINUE",
          new Vector2(160f, y),
          Calc.HexToColor("5EFF5E"),
          1f
        );
      }
      else
      {
        Draw.TextCentered(
          TFGame.Font,
          $"SELECT {MinPlayers} A {MaxPlayers} PLAYERS",
          new Vector2(160f, y),
          Color.Gray
        );
      }

      Draw.TextCentered(
        TFGame.Font,
        TournamentRoster.CanChooseSource
            ? "X: CANCEL   LEFT/RIGHT: SOURCE"
            : "X: CANCEL",
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

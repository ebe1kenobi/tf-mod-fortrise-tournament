using System;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Écran d'annonce avant chaque match du tournoi FFA
  /// </summary>
  public class TournamentMatchIntroScene : Entity
  {
    private TournamentSession session;
    private TournamentMatch currentMatch;
    private float displayTimer;
    private const float DisplayDuration = 4f;
    private float animationProgress;

    public static bool IsOpen { get; private set; }
    public static TournamentMatchIntroScene Instance { get; private set; }

    public TournamentMatchIntroScene()
    {
      session = TournamentSession.Current;
      displayTimer = 0f;
      animationProgress = 0f;
      Position = new Vector2(160f, 120f);

      if (session == null || !session.IsActive)
      {
        Logger.Info("Error: No active tournament session");
      }
      else
      {
        currentMatch = session.GetCurrentMatch();
        if (currentMatch != null)
        {
          Logger.Info($"TournamentMatchIntroScene created for Match {currentMatch.MatchIndex}");
        }
        else
        {
          Logger.Info("Error: No current match");
        }
      }
    }

    public override void Added()
    {
      base.Added();
      Instance = this;
      IsOpen = true;
      Sounds.ui_pause.Play(160f);
      Logger.Info("TournamentMatchIntroScene opened");
    }

    public override void Removed()
    {
      base.Removed();
      Instance = null;
      IsOpen = false;
      MenuInput.Clear();
      Logger.Info("TournamentMatchIntroScene closed");
    }

    public override void Update()
    {
      base.Update();
      displayTimer += Engine.DeltaTime;
      animationProgress = Math.Min(displayTimer / 1f, 1f);

      if (session == null || !session.IsActive || currentMatch == null)
      {
        Logger.Info("Invalid state, returning to bracket");
        ReturnToBracket();
        return;
      }

      if (displayTimer >= DisplayDuration)
      {
        Logger.Info("Intro complete, proceeding to map selection");
        ProceedToMapSelection();
        return;
      }

      MenuInput.Update();
      if (MenuInput.Start || MenuInput.Confirm)
      {
        Logger.Info("Intro skipped by player");
        ProceedToMapSelection();
        return;
      }
    }

    private void ProceedToMapSelection()
    {
      Logger.Info("Proceeding to map selection for tournament match");

      if (session == null || !session.IsActive || currentMatch == null)
      {
        Logger.Info("Invalid session, returning to bracket");
        ReturnToBracket();
        return;
      }

      try
      {
        ConfigurePlayersForMatch();
        ApplyTournamentMatchSettings();

        // IMPORTANT : on change de scène via Engine.Scene sans RemoveSelf().
        // Monocle n'appelle PAS Removed() lors d'un changement de scène, donc on
        // remet nous-mêmes à zéro les flags statiques.
        Instance = null;
        IsOpen = false;

        int mapMode = session.Data.MapMode;
        bool towersAvailable = GameData.VersusTowers != null && GameData.VersusTowers.Count > 0;

        if (mapMode == TournamentMapMode.Manual || !towersAvailable)
        {
          // Sélection manuelle de la map (écran habituel, chemin compatible avec les
          // mods qui patchent MapScene.StartSession, comme WiderSetMod).
          OpenMapScene();
        }
        else
        {
          // Aléatoire ou map fixe : lancement direct. Si ça échoue (ex: interaction
          // avec un mod qui patche le chargement de niveau comme WiderSetMod), on
          // retombe sur l'écran de map (chemin compatible) plutôt que sur le bracket.
          try
          {
            LaunchMatchDirectly(mapMode);
          }
          catch (Exception exDirect)
          {
            Logger.Info($"Direct match launch failed, falling back to map selection: {exDirect}");
            OpenMapScene();
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Info($"Error launching tournament match: {ex}");
        ReturnToBracket();
      }
    }

    private void OpenMapScene()
    {
      var mapScene = new MapScene(MainMenu.RollcallModes.Versus);
      Engine.Instance.Scene = mapScene;
      Logger.Info("MapScene created and set as current scene");
    }

    /// <summary>
    /// Lance le match sans passer par l'écran de sélection : choisit la tour
    /// (aléatoire ou fixe), fixe une graine, et démarre la session versus.
    /// </summary>
    private void LaunchMatchDirectly(int mapMode)
    {
      var settings = MainMenu.VersusMatchSettings;

      int towerIndex;
      if (mapMode == TournamentMapMode.Random)
        towerIndex = new Random().Next(GameData.VersusTowers.Count);
      else
        towerIndex = mapMode;

      if (towerIndex < 0 || towerIndex >= GameData.VersusTowers.Count)
        towerIndex = 0;

      settings.LevelSystem = GameData.VersusTowers[towerIndex].GetLevelSystem();
      settings.RandomVersusTower = false;
      settings.RandomLevelSeed = new Random().Next(1000000000);

      MainMenu.CurrentMatchSettings = settings;

      Logger.Info($"Launching tournament match directly on tower {towerIndex}");
      new Session(settings).StartGame();
    }

    private void ConfigurePlayersForMatch()
    {
      if (currentMatch == null)
      {
        Logger.Info("Error: No current match to configure");
        return;
      }

      for (int i = 0; i < 4; i++)
      {
        TFGame.Players[i] = false;
        TFGame.Characters[i] = i;
      }

      int playerIndex = 0;
      foreach (var playerName in currentMatch.Players)
      {
        if (playerIndex < 4 && playerName != "?")
        {
          TFGame.Players[playerIndex] = true;

          // L'application du nom custom est optionnelle : elle peut planter selon les
          // autres mods actifs (ex: WiderSetMod modifie le Rollcall et fait planter le
          // SetPlayerName du mod CustomName). On ne doit surtout pas empêcher le match
          // de se lancer pour ça -> try/catch, on continue même si le nom n'est pas posé.
          if (CustomNameImport.SetPlayerName != null)
          {
            try
            {
              CustomNameImport.SetPlayerName(playerIndex, playerName);
            }
            catch (Exception exName)
            {
              Logger.Info($"SetPlayerName failed for player {playerIndex} ({playerName}): {exName.Message}");
            }
          }

          playerIndex++;
        }
      }

      Logger.Info($"Configured FFA match with {playerIndex} players");
    }

    /// <summary>
    /// Force le match du tournoi à utiliser exactement le nombre de rounds
    /// choisi dans les settings (un seul match, GoalScore = RoundsToWin).
    /// </summary>
    private void ApplyTournamentMatchSettings()
    {
      if (session == null || session.Data == null)
        return;

      var settings = MainMenu.VersusMatchSettings;
      if (settings == null)
      {
        Logger.Info("Error: VersusMatchSettings is null, cannot apply tournament goal");
        return;
      }

      int roundsToWin = session.Data.RoundsToWin;
      if (roundsToWin < 1)
        roundsToWin = 1;

      // MatchLength.Custom + CustomGoal => GoalScore renvoie exactement CustomGoal
      settings.MatchLength = MatchSettings.MatchLengths.Custom;
      MatchSettings.CustomGoal = roundsToWin;

      Logger.Info($"Tournament match settings applied: GoalScore = {roundsToWin} (Custom)");
    }

    private void ReturnToBracket()
    {
      var bracketScene = new TournamentBracketScene();
      Scene.Add(bracketScene);
      RemoveSelf();
    }

    public override void Render()
    {

      if (session == null || !session.IsActive || currentMatch == null)
      {
        Draw.TextCentered(TFGame.Font, "ERREUR", new Vector2(160f, 120f), Color.Red);
        return;
      }

      float alpha = Calc.Clamp(animationProgress, 0f, 1f);

      string matchInfo = $"MATCH {currentMatch.MatchIndex + 1}/{session.GetTotalMatchCount()}";
      Draw.OutlineTextCentered(
        TFGame.Font,
        matchInfo,
        new Vector2(160f, 30f),
        Color.White * alpha,
        1f
      );

      string roundName = currentMatch.GetRoundName();
      Draw.OutlineTextCentered(
        TFGame.Font,
        TournamentText.Safe(roundName),
        new Vector2(160f, 45f),
        Calc.HexToColor("FFEC5E") * alpha,
        1.5f
      );

      string format = session.Data.GetFormatDisplay();
      Draw.TextCentered(
        TFGame.Font,
        format,
        new Vector2(160f, 65f),
        Calc.HexToColor("5EFF5E") * alpha
      );

      RenderPlayers(new Vector2(160f, 110f), alpha);

      if (displayTimer > 2f)
      {
        float blinkAlpha = (float)Math.Sin(displayTimer * 8f) * 0.5f + 0.5f;
        Draw.OutlineTextCentered(
          TFGame.Font,
          "GET READY!",
          new Vector2(160f, 190f),
          Color.White * alpha * blinkAlpha,
          1.5f
        );
      }

      if (displayTimer > 1f)
      {
        Draw.TextCentered(
          TFGame.Font,
          "START: PASS",
          new Vector2(160f, 220f),
          Color.Gray * alpha * 0.7f
        );
      }

      float progressWidth = 200f;
      float progressHeight = 3f;
      float progressX = 60f;
      float progressY = 230f;
      float progress = displayTimer / DisplayDuration;
      Draw.Rect(progressX, progressY, progressWidth, progressHeight, Color.Gray * 0.3f * alpha);
      Draw.Rect(progressX, progressY, progressWidth * progress, progressHeight, Calc.HexToColor("5EFF5E") * alpha);
    }

    private void RenderPlayers(Vector2 centerPosition, float alpha)
    {
      if (currentMatch.Players == null || currentMatch.Players.Count == 0)
      {
        Draw.TextCentered(TFGame.Font, "?", centerPosition, Color.Gray * alpha);
        return;
      }

      float startY = centerPosition.Y - ((currentMatch.Players.Count - 1) * 12f);
      Color[] colors = { Calc.HexToColor("5E9FFF"), Calc.HexToColor("FF5E5E"), Calc.HexToColor("5EFF5E"), Calc.HexToColor("FFEC5E") };

      for (int i = 0; i < currentMatch.Players.Count; i++)
      {
        string playerName = currentMatch.Players[i];
        if (playerName == "?")
          continue;

        float offset = (1f - animationProgress) * 50f * (i % 2 == 0 ? -1f : 1f);
        Vector2 playerPos = new Vector2(centerPosition.X + offset, startY + i * 24f);

        Draw.OutlineTextCentered(
          TFGame.Font,
          TournamentText.Safe(playerName),
          playerPos,
          colors[i % colors.Length] * alpha,
          1.2f
        );
      }
    }
  }
}

// Made with Bob

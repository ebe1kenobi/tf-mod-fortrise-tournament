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
        Logger.Info("Intro complete, proceeding to controller setup");
        ProceedToControllerSetup();
        return;
      }

      MenuInput.Update();
      if (MenuInput.Start || MenuInput.Confirm)
      {
        Logger.Info("Intro skipped by player");
        ProceedToControllerSetup();
        return;
      }
    }

    /// <summary>
    /// Enchaine sur l'ecran d'assignation des manettes, qui configure les joueurs
    /// puis lance le match. La configuration elle-meme vit dans
    /// TournamentMatchLauncher, partagee entre les deux ecrans.
    /// </summary>
    private void ProceedToControllerSetup()
    {
      if (session == null || !session.IsActive || currentMatch == null)
      {
        Logger.Info("Invalid session, returning to bracket");
        ReturnToBracket();
        return;
      }

      Scene.Add(new TournamentControllerSetupScene());
      RemoveSelf();
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

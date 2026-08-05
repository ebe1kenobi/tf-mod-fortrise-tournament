using System;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Écran de résultats après un match du tournoi
  /// </summary>
  public class TournamentMatchResultsScene : Entity
  {
    private TournamentSession session;
    private TournamentMatch completedMatch;
    private string winner;
    private float animationTimer;
    private bool canContinue;

    public static bool IsOpen { get; private set; }
    public static TournamentMatchResultsScene Instance { get; private set; }

    public TournamentMatchResultsScene(string matchWinner)
    {
      session = TournamentSession.Current;
      winner = matchWinner;
      animationTimer = 0f;
      canContinue = false;
      Position = new Vector2(160f, 120f);

      if (session == null || !session.IsActive)
      {
        Logger.Info("Error: No active tournament session");
      }
      else
      {
        completedMatch = session.GetCurrentMatch();
        if (completedMatch != null)
        {
          Logger.Info($"TournamentMatchResultsScene created for Match {completedMatch.MatchIndex}");
          Logger.Info($"Winner: {winner}");
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
      Logger.Info("TournamentMatchResultsScene opened");
    }

    public override void Removed()
    {
      base.Removed();
      Instance = null;
      IsOpen = false;
      MenuInput.Clear();
      Logger.Info("TournamentMatchResultsScene closed");
    }

    public override void Update()
    {
      base.Update();
      animationTimer += Engine.DeltaTime;

      // Permettre de continuer après 2 secondes
      if (animationTimer > 2f)
      {
        canContinue = true;
      }

      if (session == null || !session.IsActive)
      {
        Logger.Info("Invalid state, returning to menu");
        TournamentScene.ExitToMainMenu();
        return;
      }

      MenuInput.Update();

      if (canContinue && (MenuInput.Start || MenuInput.Confirm))
      {
        Logger.Info("Proceeding to next match or bracket");
        Sounds.ui_click.Play(160f, 1f);
        ProceedToNext();
        return;
      }
    }

    private void ProceedToNext()
    {
      if (session.IsTournamentComplete())
      {
        Logger.Info("Tournament complete, showing final bracket");
      }
      else
      {
        Logger.Info("Proceeding to next match");
      }

      // Retourner au bracket
      var bracketScene = new TournamentBracketScene();
      Scene.Add(bracketScene);
      RemoveSelf();
    }

    public override void Render()
    {

      if (session == null || !session.IsActive)
      {
        Draw.TextCentered(TFGame.Font, "ERREUR", new Vector2(160f, 120f), Color.Red);
        return;
      }

      // Animation de fade in
      float alpha = Math.Min(animationTimer / 1f, 1f);

      // Page dédiée quand le tournoi est terminé (grand WINNER + classement),
      // sinon résultat du match qui vient d'être joué.
      if (session.IsTournamentComplete())
      {
        RenderChampionPage(alpha);
      }
      else
      {
        RenderMatchResult(alpha);
      }

      // Instructions
      if (canContinue)
      {
        float blinkAlpha = (float)Math.Sin(animationTimer * 4f) * 0.3f + 0.7f;
        string hint = session.IsTournamentComplete() ? "START: MENU" : "START: CONTINUER";
        Draw.OutlineTextCentered(
          TFGame.Font,
          hint,
          new Vector2(160f, 228f),
          Calc.HexToColor("5EFF5E") * alpha * blinkAlpha,
          1f
        );
      }
    }

    private void RenderMatchResult(float alpha)
    {
      // Titre "WINNER!" avec animation
      float titleScale = 1f + (float)Math.Sin(animationTimer * 3f) * 0.2f;
      Draw.OutlineTextCentered(
        TFGame.Font,
        "WINNER!",
        new Vector2(160f, 40f),
        Calc.HexToColor("FFD700") * alpha,
        titleScale * 2f
      );

      // Nom du gagnant du match
      Draw.OutlineTextCentered(
        TFGame.Font,
        TournamentText.Safe(winner),
        new Vector2(160f, 70f),
        Color.White * alpha,
        1.8f
      );

      // Mini bracket mis à jour
      RenderMiniBracket(alpha);

      // Prochain match
      RenderNextMatchInfo(alpha);
    }

    /// <summary>
    /// Page finale : le champion en grand + le classement des victoires.
    /// </summary>
    private void RenderChampionPage(float alpha)
    {
      // Confettis en fond
      if (animationTimer > 0.5f)
        RenderCelebration(alpha);

      string champion = session.GetChampion();
      if (string.IsNullOrEmpty(champion))
        champion = winner;

      // Grand "WINNER"
      float titleScale = 1f + (float)Math.Sin(animationTimer * 3f) * 0.15f;
      Draw.OutlineTextCentered(
        TFGame.Font,
        "WINNER!",
        new Vector2(160f, 26f),
        Calc.HexToColor("FFD700") * alpha,
        titleScale * 2.5f
      );

      // Nom du champion en gros
      Draw.OutlineTextCentered(
        TFGame.Font,
        TournamentText.Safe(champion),
        new Vector2(160f, 56f),
        Color.White * alpha,
        2.4f
      );

      Draw.TextCentered(
        TFGame.Font,
        "CHAMPION DU TOURNOI",
        new Vector2(160f, 78f),
        Calc.HexToColor("5EFF5E") * alpha
      );

      // Classement
      RenderStandings(new Vector2(160f, 96f), alpha);
    }

    private void RenderStandings(Vector2 top, float alpha)
    {
      Draw.OutlineTextCentered(
        TFGame.Font,
        "CLASSEMENT (VICTOIRES)",
        new Vector2(top.X, top.Y),
        Calc.HexToColor("FFEC5E") * alpha,
        1f
      );

      var standings = session.GetStandings();
      int total = standings.Count;
      float startY = top.Y + 16f;
      float lineHeight = 13f;

      bool twoColumns = total > 10;
      int perColumn = twoColumns ? (total + 1) / 2 : total;
      if (perColumn < 1)
        perColumn = 1;

      for (int i = 0; i < total; i++)
      {
        var entry = standings[i];
        int rank = i + 1;

        Color rowColor =
          rank == 1 ? Calc.HexToColor("FFD700") :
          rank == 2 ? Calc.HexToColor("C0C0C0") :
          rank == 3 ? Calc.HexToColor("CD7F32") :
          Color.White;

        string row = TournamentText.Safe($"{rank}. {entry.Key} : {entry.Value}");

        if (twoColumns)
        {
          int col = i / perColumn;
          int r = i % perColumn;
          float x = col == 0 ? 14f : 170f;
          Draw.Text(TFGame.Font, row, new Vector2(x, startY + r * lineHeight), rowColor * alpha);
        }
        else
        {
          Draw.TextCentered(TFGame.Font, row, new Vector2(top.X, startY + i * lineHeight), rowColor * alpha);
        }
      }
    }

    private void RenderMiniBracket(float alpha)
    {
      // Afficher un mini bracket simplifié montrant la progression
      float startY = 120f;
      float lineHeight = 15f;

      // Sans facteur d'echelle, volontairement : la surcharge de TextCentered qui
      // en prend un passe l'origine (MeasureString / 2) telle quelle a DrawString,
      // sans l'arrondir, contrairement a OutlineTextCentered qui fait un Floor.
      // Quand la hauteur du texte est impaire, l'origine tombe sur un demi-pixel,
      // et avec le SamplerState.PointClamp du jeu la premiere ligne de pixels est
      // rognee : c'est ce qui coupait le haut de PROGRESSION. Une echelle non
      // entiere (0.8) sur une police bitmap aggravait encore le probleme.
      Draw.TextCentered(
        TFGame.Font,
        "PROGRESSION:",
        new Vector2(160f, startY),
        Color.White * alpha * 0.8f
      );

      int completedCount = session.GetCompletedMatchCount();
      int totalCount = session.GetTotalMatchCount();
      float progressPercent = session.GetProgressPercentage();

      string progressText = $"{completedCount}/{totalCount} MATCHS";
      Draw.TextCentered(
        TFGame.Font,
        progressText,
        new Vector2(160f, startY + lineHeight),
        Calc.HexToColor("FFEC5E") * alpha
      );

      // Barre de progression
      float barWidth = 200f;
      float barHeight = 8f;
      float barX = 60f;
      float barY = startY + lineHeight * 2 + 5f;

      Draw.Rect(barX, barY, barWidth, barHeight, Color.Gray * 0.3f * alpha);
      Draw.Rect(barX, barY, barWidth * (progressPercent / 100f), barHeight, Calc.HexToColor("5EFF5E") * alpha);
      Draw.HollowRect(barX, barY, barWidth, barHeight, Color.White * alpha);
    }

    private void RenderNextMatchInfo(float alpha)
    {
      float y = 170f;

      var nextMatch = TournamentBracket.GetNextMatch(session.Data.Bracket);
      if (nextMatch != null)
      {
        Draw.TextCentered(
          TFGame.Font,
          "PROCHAIN MATCH:",
          new Vector2(160f, y),
          Color.White * alpha * 0.8f
        );

        string nextMatchInfo = nextMatch.GetPlayersDisplay();
        Draw.TextCentered(
          TFGame.Font,
          TournamentText.Safe(nextMatchInfo),
          new Vector2(160f, y + 12f),
          Calc.HexToColor("FFEC5E") * alpha
        );

        Draw.TextCentered(
          TFGame.Font,
          TournamentText.Safe(nextMatch.GetRoundName()),
          new Vector2(160f, y + 24f),
          Color.Gray * alpha
        );
      }
    }

    private void RenderCelebration(float alpha)
    {
      // Animation simple d'étoiles/confettis
      Random random = new Random((int)(animationTimer * 1000));

      for (int i = 0; i < 20; i++)
      {
        float x = random.Next(0, 320);
        float y = ((animationTimer * 50f + i * 20f) % 240f);
        float starAlpha = alpha * (1f - (y / 240f)) * 0.5f;

        Color starColor = i % 3 == 0 ? Calc.HexToColor("FFD700") :
                         i % 3 == 1 ? Calc.HexToColor("5EFF5E") :
                         Calc.HexToColor("5E9FFF");

        Draw.Rect(x, y, 1, 1, starColor * starAlpha);

        // Étoile simple
        if (i % 2 == 0)
        {
          Draw.Line(new Vector2(x - 2, y), new Vector2(x + 2, y), starColor * starAlpha);
          Draw.Line(new Vector2(x, y - 2), new Vector2(x, y + 2), starColor * starAlpha);
        }
      }
    }
  }
}

// Made with Bob

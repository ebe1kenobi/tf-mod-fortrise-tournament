using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Écran d'affichage du bracket du tournoi FFA
  /// </summary>
  public class TournamentBracketScene : Entity
  {
    private TournamentSession session;
    private bool confirmingQuit;
    private float animationTimer;

    public static bool IsOpen { get; private set; }
    public static TournamentBracketScene Instance { get; private set; }

    public TournamentBracketScene()
    {
      session = TournamentSession.Current;
      confirmingQuit = false;
      animationTimer = 0f;
      Position = new Vector2(160f, 120f);
      
      if (session == null || !session.IsActive)
      {
        Logger.Info("Error: No active tournament session");
      }
      else
      {
        Logger.Info($"TournamentBracketScene created for tournament with {session.Data.GetPlayerCount()} players");
      }
    }

    public override void Added()
    {
      base.Added();
      Instance = this;
      IsOpen = true;
      Sounds.ui_pause.Play(160f);
      Logger.Info("TournamentBracketScene opened");
    }

    public override void Removed()
    {
      base.Removed();
      Instance = null;
      IsOpen = false;
      Sounds.ui_unpause.Play(160f);
      MenuInput.Clear();
      Logger.Info("TournamentBracketScene closed");
    }

    public override void Update()
    {
      base.Update();
      MenuInput.Update();
      animationTimer += Engine.DeltaTime;

      if (session == null || !session.IsActive)
      {
        Logger.Info("No active session, returning to menu");
        TournamentScene.ExitToMainMenu();
        return;
      }

      // Mode confirmation de sortie
      if (confirmingQuit)
      {
        if (MenuInput.Confirm)
        {
          Logger.Info("Quitting tournament");
          Sounds.ui_click.Play(160f, 1f);
          TournamentSession.EndTournament();
          TournamentScene.ExitToMainMenu();
          return;
        }

        if (MenuInput.Back)
        {
          confirmingQuit = false;
          Sounds.ui_click.Play(160f, 1f);
          return;
        }

        return;
      }

      // Lancer le match en cours
      if (MenuInput.Start || MenuInput.Confirm)
      {
        if (session.IsTournamentComplete())
        {
          Logger.Info("Tournament complete, returning to menu");
          Sounds.ui_click.Play(160f, 1f);
          TournamentSession.EndTournament();
          TournamentScene.ExitToMainMenu();
        }
        else
        {
          var currentMatch = session.GetCurrentMatch();
          if (currentMatch != null && !currentMatch.IsPlayed)
          {
            Logger.Info($"Starting match {currentMatch.MatchIndex}");
            Sounds.ui_click.Play(160f, 1f);
            StartCurrentMatch();
          }
          else
          {
            Sounds.ui_invalid.Play(160f, 1f);
          }
        }
        return;
      }

      // Demander confirmation pour quitter
      if (MenuInput.Back)
      {
        confirmingQuit = true;
        Sounds.ui_click.Play(160f, 1f);
        return;
      }
    }

    private void StartCurrentMatch()
    {
      var matchIntroScene = new TournamentMatchIntroScene();
      Scene.Add(matchIntroScene);
      RemoveSelf();
    }

    public override void Render()
    {
      // Fond semi-transparent

      if (session == null || !session.IsActive || session.Data == null)
      {
        Draw.TextCentered(TFGame.Font, "ERREUR: PAS DE TOURNOI ACTIF", new Vector2(160f, 120f), Color.Red);
        return;
      }

      // Titre
      Draw.OutlineTextCentered(
        TFGame.Font, 
        "FFA TOURNAMENT - BRACKET", 
        new Vector2(160f, 10f), 
        Color.White, 
        1.5f
      );

      // Informations du tournoi
      RenderTournamentInfo();

      // Arbre du bracket (élimination) ou classement (round-robin)
      RenderBracketOrStandings();

      // Instructions ou confirmation de sortie
      if (confirmingQuit)
      {
        RenderQuitConfirmation();
      }
      else
      {
        RenderInstructions();
      }
    }

    private void RenderTournamentInfo()
    {
      string format = session.Data.GetFormatDisplay();
      string progress = $"{session.GetCompletedMatchCount()}/{session.GetTotalMatchCount()} MATCHS";
      
      Draw.TextCentered(
        TFGame.Font, 
        $"{format} - {progress}", 
        new Vector2(160f, 25f), 
        Calc.HexToColor("5EFF5E")
      );
    }

    private void RenderBracketOrStandings()
    {
      // Round-robin et King of the Hill se jugent aux victoires -> on affiche un classement.
      if (session.Data.Type == TournamentType.RoundRobin || session.Data.Type == TournamentType.KingOfTheHill)
        RenderStandingsBody();
      else
        RenderBracketTree();
    }

    // Curseur utilisé pendant le placement récursif des feuilles du bracket.
    private int bracketLeafCursor;

    /// <summary>
    /// Dessine un arbre d'élimination : une colonne par round (premier tour à gauche,
    /// finale à droite). Chaque match est centré verticalement sur ses matchs
    /// "nourriciers" (ses enfants), et les feuilles (1er tour) sont réparties
    /// régulièrement -> arbre homogène. Les boîtes sont calées sur la hauteur réelle
    /// de la police et encadrées.
    /// </summary>
    private void RenderBracketTree()
    {
      var bracket = session.Data.Bracket;
      if (bracket == null || bracket.Count == 0)
        return;

      int maxRound = 0;
      foreach (var m in bracket)
        if (m.Round > maxRound)
          maxRound = m.Round;

      int numCols = maxRound + 1;
      const float leftX = 6f;
      const float rightX = 314f;
      const float topY = 42f;
      const float bottomY = 200f;
      float colW = (rightX - leftX) / numCols;
      float nodeW = colW - 10f;

      // Matchs "nourriciers" (enfants) de chaque match.
      var feeders = new Dictionary<int, List<TournamentMatch>>();
      foreach (var m in bracket)
      {
        if (m.NextMatchIndex < 0)
          continue;
        if (!feeders.ContainsKey(m.NextMatchIndex))
          feeders[m.NextMatchIndex] = new List<TournamentMatch>();
        feeders[m.NextMatchIndex].Add(m);
      }
      foreach (var kv in feeders)
        kv.Value.Sort((a, b) => a.MatchIndex.CompareTo(b.MatchIndex));

      // Nombre de feuilles (matchs sans nourricier = 1er tour) pour espacer régulièrement.
      int leafCount = 0;
      foreach (var m in bracket)
        if (!feeders.ContainsKey(m.MatchIndex))
          leafCount++;
      if (leafCount < 1)
        leafCount = 1;
      float leafSpacing = (bottomY - topY) / leafCount;

      // Y de chaque match : feuilles réparties régulièrement, parents centrés sur enfants.
      var yById = new Dictionary<int, float>();
      bracketLeafCursor = 0;
      foreach (var m in bracket)
        if (m.NextMatchIndex < 0)
          AssignBracketY(m, feeders, yById, leafSpacing, topY);

      // Positions (bord gauche + centre vertical).
      var centers = new Dictionary<int, Vector2>();
      foreach (var m in bracket)
      {
        int col = maxRound - m.Round; // 0 = colonne de gauche (1er tour)
        float x = leftX + col * colW;
        float cy = yById.ContainsKey(m.MatchIndex) ? yById[m.MatchIndex] : (topY + bottomY) / 2f;
        centers[m.MatchIndex] = new Vector2(x, cy);
      }

      // Échelle du texte : réduite seulement si les boîtes ne tiennent pas dans un slot.
      float fontH1 = TFGame.Font.MeasureString("A").Y;
      int maxPlayersPerMatch = 2;
      foreach (var m in bracket)
        if (m.Players.Count > maxPlayersPerMatch)
          maxPlayersPerMatch = m.Players.Count;
      float neededH = maxPlayersPerMatch * (fontH1 + 1f) + 4f;
      float nodeScale = 1f;
      if (neededH > leafSpacing && neededH > 0f)
        nodeScale = Math.Max(0.7f, leafSpacing / neededH);

      // Traits reliant chaque match à son match suivant (derrière les boîtes).
      foreach (var m in bracket)
      {
        if (m.NextMatchIndex < 0)
          continue;
        if (!centers.ContainsKey(m.MatchIndex) || !centers.ContainsKey(m.NextMatchIndex))
          continue;

        Vector2 from = centers[m.MatchIndex];
        Vector2 to = centers[m.NextMatchIndex];
        float fromX = from.X + nodeW;
        float midX = (fromX + to.X) / 2f;
        Color lc = m.IsPlayed ? Calc.HexToColor("5EFF5E") * 0.8f : Color.Gray * 0.5f;

        Draw.Line(new Vector2(fromX, from.Y), new Vector2(midX, from.Y), lc);
        Draw.Line(new Vector2(midX, from.Y), new Vector2(midX, to.Y), lc);
        Draw.Line(new Vector2(midX, to.Y), new Vector2(to.X, to.Y), lc);
      }

      // Boîtes des matchs.
      foreach (var m in bracket)
      {
        if (centers.ContainsKey(m.MatchIndex))
          RenderBracketNode(m, centers[m.MatchIndex], nodeW, nodeScale);
      }
    }

    private float AssignBracketY(TournamentMatch match, Dictionary<int, List<TournamentMatch>> feeders,
                                 Dictionary<int, float> yById, float leafSpacing, float topY)
    {
      float y;
      if (!feeders.ContainsKey(match.MatchIndex))
      {
        // Feuille : slot régulier.
        y = topY + (bracketLeafCursor + 0.5f) * leafSpacing;
        bracketLeafCursor++;
      }
      else
      {
        // Parent : centré sur la moyenne des enfants.
        float sum = 0f;
        var fs = feeders[match.MatchIndex];
        foreach (var f in fs)
          sum += AssignBracketY(f, feeders, yById, leafSpacing, topY);
        y = sum / fs.Count;
      }
      yById[match.MatchIndex] = y;
      return y;
    }

    private void RenderBracketNode(TournamentMatch match, Vector2 leftCenter, float width, float scale)
    {
      var font = TFGame.Font;
      float fontH = font.MeasureString("A").Y * scale;
      const float pad = 2f;
      float lineH = fontH + 1f;
      int lines = Math.Max(match.Players.Count, 1);
      float boxH = lines * lineH + pad * 2f;
      float x = leftCenter.X;
      float y = leftCenter.Y - boxH / 2f;

      bool isCurrent = match.MatchIndex == session.Data.CurrentMatchIndex && !match.IsPlayed;

      // Fond.
      Color bg = isCurrent ? Calc.HexToColor("FFEC5E") * 0.22f
               : match.IsPlayed ? Calc.HexToColor("5EFF5E") * 0.12f
               : Color.Black * 0.4f;
      Draw.Rect(x, y, width, boxH, bg);

      // Cadre.
      Color border;
      if (isCurrent)
      {
        float pulse = (float)Math.Sin(animationTimer * 4f) * 0.3f + 0.7f;
        border = Calc.HexToColor("FFEC5E") * pulse;
      }
      else
      {
        border = match.IsPlayed ? Calc.HexToColor("5EFF5E") * 0.6f : Color.White * 0.35f;
      }
      Draw.HollowRect(x, y, width, boxH, border);

      // Noms.
      for (int i = 0; i < match.Players.Count; i++)
      {
        string name = match.Players[i];
        Color c = (match.IsPlayed && name == match.Winner) ? Calc.HexToColor("FFD700")
                : name == "?" ? Color.Gray
                : Color.White;
        string label = FitText(name, width - pad * 2f, scale);
        Draw.Text(
          font,
          label,
          new Vector2(x + pad, y + pad + i * lineH),
          c,
          Vector2.Zero,
          new Vector2(scale, scale),
          0f
        );
      }
    }

    /// <summary>
    /// Round-robin : pas d'arbre, on affiche le classement des victoires. Au-delà de
    /// 10 joueurs on passe sur deux colonnes pour tous les voir.
    /// </summary>
    private void RenderStandingsBody()
    {
      Draw.OutlineTextCentered(
        TFGame.Font,
        "RANK (VICTORIES)",
        new Vector2(160f, 45f),
        Calc.HexToColor("FFEC5E"),
        1f
      );

      var standings = session.GetStandings();
      int total = standings.Count;
      const float startY = 62f;
      const float lineH = 13f;

      bool twoColumns = total > 10;
      int perColumn = twoColumns ? (total + 1) / 2 : total;
      if (perColumn < 1)
        perColumn = 1;

      for (int i = 0; i < total; i++)
      {
        var entry = standings[i];
        Color c =
          i == 0 ? Calc.HexToColor("FFD700") :
          i == 1 ? Calc.HexToColor("C0C0C0") :
          i == 2 ? Calc.HexToColor("CD7F32") :
          Color.White;

        string text = TournamentText.Safe($"{i + 1}. {entry.Key} : {entry.Value}");

        if (twoColumns)
        {
          int col = i / perColumn;
          int row = i % perColumn;
          float x = col == 0 ? 14f : 170f;
          Draw.Text(TFGame.Font, text, new Vector2(x, startY + row * lineH), c);
        }
        else
        {
          Draw.TextCentered(TFGame.Font, text, new Vector2(160f, startY + i * lineH), c);
        }
      }
    }

    /// <summary>
    /// Tronque un nom pour qu'il tienne dans la largeur donnée (à l'échelle indiquée).
    /// </summary>
    private string FitText(string text, float maxWidth, float scale)
    {
      text = TournamentText.Safe(text);
      if (string.IsNullOrEmpty(text))
        return text;

      var font = TFGame.Font;
      if (font.MeasureString(text).X * scale <= maxWidth)
        return text;

      while (text.Length > 1 && font.MeasureString(text + ".").X * scale > maxWidth)
        text = text.Substring(0, text.Length - 1);

      return text + ".";
    }

    private void RenderQuitConfirmation()
    {
      Draw.Rect(60f, 90f, 200f, 60f, Color.Black * 0.9f);
      Draw.HollowRect(60f, 90f, 200f, 60f, Color.Red);

      Draw.OutlineTextCentered(
        TFGame.Font, 
        "LEAVE THE TOURNAMENT?", 
        new Vector2(160f, 105f), 
        Color.Red, 
        1f
      );

      Draw.TextCentered(
        TFGame.Font, 
        "A: CONFIRM", 
        new Vector2(160f, 120f), 
        Color.White
      );

      Draw.TextCentered(
        TFGame.Font, 
        "B: BACK", 
        new Vector2(160f, 132f), 
        Color.Gray
      );
    }

    private void RenderInstructions()
    {
      float y = 210f;

      if (session.IsTournamentComplete())
      {
        Draw.OutlineTextCentered(
          TFGame.Font, 
          "START: BACK TO MENU", 
          new Vector2(160f, y), 
          Calc.HexToColor("5EFF5E"), 
          1f
        );
      }
      else
      {
        var currentMatch = session.GetCurrentMatch();
        if (currentMatch != null && !currentMatch.IsPlayed)
        {
          Draw.OutlineTextCentered(
            TFGame.Font, 
            "START : START THE MATCH", 
            new Vector2(160f, y), 
            Calc.HexToColor("5EFF5E"), 
            1f
          );
        }
      }

      Draw.TextCentered(
        TFGame.Font, 
        "SELECT: LEAVE TOURNAMENT", 
        new Vector2(160f, y + 12f), 
        Color.Gray
      );
    }
  }
}

// Made with Bob

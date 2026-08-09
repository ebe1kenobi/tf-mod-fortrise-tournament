using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Ecran de choix des variantes du tournoi, ouvert depuis l'ecran de configuration.
  ///
  /// L'ecran du jeu n'est pas reutilisable : MatchVariants.BuildMenu prend un
  /// MainMenu, y ajoute ses entites et pilote ses guides de boutons, alors que le
  /// tournoi vit dans sa propre Scene. On rejoue donc le meme contenu ici : une
  /// grille d'icones groupee par en-tete, et le nom de la seule variante survolee,
  /// comme dans le menu versus ou les libelles n'apparaissent qu'au survol.
  ///
  /// Les valeurs sont ecrites dans MainMenu.VersusMatchSettings.Variants, l'objet
  /// partage avec le versus ; l'ecran de configuration en fait ensuite une liste
  /// d'identifiants figee dans les donnees du tournoi.
  /// </summary>
  public class TournamentVariantsScene : Entity
  {
    private const int Columns = 13;
    private const float CellSize = 20f;
    private const float GridTop = 50f;
    private const int VisibleLines = 7;

    // Couleurs du cadre de selection, reprises de VariantItem.
    private static readonly Color ActiveSelection = Calc.HexToColor("D8F878");
    private static readonly Color NormalSelection = Calc.HexToColor("3CBCFC");

    // Une ligne de la grille : un en-tete, la commande de remise a zero, ou une
    // rangee d'icones.
    private sealed class Line
    {
      public string Header;
      public bool IsReset;
      public List<TournamentVariants.Entry> Items;
      public bool IsGrid { get { return Items != null; } }
    }

    private readonly TournamentSettingsScene owner;
    private readonly List<Line> lines = new List<Line>();
    private int selectedLine;
    private int selectedColumn;
    private int scroll;

    public static bool IsOpen { get; private set; }

    public TournamentVariantsScene(TournamentSettingsScene owner)
    {
      this.owner = owner;
      Position = new Vector2(160f, 120f);
      BuildLines();
    }

    private void BuildLines()
    {
      // Remise a zero en tete, atteignable sans parcourir toute la grille.
      lines.Add(new Line { IsReset = true });

      string currentHeader = null;
      List<TournamentVariants.Entry> row = null;

      foreach (TournamentVariants.Entry entry in TournamentVariants.GetEntries())
      {
        string header = string.IsNullOrEmpty(entry.Header) ? "GENERAL" : entry.Header.ToUpperInvariant();
        if (header != currentHeader)
        {
          currentHeader = header;
          lines.Add(new Line { Header = header });
          row = null;
        }

        if (row == null || row.Count >= Columns)
        {
          row = new List<TournamentVariants.Entry>();
          lines.Add(new Line { Items = row });
        }

        row.Add(entry);
      }

      selectedLine = 0;
      selectedColumn = 0;
      MoveToSelectable(1);
    }

    public override void Added()
    {
      base.Added();
      IsOpen = true;
      Sounds.ui_pause.Play(160f);
    }

    public override void Removed()
    {
      base.Removed();
      IsOpen = false;
      MenuInput.Clear();
    }

    private bool IsSelectable(Line line)
    {
      return line.IsReset || line.IsGrid;
    }

    /// <summary>Avance jusqu'a une ligne utilisable, en sautant les en-tetes.</summary>
    private void MoveToSelectable(int direction)
    {
      if (lines.Count == 0) return;

      int guard = 0;
      while (guard++ <= lines.Count)
      {
        if (selectedLine < 0) { selectedLine = 0; direction = 1; }
        if (selectedLine >= lines.Count) { selectedLine = lines.Count - 1; direction = -1; }

        if (IsSelectable(lines[selectedLine]))
          break;

        selectedLine += direction;
      }

      ClampColumn();
      AdjustScroll();
    }

    private void ClampColumn()
    {
      Line line = lines[selectedLine];
      if (!line.IsGrid)
      {
        selectedColumn = 0;
        return;
      }

      if (selectedColumn >= line.Items.Count)
        selectedColumn = line.Items.Count - 1;
      if (selectedColumn < 0)
        selectedColumn = 0;
    }

    private void AdjustScroll()
    {
      // On garde l'en-tete du groupe visible tant que possible.
      if (selectedLine < scroll + 1)
        scroll = Math.Max(0, selectedLine - 1);
      else if (selectedLine >= scroll + VisibleLines)
        scroll = selectedLine - VisibleLines + 1;

      int max = Math.Max(0, lines.Count - VisibleLines);
      if (scroll > max) scroll = max;
      if (scroll < 0) scroll = 0;
    }

    private TournamentVariants.Entry Selected
    {
      get
      {
        Line line = lines[selectedLine];
        if (!line.IsGrid || selectedColumn >= line.Items.Count)
          return null;
        return line.Items[selectedColumn];
      }
    }

    public override void Update()
    {
      base.Update();
      MenuInput.Update();

      if (lines.Count == 0)
      {
        if (MenuInput.Back || MenuInput.Confirm) Close();
        return;
      }

      if (MenuInput.Up)
      {
        selectedLine--;
        MoveToSelectable(-1);
        Sounds.ui_move1.Play(160f, 1f);
        return;
      }

      if (MenuInput.Down)
      {
        selectedLine++;
        MoveToSelectable(1);
        Sounds.ui_move1.Play(160f, 1f);
        return;
      }

      // Gauche/droite parcourent la rangee et debordent sur la precedente ou la
      // suivante : la grille se traverse d'un bout a l'autre sans passer par haut/bas.
      if (MenuInput.Left)
      {
        MoveHorizontally(-1);
        return;
      }

      if (MenuInput.Right)
      {
        MoveHorizontally(1);
        return;
      }

      if (MenuInput.Confirm)
      {
        if (lines[selectedLine].IsReset)
        {
          TournamentVariants.ResetAll();
          Sounds.ui_click.Play(160f, 1f);
          return;
        }

        TournamentVariants.Entry entry = Selected;
        if (entry != null)
        {
          // SetValue et non Variant.Value : le setter du jeu ignore les variantes
          // par joueur tant qu'aucun joueur n'est actif (voir TournamentVariants).
          TournamentVariants.SetValue(entry.Variant, !entry.Variant.Value);
          Sounds.ui_click.Play(160f, 1f);
        }
        return;
      }

      if (MenuInput.Back || MenuInput.Start)
      {
        Close();
        return;
      }
    }

    private void MoveHorizontally(int direction)
    {
      Line line = lines[selectedLine];

      if (line.IsGrid)
      {
        int next = selectedColumn + direction;
        if (next >= 0 && next < line.Items.Count)
        {
          selectedColumn = next;
          Sounds.ui_move1.Play(160f, 1f);
          return;
        }
      }

      // Debordement : on cherche la rangee d'icones voisine.
      int probe = selectedLine + direction;
      while (probe >= 0 && probe < lines.Count)
      {
        if (lines[probe].IsGrid)
        {
          selectedLine = probe;
          selectedColumn = direction > 0 ? 0 : lines[probe].Items.Count - 1;
          AdjustScroll();
          Sounds.ui_move1.Play(160f, 1f);
          return;
        }
        probe += direction;
      }
    }

    private void Close()
    {
      Sounds.ui_click.Play(160f, 1f);
      if (owner != null)
        owner.OnVariantsClosed();

      Scene.Add(owner);
      RemoveSelf();
    }

    public override void Render()
    {
      Draw.Rect(0f, 0f, 320f, 240f, Color.Black * 0.85f);

      Draw.OutlineTextCentered(TFGame.Font, "VARIANTS",
          new Vector2(160f, 18f), Color.White, 1.5f);

      if (lines.Count == 0)
      {
        Draw.TextCentered(TFGame.Font, "NO VARIANT AVAILABLE",
            new Vector2(160f, 120f), Color.Gray);
        return;
      }

      RenderGrid();
      RenderFooter();
    }

    private void RenderGrid()
    {
      float startX = 160f - (Columns * CellSize) / 2f + CellSize / 2f;
      int end = Math.Min(scroll + VisibleLines, lines.Count);

      for (int i = scroll; i < end; i++)
      {
        float y = GridTop + (i - scroll) * CellSize;
        Line line = lines[i];

        if (line.IsReset)
        {
          Draw.OutlineTextCentered(TFGame.Font, "RESET ALL VARIANTS",
              new Vector2(160f, y),
              i == selectedLine ? Calc.HexToColor("FFEC5E") : Color.Gray, 1f);
          continue;
        }

        if (!line.IsGrid)
        {
          Draw.OutlineTextCentered(TFGame.Font, line.Header,
              new Vector2(160f, y), Calc.HexToColor("FFEC5E"), 1f);
          continue;
        }

        for (int c = 0; c < line.Items.Count; c++)
        {
          TournamentVariants.Entry entry = line.Items[c];
          Vector2 pos = new Vector2(startX + c * CellSize, y);
          bool on = entry.Variant.Value;
          bool cursor = i == selectedLine && c == selectedColumn;

          // Icone grisee quand la variante est inactive, pour lire l'etat d'un
          // coup d'oeil sans avoir a survoler chaque case.
          Subtexture icon = entry.Variant.Icon;
          if (icon != null)
            Draw.TextureCentered(icon, pos, on ? Color.White : Color.Gray * 0.6f);
          else
            Draw.TextCentered(TFGame.Font, "?", pos, on ? Color.White : Color.Gray);

          if (on)
            Draw.HollowRect(pos.X - 9f, pos.Y - 9f, 18f, 18f, ActiveSelection);

          if (cursor)
            Draw.HollowRect(pos.X - 10f, pos.Y - 10f, 20f, 20f, NormalSelection);
        }
      }
    }

    private void RenderFooter()
    {
      // Le nom n'apparait que pour la case survolee, comme dans le menu du jeu.
      TournamentVariants.Entry entry = Selected;
      if (entry != null)
      {
        Draw.OutlineTextCentered(TFGame.Font, TournamentText.Safe(entry.Title),
            new Vector2(160f, 198f), Color.White, 1f);

        // Une variante « par joueur » ne se regle finement que sur l'ecran du jeu :
        // ici elle s'applique a tout le monde, autant le dire.
        if (entry.PerPlayer)
          Draw.TextCentered(TFGame.Font, "APPLIES TO ALL PLAYERS",
              new Vector2(160f, 210f), Color.Gray * 0.8f);
      }
      else if (lines[selectedLine].IsReset)
      {
        Draw.TextCentered(TFGame.Font, "TURN EVERY VARIANT OFF",
            new Vector2(160f, 198f), Color.Gray * 0.8f);
      }

      Draw.TextCentered(TFGame.Font, TournamentVariants.CountActive() + " ACTIVE",
          new Vector2(160f, 222f), Calc.HexToColor("5EFF5E"));

      Draw.TextCentered(TFGame.Font, "A: TOGGLE   B: DONE",
          new Vector2(160f, 232f), Color.Gray * 0.8f);
    }
  }
}

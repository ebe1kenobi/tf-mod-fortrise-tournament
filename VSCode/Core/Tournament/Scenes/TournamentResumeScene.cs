using System;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Écran proposé à l'ouverture du tournoi quand une sauvegarde en cours existe :
  /// reprendre là où on s'était arrêté, ou démarrer un nouveau tournoi.
  /// </summary>
  public class TournamentResumeScene : Entity
  {
    private readonly TournamentData savedData;
    private float animationTimer;

    public static bool IsOpen { get; private set; }
    public static TournamentResumeScene Instance { get; private set; }

    public TournamentResumeScene(TournamentData data)
    {
      savedData = data;
      Position = new Vector2(160f, 120f);
    }

    public override void Added()
    {
      base.Added();
      Instance = this;
      IsOpen = true;
      Sounds.ui_pause.Play(160f);
    }

    public override void Removed()
    {
      base.Removed();
      Instance = null;
      IsOpen = false;
      MenuInput.Clear();
    }

    public override void Update()
    {
      base.Update();
      animationTimer += Engine.DeltaTime;
      MenuInput.Update();

      if (MenuInput.Start || MenuInput.Confirm)
      {
        Sounds.ui_click.Play(160f, 1f);
        TournamentSession.StartTournament(savedData);
        Scene.Add(new TournamentBracketScene());
        RemoveSelf();
        return;
      }

      if (MenuInput.Back)
      {
        Sounds.ui_click.Play(160f, 1f);
        TournamentSave.Delete();
        var roster = TournamentPlayerManager.LoadPlayerNames();
        Scene.Add(new TournamentPlayerSelectionScene(roster));
        RemoveSelf();
        return;
      }
    }

    public override void Render()
    {
      Draw.OutlineTextCentered(
        TFGame.Font,
        "TOURNOI EN COURS",
        new Vector2(160f, 60f),
        Calc.HexToColor("FFD700"),
        1.5f
      );

      if (savedData != null)
      {
        int done = 0;
        foreach (var m in savedData.Bracket)
          if (m.IsPlayed)
            done++;

        string info = TournamentText.Safe(
          $"{savedData.GetPlayerCount()} JOUEURS - {done}/{savedData.Bracket.Count} MATCHS JOUES");
        Draw.TextCentered(TFGame.Font, info, new Vector2(160f, 90f), Calc.HexToColor("5EFF5E"));
      }

      float blink = (float)Math.Sin(animationTimer * 4f) * 0.3f + 0.7f;
      Draw.OutlineTextCentered(
        TFGame.Font,
        "START: REPRENDRE",
        new Vector2(160f, 130f),
        Calc.HexToColor("5EFF5E") * blink,
        1f
      );

      Draw.TextCentered(
        TFGame.Font,
        "SELECT: NOUVEAU TOURNOI",
        new Vector2(160f, 150f),
        Color.Gray
      );
    }
  }
}

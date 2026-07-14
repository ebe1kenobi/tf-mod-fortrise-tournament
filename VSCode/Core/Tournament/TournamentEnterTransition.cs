using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Petite entité posée sur le MainMenu quand on ouvre le tournoi : elle rejoue
  /// l'animation de sortie du menu (logo qui s'en va, boutons qui s'envolent), comme
  /// quand on appuie sur VERSUS, puis bascule vers la vraie scène du tournoi une fois
  /// les éléments sortis de l'écran.
  /// </summary>
  public class TournamentEnterTransition : Entity
  {
    private readonly MainMenu menu;
    private int timer;
    // Assez long pour bien voir les boutons quitter l'écran (comme sur VERSUS).
    private const int Duration = 24;

    public TournamentEnterTransition(MainMenu menu)
      : base(Vector2.Zero, 0)
    {
      this.menu = menu;
    }

    public override void Added()
    {
      base.Added();

      // Le logo TowerFall s'en va (comme sur VERSUS).
      if (menu != null && menu.Logo != null)
      {
        menu.Logo.TweenOut();
        menu.Logo.TweenOutLetters();
      }

      // Tous les boutons de mode se désélectionnent et s'envolent hors de l'écran.
      if (menu != null && menu.Layers.ContainsKey(-1))
      {
        foreach (var item in menu.Layers[-1].GetList<MenuItem>())
        {
          item.Selected = false;
          item.TweenOut();
        }
      }
    }

    public override void Update()
    {
      base.Update();
      timer++;

      if (timer >= Duration)
      {
        // Les éléments du menu sont sortis : on passe à la vraie scène du tournoi
        // (reprise si une sauvegarde existe, sinon sélection des joueurs).
        Engine.Instance.Scene = new TournamentScene(MyVersusModeButton.CreateFirstTournamentPage());
        RemoveSelf();
      }
    }
  }
}

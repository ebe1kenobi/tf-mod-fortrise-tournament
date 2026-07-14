using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Vraie scène (page) du tournoi, avec le fond animé des tours du menu principal.
  /// Les différents écrans (sélection, settings, bracket, intro, résultats) sont des
  /// entités ajoutées sur la couche 0 ; ils naviguent entre eux via Scene.Add/RemoveSelf,
  /// exactement comme avant, mais dans cette scène dédiée au lieu d'une surimpression.
  /// </summary>
  public class TournamentScene : Scene
  {
    public TournamentScene(Entity firstPage)
    {
      // -3 : fond animé (MenuBackground se place tout seul sur cette couche).
      SetLayer(-3, new Layer());
      // 0 : pages du tournoi, coordonnées écran fixes (cameraMultiplier 0).
      SetLayer(0, new Layer(0f));

      Engine.Instance.Screen.ClearColor = Color.Black;

      Add(new MenuBackground());

      if (firstPage != null)
        Add(firstPage);
    }

    /// <summary>
    /// Quitte le tournoi et revient au menu principal en rejouant l'animation d'entrée
    /// (boutons + logo qui reviennent en volant). On ne peut pas partir de l'état "None"
    /// (ScreenTitle plante), donc on crée un MainMenu normal (Main) et on demande à
    /// MyTournamentMenuButton de déclencher les TweenIn juste après son Begin().
    /// </summary>
    public static void ExitToMainMenu()
    {
      MyTournamentMenuButton.RequestReturnAnimation();
      Engine.Instance.Scene = new MainMenu(MainMenu.MenuState.Main);
    }
  }
}

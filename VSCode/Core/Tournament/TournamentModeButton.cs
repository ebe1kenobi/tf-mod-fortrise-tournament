using Microsoft.Xna.Framework;
using TowerFall;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Bouton "TOURNOI" sur l'écran de sélection de mode du menu principal,
  /// entre VERSUS et CO-OP. Ouvre l'écran de sélection des joueurs du tournoi.
  /// </summary>
  public class TournamentModeButton : MainModeButton
  {
    // Pas d'icône dédiée : le bouton est textuel. Ces champs stockent simplement
    // les valeurs attendues par la base (animations de sélection).
    private float imageScale = 1f;
    private float imageRotation;
    private float imageY;

    public TournamentModeButton(Vector2 position, Vector2 tweenFrom)
      : base(position, tweenFrom, "TOURNAMENT", "2-16 ARCHERS")
    {
    }

    // On laisse la base gérer OnConfirm (animation de "pression" du bouton, son),
    // qui appellera MenuAction ensuite — même timing que le bouton VERSUS.
    // Le menu entier est de toute façon abandonné quand on ouvre le tournoi, donc
    // aucun risque de bouton "resté pressé".

    protected override void MenuAction()
    {
      // Si aucune sauvegarde à reprendre, il faut assez de joueurs avant de lancer
      // l'animation de transition du menu.
      if (!TournamentSave.Exists())
      {
        var players = MyVersusModeButton.TryGetTournamentPlayers();
        if (players == null)
          return;
      }

      if (MainMenu != null)
      {
        // Rejoue l'animation de sortie du menu (logo + boutons), puis bascule vers
        // la scène du tournoi, exactement comme le bouton VERSUS.
        MainMenu.Add(new TournamentEnterTransition(MainMenu));
      }
      else
      {
        MyVersusModeButton.OpenTournamentMode();
      }
    }

    // Titre un peu plus petit que VERSUS/CO-OP pour tenir entre les deux.
    public override float BaseTextScale
    {
      get { return 1.3f; }
    }

    public override float BaseScale
    {
      get { return 1f; }
    }

    public override float ImageScale
    {
      get { return imageScale; }
      set { imageScale = value; }
    }

    public override float ImageRotation
    {
      get { return imageRotation; }
      set { imageRotation = value; }
    }

    public override float ImageY
    {
      get { return imageY; }
      set { imageY = value; }
    }
  }
}

using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Bouton "TOURNOI" sur l'écran de sélection de mode du menu principal,
  /// entre VERSUS et CO-OP. Ouvre l'écran de sélection des joueurs du tournoi.
  /// </summary>
  public class TournamentModeButton : MainModeButton
  {
    // Icône du bouton (une coupe), enregistrée par le module au démarrage. Si elle
    // manque, image reste null et le bouton se comporte comme avant : purement
    // textuel. Les champs image* servent alors de simple stockage pour les
    // animations de sélection que la classe de base pilote.
    private readonly Image image;
    private float imageScale = 1f;
    private float imageRotation;
    private float imageY;

    public TournamentModeButton(Vector2 position, Vector2 tweenFrom)
      : base(position, tweenFrom, "TOURNAMENT", "2-16 ARCHERS")
    {
      Subtexture icon = TFModFortRiseTournamentModule.TournamentIcon;
      if (icon == null)
        return;

      image = new Image(icon, null);
      image.CenterOrigin();
      Add(image);
    }

    public override void Render()
    {
      // Comme FightButton : le contour détache l'icône du fond du menu.
      if (image != null)
        image.DrawOutline(1);

      base.Render();
    }

    // On laisse la base gérer OnConfirm (animation de "pression" du bouton, son),
    // qui appellera MenuAction ensuite — même timing que le bouton VERSUS.
    // Le menu entier est de toute façon abandonné quand on ouvre le tournoi, donc
    // aucun risque de bouton "resté pressé".

    protected override void MenuAction()
    {
      // Plus de blocage sur le nombre de joueurs : l'ecran de selection permet
      // d'en ajouter (Y), il doit donc rester accessible meme avec un fichier
      // absent ou incomplet.

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

    // La classe de base anime l'icone via ces trois proprietes (agrandissement a
    // la selection, rotation, remontee). On les repercute sur l'Image quand elle
    // existe, sinon on se contente de memoriser la valeur.
    public override float ImageScale
    {
      get { return imageScale; }
      set
      {
        imageScale = value;
        if (image != null) image.Scale = Vector2.One * value;
      }
    }

    public override float ImageRotation
    {
      get { return imageRotation; }
      set
      {
        imageRotation = value;
        if (image != null) image.Rotation = value;
      }
    }

    public override float ImageY
    {
      get { return imageY; }
      set
      {
        imageY = value;
        if (image != null) image.Y = value;
      }
    }
  }
}

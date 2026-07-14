using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using TowerFall;
using TFModFortRiseTournament.Tournament;

namespace TFModFortRiseTournament
{
  /// <summary>
  /// Injecte un bouton "TOURNOI" sur l'écran de mode du menu principal, entre les
  /// boutons VERSUS (FightButton) et CO-OP (CoOpButton), et le câble dans la
  /// navigation clavier/manette (Versus &lt;-&gt; Tournoi &lt;-&gt; Coop).
  /// </summary>
  public static class MyTournamentMenuButton
  {
    // Positions de la rangée de mode (on écarte un peu Versus/Coop pour insérer Tournoi).
    private static readonly Vector2 FightPos = new Vector2(75f, 140f);
    private static readonly Vector2 TournamentPos = new Vector2(160f, 140f);
    private static readonly Vector2 CoopPos = new Vector2(245f, 140f);

    // Champ privé "tweenTo" de MainModeButton (cible du tween d'entrée).
    private static readonly FieldInfo TweenToField =
      typeof(MainModeButton).GetField("tweenTo", BindingFlags.NonPublic | BindingFlags.Instance);

    // Quand on revient du tournoi, on veut rejouer l'animation d'entrée du menu.
    private static bool playReturnAnimation;

    public static void Load()
    {
      On.TowerFall.MainMenu.CreateMain += CreateMain_patch;
      On.TowerFall.MainMenu.Begin += Begin_patch;
    }

    public static void Unload()
    {
      On.TowerFall.MainMenu.CreateMain -= CreateMain_patch;
      On.TowerFall.MainMenu.Begin -= Begin_patch;
    }

    /// <summary>
    /// Demande que le prochain MainMenu rejoue l'animation d'entrée (retour du tournoi).
    /// </summary>
    public static void RequestReturnAnimation()
    {
      playReturnAnimation = true;
    }

    private static void Begin_patch(On.TowerFall.MainMenu.orig_Begin orig, MainMenu self)
    {
      orig(self);

      if (!playReturnAnimation)
        return;
      playReturnAnimation = false;

      // Les boutons de mode reviennent en volant (depuis leur position hors-écran).
      if (self.Layers.ContainsKey(-1))
      {
        foreach (var item in self.Layers[-1].GetList<MenuItem>())
          item.TweenIn();
      }

      // Le logo TowerFall revient.
      if (self.Logo != null)
      {
        self.Logo.StartOut();
        self.Logo.TweenIn(false);
      }
    }

    private static void CreateMain_patch(On.TowerFall.MainMenu.orig_CreateMain orig, MainMenu self)
    {
      orig(self);

      // Les boutons viennent d'être ajoutés en attente : on flush pour pouvoir les
      // retrouver et les recâbler.
      self.UpdateEntityLists();

      FightButton fight = null;
      CoOpButton coop = null;
      foreach (var item in self.Layers[-1].GetList<MenuItem>())
      {
        if (fight == null && item is FightButton f)
          fight = f;
        else if (coop == null && item is CoOpButton c)
          coop = c;
      }

      if (fight == null || coop == null)
      {
        Logger.Info("Tournament button: could not find Versus/Coop buttons, skipping injection");
        return;
      }

      // Écarter Versus et Coop pour faire de la place au centre.
      // TweenIn (appelé après CreateMain) utilise le champ privé tweenTo comme cible.
      if (TweenToField != null)
      {
        TweenToField.SetValue(fight, FightPos);
        TweenToField.SetValue(coop, CoopPos);
      }

      // Créer le bouton tournoi (glisse depuis le bas comme cible centrale).
      var tournament = new TournamentModeButton(TournamentPos, new Vector2(160f, 300f));
      self.Add(tournament);

      // Câbler la navigation horizontale : Versus <-> Tournoi <-> Coop.
      fight.RightItem = tournament;
      tournament.LeftItem = fight;
      tournament.RightItem = coop;
      coop.LeftItem = tournament;

      // Navigation verticale : descendre depuis le tournoi rejoint la même destination
      // que Coop (sous-boutons Options/Trials/etc.).
      tournament.DownItem = coop.DownItem;

      Logger.Info("Tournament mode button injected between Versus and Coop");
    }
  }
}

//quand on reient en arriere a partir de la map -> archer selection
//quand on arrete un match avec pause -> quit : archer selection

using System;
using System.Diagnostics;
using System.IO;
using FortRise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Monocle;
using MonoMod.ModInterop;
using TowerFall;

//CHAR_A_DIE -> orange aie
//    *         ouille
namespace TFModFortRiseTournament
{
  [Fort("com.ebe1.kenobi.TFModFortRiseTournament", "TFModFortRiseTournament")]
  public class TFModFortRiseTournamentModule : FortModule
  {
    public static TFModFortRiseTournamentModule Instance;
    public override Type SettingsType => typeof(TFModFortRiseTournamentSettings);
    public static TFModFortRiseTournamentSettings Settings => (TFModFortRiseTournamentSettings)Instance.InternalSettings;
    public TFModFortRiseTournamentModule()
    {
      if (!Debugger.IsAttached)
      {
        //Debugger.Launch(); // Proposera d’attacher Visual Studio
      }
      Instance = this;
      Logger.Init("TFModFortRiseTournament");
    }

    public override void LoadContent()
    {
    }

    public override void Load()
    {
      MyTournamentMenuButton.Load();
      typeof(CustomNameImport).ModInterop();

      // Tournament system
      Logger.Info("Initializing Tournament system...");
      Tournament.TournamentPlayerManager.Initialize();
      Tournament.TournamentSession.Load();
      Logger.Info("Tournament system loaded");
    }


    public override void Unload()
    {
      MyTournamentMenuButton.Unload();
      // Tournament system
      Tournament.TournamentSession.Unload();
      Logger.Info("Tournament system unloaded");
    }
  }
}

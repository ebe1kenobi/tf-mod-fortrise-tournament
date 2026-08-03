//todo ajout tournament variant, desactivate variant orb ...
using System;
using System.Diagnostics;
using System.IO;
using FortRise;
using Microsoft.Extensions.Logging;

//CHAR_A_DIE -> orange aie
//    *         ouille
namespace TFModFortRiseTournament
{
  public class TFModFortRiseTournamentModule : Mod
  {
    public static TFModFortRiseTournamentModule Instance;

    // TournamentSession ne pose aucun hook (son Load() ne fait que journaliser) :
    // il n'est donc pas dans cette liste, mais appele directement plus bas.
    internal Type[] Hookables = [
        typeof(MyTournamentMenuButton),
        typeof(MyVersusMatchResults),
    ];

    // Racine des donnees du mod (Saves/<nom du mod>/). FortRise 5 vit hors du
    // repertoire de TowerFall : on n'ecrit plus dans .\Mods\... ni dans le dossier
    // du jeu.
    public static string SavePath => Path.Combine(ModIO.GetRootPath(), "Saves", Instance.Meta.Name);

    public TFModFortRiseTournamentModule(IModContent content, IModuleContext context, ILogger logger) : base(content, context, logger)
    {
      if (!Debugger.IsAttached)
      {
        //Debugger.Launch(); // Proposera d’attacher Visual Studio
      }
      Instance = this;

      TFModFortRiseTournament.Logger.Init(SavePath);

      // CustomName n'exporte plus via MonoMod.ModInterop en FortRise 5 : il publie
      // une interface via GetApi(). Dependance optionnelle, d'ou le null tolere.
      CustomNameImport.Api = context.Interop.GetApi<ICustomNameModApi>("CustomName");
      if (CustomNameImport.Api == null)
        TFModFortRiseTournament.Logger.Info("[CustomName] mod absent : repli sur les noms P1..P8");

      foreach (var hookable in Hookables)
      {
        hookable.GetMethod(nameof(IHookable.Load))!.Invoke(null, [context.Harmony]);
      }

      // Tournament system
      TFModFortRiseTournament.Logger.Info("Initializing Tournament system...");
      Tournament.TournamentPlayerManager.Initialize();
      Tournament.TournamentSession.Load();
      TFModFortRiseTournament.Logger.Info("Tournament system loaded");
    }
  }
}

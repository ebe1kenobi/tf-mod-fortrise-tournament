//todo ajout tournament variant, desactivate variant orb ...
using System;
using System.Diagnostics;
using System.IO;
using FortRise;
using Microsoft.Extensions.Logging;
using Monocle;

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

    // Entree renvoyee par le registre. Sa propriete Subtexture n'est PAS lue ici :
    // elle declenche Texture2D.FromStream(Engine.Instance.GraphicsDevice, ...), et
    // au moment ou les modules sont construits le peripherique graphique n'existe
    // pas encore. La resoudre trop tot levait, l'icone restait donc absente.
    private static ISubtextureEntry tournamentIconEntry;
    private static Subtexture tournamentIcon;
    private static bool tournamentIconFailed;

    /// <summary>
    /// Icone du bouton TOURNAMENT du menu principal (64x64, comme celles des
    /// boutons du jeu). Chargee au premier acces, c'est-a-dire a la construction
    /// du bouton : le menu existe alors et le peripherique graphique est pret.
    /// Null si la texture est absente ou illisible ; le bouton reste alors
    /// purement textuel, comme avant.
    /// </summary>
    public static Subtexture TournamentIcon
    {
      get
      {
        if (tournamentIcon != null || tournamentIconFailed || tournamentIconEntry == null)
          return tournamentIcon;

        try
        {
          tournamentIcon = tournamentIconEntry.Subtexture;
        }
        catch (Exception ex)
        {
          // Une seule tentative : sinon on relancerait le chargement a chaque
          // construction du bouton.
          tournamentIconFailed = true;
          TFModFortRiseTournament.Logger.Info($"[Icon] texture illisible : {ex.Message}");
        }

        return tournamentIcon;
      }
    }

    /// <summary>
    /// Enregistre les textures livrees avec le mod dans l'atlas du menu. Le PNG est
    /// genere par tools/make_icon.py et vit dans ModFile/Content/Atlas/.
    /// </summary>
    private static void RegisterTextures(IModContent content, IModuleContext context)
    {
      try
      {
        IResourceInfo info;
        if (!content.Root.TryGetRelativePath("Content/Atlas/tournamentMode.png", out info))
        {
          TFModFortRiseTournament.Logger.Info("[Icon] tournamentMode.png introuvable dans le contenu du mod");
          return;
        }

        // MenuAtlas : c'est celui ou vivent les icones des boutons du menu.
        // On garde l'entree telle quelle ; la texture ne sera lue qu'au premier
        // acces a TournamentIcon (voir le commentaire la-bas).
        tournamentIconEntry = context.Registry.Subtextures.RegisterTexture(
            info, SubtextureAtlasDestination.MenuAtlas);

        TFModFortRiseTournament.Logger.Info(tournamentIconEntry != null
            ? "[Icon] icone du bouton tournoi enregistree"
            : "[Icon] enregistrement refuse, bouton textuel");
      }
      catch (Exception ex)
      {
        // Une icone manquante ne doit pas empecher le mod de se charger.
        TFModFortRiseTournament.Logger.Info($"[Icon] chargement impossible : {ex.Message}");
      }
    }

    public TFModFortRiseTournamentModule(IModContent content, IModuleContext context, ILogger logger) : base(content, context, logger)
    {
      if (!Debugger.IsAttached)
      {
        //Debugger.Launch(); // Proposera dâ€™attacher Visual Studio
      }
      Instance = this;

      TFModFortRiseTournament.Logger.Init(logger);

      RegisterTextures(content, context);

      // Profiles est une dependance optionnelle : le tournoi tourne sans lui, sur son
      // fichier de noms. Present, il fournit les noms de joueurs affiches en jeu, et
      // sa liste de profils devient une source de roster au choix.
      //
      // L'interop de FortRise construit son proxy sur la forme des membres : il suffit
      // que IProfilesModApi decrive ce que Profiles expose.
      ProfilesImport.Api = context.Interop.GetApi<IProfilesModApi>("Archer");
      if (ProfilesImport.Api == null)
        TFModFortRiseTournament.Logger.Info("[Profiles] mod absent : repli sur les noms P1..P8");

      // Le roster est demande a part, avec une version minimale : Profiles ne le
      // publie que depuis la 1.16, et reclamer un membre absent ferait echouer le
      // proxy - donc perdre aussi les noms de joueurs, qui eux marchent depuis
      // toujours.
      ProfilesImport.Roster = context.Interop.GetApi<IProfilesRosterApi>(
          "Archer", new SemanticVersion(1, 16, 0));
      if (ProfilesImport.Api != null && ProfilesImport.Roster == null)
        TFModFortRiseTournament.Logger.Info(
            "[Profiles] version anterieure a 1.16 : roster limite au fichier JSON");

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

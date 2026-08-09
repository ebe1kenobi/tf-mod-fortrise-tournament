using System;
using Monocle;
using TowerFall;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Configuration et lancement d'un match du tournoi.
  ///
  /// Extrait de TournamentMatchIntroScene pour que l'ecran d'assignation des
  /// manettes puisse lancer le match sans dupliquer cette logique.
  /// </summary>
  public static class TournamentMatchLauncher
  {
    /// <summary>
    /// Active les joueurs du match.
    ///
    /// inputOfSlot[i] donne la MANETTE qui tient le i-eme joueur du match ; -1
    /// signifie "meme index que le slot", c'est-a-dire l'ancien comportement ou le
    /// premier nomme devait tenir la manette 0. C'est precisement ce que l'ecran
    /// d'assignation supprime : il fournit la manette reellement tenue par chacun,
    /// et le nom suit la manette au lieu de l'inverse.
    ///
    /// archers[i] / alts[i] : archer retenu pour ce joueur (null = laisser tel quel).
    /// </summary>
    public static void ConfigurePlayers(TournamentMatch match, int[] inputOfSlot,
                                        int[] archers, int[] alts)
    {
      if (match == null)
      {
        Logger.Info("Error: No current match to configure");
        return;
      }

      for (int i = 0; i < 4; i++)
      {
        TFGame.Players[i] = false;
        TFGame.Characters[i] = i;
      }

      int slot = 0;
      foreach (var playerName in match.Players)
      {
        if (slot >= 4) break;
        if (playerName == "?") continue;

        int input = (inputOfSlot != null && slot < inputOfSlot.Length && inputOfSlot[slot] >= 0)
            ? inputOfSlot[slot]
            : slot;
        if (input < 0 || input > 3) input = slot;

        TFGame.Players[input] = true;

        if (archers != null && slot < archers.Length && archers[slot] >= 0)
          TFGame.Characters[input] = archers[slot];
        if (alts != null && slot < alts.Length && alts[slot] >= 0)
          TFGame.AltSelect[input] = (ArcherData.ArcherTypes)alts[slot];

        // Le tournoi ne passe pas par l'ecran de selection des archers : c'est la que
        // Profiles rattache normalement un profil a un joueur. Sans ce rattachement,
        // le nom s'affiche mais rien d'autre ne suit - ni couleurs, ni sons, ni
        // portraits. Un nom qui ne designe aucun profil detache l'emplacement, pour
        // que le joueur du match precedent n'y laisse pas les siens.
        ProfilesImport.AssignProfile(input, playerName);

        // L'application du nom custom est optionnelle : elle peut planter selon les
        // autres mods actifs (ex: WiderSetMod modifie le Rollcall et fait planter le
        // SetPlayerName du mod Profiles). On ne doit surtout pas empecher le match
        // de se lancer pour ca -> try/catch, on continue meme si le nom n'est pas pose.
        if (ProfilesImport.IsAvailable)
        {
          try
          {
            ProfilesImport.SetPlayerName(input, playerName);
          }
          catch (Exception exName)
          {
            Logger.Info($"SetPlayerName failed for input {input} ({playerName}): {exName.Message}");
          }
        }

        slot++;
      }

      Logger.Info($"Configured FFA match with {slot} players");
    }

    /// <summary>
    /// Ramene le mode de jeu au dernier survivant, sans tournoi en cours.
    ///
    /// MainMenu.VersusMatchSettings est l'objet partage avec le versus normal : il
    /// conserve le dernier mode joue, y compris un mode ajoute par un mod (Playtag,
    /// Bartizan...). Cette remise a zero sert de point de depart a l'ecran de
    /// configuration ; des qu'un tournoi existe, c'est son propre mode qui prime
    /// (voir ApplyTournamentRules).
    /// </summary>
    public static void ResetGameMode()
    {
      TournamentGameModes.Apply(TournamentGameModes.Default);
    }

    /// <summary>
    /// Applique le mode et les variantes retenus pour ce tournoi.
    ///
    /// Rejoue avant chaque match, et pas seulement au demarrage : quitter le tournoi
    /// pour un versus dans un autre mode, ou avec d'autres variantes, puis reprendre
    /// le tournoi ne doit rien changer a ses regles.
    /// </summary>
    public static void ApplyTournamentRules(TournamentSession session)
    {
      if (session == null || session.Data == null)
      {
        ResetGameMode();
        return;
      }

      TournamentGameModes.Apply(session.Data.GameMode);

      // Null = tournoi cree avant l'ecran des variantes : on laisse celles en place
      // plutot que de tout desactiver dans le dos de l'utilisateur.
      if (session.Data.ActiveVariants != null)
        TournamentVariants.ApplyActiveIds(session.Data.ActiveVariants);
    }

    /// <summary>
    /// Force le match du tournoi a utiliser exactement le nombre de rounds
    /// choisi dans les settings (un seul match, GoalScore = RoundsToWin).
    /// </summary>
    public static void ApplyMatchSettings(TournamentSession session)
    {
      if (session == null || session.Data == null)
        return;

      var settings = MainMenu.VersusMatchSettings;
      if (settings == null)
      {
        Logger.Info("Error: VersusMatchSettings is null, cannot apply tournament goal");
        return;
      }

      // Rejoue a chaque match : quitter un tournoi pour un versus dans un autre
      // mode ou d'autres variantes puis reprendre le tournoi ne doit rien changer.
      ApplyTournamentRules(session);

      int roundsToWin = session.Data.RoundsToWin;
      if (roundsToWin < 1)
        roundsToWin = 1;

      // MatchLength.Custom + CustomGoal => GoalScore renvoie exactement CustomGoal
      settings.MatchLength = MatchSettings.MatchLengths.Custom;
      MatchSettings.CustomGoal = roundsToWin;

      Logger.Info($"Tournament match settings applied: GoalScore = {roundsToWin} (Custom)");
    }

    /// <summary>
    /// Enchaine vers la map (manuelle ou directe) selon le mode configure.
    /// </summary>
    public static void ProceedToMap(TournamentSession session)
    {
      int mapMode = session.Data.MapMode;
      bool towersAvailable = GameData.VersusTowers != null && GameData.VersusTowers.Count > 0;

      if (mapMode == TournamentMapMode.Manual || !towersAvailable)
      {
        // Selection manuelle de la map (ecran habituel, chemin compatible avec les
        // mods qui patchent MapScene.StartSession, comme WiderSetMod).
        OpenMapScene();
        return;
      }

      // Aleatoire ou map fixe : lancement direct. Si ca echoue (ex: interaction
      // avec un mod qui patche le chargement de niveau comme WiderSetMod), on
      // retombe sur l'ecran de map (chemin compatible) plutot que sur le bracket.
      try
      {
        LaunchMatchDirectly(mapMode);
      }
      catch (Exception exDirect)
      {
        Logger.Info($"Direct match launch failed, falling back to map selection: {exDirect}");
        OpenMapScene();
      }
    }

    public static void OpenMapScene()
    {
      var mapScene = new MapScene(MainMenu.RollcallModes.Versus);
      Engine.Instance.Scene = mapScene;
      Logger.Info("MapScene created and set as current scene");
    }

    /// <summary>
    /// Lance le match sans passer par l'ecran de selection : choisit la tour
    /// (aleatoire ou fixe), fixe une graine, et demarre la session versus.
    /// </summary>
    private static void LaunchMatchDirectly(int mapMode)
    {
      var settings = MainMenu.VersusMatchSettings;

      int towerIndex;
      if (mapMode == TournamentMapMode.Random)
        towerIndex = new Random().Next(GameData.VersusTowers.Count);
      else
        towerIndex = mapMode;

      if (towerIndex < 0 || towerIndex >= GameData.VersusTowers.Count)
        towerIndex = 0;

      settings.LevelSystem = GameData.VersusTowers[towerIndex].GetLevelSystem();
      settings.RandomVersusTower = false;
      settings.RandomLevelSeed = new Random().Next(1000000000);

      MainMenu.CurrentMatchSettings = settings;

      Logger.Info($"Launching tournament match directly on tower {towerIndex}");
      new Session(settings).StartGame();
    }
  }
}

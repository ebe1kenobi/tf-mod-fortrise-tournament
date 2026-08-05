using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Ecran d'assignation des manettes, insere entre l'annonce du match et la map.
  ///
  /// Probleme resolu : le tournoi activait les joueurs dans l'ordre du bracket
  /// (premier nomme = manette 0), ce qui obligeait les joueurs a se passer les
  /// manettes entre chaque match sans jamais savoir laquelle etait laquelle. Ici
  /// c'est l'inverse : chaque joueur prend la place a son nom avec la manette
  /// qu'il a deja en main, et le match suit cette assignation.
  ///
  /// Fonctionnement facon rollcall : une manette libre se deplace entre les places
  /// disponibles et confirme celle qu'elle veut ; elle choisit ensuite son archer,
  /// puis valide. Le match part quand toutes les places sont validees.
  /// </summary>
  public class TournamentControllerSetupScene : Entity
  {
    private const int MaxInputs = 4;
    private const float SlotSpacing = 72f;
    private const float SlotY = 100f;

    private sealed class Slot
    {
      public string Name;
      public int InputIndex = -1;                 // manette assignee, -1 si libre
      public int CharacterIndex;
      public ArcherData.ArcherTypes AltSelect = ArcherData.ArcherTypes.Normal;
      public bool Ready;
      public ArcherPortrait Portrait;
      public Vector2 Position;
    }

    private readonly TournamentSession session;
    private readonly TournamentMatch currentMatch;
    private readonly List<Slot> slots = new List<Slot>();

    // Place survolee par chaque manette tant qu'elle n'est assignee a aucune.
    private readonly int[] cursor = new int[MaxInputs];

    private float elapsed;
    private bool proceeding;

    public static bool IsOpen { get; private set; }
    public static TournamentControllerSetupScene Instance { get; private set; }

    public TournamentControllerSetupScene()
    {
      // Les portraits sont des composants rendus relativement a l'entite : on la
      // laisse a l'origine pour raisonner en coordonnees ecran (320x240).
      Position = Vector2.Zero;

      session = TournamentSession.Current;
      if (session != null && session.IsActive)
        currentMatch = session.GetCurrentMatch();

      if (currentMatch == null)
      {
        Logger.Info("TournamentControllerSetupScene: no current match");
        return;
      }

      BuildSlots();
    }

    private void BuildSlots()
    {
      List<string> names = new List<string>();
      foreach (string name in currentMatch.Players)
      {
        if (name != "?" && names.Count < MaxInputs)
          names.Add(name);
      }

      float startX = 160f - (names.Count * SlotSpacing) / 2f + SlotSpacing / 2f;

      for (int i = 0; i < names.Count; i++)
      {
        Slot slot = new Slot
        {
          Name = names[i],
          Position = new Vector2(startX + i * SlotSpacing, SlotY),
        };

        RestoreArcher(slot, i);

        slot.Portrait = new ArcherPortrait(slot.Position, slot.CharacterIndex, slot.AltSelect, true);
        Add(slot.Portrait);
        slots.Add(slot);
      }

      for (int i = 0; i < MaxInputs; i++)
        cursor[i] = 0;

      Logger.Info($"TournamentControllerSetupScene: {slots.Count} places a pourvoir");
    }

    /// <summary>
    /// Reprend l'archer memorise pour ce joueur ; a defaut, le premier archer
    /// disponible, pour ne jamais partir sur un archer deja pris par un voisin.
    /// </summary>
    private void RestoreArcher(Slot slot, int index)
    {
      TournamentArcherChoice choice = null;
      if (session != null && session.Data != null && session.Data.ArcherChoices != null)
        session.Data.ArcherChoices.TryGetValue(slot.Name, out choice);

      if (choice != null && IsArcherSelectable(choice.CharacterIndex, null))
      {
        slot.CharacterIndex = choice.CharacterIndex;
        slot.AltSelect = (ArcherData.ArcherTypes)choice.AltSelect;
        return;
      }

      slot.CharacterIndex = FirstFreeArcher(index);
    }

    private int FirstFreeArcher(int fallback)
    {
      for (int c = 0; c < ArcherData.Amount; c++)
      {
        if (IsArcherSelectable(c, null))
          return c;
      }
      return fallback % Math.Max(1, ArcherData.Amount);
    }

    /// <summary>
    /// Un archer est selectionnable s'il est debloque et qu'aucune AUTRE place ne
    /// l'a deja pris. On ne s'appuie pas sur TFGame.CharacterTaken : les joueurs
    /// du match ne sont pas encore actifs a ce stade.
    /// </summary>
    private bool IsArcherSelectable(int characterIndex, Slot forSlot)
    {
      if (characterIndex < 0 || characterIndex >= ArcherData.Amount)
        return false;

      if (!SaveData.Instance.Unlocks.GetArcherUnlocked(characterIndex))
        return false;

      foreach (Slot other in slots)
      {
        if (ReferenceEquals(other, forSlot)) continue;
        if (other.CharacterIndex == characterIndex) return false;
      }
      return true;
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
      elapsed += Engine.DeltaTime;

      if (proceeding) return;

      if (session == null || !session.IsActive || currentMatch == null || slots.Count == 0)
      {
        Logger.Info("TournamentControllerSetupScene: etat invalide, retour au bracket");
        ReturnToBracket();
        return;
      }

      for (int i = 0; i < MaxInputs; i++)
      {
        PlayerInput input = ActiveInput(i);
        if (input == null) continue;

        Slot mine = SlotOfInput(i);
        if (mine == null) UpdateUnassigned(i, input);
        else if (!mine.Ready) UpdateChoosing(i, input, mine);
        else UpdateReady(i, input, mine);

        if (proceeding) return;
      }

      if (AllReady())
        Proceed();
    }

    // --- manette pas encore installee : elle se deplace et confirme une place ---

    /// <summary>
    /// Manette utilisable : branchee, donc capable de prendre une place. Sans ce
    /// filtre, une manette absente afficherait un curseur fantome et laisserait
    /// croire qu'une place est en train d'etre prise.
    /// </summary>
    private static PlayerInput ActiveInput(int index)
    {
      if (index < 0 || index >= TFGame.PlayerInputs.Length) return null;
      PlayerInput input = TFGame.PlayerInputs[index];
      if (input == null || !input.Attached) return null;
      return input;
    }

    private void UpdateUnassigned(int inputIndex, PlayerInput input)
    {
      if (input.MenuLeft) MoveCursor(inputIndex, -1);
      if (input.MenuRight) MoveCursor(inputIndex, 1);

      // Echappatoire : sans elle, une soiree avec moins de manettes que de joueurs
      // au match resterait bloquee sur cet ecran, AllReady ne pouvant jamais etre
      // vrai. Une manette sans place ramene donc tout le monde au bracket.
      if (input.MenuBack)
      {
        Logger.Info("Assignation annulee, retour au bracket");
        proceeding = true;
        ReturnToBracket();
        return;
      }

      if (input.MenuConfirm || input.MenuStart)
      {
        Slot target = SlotAt(cursor[inputIndex]);
        if (target == null || target.InputIndex >= 0)
        {
          Sounds.ui_invalid.Play(160f, 1f);
          return;
        }

        target.InputIndex = inputIndex;
        Sounds.ui_click.Play(target.Position.X, 1f);
      }
    }

    /// <summary>Deplace le curseur sur la place libre suivante, en bouclant.</summary>
    private void MoveCursor(int inputIndex, int dir)
    {
      int start = cursor[inputIndex];
      for (int step = 1; step <= slots.Count; step++)
      {
        int candidate = ((start + dir * step) % slots.Count + slots.Count) % slots.Count;
        if (slots[candidate].InputIndex < 0)
        {
          if (candidate != start)
          {
            cursor[inputIndex] = candidate;
            Sounds.ui_move1.Play(160f, 1f);
          }
          return;
        }
      }
    }

    // --- manette installee : choix de l'archer, puis validation ---

    private void UpdateChoosing(int inputIndex, PlayerInput input, Slot slot)
    {
      if (input.MenuLeft) ChangeArcher(slot, -1);
      if (input.MenuRight) ChangeArcher(slot, 1);

      // Variante de l'archer (skin alternatif), comme au rollcall.
      if (input.MenuAlt) ToggleAlt(slot);

      if (input.MenuConfirm || input.MenuStart)
      {
        slot.Ready = true;
        slot.Portrait.Join(false);
        return;
      }

      if (input.MenuBack)
      {
        // On rend la place : le joueur s'est trompe de manette.
        slot.InputIndex = -1;
        cursor[inputIndex] = slots.IndexOf(slot);
        Sounds.ui_click.Play(slot.Position.X, 1f);
      }
    }

    private void UpdateReady(int inputIndex, PlayerInput input, Slot slot)
    {
      if (input.MenuBack)
      {
        slot.Ready = false;
        // SetCharacter est sans effet tant que le portrait est "joined" : il faut
        // le liberer avant de pouvoir rechanger d'archer.
        slot.Portrait.Leave();
      }
    }

    private void ChangeArcher(Slot slot, int dir)
    {
      for (int step = 1; step <= ArcherData.Amount; step++)
      {
        int candidate = ((slot.CharacterIndex + dir * step) % ArcherData.Amount + ArcherData.Amount) % ArcherData.Amount;
        if (!IsArcherSelectable(candidate, slot)) continue;

        slot.CharacterIndex = candidate;
        slot.Portrait.SetCharacter(slot.CharacterIndex, slot.AltSelect, dir);
        return;
      }
      Sounds.ui_invalid.Play(slot.Position.X, 1f);
    }

    private void ToggleAlt(Slot slot)
    {
      slot.AltSelect = slot.AltSelect == ArcherData.ArcherTypes.Alt
          ? ArcherData.ArcherTypes.Normal
          : ArcherData.ArcherTypes.Alt;
      slot.Portrait.SetCharacter(slot.CharacterIndex, slot.AltSelect, 0);
      Sounds.ui_click.Play(slot.Position.X, 1f);
    }

    private Slot SlotOfInput(int inputIndex)
    {
      foreach (Slot slot in slots)
      {
        if (slot.InputIndex == inputIndex) return slot;
      }
      return null;
    }

    private Slot SlotAt(int index)
    {
      if (index < 0 || index >= slots.Count) return null;
      return slots[index];
    }

    private bool AllReady()
    {
      foreach (Slot slot in slots)
      {
        if (slot.InputIndex < 0 || !slot.Ready) return false;
      }
      return true;
    }

    // --- sortie ---

    private void Proceed()
    {
      proceeding = true;

      int[] inputOfSlot = new int[slots.Count];
      int[] archers = new int[slots.Count];
      int[] alts = new int[slots.Count];

      for (int i = 0; i < slots.Count; i++)
      {
        inputOfSlot[i] = slots[i].InputIndex;
        archers[i] = slots[i].CharacterIndex;
        alts[i] = (int)slots[i].AltSelect;
        Logger.Info($"{slots[i].Name} -> manette {slots[i].InputIndex}, archer {slots[i].CharacterIndex}");
      }

      SaveArcherChoices();

      try
      {
        TournamentMatchLauncher.ConfigurePlayers(currentMatch, inputOfSlot, archers, alts);
        TournamentMatchLauncher.ApplyMatchSettings(session);

        // Monocle n'appelle pas Removed() lors d'un changement de scene : on remet
        // nous-memes les flags statiques a zero avant de partir.
        Instance = null;
        IsOpen = false;

        TournamentMatchLauncher.ProceedToMap(session);
      }
      catch (Exception ex)
      {
        Logger.Info($"Error launching tournament match: {ex}");
        proceeding = false;
        ReturnToBracket();
      }
    }

    /// <summary>Memorise l'archer de chacun pour les matchs suivants.</summary>
    private void SaveArcherChoices()
    {
      if (session == null || session.Data == null) return;

      if (session.Data.ArcherChoices == null)
        session.Data.ArcherChoices = new Dictionary<string, TournamentArcherChoice>();

      foreach (Slot slot in slots)
      {
        session.Data.ArcherChoices[slot.Name] = new TournamentArcherChoice
        {
          CharacterIndex = slot.CharacterIndex,
          AltSelect = (int)slot.AltSelect,
        };
      }

      try
      {
        TournamentSave.Save(session.Data);
      }
      catch (Exception ex)
      {
        // Perdre la memorisation est benin : le match doit partir quand meme.
        Logger.Info($"Sauvegarde des archers impossible : {ex.Message}");
      }
    }

    private void ReturnToBracket()
    {
      Scene.Add(new TournamentBracketScene());
      RemoveSelf();
    }

    // --- rendu ---

    public override void Render()
    {
      if (session == null || currentMatch == null || slots.Count == 0)
      {
        Draw.TextCentered(TFGame.Font, "ERROR", new Vector2(160f, 120f), Color.Red);
        return;
      }

      Draw.OutlineTextCentered(TFGame.Font, "TAKE YOUR CONTROLLER",
          new Vector2(160f, 20f), Color.White, 1.2f);

      Draw.TextCentered(TFGame.Font, TournamentText.Safe(currentMatch.GetRoundName()),
          new Vector2(160f, 36f), Calc.HexToColor("FFEC5E"));

      // Portraits (composants de l'entite).
      base.Render();

      for (int i = 0; i < slots.Count; i++)
        RenderSlot(slots[i], i);

      RenderCursors();
      RenderHint();
    }

    private void RenderSlot(Slot slot, int index)
    {
      // Nom du joueur au-dessus du portrait : c'est lui qui fait le lien entre le
      // bracket et la manette.
      Color nameColor = slot.Ready ? Calc.HexToColor("5EFF5E")
          : (slot.InputIndex >= 0 ? Color.White : Color.Gray);

      Draw.OutlineTextCentered(TFGame.Font, TournamentText.Safe(slot.Name),
          slot.Position + new Vector2(0f, -52f), nameColor, 1f);

      if (slot.InputIndex < 0)
      {
        Draw.TextCentered(TFGame.Font, "FREE",
            slot.Position + new Vector2(0f, 52f), Color.Gray * 0.8f);
        return;
      }

      // Icone de la manette reellement tenue : c'est la verification visuelle que
      // tout cet ecran existe pour permettre.
      PlayerInput input = ActiveInput(slot.InputIndex);
      Subtexture icon = input != null ? input.Icon : null;
      if (icon != null)
        Draw.TextureCentered(icon, slot.Position + new Vector2(0f, 54f), Color.White);

      Draw.TextCentered(TFGame.Font, "P" + (slot.InputIndex + 1),
          slot.Position + new Vector2(0f, 68f), nameColor);

      if (slot.Ready)
        Draw.OutlineTextCentered(TFGame.Font, "READY",
            slot.Position + new Vector2(0f, 40f), Calc.HexToColor("5EFF5E"), 1f);
    }

    /// <summary>Curseur des manettes qui n'ont pas encore de place.</summary>
    private void RenderCursors()
    {
      float blink = (float)Math.Sin(elapsed * 6f) * 0.5f + 0.5f;

      for (int i = 0; i < MaxInputs; i++)
      {
        if (ActiveInput(i) == null) continue;
        if (SlotOfInput(i) != null) continue;

        Slot target = SlotAt(cursor[i]);
        if (target == null) continue;

        // Decale verticalement par manette pour que deux curseurs sur la meme
        // place restent lisibles.
        Vector2 pos = target.Position + new Vector2(0f, 82f + i * 9f);
        Draw.TextCentered(TFGame.Font, "P" + (i + 1) + " >",
            pos, Color.White * (0.35f + 0.65f * blink));
      }
    }

    private void RenderHint()
    {
      string hint;
      if (!AnyAssigned())
        hint = "LEFT/RIGHT: SLOT   A: TAKE   B: BACK";
      else
        hint = "LEFT/RIGHT: ARCHER   A: READY   B: CANCEL";

      Draw.TextCentered(TFGame.Font, hint, new Vector2(160f, 224f), Color.Gray * 0.8f);
    }

    private bool AnyAssigned()
    {
      foreach (Slot slot in slots)
      {
        if (slot.InputIndex >= 0) return true;
      }
      return false;
    }
  }
}

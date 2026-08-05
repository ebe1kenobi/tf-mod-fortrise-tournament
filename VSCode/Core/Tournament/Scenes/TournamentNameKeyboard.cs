using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using TowerFall;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Clavier virtuel de saisie d'un nom de joueur, inspire de celui du mod
  /// CustomName mais dispose en vraie grille (navigation haut/bas/gauche/droite
  /// coherente) et utilisable au clavier physique.
  ///
  /// Point delicat : au clavier, TowerFall mappe MenuConfirm sur la touche de saut
  /// et MenuBack sur la touche de tir (X par defaut). Taper un nom declencherait
  /// donc la validation ou la sortie de l'ecran des qu'on tape ces lettres. Les
  /// entrees de type KeyboardInput sont donc IGNOREES pour la navigation : au
  /// clavier on passe par TextInputEXT, et seule Echap garde un role direct.
  ///
  /// La frappe utilise la saisie texte du systeme (TextInputEXT, le meme mecanisme
  /// que UIInputText de FortRise) et non les codes de touches : Keys.D8 designe la
  /// touche physique a la position du 8 en QWERTY, si bien qu'en AZERTY elle
  /// produisait toujours "8" au lieu du tiret bas. TextInputEXT rend le caractere
  /// reellement tape, disposition clavier et Shift compris.
  /// </summary>
  public class TournamentNameKeyboard : Entity
  {
    private const int Columns = 10;
    private const int MaxLength = 12;

    // Une case par caractere saisissable. Tout caractere tape au clavier qui n'est
    // pas dans cette liste est refuse : la police du jeu ne sait pas tout rendre.
    private const string Charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 .'-_?!:()\\/";

    // Repetition quand on maintient une direction : une premiere pause pour
    // permettre le pas a pas, puis un defilement continu.
    private const float RepeatFirstDelay = 0.35f;
    private const float RepeatDelay = 0.07f;

    private static readonly Vector2 GridOrigin = new Vector2(70f, 92f);
    private const float CellW = 18f;
    private const float CellH = 16f;

    private readonly Func<string, bool> onValidate;   // rend false si le nom est refuse
    private readonly Action onCancel;
    private readonly List<string> existingNames;

    private string current = "";
    private int selected;
    private float repeatTimer;
    private int heldX, heldY;
    private float blink;
    private string error;

    public static bool IsOpen { get; private set; }

    public TournamentNameKeyboard(List<string> existingNames,
                                  Func<string, bool> onValidate, Action onCancel)
    {
      this.existingNames = existingNames ?? new List<string>();
      this.onValidate = onValidate;
      this.onCancel = onCancel;
      Depth = -100000;
    }

    public override void Added()
    {
      base.Added();
      IsOpen = true;
      TextInputEXT.TextInput += HandleChar;
      TextInputEXT.StartTextInput();
      Sounds.ui_pause.Play(160f);
    }

    public override void Removed()
    {
      base.Removed();
      IsOpen = false;
      // L'evenement est statique : ne pas se desabonner laisserait cet ecran
      // capter la frappe pour toute la duree du jeu.
      TextInputEXT.TextInput -= HandleChar;
      TextInputEXT.StopTextInput();
      MenuInput.Clear();
    }

    /// <summary>
    /// Caractere reellement produit par le clavier (disposition et Shift compris).
    /// FNA fait passer par ce meme canal le retour arriere (8) et l'entree (10).
    /// </summary>
    private void HandleChar(char c)
    {
      if (c == 8)
      {
        Backspace();
        return;
      }

      if (c == 10 || c == 13)
      {
        Validate();
        return;
      }

      if (c == '\t') return;

      char upper = char.ToUpperInvariant(c);
      if (Charset.IndexOf(upper) < 0)
      {
        // Accent, symbole exotique... : refuse plutot que d'ecrire un caractere
        // que la police ne rend pas.
        Sounds.ui_invalid.Play(160f, 1f);
        return;
      }

      Append(upper);
    }

    // --- saisie ---

    private void Append(char c)
    {
      if (current.Length >= MaxLength)
      {
        Sounds.ui_invalid.Play(160f, 1f);
        return;
      }
      current += c;
      error = null;
      Sounds.ui_move1.Play(160f, 1f);
    }

    private void Backspace()
    {
      if (current.Length == 0) return;
      current = current.Substring(0, current.Length - 1);
      error = null;
      Sounds.ui_move1.Play(160f, 1f);
    }

    private void Validate()
    {
      string name = current.Trim().ToUpper();

      if (name.Length == 0)
      {
        error = "EMPTY NAME";
        Sounds.ui_invalid.Play(160f, 1f);
        return;
      }

      if (existingNames.Contains(name))
      {
        error = "ALREADY IN THE LIST";
        Sounds.ui_invalid.Play(160f, 1f);
        return;
      }

      if (onValidate != null && !onValidate(name))
      {
        error = "COULD NOT SAVE";
        Sounds.ui_invalid.Play(160f, 1f);
        return;
      }

      Sounds.ui_click.Play(160f, 1f);
      Close();
    }

    private void Cancel()
    {
      Sounds.ui_click.Play(160f, 1f);
      if (onCancel != null) onCancel();
      Close();
    }

    private void Close()
    {
      IsOpen = false;
      RemoveSelf();
    }

    // --- boucle ---

    public override void Update()
    {
      base.Update();
      repeatTimer -= Engine.DeltaTime;
      blink += Engine.DeltaTime;

      UpdatePhysicalKeyboard();
      UpdateGamepads();
    }

    /// <summary>
    /// Seules les touches que la saisie texte ne transmet pas restent lues ici.
    /// Les lettres et symboles passent par HandleChar (voir le commentaire de
    /// classe) : les lire par code de touche ignorerait la disposition du clavier.
    /// </summary>
    private void UpdatePhysicalKeyboard()
    {
      if (MInput.Keyboard == null) return;

      if (MInput.Keyboard.Pressed(Keys.Escape)) Cancel();
      if (MInput.Keyboard.Pressed(Keys.Delete)) Backspace();
    }

    /// <summary>
    /// Navigation dans la grille, manettes uniquement. N'importe quelle manette
    /// pilote le clavier : celui qui a demande l'ajout n'est pas forcement celui
    /// qui tape.
    /// </summary>
    private void UpdateGamepads()
    {
      int dx = 0, dy = 0;

      for (int i = 0; i < TFGame.PlayerInputs.Length; i++)
      {
        PlayerInput input = TFGame.PlayerInputs[i];
        if (input == null || !input.Attached) continue;

        // Le clavier est traite a part : le prendre aussi ici ferait valider ou
        // quitter des qu'on tape la lettre de saut ou de tir.
        if (input is KeyboardInput) continue;

        // ...Check et non le pressed : c'est ce qui permet au curseur de continuer
        // a defiler tant que la direction est maintenue.
        if (input.MenuRightCheck) dx = 1;
        else if (input.MenuLeftCheck) dx = -1;
        if (input.MenuDownCheck) dy = 1;
        else if (input.MenuUpCheck) dy = -1;

        if (input.MenuConfirm) Append(Charset[selected]);
        if (input.MenuAlt) Backspace();
        if (input.MenuStart) Validate();
        if (input.MenuBack) Cancel();
      }

      UpdateRepeat(dx, dy);
    }

    /// <summary>
    /// Deplacement au maintien : un pas immediat, une pause, puis un defilement
    /// continu. Le compteur repart des que la direction change ou est relachee,
    /// pour que le pas a pas reste possible.
    /// </summary>
    private void UpdateRepeat(int dx, int dy)
    {
      if (dx == 0 && dy == 0)
      {
        heldX = heldY = 0;
        repeatTimer = 0f;
        return;
      }

      if (dx != heldX || dy != heldY)
      {
        heldX = dx;
        heldY = dy;
        repeatTimer = RepeatFirstDelay;
        Move(dx, dy);
        return;
      }

      if (repeatTimer <= 0f)
      {
        repeatTimer = RepeatDelay;
        Move(dx, dy);
      }
    }

    private void Move(int dx, int dy)
    {
      int index = selected + dx + dy * Columns;

      // On borne au lieu de boucler : sauter d'un bout a l'autre de la grille sur
      // une simple pression est desorientant.
      if (index < 0 || index >= Charset.Length) return;

      selected = index;
      Sounds.ui_move1.Play(160f, 1f);
    }

    // --- rendu ---

    public override void Render()
    {
      Draw.Rect(0f, 0f, 320f, 240f, new Color(0, 0, 0, 200));

      Draw.OutlineTextCentered(TFGame.Font, "NEW PLAYER",
          new Vector2(160f, 26f), Color.White, 1.2f);

      // Champ de saisie, avec un curseur clignotant pour montrer qu'on peut taper.
      string shown = current + (((int)(blink * 2f) % 2) == 0 ? "_" : " ");
      Draw.OutlineTextCentered(TFGame.Font, shown,
          new Vector2(160f, 52f), Calc.HexToColor("FFEC5E"), 1.4f);

      if (error != null)
        Draw.TextCentered(TFGame.Font, error, new Vector2(160f, 70f), Color.Red);

      for (int i = 0; i < Charset.Length; i++)
      {
        int col = i % Columns;
        int row = i / Columns;
        Vector2 pos = GridOrigin + new Vector2(col * CellW, row * CellH);

        bool active = i == selected;
        // "SP" et non "_" : le tiret bas est desormais un caractere a part entiere
        // de la grille, les confondre rendrait l'un des deux introuvable.
        string label = Charset[i] == ' ' ? "SP" : Charset[i].ToString();

        if (active)
          Draw.Rect(pos.X - 8f, pos.Y - 7f, 16f, 14f, Color.White * 0.25f);

        Draw.TextCentered(TFGame.Font, label, pos,
            active ? Calc.HexToColor("FFEC5E") : Color.White);
      }

      Draw.TextCentered(TFGame.Font, "TYPE ON KEYBOARD OR PICK A LETTER",
          new Vector2(160f, 196f), Color.Gray * 0.8f);
      Draw.TextCentered(TFGame.Font, "A: LETTER  RB: DELETE  START/ENTER: OK  B/ESC: CANCEL",
          new Vector2(160f, 210f), Color.Gray * 0.8f);
    }
  }
}

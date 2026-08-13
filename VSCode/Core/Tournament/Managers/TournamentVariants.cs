using System;
using System.Collections.Generic;
using System.Reflection;
using FortRise;
using TowerFall;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Catalogue des variantes selectionnables, et lecture/ecriture de leur etat.
  ///
  /// L'ecran de variantes du jeu n'est pas reutilisable ici : MatchVariants.BuildMenu
  /// prend un MainMenu, y ajoute ses entites et pilote ses guides de boutons, alors
  /// que le tournoi vit dans sa propre Scene. On reconstruit donc la liste nous-memes
  /// a partir des memes donnees.
  ///
  /// Identifiants : pour une variante du jeu c'est le nom du champ de MatchVariants
  /// (la meme cle que ModVariants.GetVariant utilise), pour une variante de mod c'est
  /// sa cle dans CustomVariants. Ce sont ces identifiants qui sont sauvegardes avec
  /// le tournoi.
  /// </summary>
  public static class TournamentVariants
  {
    public sealed class Entry
    {
      public string Id;
      public string Title;
      public string Header;
      public Variant Variant;
      public bool PerPlayer;
    }

    private static List<Entry> cache;
    private static MatchVariants cachedFor;

    /// <summary>
    /// Variantes affichables, dans l'ordre : celles du jeu puis celles des mods,
    /// chacune regroupee par son en-tete.
    /// </summary>
    public static List<Entry> GetEntries()
    {
      MatchVariants variants = Current;
      if (variants == null)
        return new List<Entry>();

      // Le tableau est reconstruit a chaque MatchVariants : on recalcule si l'objet
      // a change (nouveau match, rechargement).
      if (cache != null && ReferenceEquals(cachedFor, variants))
        return cache;

      var entries = new List<Entry>();
      var seen = new HashSet<Variant>();

      foreach (FieldInfo field in typeof(MatchVariants).GetFields(BindingFlags.Public | BindingFlags.Instance))
      {
        if (field.FieldType != typeof(Variant))
          continue;

        var variant = field.GetValue(variants) as Variant;
        if (variant == null || !variant.VisibleInMenu || !seen.Add(variant))
          continue;

        entries.Add(new Entry
        {
          Id = field.Name,
          Title = Label(variant, field.Name),
          Header = variant.Header,
          Variant = variant,
          PerPlayer = variant.PerPlayer,
        });
      }

      foreach (var pair in variants.CustomVariants)
      {
        var variant = pair.Value;
        if (variant == null || !variant.VisibleInMenu || !seen.Add(variant))
          continue;

        entries.Add(new Entry
        {
          Id = pair.Key,
          Title = Label(variant, pair.Key),
          Header = variant.Header,
          Variant = variant,
          PerPlayer = variant.PerPlayer,
        });
      }

      cachedFor = variants;
      cache = entries;
      return entries;
    }

    private static string Label(Variant variant, string fallback)
    {
      return string.IsNullOrEmpty(variant.Title) ? fallback : variant.Title;
    }

    /// <summary>
    /// Active ou desactive une variante depuis un ecran de configuration, ou aucun
    /// joueur n'est encore actif.
    ///
    /// Le setter de Variant.Value ne convient pas pour une variante « par joueur » :
    /// il ne retient la valeur que pour les indices ou TFGame.Players[i] est vrai, et
    /// force les autres a false. Or les joueurs ne sont actives qu'au lancement du
    /// match : ici le tableau est entierement faux, la variante etait donc remise a
    /// zero aussitot cochee. C'est ce qui empechait de selectionner les variantes par
    /// joueur (Big Heads, Cursed Bows, les variantes de fleches, les boucliers...).
    ///
    /// On ecrit donc directement dans le tableau interne, pour tous les indices.
    /// </summary>
    public static void SetValue(Variant variant, bool value)
    {
      if (variant == null)
        return;

      if (!variant.PerPlayer)
      {
        variant.Value = value;
        return;
      }

      bool[] values = GetPlayerValues(variant);
      if (values == null)
      {
        // Repli : sans acces au tableau, mieux vaut le comportement partiel que rien.
        variant.Value = value;
        return;
      }

      for (int i = 0; i < values.Length; i++)
        values[i] = value;

      // Le setter d'origine desactive les variantes liees quand on en active une :
      // on le reproduit, sinon deux variantes incompatibles pourraient coexister.
      if (value && variant.Links != null)
      {
        foreach (Variant linked in variant.Links)
          SetValue(linked, false);
      }
    }

    private static FieldInfo playerValuesField;

    private static bool[] GetPlayerValues(Variant variant)
    {
      try
      {
        if (playerValuesField == null)
        {
          playerValuesField = typeof(Variant).GetField(
              "playerValues", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        return playerValuesField != null ? playerValuesField.GetValue(variant) as bool[] : null;
      }
      catch (Exception ex)
      {
        Logger.Info($"[Tournament] variante par joueur inaccessible : {ex.Message}");
        return null;
      }
    }

    /// <summary>Desactive toutes les variantes.</summary>
    public static void ResetAll()
    {
      foreach (Entry entry in GetEntries())
        SetValue(entry.Variant, false);
    }

    /// <summary>
    /// Applique le preregage « regles de tournoi » du jeu : chaque variante prend la
    /// valeur de son propre indicateur TournamentRules.
    ///
    /// On ne peut pas appeler MatchVariants.TournamentRules() : il passe par
    /// Variant.Value, qui ignore les variantes par joueur tant qu'aucun joueur n'est
    /// actif (voir SetValue). Depuis un ecran de configuration, la moitie du
    /// preregage serait donc perdue.
    /// </summary>
    /// <summary>
    /// Le point de depart d'un nouveau tournoi : aucune variante, sauf NO AUTOBALANCE.
    ///
    /// Ce n'etait pas le cas : on posait les REGLES DE TOURNOI du jeu, soit
    /// l'ensemble des variantes marquees TournamentRules - NO AUTOBALANCE, mais aussi
    /// SYMMETRICAL TREASURE. La seconde arrivait cochee sans que personne l'ait
    /// demandee, et se retrouvait dans toutes les parties du tournoi.
    ///
    /// Ne reste que celle qui a une raison d'etre imposee : sans elle, le jeu donne
    /// des fleches et des boucliers a celui qui perd, ce qui n'a pas sa place dans une
    /// competition. Le reste est un choix, et un choix se coche.
    /// </summary>
    public static void ApplyTournamentRules()
    {
      foreach (Entry entry in GetEntries())
        SetValue(entry.Variant, ReferenceEquals(entry.Variant, Current?.NoAutobalance));
    }

    /// <summary>Variantes du match versus, partagees avec le reste du jeu.</summary>
    public static MatchVariants Current
    {
      get
      {
        var settings = MainMenu.VersusMatchSettings;
        return settings != null ? settings.Variants : null;
      }
    }

    /// <summary>Identifiants des variantes actuellement actives.</summary>
    public static List<string> GetActiveIds()
    {
      var ids = new List<string>();
      foreach (Entry entry in GetEntries())
      {
        if (entry.Variant.Value)
          ids.Add(entry.Id);
      }
      return ids;
    }

    /// <summary>
    /// Remet les variantes dans l'etat decrit par la liste : celles qui y figurent
    /// sont activees, toutes les autres desactivees. Sans ce second point, jouer un
    /// versus entre deux matchs laisserait ses propres variantes dans le tournoi.
    /// </summary>
    public static void ApplyActiveIds(List<string> ids)
    {
      if (ids == null)
        return;

      var wanted = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);

      foreach (Entry entry in GetEntries())
      {
        bool on = wanted.Contains(entry.Id);
        if (entry.Variant.Value != on)
          SetValue(entry.Variant, on);
      }
    }

    /// <summary>Nombre de variantes actives, pour l'affichage recapitulatif.</summary>
    public static int CountActive()
    {
      int n = 0;
      foreach (Entry entry in GetEntries())
      {
        if (entry.Variant.Value) n++;
      }
      return n;
    }
  }
}

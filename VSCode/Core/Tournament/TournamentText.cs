using System.Globalization;
using System.Text;
using TowerFall;

namespace TFModFortRiseTournament.Tournament
{
  /// <summary>
  /// Utilitaire pour rendre du texte sûr vis-à-vis de la police du jeu (TFGame.Font).
  /// La police "Archer" de TowerFall ne contient qu'un jeu de caractères limité :
  /// dessiner un caractère absent (flèche "→", lettres accentuées, etc.) fait planter
  /// SpriteFont.MeasureString avec ArgumentException.
  /// </summary>
  public static class TournamentText
  {
    /// <summary>
    /// Nettoie une chaîne pour qu'elle ne contienne que des caractères présents dans
    /// TFGame.Font. Remplace quelques symboles courants, retire les accents, puis filtre
    /// tout caractère restant non supporté.
    /// </summary>
    public static string Safe(string text)
    {
      if (string.IsNullOrEmpty(text))
        return string.Empty;

      // Remplacements lisibles avant filtrage
      text = text
        .Replace("→", "->")
        .Replace("←", "<-")
        .Replace("’", "'")
        .Replace("œ", "oe")
        .Replace("Œ", "OE");

      // Retirer les accents (é -> e, etc.)
      text = RemoveDiacritics(text);

      var font = TFGame.Font;
      if (font == null || font.Characters == null)
        return text;

      var sb = new StringBuilder(text.Length);
      foreach (char c in text)
      {
        if (c == '\n' || font.Characters.Contains(c))
          sb.Append(c);
        // sinon : caractère non supporté, on l'ignore
      }

      return sb.ToString();
    }

    private static string RemoveDiacritics(string text)
    {
      if (string.IsNullOrEmpty(text))
        return text;

      var normalized = text.Normalize(NormalizationForm.FormD);
      var sb = new StringBuilder(normalized.Length);

      foreach (var c in normalized)
      {
        if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
          sb.Append(c);
      }

      return sb.ToString().Normalize(NormalizationForm.FormC);
    }
  }
}

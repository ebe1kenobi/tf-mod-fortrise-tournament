// Aucune option exposee : la classe de reglages etait vide en FortRise 4, et
// ModuleSettings.Create est abstraite en FortRise 5 (une classe vide ne compile
// plus). Pour ajouter des reglages : decommenter, implementer Create, et
// surcharger CreateSettings() dans le module.

//using FortRise;

//namespace TFModFortRiseTournament
//{
//  public class TFModFortRiseTournamentSettings : ModuleSettings
//  {
//    public override void Create(ISettingsCreate settings)
//    {
//    }
//  }
//}

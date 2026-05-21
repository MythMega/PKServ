using PKServ.Configuration;
using System.Threading.Tasks;

namespace PKServ.Business
{
    /// <summary>
    /// Conservé pour compatibilité de compilation.
    /// La génération du rapport raid est désormais assurée par Raid.generateStatsCSV
    /// qui exporte un JSON dans WebExport/Data/raids/.
    /// La visualisation se fait via WebExport/raids.html.
    /// </summary>
    public static class RaidStatsReportImpl
    {
        [System.Obsolete("Remplacé par l'export JSON dans Raid.generateStatsCSV. Voir raids.html.")]
        public static Task<string> GenerateRaidReport(PKServ.Raid raid, AppSettings appSettings, GlobalAppSettings globalAppSettings, DataConnexion dataConnexion, bool shiny)
            => Task.FromResult(string.Empty);
    }
}

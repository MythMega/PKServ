using PKServ.Configuration;
using PKServ.Entity.Raid.ManualRandomRaid;
using System.Text.Json;

namespace PKServ.Business.Raid
{
    public static class ManualRandomRaidImpl
    {
        internal static string StartRandomRaid
            (
                ManualRandomRaidRequest manualRandomRaidRequest,
                GlobalAppSettings globalAppSettings,
                AppSettings settings,
                DataConnexion data,
                int userCount,
                JsonSerializerOptions optionsJson
            )
        {
            string response = string.Empty;
            ManualRandomRaid raidToStart = globalAppSettings.RaidSettings.ManualRandomRaid;
            if (manualRandomRaidRequest.ManualRandomRaid is not null)
            {
                raidToStart = manualRandomRaidRequest.ManualRandomRaid.ToManualRandomRaid(raidToStart);
            }

            PKServ.Raid raidGenerated = new(raidToStart, settings, userCount, data);

            if (settings.ActiveRaid is not null && globalAppSettings.RaidSettings.ManualRandomRaidSaveCurrentRaid)
            {
                RaidSaverImpl.SaveRaid(settings, optionsJson);
            }

            settings.ActiveRaid = raidGenerated;
            response = $"Raid : {settings.ActiveRaid.Boss.Name_FR} / {settings.ActiveRaid.Boss.Name_EN} ({settings.ActiveRaid.PVMax} HP) - {(settings.ActiveRaid.DisplayShiny ? "Shiny" : "Normal")}";
            return response;
        }
    }
}
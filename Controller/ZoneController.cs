using PKServ.Business;
using PKServ.Configuration;
using PKServ.Entity;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKServ.Controller
{
    /// <summary>
    /// Routes Zone/* :
    /// Zone/GetCurrentZone, Zone/Move/Normal, Zone/Move/Random, Zone/Move/Smart
    /// </summary>
    public class ZoneController : BaseController
    {
        public ZoneController(ControllerContext ctx) : base(ctx) { }

        public override async Task<string> HandlePostAsync(string path, string body)
        {
            switch (path)
            {
                case "Zone/GetCurrentZone":
                    User userZone = JsonSerializer.Deserialize<User>(body, Ctx.JsonOptions);
                    return Ctx.Data.GetZoneUser(userZone.Code_user, Ctx.Settings.Zones)?.Name;

                case "Zone/Move/Normal":
                    ZoneChange rqZoneChange = JsonSerializer.Deserialize<ZoneChange>(body, Ctx.JsonOptions);
                    if (Ctx.GlobalSettings.AutoSignInGiveAway)
                        Ctx.AddToHere(new User(rqZoneChange.User.Pseudo, rqZoneChange.User.Platform, rqZoneChange.User.Code_user, Ctx.Data));
                    if (!rqZoneChange.IsValide(Ctx.Settings))
                        return "Erreur, soit ça existe po, soit y a un bug";
                    var errors = rqZoneChange.ListErrors(Ctx.Settings, Ctx.GlobalSettings);
                    if (errors.Count > 0)
                        return "Failed : " + string.Join(" ; ", errors);
                    string zoneResult = await rqZoneChange.DoResult(Ctx.Settings, Ctx.Data);
                    Ctx.MarkForExport(rqZoneChange.User);
                    return zoneResult;

                case "Zone/Move/Random":
                    ZoneChangeAuto rqRandom = JsonSerializer.Deserialize<ZoneChangeAuto>(body, Ctx.JsonOptions);
                    rqRandom.SmartMode = false;
                    rqRandom.SetPokesZones(Ctx.Settings.pokemons, Ctx.Settings.Zones);
                    return await rqRandom.DoResult(Ctx.Settings, Ctx.Data, Ctx.GlobalSettings);

                case "Zone/Move/Smart":
                    ZoneChangeAuto rqSmart = JsonSerializer.Deserialize<ZoneChangeAuto>(body, Ctx.JsonOptions);
                    rqSmart.SmartMode = true;
                    rqSmart.SetPokesZones(Ctx.Settings.pokemons, Ctx.Settings.Zones);
                    return await rqSmart.DoResult(Ctx.Settings, Ctx.Data, Ctx.GlobalSettings);

                default:
                    return $"[ZoneController] Route non reconnue : {path}";
            }
        }
    }
}

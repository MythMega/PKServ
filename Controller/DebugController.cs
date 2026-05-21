using PKServ.Admin;
using PKServ.Business;
using PKServ.Business.Admin;
using PKServ.Configuration;
using PKServ.Entity;
using System.Collections.Specialized;
using System.Linq;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKServ.Controller
{
    /// <summary>
    /// Routes Debug/* :
    /// POST : Debug/GetAllData
    /// GET  : Debug/CatchHistory
    /// </summary>
    public class DebugController : BaseController
    {
        public DebugController(ControllerContext ctx) : base(ctx) { }

        public override async Task<string> HandlePostAsync(string path, string body)
        {
            switch (path)
            {
                case "Debug/GetAllData":
                    return await DebugAllDataImpl.DebugAllDataAsync(Ctx.Settings);

                default:
                    return $"[DebugController] Route non reconnue : {path}";
            }
        }

        public override Task<string> HandleGetAsync(string path, NameValueCollection query)
        {
            switch (path)
            {
                case "Debug/CatchHistory":
                    if (query.Count > 0 && query.AllKeys[0] == "Count" && int.TryParse(query["Count"], out int count))
                    {
                        string result = string.Empty;
                        foreach (CatchHistory ch in Ctx.Settings.catchHistory.OrderByDescending(o => o.time).Take(count))
                        {
                            string shinyStatut = ch.shiny ? "shiny" : "normal";
                            result += $"{ch.time} {ch.User} - {ch.Pokemon.Name_FR} ({shinyStatut}) - {ch.Ball.Name}\n";
                        }
                        return Task.FromResult(result);
                    }
                    return Task.FromResult($"[DebugController] Paramètre Count manquant.");

                default:
                    return Task.FromResult($"[DebugController] Route GET non reconnue : {path}");
            }
        }
    }
}

using PKServ.Binding;
using PKServ.Configuration;
using PKServ.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKServ.Business.Exports.JsonExporters
{
    /// <summary>
    /// Génère un JSON par utilisateur : users/{platform}_{username}.json
    /// </summary>
    public static class JsonExportUser
    {
        // Dictionnaire index pré-calculé : AltName/Name_FR → position dans allPokemons
        // Recalculé à chaque export (settings peut changer entre deux exports)
        private static Dictionary<string, int> BuildOrderIndex(AppSettings settings)
        {
            var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < settings.allPokemons.Count; i++)
            {
                var p = settings.allPokemons[i];
                if (!string.IsNullOrEmpty(p.AltName) && !index.ContainsKey(p.AltName))
                    index[p.AltName] = i;
                if (!string.IsNullOrEmpty(p.Name_FR) && !index.ContainsKey(p.Name_FR))
                    index[p.Name_FR] = i;
                if (!string.IsNullOrEmpty(p.Name_EN) && !index.ContainsKey(p.Name_EN))
                    index[p.Name_EN] = i;
            }
            return index;
        }

        // Lookup rapide pokémon par nom
        private static Dictionary<string, Pokemon> BuildPokeIndex(AppSettings settings)
        {
            var index = new Dictionary<string, Pokemon>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in settings.allPokemons)
            {
                if (!string.IsNullOrEmpty(p.AltName) && !index.ContainsKey(p.AltName))
                    index[p.AltName] = p;
                if (!string.IsNullOrEmpty(p.Name_FR) && !index.ContainsKey(p.Name_FR))
                    index[p.Name_FR] = p;
                if (!string.IsNullOrEmpty(p.Name_EN) && !index.ContainsKey(p.Name_EN))
                    index[p.Name_EN] = p;
            }
            return index;
        }

        public static async Task ExportUserAsync(
            User user,
            DataConnexion data,
            AppSettings settings,
            GlobalAppSettings globalAppSettings,
            Dictionary<string, Pokemon>? pokeIndex = null,
            Dictionary<string, int>? orderIndex = null)
        {
            try
            {
                user.generateStats();
                user.generateStatsAchievement(settings, globalAppSettings);

                List<Entrie> entries = data.GetEntriesByPseudo(user.Pseudo, user.Platform);
                string zone             = data.GetZoneUser(user.Code_user, settings.Zones)?.Name;
                string favoriteSprite   = data.GetSpriteFavoriteCreature(user, settings);
                string cardBackground   = data.GetCardBackgroundUrl(user)
                    ?? "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/background/cards/base/base_card.jpg";

                // GetAllUserPlatforms() ne charge que Pseudo/Platform/CODE_USER → AvatarUrl est null.
                // On lit explicitement l'avatar depuis la DB pour garantir qu'il est toujours présent
                // dans le JSON exporté, quelle que soit l'origine de l'objet User.
                string avatarUrl = user.AvatarUrl ?? data.GetAvatarUrl(user);

                // Utilise les index partagés si fournis, sinon les construit localement
                pokeIndex  ??= BuildPokeIndex(settings);
                orderIndex ??= BuildOrderIndex(settings);

                var entriesJson = entries
                    .Select(e =>
                    {
                        pokeIndex.TryGetValue(e.PokeName, out var poke);
                        if (poke is null)
                            poke = settings.allPokemons.FirstOrDefault(p => Commun.isSamePoke(p, e.PokeName));
                        int order = poke is not null && orderIndex.TryGetValue(poke.AltName ?? poke.Name_FR ?? "", out int o)
                            ? o : int.MaxValue;
                        return (order, e, poke);
                    })
                    .OrderBy(x => x.order)
                    .Select(x => new
                    {
                        PokeName       = x.e.PokeName,
                        SpriteNormal   = x.poke?.Sprite_Normal,
                        SpriteShiny    = x.poke?.Sprite_Shiny,
                        x.e.CountNormal,
                        x.e.CountShiny,
                        DateFirstCatch = x.e.dateFirstCatch,
                        DateLastCatch  = x.e.dateLastCatch
                    })
                    .ToList();

                var userJson = new
                {
                    user.Pseudo,
                    user.Platform,
                    user.Code_user,
                    AvatarUrl      = avatarUrl,
                    CardBackground = cardBackground,
                    Zone           = zone,
                    Level          = user.Stats?.level,
                    CurrentXP      = user.Stats?.currentXP,
                    MaxXP          = user.Stats?.MaxXPLevel,
                    DexCount       = user.Stats?.dexCount,
                    ShinyDex       = user.Stats?.shinydex,
                    FirstCatch     = user.Stats?.firstCatch,
                    BallLaunched   = user.Stats?.ballLaunched,
                    PokeCaught     = user.Stats?.pokeCaught,
                    ShinyCaught    = user.Stats?.shinyCaught,
                    MoneySpent     = user.Stats?.moneySpent,
                    CustomMoney    = user.Stats?.CustomMoney,
                    GiveawayNormal = user.Stats?.giveawayNormal,
                    GiveawayShiny  = user.Stats?.giveawayShiny,
                    ScrappedNormal = user.Stats?.scrappedNormal,
                    ScrappedShiny  = user.Stats?.scrappedShiny,
                    TradeCount     = user.Stats?.TradeCount,
                    RaidCount      = user.Stats?.RaidCount,
                    RaidTotalDmg   = user.Stats?.RaidTotalDmg,
                    FavoriteCreature = user.Stats?.favoritePoke,
                    FavoriteSprite = favoriteSprite,
                    TotalPokemons  = settings.pokemons.Count,
                    ExportedAt     = DateTime.UtcNow,
                    Badges = user.Stats?.badges?.Select(b => new
                    {
                        Name     = b.Title,
                        b.Obtained,
                        ImageUrl = b.IconUrl,
                        b.Description,
                        b.Rarity
                    }).ToList(),
                    Entries = entriesJson
                };

                string filename = $"{user.Platform}_{user.Pseudo}.json"
                    .Replace(" ", "_")
                    .Replace("/", "_")
                    .Replace("\\", "_");

                await StaticFileCopier.WriteJsonAsync(Path.Combine("users", filename), userJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Export] Erreur utilisateur {user.Pseudo}: {ex.Message}");
            }
        }

        /// <summary>
        /// Exporte tous les utilisateurs en parallèle avec index pokémon partagés.
        /// </summary>
        public static async Task ExportAllUsersAsync(DataConnexion data, AppSettings settings, GlobalAppSettings globalAppSettings)
        {
            var users      = data.GetAllUserPlatforms();
            var pokeIndex  = BuildPokeIndex(settings);
            var orderIndex = BuildOrderIndex(settings);

            await Task.WhenAll(users.Select(u =>
                ExportUserAsync(u, data, settings, globalAppSettings, pokeIndex, orderIndex)));

            ExportUsersByPlatform(users);
        }

        /// <summary>
        /// Génère users_by_platform.json : { twitch: [...], youtube: [...], ... }
        /// </summary>
        public static void ExportUsersByPlatform(List<User> users)
        {
            var platforms = new[] { "twitch", "youtube", "tiktok", "discord" };
            var grouped = platforms.ToDictionary(
                p => p,
                p => users
                    .Where(u => string.Equals(u.Platform, p, StringComparison.OrdinalIgnoreCase))
                    .Select(u => u.Pseudo)
                    .OrderBy(pseudo => pseudo)
                    .ToList());

            string path = Path.Combine("WebExport", "Data", "json", "users_by_platform.json");
            File.WriteAllText(path, JsonSerializer.Serialize(grouped, StaticFileCopier.GetOptions()));
        }
    }
}

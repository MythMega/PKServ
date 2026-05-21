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
    /// Génère les JSON pour toutes les pages "liste".
    /// Toutes les méthodes sont async et utilisent WriteJsonAsync.
    /// </summary>
    public static class JsonExportPages
    {
        // ── main.json ─────────────────────────────────────────────────────────

        public static async Task ExportMainAsync(DataConnexion data, AppSettings settings, GlobalAppSettings globalAppSettings)
        {
            List<User> users = data.GetAllUserPlatforms();
            List<Entrie> allEntries = data.GetAllEntries();

            int requiredToEvolve = globalAppSettings.EvolveSettings.RequiredCreatureToEvolve;
            List<string> pokemonsEvolved = settings.pokemons.Where(a => a.EvolveFrom is not null).Select(a => a.EvolveFrom).Distinct().ToList();
            List<(Pokemon baseform, List<Pokemon> evolutions)> canEvolve = [];
            foreach (var crea in pokemonsEvolved)
            {
                var baseFormCreature = settings.pokemons.FirstOrDefault(w => Commun.isSamePoke(w, crea));
                if (baseFormCreature != null)
                {
                    var listEvolutions = settings.pokemons.Where(w => w.EvolveFrom == baseFormCreature.AltName ||
                     w.EvolveFrom == baseFormCreature.Name_EN ||
                       w.EvolveFrom == baseFormCreature.Name_FR).ToList();

                    if (listEvolutions.Any())
                    {
                        canEvolve.Add((baseFormCreature, listEvolutions));
                    }
                }
            }

            // Pré-calcul : entries groupées depuis GetAllEntries (1 seule requête DB au lieu de N)
            var entriesByUser = users.ToDictionary(
                u => u,
                u => allEntries.Where(e =>
                    string.Equals(e.Pseudo, u.Pseudo, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(e.Platform, u.Platform, StringComparison.OrdinalIgnoreCase))
                .ToList());

            // Sets pour le dex global (évite allEntries.Any par pokémon)
            var capturedNormalNames = new HashSet<string>(
                allEntries.Where(e => e.CountNormal > 0).Select(e => e.PokeName),
                StringComparer.OrdinalIgnoreCase);
            var capturedShinyNames = new HashSet<string>(
                allEntries.Where(e => e.CountShiny > 0).Select(e => e.PokeName),
                StringComparer.OrdinalIgnoreCase);

            int globalDexNormal = settings.pokemons.Count(p =>
                capturedNormalNames.Any(name => Commun.isSamePoke(p, name)));
            int globalDexShiny = settings.pokemons.Count(p =>
                capturedShinyNames.Any(name => Commun.isSamePoke(p, name)));

            // Stats users (une seule fois, pas de requête DB par user)
            users.ForEach(u => u.generateStats());

            var top3Balls = users.OrderByDescending(u => u.Stats.ballLaunched).Take(3)
                .Select(u => new { u.Pseudo, u.Platform, BallLaunched = u.Stats.ballLaunched }).ToList();
            var top3Money = users.OrderByDescending(u => u.Stats.moneySpent).Take(3)
                .Select(u => new { u.Pseudo, u.Platform, MoneySpent = u.Stats.moneySpent }).ToList();
            var top3Shiny = users.OrderByDescending(u => entriesByUser[u].Count(e => e.CountShiny > 0)).Take(3)
                .Select(u => new { u.Pseudo, u.Platform, ShinyCount = entriesByUser[u].Count(e => e.CountShiny > 0) }).ToList();
            var top3Dex = users.OrderByDescending(u => entriesByUser[u].Count).Take(3)
                .Select(u => new { u.Pseudo, u.Platform, DexCount = entriesByUser[u].Count }).ToList();

            var recent = settings.catchHistory
                .OrderByDescending(c => c.time).Take(20)
                .Select(c => new
                {
                    PokeName = c.Pokemon?.Name_FR ?? c.Pokemon?.Name_EN,
                    SpriteUrl = c.shiny ? c.Pokemon?.Sprite_Shiny : c.Pokemon?.Sprite_Normal,
                    c.User?.Pseudo,
                    c.User?.Platform,
                    IsShiny = c.shiny,
                    BallName = c.Ball?.Name,
                    Time = c.time
                }).ToList();

            await StaticFileCopier.WriteJsonAsync("main.json", new
            {
                LastUpdate = DateTime.Now,
                GlobalStats = new
                {
                    TotalBallLaunched = users.Sum(u => u.Stats.ballLaunched),
                    TotalPokeCaught = users.Sum(u => u.Stats.pokeCaught),
                    TotalShinyCaught = users.Sum(u => u.Stats.shinyCaught),
                    TotalMoneySpent = users.Sum(u => u.Stats.moneySpent),
                    TotalUsers = users.Count,
                    TotalPokemons = settings.pokemons.Count,
                    GlobalDexNormal = globalDexNormal,
                    GlobalDexShiny = globalDexShiny,
                    TotalGiveawayNormal = users.Sum(u => u.Stats.giveawayNormal),
                    TotalGiveawayShiny = users.Sum(u => u.Stats.giveawayShiny)
                },
                Rankings = new
                {
                    TopBalls = top3Balls,
                    TopMoney = top3Money,
                    TopShiny = top3Shiny,
                    TopDex = top3Dex
                },
                RecentCatches = recent,
                EvolveData = new
                {
                    RequiredToEvolve = requiredToEvolve,
                    Evolutions = canEvolve.Select(c => new
                    {
                        BaseAltName  = c.baseform.AltName ?? c.baseform.Name_FR ?? c.baseform.Name_EN,
                        BaseName_FR  = c.baseform.Name_FR,
                        BaseName_EN  = c.baseform.Name_EN,
                        Evolutions   = c.evolutions.Select(e => new
                        {
                            AltName  = e.AltName ?? e.Name_FR ?? e.Name_EN,
                            Name_FR  = e.Name_FR,
                            Name_EN  = e.Name_EN,
                        }).ToList()
                    }).ToList()
                },
                ShopData = new
                {
                    CmdBuy = globalAppSettings.CommandSettings.CmdBuy,
                    Items  = settings.pokemons
                        .Where(p => p.priceNormal.HasValue || p.priceShiny.HasValue)
                        .Select(p => new
                        {
                            AltName     = p.AltName ?? p.Name_FR ?? p.Name_EN,
                            Name_FR     = p.Name_FR,
                            PriceNormal = p.priceNormal,
                            PriceShiny  = p.priceShiny,
                        }).ToList()
                }
            });
        }

        public static void ExportMain(DataConnexion data, AppSettings settings, GlobalAppSettings globalAppSettings)
            => ExportMainAsync(data, settings, globalAppSettings).Wait();

        // ── buypokemon.json ───────────────────────────────────────────────────

        public static async Task ExportBuyListAsync(AppSettings settings, GlobalAppSettings globalAppSettings)
        {
            var items = settings.pokemons
                .Where(p => p.priceNormal.HasValue || p.priceShiny.HasValue)
                .Select(p =>
                {
                    string displayName = globalAppSettings.LanguageCode == "fr" ? p.Name_FR : p.Name_EN;
                    return new
                    {
                        Name = displayName,
                        NameEN = p.Name_EN,
                        AltName = p.AltName,
                        SpriteNormal = p.Sprite_Normal,
                        SpriteShiny = p.Sprite_Shiny,
                        PriceNormal = p.priceNormal,
                        PriceShiny = p.priceShiny
                    };
                }).ToList();

            await StaticFileCopier.WriteJsonAsync("buypokemon.json",
                new { CmdBuy = globalAppSettings.CommandSettings.CmdBuy, Items = items });
        }

        public static void ExportBuyList(AppSettings settings, GlobalAppSettings globalAppSettings)
            => ExportBuyListAsync(settings, globalAppSettings).Wait();

        // ── scrappokemon.json ─────────────────────────────────────────────────

        public static async Task ExportScrapListAsync(AppSettings settings, GlobalAppSettings globalAppSettings)
        {
            int defNormal = globalAppSettings.ScrapSettings.ValueDefaultNormal;
            int defShiny = globalAppSettings.ScrapSettings.ValueDefaultShiny;
            int legMult = globalAppSettings.ScrapSettings.legendaryMultiplier;

            var items = settings.pokemons.Where(p => p.enabled).Select(p =>
            {
                int valNormal = p.valueNormal ?? (p.isLegendary ? defNormal * legMult : defNormal);
                int valShiny = p.valueShiny ?? (p.isLegendary ? defShiny * legMult : defShiny);
                string display = globalAppSettings.LanguageCode == "fr" ? p.Name_FR : p.Name_EN;
                return new
                {
                    Name = display,
                    NameEN = p.Name_EN,
                    AltName = p.AltName,
                    SpriteNormal = p.Sprite_Normal,
                    SpriteShiny = p.Sprite_Shiny,
                    ValueNormal = valNormal,
                    ValueShiny = valShiny,
                    p.isLegendary,
                    IsShinyLock = p.isShinyLock
                };
            }).ToList();

            await StaticFileCopier.WriteJsonAsync("scrappokemon.json",
                new { CmdScrap = globalAppSettings.CommandSettings.CmdScrap, Items = items });
        }

        public static void ExportScrapList(AppSettings settings, GlobalAppSettings globalAppSettings)
            => ExportScrapListAsync(settings, globalAppSettings).Wait();

        // ── pokestats.json ────────────────────────────────────────────────────

        public static async Task ExportPokeStatsAsync(DataConnexion data, AppSettings settings, GlobalAppSettings globalAppSettings)
        {
            List<Entrie> allEntries = data.GetAllEntries();

            // Grouper les entries par nom — lookup O(1) au lieu de Where(isSamePoke) par pokémon
            var entriesByName = allEntries
                .GroupBy(e => e.PokeName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var creatureStats = settings.pokemons.Select(p =>
            {
                var related = new List<Entrie>();
                foreach (var kv in entriesByName)
                    if (Commun.isSamePoke(p, kv.Key))
                        related.AddRange(kv.Value);

                if (related.Count == 0) return null;

                int totalNormal = related.Sum(e => e.CountNormal);
                int totalShiny = related.Sum(e => e.CountShiny);
                if (totalNormal + totalShiny == 0) return null;

                return (object)new
                {
                    Name = p.Name_FR,
                    NameEN = p.Name_EN,
                    SpriteNormal = p.Sprite_Normal,
                    TotalCatch = totalNormal + totalShiny,
                    TotalNormal = totalNormal,
                    TotalShiny = totalShiny,
                    p.isLegendary,
                    FirstCatch = related.Min(e => e.dateFirstCatch),
                    LastCatch = related.Max(e => e.dateLastCatch)
                };
            }).Where(c => c is not null).ToList();

            int totalCatch = creatureStats.Cast<dynamic>().Sum(c => (int)c.TotalCatch);
            int totalShinyAll = creatureStats.Cast<dynamic>().Sum(c => (int)c.TotalShiny);

            await StaticFileCopier.WriteJsonAsync("pokestats.json", new
            {
                LastUpdate = DateTime.Now,
                GlobalStats = new
                {
                    TotalCatch = totalCatch,
                    TotalShiny = totalShinyAll,
                    SpeciesCaught = creatureStats.Count,
                    SpeciesShiny = creatureStats.Cast<dynamic>().Count(c => (int)c.TotalShiny > 0)
                },
                CreatureStats = creatureStats
            });
        }

        public static void ExportPokeStats(DataConnexion data, AppSettings settings, GlobalAppSettings globalAppSettings)
            => ExportPokeStatsAsync(data, settings, globalAppSettings).Wait();

        // ── records.json ──────────────────────────────────────────────────────

        public static async Task ExportRecordsAsync(DataConnexion data, AppSettings settings)
        {
            List<Records> records = data.GetRecords();

            var items = records.Select(r =>
            {
                var poke = settings.allPokemons.FirstOrDefault(p => Commun.isSamePoke(p, r.CreatureName));
                return new
                {
                    r.ID,
                    r.CreatureName,
                    SpriteUrl = r.Statut?.ToLower() == "shiny" ? poke?.Sprite_Shiny : poke?.Sprite_Normal,
                    r.Statut,
                    r.Type,
                    r.Date
                };
            }).ToList();

            await StaticFileCopier.WriteJsonAsync("records.json", new { Records = items });
        }

        public static void ExportRecords(DataConnexion data, AppSettings settings)
            => ExportRecordsAsync(data, settings).Wait();

        // ── commandgenerator_data.json ────────────────────────────────────────

        public static async Task ExportCommandGeneratorDataAsync(AppSettings settings, GlobalAppSettings globalAppSettings)
        {
            var buyableCreatures = settings.pokemons
                .Where(p => p.enabled && (p.priceNormal.HasValue || p.priceShiny.HasValue))
                .Select(p => new
                {
                    Name = p.AltName?.Replace(" ", "_") ?? p.Name_FR?.Replace(" ", "_"),
                    DisplayName = p.Name_FR ?? p.Name_EN,
                    p.Sprite_Normal,
                    PriceNormal = p.priceNormal,
                    PriceShiny = p.priceShiny
                }).ToList();

            var allCreatures = settings.pokemons
                .Select(p => new
                {
                    Name = p.AltName?.Replace(" ", "_") ?? p.Name_FR?.Replace(" ", "_"),
                    DisplayName = p.Name_FR ?? p.Name_EN,
                    p.Sprite_Normal
                }).ToList();

            var backgroundGroups = settings.TrainerCardsBackgrounds
                .GroupBy(bg => bg.Group)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(bg => new
                    {
                        bg.Name,
                        bg.Url,
                        bg.Group,
                        Requirements = bg.requirements?.Select(r => new { r.Type, r.Value }).ToList()
                    }).ToList());

            var zones = settings.Zones
                .OrderBy(z => z.DexRequirement)
                .Select(z => new
                {
                    z.Name,
                    z.Region,
                    z.Image,
                    z.DexRequirement,
                    z.LevelRequirement,
                    z.Description
                }).ToList();

            await StaticFileCopier.WriteJsonAsync("commandgenerator_data.json", new
            {
                CmdBuy = globalAppSettings.CommandSettings.CmdBuy,
                CmdScrap = "!scrap",
                CmdTrade = "!trade",
                CmdZone = "!changeZone",
                CmdCard = "!changeCard",
                BuyableCreatures = buyableCreatures,
                AllCreatures = allCreatures,
                BackgroundGroups = backgroundGroups,
                Zones = zones,
                Regions = zones.Select(z => z.Region).Distinct().ToList()
            });
        }

        public static void ExportCommandGeneratorData(AppSettings settings, GlobalAppSettings globalAppSettings)
            => ExportCommandGeneratorDataAsync(settings, globalAppSettings).Wait();

        // ── rankings.json ─────────────────────────────────────────────────────

        public static async Task ExportRankingsAsync(DataConnexion data, AppSettings settings, GlobalAppSettings globalAppSettings)
        {
            List<User> users = data.GetAllUserPlatforms()
                .Where(u => !string.Equals(u.Platform, "system", StringComparison.OrdinalIgnoreCase))
                .ToList();
            users.ForEach(u => { u.generateStats(); u.generateStatsAchievement(settings, globalAppSettings); });

            // Entrées groupées par user pour dex/shiny
            List<Entrie> allEntries = data.GetAllEntries();
            var entriesByUser = users.ToDictionary(
                u => u.Code_user,
                u => allEntries.Where(e =>
                    string.Equals(e.Pseudo, u.Pseudo, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(e.Platform, u.Platform, StringComparison.OrdinalIgnoreCase)).ToList());

            // Lookup rareté par nom de pokémon (AltName > Name_FR > Name_EN)
            var rarityByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in settings.allPokemons)
            {
                if (string.IsNullOrWhiteSpace(p.Rarity)) continue;
                foreach (var name in new[] { p.AltName, p.Name_FR, p.Name_EN })
                    if (!string.IsNullOrWhiteSpace(name) && !rarityByName.ContainsKey(name))
                        rarityByName[name] = p.Rarity;
            }

            object PlayerEntry(User u)
            {
                var ent = entriesByUser.TryGetValue(u.Code_user, out var e1) ? e1 : new List<Entrie>();
                int dexN = ent.Count(e => e.CountNormal > 0);
                int dexS = ent.Count(e => e.CountShiny > 0);
                int caught = u.Stats?.pokeCaught ?? 0;
                int shiny = u.Stats?.shinyCaught ?? 0;
                int raids = u.Stats?.RaidCount ?? 0;
                int raidDmg = u.Stats?.RaidTotalDmg ?? 0;

                DateTime? firstCatch = ent.Count > 0 ? ent.Min(e => e.dateFirstCatch) : (DateTime?)null;
                DateTime? lastCatch = ent.Count > 0 ? ent.Max(e => e.dateLastCatch) : (DateTime?)null;
                int daysActive = (firstCatch.HasValue && lastCatch.HasValue)
                    ? Math.Max(1, (int)(lastCatch.Value - firstCatch.Value).TotalDays)
                    : 0;

                // Compter pokémon distincts capturés par rareté
                var rarityCounts = ent
                    .Where(e => e.CountNormal > 0 || e.CountShiny > 0)
                    .GroupBy(e => rarityByName.TryGetValue(e.PokeName, out var r) ? r : "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count());

                return new
                {
                    pseudo = u.Pseudo,
                    platform = u.Platform,
                    avatarUrl = u.AvatarUrl,
                    level = u.Stats?.level ?? 0,
                    currentXP = u.Stats?.currentXP ?? 0,
                    ballLaunched = u.Stats?.ballLaunched ?? 0,
                    pokeCaught = caught,
                    shinyCaught = shiny,
                    moneySpent = u.Stats?.moneySpent ?? 0,
                    customMoney = u.Stats?.CustomMoney ?? 0,
                    dexCount = dexN,
                    shinyDex = dexS,
                    raidCount = raids,
                    raidTotalDmg = raidDmg,
                    raidAvgDmg = raids > 0 ? raidDmg / raids : 0,
                    tradeCount = u.Stats?.TradeCount ?? 0,
                    scrappedNormal = u.Stats?.scrappedNormal ?? 0,
                    scrappedShiny = u.Stats?.scrappedShiny ?? 0,
                    giveawayNormal = u.Stats?.giveawayNormal ?? 0,
                    giveawayShiny = u.Stats?.giveawayShiny ?? 0,
                    favoriteCreature = u.Stats?.favoritePoke,
                    daysActive = daysActive,
                    totalScrapped = (u.Stats?.scrappedNormal ?? 0) + (u.Stats?.scrappedShiny ?? 0),
                    rarityCounts
                };
            }

            var players = users.Select(PlayerEntry).ToList();

            await StaticFileCopier.WriteJsonAsync("rankings.json", new { players });
        }

        public static void ExportRankings(DataConnexion data, AppSettings settings, GlobalAppSettings globalAppSettings)
            => ExportRankingsAsync(data, settings, globalAppSettings).Wait();
    }
}
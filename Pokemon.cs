using PKServ.Binding;
using PKServ.Configuration;
using PKServ.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace PKServ
{
    public class Pokemon
    {
        /// <summary>
        /// Name, in french
        /// </summary>
        public string Name_FR;

        /// <summary>
        /// Name, in english
        /// </summary>
        public string Name_EN;

        /// <summary>
        /// sprite link in shiny
        /// if not specified, will grab from pokemondb, the sprite from Pokémon Home
        /// except if the poke is custom
        /// </summary>
        public string Sprite_Shiny;

        /// <summary>
        /// sprite link in normal
        /// if not specified, will grab from pokemondb, the sprite from Pokémon Home
        /// except if the poke is custom
        /// </summary>
        public string Sprite_Normal;

        /// <summary>
        /// is that poke custom
        /// </summary>
        public bool isCustom;

        /// <summary>
        /// is that poke locked ?
        /// if locked, that poke won't be available to catch
        /// </summary>
        public bool isLock;

        /// <summary>
        /// is that poke legendary ?
        /// if legendary, a special message will be returned
        /// </summary>
        public bool isLegendary;

        public string Type1 { get; set; }

        public string Type2 { get; set; }

        public bool isShiny = false;

        public bool isShinyLock;

        public bool enabled { get; set; }

        public int? valueNormal { get; set; }
        public int? valueShiny { get; set; }
        public int? priceNormal { get; set; }
        public int? priceShiny { get; set; }
        public string? Rarity { get; set; }

        public string? AltName { get; set; }

        public bool AltNameForced { get; set; } = true;
        public string? Serie { get; set; }
        public string? EvolveFrom { get; set; }

        public List<Artist> Artist { get; set; }

        public List<String> ZonesNames { get; set; } = [];

        public bool IsZoneExclusive { get; set; } = false;

        public List<Zone> ZonesList { get; set; } = [];

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="name_FR"></param>
        /// <param name="name_EN"></param>
        /// <param name="sprite_Shiny">generated if = ""</param>
        /// <param name="sprite_Normal">generated if = ""</param>
        /// <param name="isCustom">false by default</param>
        /// <param name="isLock">false by default</param>
        /// <param name="isLegendary">false by default</param>
        public Pokemon(string name_FR, string name_EN, string sprite_Shiny, string sprite_Normal, List<Artist> artist, List<String> ZonesNames, List<Zone> ZonesList, bool isCustom = false, bool isLock = false, bool isLegendary = false, bool isShinyLock = false, int? valueNormal = null, int? valueShiny = null, int? priceNormal = null, int? priceShiny = null, string? Rarity = "COMMON", string AltName = null, string Serie = "main", bool IsZoneExclusive = true, string evolveFrom = null)
        {
            Name_FR = name_FR;
            Name_EN = name_EN;
            Sprite_Shiny = string.IsNullOrEmpty(sprite_Shiny) && !isCustom ? $"https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/poke/vanilla/{Commun.CapitalizeString(name_EN)}_shiny.gif" : sprite_Shiny;
            Sprite_Normal = string.IsNullOrEmpty(sprite_Normal) && !isCustom ? $"https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/poke/vanilla/{Commun.CapitalizeString(name_EN)}_normal.gif" : sprite_Normal;
            this.isCustom = isCustom;
            this.isLock = isLock;
            this.isLegendary = isLegendary;
            this.isShinyLock = isShinyLock;
            this.valueNormal = valueNormal;
            this.valueShiny = valueShiny;
            this.priceNormal = priceNormal;
            this.priceShiny = priceShiny;
            this.Rarity = Rarity.ToUpper();
            if (AltName is null)
            {
                this.AltNameForced = true;
                this.AltName = Name_FR;
            }
            else
            {
                this.AltNameForced = false;
                this.AltName = AltName;
            }
            this.Serie = Serie;
            this.EvolveFrom = evolveFrom;
            this.Artist = artist is null ? [] : artist;
            this.IsZoneExclusive = IsZoneExclusive;
            this.ZonesNames = ZonesNames is null ? [] : ZonesNames;
            if (this.ZonesNames.Count == 0 && !isLock)
                this.ZonesList.Add(Commun.GetBaseZone());
        }

        public Pokemon Clone()
        {
            return new Pokemon(
                this.Name_FR,
                this.Name_EN,
                this.Sprite_Shiny,
                this.Sprite_Normal,
                this.Artist,
                this.ZonesNames,
                this.ZonesList,
                this.isCustom,
                this.isLock,
                this.isLegendary,
                this.isShinyLock,
                this.valueNormal,
                this.valueShiny,
                this.priceNormal,
                this.priceShiny,
                this.Rarity,
                this.AltName,
                this.Serie,
                this.IsZoneExclusive,
                this.EvolveFrom
            )
            {
                isShiny = this.isShiny,
                AltNameForced = this.AltNameForced,
                enabled = this.enabled,
                Type1 = this.Type1,
                Type2 = this.Type2
            };
        }

        public string GetAdditionalInfosString(GlobalAppSettings gas)
        {
            List<string> tags = [];

            switch(this.Rarity)
            {
                case CreatureRarity.COMMON:
                    tags.AddRange(["common", "commun"]);
                    break;
                case CreatureRarity.UNCOMMON:
                    tags.AddRange(["uncommon", "peucommun"]);
                    break;
                case CreatureRarity.RARE:
                    tags.AddRange(["rare"]);
                    break;
                case CreatureRarity.EPIC:
                    tags.AddRange(["epic", "epique"]);
                    break;
                case CreatureRarity.LEGENDARY:
                    tags.AddRange(["legendary", "legendaire"]);
                    break;
                case CreatureRarity.MYTHICAL:
                    tags.AddRange(["mythical", "mythique"]);
                    break;
            }

            if (this.isLegendary)
            {
                tags.Add("légendaire legendaire legendary");
            }
            if (this.isCustom)
            {
                tags.Add("custom");
            }

            if (this.Type1 is not null)
            {
                tags.Add(this.Type1);
                try
                {
                    tags.Add(Commun.GetTranslatedType(gas, this.Type1));
                }
                catch { }
            }
            if (this.Type2 is not null)
            {
                tags.Add(this.Type2);
                try
                {
                    tags.Add(Commun.GetTranslatedType(gas, this.Type2));
                }
                catch { }
            }

            if (this.Serie is not null)
            {
                tags.Add(this.Serie);
            }

            return String.Join(" ; ", tags);
        }

        internal void SetData(List<Zone> zones)
        {
            foreach (string zoneName in this.ZonesNames)
            {
                Zone zone = zones.FirstOrDefault(x => x.Name.ToLower() == zoneName.ToLower());
                if (zone is not null)
                {
                    this.ZonesList.Add(zone);
                }
            }
        }
    }

    public class CreatureEvolutionRequest
    {
        public string Name { get; set; }
        public string Shiny { get; set; }
        public string ChannelName { get; set; }
        public User User { get; set; }

        public Pokemon CreatureBase { get; set; }
        public Pokemon CreatureEvolved { get; set; }

        [JsonConstructor]
        public CreatureEvolutionRequest(string Name, string Shiny, User User, string ChannelName)
        {
            this.Name = Name.ToLower().Replace(" ", "_").Trim();
            this.Shiny = Shiny.ToLower().Trim();
            this.User = User;
            this.ChannelName = ChannelName;
        }

        public void SetCreatures(List<Pokemon> pokemons, GlobalAppSettings globalAppSettings)
        {
            this.CreatureBase = pokemons.FirstOrDefault(x => Commun.isSamePoke(x, Name));
            if (this.CreatureBase == null)
                throw new Exception(globalAppSettings.Texts.TranslationEvolving.CreatureNotFound);
            List<Pokemon> TargetPossibles = pokemons.Where(x => x.EvolveFrom is not null).Where(x => Commun.isSamePoke(this.CreatureBase, x.EvolveFrom)).ToList();
            if (TargetPossibles.Count == 0)
                throw new Exception(globalAppSettings.Texts.TranslationEvolving.EvolutionNotFound);
            if (TargetPossibles.Count == 1)
                this.CreatureEvolved = TargetPossibles.First();
            else
            {
                Random random = new Random();
                this.CreatureEvolved = TargetPossibles[random.Next(TargetPossibles.Count)];
            }

            this.CreatureBase.isShiny = this.Shiny.StartsWith("sh");
            this.CreatureEvolved.isShiny = this.CreatureBase.isShiny;
            if (!globalAppSettings.EvolveSettings.AllowShiny && this.CreatureBase.isShiny)
            {
                throw new Exception(globalAppSettings.Texts.TranslationEvolving.CannotEvolveIfShiny);
            }

            if (!globalAppSettings.EvolveSettings.AllowEvolutionLocked && this.CreatureEvolved.isLock)
            {
                throw new Exception(globalAppSettings.Texts.TranslationEvolving.CannotEvolveIfEvolutionLocked);
            }

            if (!globalAppSettings.EvolveSettings.AllowEvolutionShinyLocked && this.CreatureBase.isShiny)
            {
                throw new Exception(globalAppSettings.Texts.TranslationEvolving.CannotEvolveIfEvolutionShinyLocked);
            }
        }

        public string DoEvolve(DataConnexion data, GlobalAppSettings globalAppSettings)
        {
            string result = String.Empty;

            List<Entrie> entries = data.GetEntriesByPseudo(User.Pseudo, User.Platform);
            Entrie targetEntrieBase = entries.Where(x => x.PokeName.ToLower() == CreatureBase.AltName.ToLower() || x.PokeName.ToLower() == CreatureBase.Name_EN.ToLower() || x.PokeName.ToLower() == CreatureBase.Name_FR.ToLower()).FirstOrDefault();
            if (targetEntrieBase is null)
            {
                result = globalAppSettings.Texts.TranslationEvolving.CreatureBaseNotOwned;
            }
            else
            {
                if ((this.CreatureBase.isShiny && (targetEntrieBase.CountShiny < globalAppSettings.EvolveSettings.RequiredCreatureToEvolve))
                    || !this.CreatureBase.isShiny && (targetEntrieBase.CountNormal < globalAppSettings.EvolveSettings.RequiredCreatureToEvolve))
                {
                    result = globalAppSettings.Texts.TranslationEvolving.NotEnoughCreatureToEvolve;
                }
                else
                {
                    bool needNewLine = false;
                    Entrie targetEntrieEvolved = entries.Where(x => x.PokeName.ToLower() == CreatureEvolved.AltName.ToLower() || x.PokeName.ToLower() == CreatureEvolved.Name_EN.ToLower() || x.PokeName.ToLower() == CreatureEvolved.Name_FR.ToLower()).FirstOrDefault();
                    if (targetEntrieEvolved is null)
                    {
                        needNewLine = true;
                        targetEntrieEvolved = new Entrie(0, User.Pseudo, ChannelName, User.Platform, this.CreatureEvolved.Name_FR, this.CreatureEvolved.isShiny ? 0 : 1, this.CreatureEvolved.isShiny ? 1 : 0, DateTime.Now, DateTime.Now, User.Code_user);
                    }
                    else
                    {
                        targetEntrieEvolved.CountNormal += this.CreatureEvolved.isShiny ? 0 : 1;
                        targetEntrieEvolved.CountShiny += this.CreatureEvolved.isShiny ? 1 : 0;
                    }
                    targetEntrieBase.CountNormal -= this.CreatureEvolved.isShiny ? 0 : globalAppSettings.EvolveSettings.RequiredCreatureToEvolve;
                    targetEntrieBase.CountShiny -= this.CreatureEvolved.isShiny ? globalAppSettings.EvolveSettings.RequiredCreatureToEvolve : 0;

                    targetEntrieBase.Validate(false);
                    targetEntrieEvolved.Validate(needNewLine);

                    return globalAppSettings.Texts.TranslationEvolving.EvolutionSucceed
                        .Replace("[COUNT]", $"{globalAppSettings.EvolveSettings.RequiredCreatureToEvolve}")
                        .Replace("[CREATURE_BASE]", $"{this.CreatureBase.Name_FR} / {this.CreatureBase.Name_EN}")
                        .Replace("[CREATURE_EVOLVED]", $"{this.CreatureEvolved.Name_FR} / {this.CreatureEvolved.Name_EN}");
                }
            }

            return result;
        }
    }
}
using PKServ.Configuration;
using PKServ.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PKServ.Business
{
    public class GiveawayImpl
    {
        internal static string DoGiveaway(GiveawayClaim giveawayClaim, Giveaway giveaway, GlobalAppSettings globalAppSettings, AppSettings appSettings, DataConnexion data)
        {
            if (GiveawayImpl.EligibleToGiveaway(appSettings, giveawayClaim.User, giveaway, data))
            {
                try
                {
                    string result = "";
                    giveawayClaim.User.generateStats();
                    if (giveaway.Pokemons.Count > 0)
                    {
                        switch (giveaway.Mode)
                        {
                            case GiveawayMode.All:
                                foreach (Pokemon pokemon in giveaway.Pokemons)
                                {
                                    Commun.ObtainPoke(giveawayClaim.User, pokemon, data, giveawayClaim.ChannelName);
                                }
                                result += $"{giveaway.Pokemons.Count} creatures received. ";
                                break;

                            case GiveawayMode.RandomOne:
                                Pokemon randomSelected = giveaway.Pokemons[new Random().Next(giveaway.Pokemons.Count())];
                                Commun.ObtainPoke(giveawayClaim.User, randomSelected, data, giveawayClaim.ChannelName);
                                result += $"{randomSelected.Name_FR}/{randomSelected.Name_EN} received. ";
                                break;

                            default:

                                break;
                        }
                    }

                    if (giveaway.Money > 0)
                    {
                        giveawayClaim.User.Stats.CustomMoney += giveaway.Money;
                        result += $"{giveaway.Money} {globalAppSettings.Texts.emotes.money} received.";
                    }
                    giveawayClaim.User.ValidateStatsBDD();

                    GiveawayImpl.RegisterGiveaway(giveawayClaim, data);

                    return result;
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
            else
                return globalAppSettings.Texts.TranslationGiveaway.AlreadyClaimed;
        }

        private static void RegisterGiveaway(GiveawayClaim giveawayClaim, DataConnexion data)
        {
            data.RegisterGiveaway(giveawayClaim);
        }

        private static bool EligibleToGiveaway(AppSettings appSettings, User user, Giveaway giveaway, DataConnexion data)
        {
            // si on trouve un code correspondant, alors il n'est pas éligible
            return !data.GetGiveawayUser(appSettings, user).Any(give => give.Code == giveaway.Code);
        }
    }

    public static class GiveawayInitializer
    {
        /// <summary>
        /// Initialise les giveaways et les complètes
        /// </summary>
        /// <returns></returns>
        public static List<Giveaway> GetGiveaways(AppSettings settings)
        {
            List<Giveaway> result = JsonSerializer.Deserialize<List<Giveaway>>(File.ReadAllText("./Data/StreamDex/giveaways.json"), Commun.GetJsonSerializerOptions());
            result.AddRange(LoadCustomGiveaway());
            result.ForEach(ga =>
            {
                List<Pokemon> pokemons = new List<Pokemon>();
                ga.pokeList.ForEach(poke =>
                {
                    Pokemon creatureToAddInList = settings.pokemons.FirstOrDefault(p => Commun.isSamePoke(p, poke.Name));
                    if (creatureToAddInList is null)
                    {
                        Console.WriteLine($"Error Giveaway : creature {poke.Name} not found in enabled creatures");
                    }
                    else
                    {
                        Pokemon clonedPokemon = creatureToAddInList.Clone();
                        clonedPokemon.isShiny = poke.shiny;
                        pokemons.Add(clonedPokemon);
                    }
                });
                ga.Pokemons = pokemons;
            });
            return result;
        }

        private static List<Giveaway> LoadCustomGiveaway()
        {
            // Giveaway personnalisées
            List<Giveaway> custom = new List<Giveaway>();
            if (!Directory.Exists("./Data/Custom/Overlays"))
            {
                Directory.CreateDirectory("./Data/Custom/Overlays");
            }
            foreach (string file in Directory.GetFiles("./Data/Custom/Overlays", "*.json"))
            {
                try
                {
                    List<Giveaway> giveawayInFile = JsonSerializer.Deserialize<List<Giveaway>>(File.ReadAllText(file), Commun.GetJsonSerializerOptions());
                    custom.AddRange(giveawayInFile);
                    Commun.Logger($"white#Custom Pokémon loaded from file: |yellow#{Path.GetFileName(file)}|white# : |aqua#{giveawayInFile.Count}|white#.");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error reading {file}: {e.Message}");
                }
            }
            return custom;
        }
    }
}
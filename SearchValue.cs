using System;
using System.Collections.Generic;
using System.Linq;

namespace PKServ
{
    internal class SearchValue
    {
        public string value { get; set; }
        public User User { get; set; }
        public UserRequest UserRequest { get; set; }
        private DataConnexion dataConnexion { get; set; }
        private GlobalAppSettings globalAppSettings { get; set; }
        private AppSettings appSettings { get; set; }

        public List<User> UserHere { get; set; }

        public SearchValue()
        {
            this.dataConnexion = null;
            this.appSettings = null;
            this.globalAppSettings = null;
        }

        public SearchValue(DataConnexion data, AppSettings appSettings, GlobalAppSettings globalAppSettings)
        {
            this.dataConnexion = data;
            this.appSettings = appSettings;
            this.globalAppSettings = globalAppSettings;
        }


        internal void SetEnv(DataConnexion data, AppSettings settings, GlobalAppSettings globalAppSettings, List<User> users)
        {
            this.dataConnexion = data;
            this.globalAppSettings = globalAppSettings;
            this.appSettings = settings;
            UserHere = users;

        }

        public bool IsValide()
        {
            return
                (new List<string> {
                    "pokecount",
                    "globaldex",
                    "userdexprogress",
                    "overlayprogressglobaldex",
                    "overlayprogressglobalshinydex",
                    "lastpokecaughtsprite",
                }.Contains(value.ToLower()));
        }

        public string searchValue(string value)
        {
            this.value = value.ToLower();
            return searchResult();
        }

        public string searchResult()
        {
            var allentries = dataConnexion.GetAllEntries();
            string result = string.Empty;
            if (value is not null)
            {
                switch (value.ToLower())
                {
                        // partie globale

                    // nombre de poké
                    case "pokecount":
                        result = appSettings.pokemons.Count.ToString();
                        break;

                    // pokédex global
                    case "globaldex":
                        result = dataConnexion.GetAllEntries().GroupBy(e => e.PokeName).Select(g => g.First()).Count().ToString();
                        break;

                        // partie user

                    // pokedex perso
                    case "userDexProgress":
                        result = User.Stats.dexCount.ToString();
                        break;

                    // partie overlay

                    // bar custom : pokegoal
                    case "globaltotalcaughtgoal":
                        result = @$"{{
    ""progress"":{allentries.Sum(x => x.CountNormal + x.CountShiny).ToString()},
    ""total"":{globalAppSettings.OverlaySettings.GlobalTotalCaughtGoal.GoalValue}
}} ";
                        break;

                    // bar custom : pokegoal shiny
                    case "globalshinycaughtgoal":
                        result = @$"{{
    ""progress"":{allentries.Sum(x => x.CountShiny).ToString()},
    ""total"":{globalAppSettings.OverlaySettings.GlobalShinyCaughtGoal.GoalValue}
}} ";
                        break;

                    // bar custom : pokegoal session
                    case "sessiontotalcaughtgoal":
                        result = @$"{{
    ""progress"":{appSettings.catchHistory.Count},
    ""total"":{globalAppSettings.OverlaySettings.SessionTotalCaughtGoal.GoalValue}
}} ";
                        break;

                    // bar custom : pokegoal shiny session
                    case "sessionshinycaughtgoal":
                        result = @$"{{
    ""progress"":{appSettings.catchHistory.Where(w => w.shiny).Count()},
    ""total"":{globalAppSettings.OverlaySettings.SessionShinyCaughtGoal.GoalValue}
}} ";
                        break;

                    // bar custom : user here
                    case "sessionparticipantsgoal":
                        result = @$"{{
    ""progress"":{UserHere.Count-1},
    ""total"":{globalAppSettings.OverlaySettings.SessionParticipantsGoal.GoalValue}
}} ";
                        break;

                    // bar progress
                    case "overlayprogressglobaldex":
                        result = @$"{{
    ""progress"":{dataConnexion.GetAllEntries().GroupBy(e => e.PokeName).Select(g => g.First()).Count()},
    ""total"":{appSettings.pokemons.Count}
}} ";
                        break;


                    // bar shinyprogress
                    case "overlayprogressglobalshinydex":
                        result = @$"{{
    ""progress"":{dataConnexion.GetAllEntries().Where(entry => entry.CountShiny > 0).GroupBy(e => e.PokeName).Select(g => g.First()).Count()},
    ""total"":{appSettings.pokemons.Count}
}} ";
                        break;
                    // bar progress
                    case "lastpokecaughtsprite":

                        // une image transparente par défaut
                        string sprite = "https://upload.wikimedia.org/wikipedia/commons/thumb/8/89/HD_transparent_picture.png/1200px-HD_transparent_picture.png";

                        CatchHistory lastCatchHistory = appSettings.catchHistory.Where(x => x.shownInOverlay_lastCaughtPokeSprite == false).FirstOrDefault();
                        if(lastCatchHistory != null)
                        {
                            sprite = lastCatchHistory.shiny ? lastCatchHistory.Pokemon.Sprite_Shiny : lastCatchHistory.Pokemon.Sprite_Normal;
                            Console.Write("sprite -> " + sprite);
                            appSettings.catchHistory.Where(x => x.shownInOverlay_lastCaughtPokeSprite == false).FirstOrDefault().shownInOverlay_lastCaughtPokeSprite = true;
                        }
                        


                        result = @$"{{
    ""imageUrl"":""{sprite}""
}} ";
                        break;
                    default:
                        result = "invalid search";
                        break;
                }
            }
            else
            {
                result = "invalid search";
            }
            return result;
        }
    }
}
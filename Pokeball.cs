namespace PKServ
{
    public class Pokeball
    {
        public string Name;

        //value between 0 & 100, 100 = always catch, 0 never catch
        public int catchrate;

        //value between 0 & 100, 100 = always shiny, 0 never shiny, default = 3
        public int shinyrate;

        //flat boost while time between 6pm & 6am, default = 0
        public int nightAdditionalRate;

        //flat boost when pokemon already in dex, default = 0
        public int alreadyCaughtAdditionalRate;

        //reward sources trigger of launching that ball
        public string rewardSource;

        //% bonus for 100 poke in the catchrate
        public int dexRelativeBonusCatchrate;

        //% bonus for 100 poke in the shinyrate
        public int dexRelativeBonusShinyrate;

        //number of reroll until you get a an item you don't have
        public int rerollItemForUncaught;

        //type that this pokeball can catch
        public string? exclusiveType;

        //series that pokeball can target
        public string? exlusiveSerie;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="catchrate"></param>
        /// <param name="rewardSource"></param>
        /// <param name="shinyrate">3 by default</param>
        /// <param name="nightAdditionalRate">0 by default</param>
        /// <param name="alreadyCaughtAdditionalRate">0 by default</param>
        public Pokeball(string name, int catchrate, string rewardSource, int shinyrate = 3, int nightAdditionalRate = 0, int alreadyCaughtAdditionalRate = 0, int dexRelativeBonusCatchrate = 0, int dexRelativeBonusShinyrate = 0, string? exclusiveType = null, string exlusiveSerie = null)
        {
            Name = name;
            this.rewardSource = rewardSource;
            this.catchrate = catchrate;
            this.shinyrate = shinyrate;
            this.nightAdditionalRate = nightAdditionalRate;
            this.alreadyCaughtAdditionalRate = alreadyCaughtAdditionalRate;
            this.exclusiveType = exclusiveType;
            this.exlusiveSerie = exlusiveSerie;
        }
    }
}
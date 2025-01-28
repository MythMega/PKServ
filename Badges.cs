namespace PKServ
{
    public class Badge
    {
        /// <summary>
        /// can be :
        /// - TotalCatch (total poke caught)
        /// - ShinyCatch (total shiny caught)
        /// - TotalRegistered (total poke registered)
        /// - ShinyRegistered (total shiny registered)
        /// - BallLaunched (total ball launched)
        /// - DaySinceStart (days since starts of being a trainer)
        /// - MoneySpent (total money spent)
        /// - LengendariesRegistered (total legendaries registered)
        /// - CustomRegistered (total legendaries registered)
        /// - TotalGiven (total poké given through giveaway)
        /// - ShinyGiven (total shiny given through giveaway)
        /// - SpecificPoke (specific pokemon captured)
        /// - MultiplePoke (multiple pokemon captured) - here the 'value' is useless but must have to be set
        /// </summary>
        public string Type { get; set; }

        public int Value { get; set; } = 0;
        public string SpecificValue { get; set; } = "";

        /// <summary>
        /// can be :
        /// - common
        /// - uncommon
        /// - rare
        /// - epic
        /// - legendary
        /// - exotic
        /// </summary>
        public string Rarity { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        public int XP { get; set; }
        public string IconUrl { get; set; }
        public bool Locked { get; set; }
        public string Group { get; set; } = "Main"; // les badges seront triés par groupes à l'affichage
        public string SubGroup { get; set; } = "Common"; // les badges seront triés par sous groupes au sein d'un groupe
        public bool Obtained { get; set; } = false;
    }
}
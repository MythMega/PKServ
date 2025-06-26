namespace PKServ.Entity.Raid
{
    public class UserRaidStats(User user)
    {
        public User User { get; set; } = user;

        // nombre de personnes soignées
        public int HealPeople { get; set; } = 0;

        public int HealSelf { get; set; } = 0;

        // nombre de fois où l'user a empoisonné quelqu'un
        public int PoisonOther { get; set; } = 0;

        // nombre de fois où tombé KO
        public int StatutCountKo { get; set; } = 0;

        // nombre de fois où a été paralysé
        public int StatutCountPara { get; set; } = 0;

        // nombre de fois où a été gelé
        public int StatutCountFrozen { get; set; } = 0;

        // nombre de fois où a été brulé
        public int StatutCountBurnt { get; set; } = 0;

        // nombre de fois où a été confus
        public int StatutCountConfused { get; set; } = 0;

        // nombre de fois où a été placé face au vent arrière
        public int StatutCountBackWind { get; set; } = 0;

        // nombre de fois où a été endormis
        public int StatutCountAsleep { get; set; } = 0;

        // nombre de fois où a été soigneur
        public int StatutCountHealing { get; set; } = 0;

        // nombre de fois où a été empoisonné
        public int StatutCountPoisoned { get; set; } = 0;

        // nombre de fois tours passé sous statut
        public int TotalRoundUnderEffect { get; set; } = 0;
    }
}
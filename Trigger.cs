namespace PKServ
{
    public class Trigger
    {
        /// <summary>
        /// Name of the trigger, can be the command or the reward used
        /// </summary>
        public string name;

        /// <summary>
        /// description, full free text, no incidences
        /// </summary>
        public string description;

        /// <summary>
        /// type of trigger, can be :
        /// "COMMAND"
        /// "REWARD"
        /// "OTHER"
        /// </summary>
        public string type;

        /// <summary>
        /// effect of the trigger, can be
        /// "BALL"
        /// "EXPORTDEX"
        /// "EXPORTDATA"
        /// "STATS"
        /// </summary>
        public string effect;

        public string ballName;

        public Trigger(string name, string description, string type, string effect, string ballName)
        {
            this.name = name;
            this.description = description;
            this.type = type;
            this.effect = effect;
            this.ballName = ballName;
        }
    }
}
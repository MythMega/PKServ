using System;

namespace PKServ.Configuration
{
    public class BallThrowHistory
    {
        // propriétés de définitions
        public Pokeball Ball { get; set; }

        public Pokemon Pokemon { get; set; }
        public User User { get; set; }

        // propriétés autres

        public DateTime time = DateTime.Now;

        public bool isNew = false;

        public bool catchResult { get; set; } = false;

        public bool shiny = false;

        public int price = 0;

        // propriétés d'Overlay

        public bool shownInOverlay_bar = false;
    }
}
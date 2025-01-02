using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ
{
    public class CatchHistory
    {

        // propriétés de définitions
        public Pokeball Ball {  get; set; }
        public Pokemon Pokemon { get; set; }
        public User User { get; set; }

        // propriétés autres

        public DateTime time = DateTime.Now;

        public bool shiny = false;

        public int price = 0;

        // propriétés d'Overlay

        public bool shownInOverlay_lastCaughtPokeSprite = false;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ
{
    public class Artist
    {
        /// <summary>
        /// Name of the artist
        /// </summary>
        public string ArtistName { get; set; }

        /// <summary>
        /// Link to the artist's page
        /// </summary>
        public string ArtistLink { get; set; }

        /// <summary>
        /// Credit to the artist
        /// </summary>
        public string ArtistCredit { get; set; }

        public Artist(string artistName = "", string artistLink = "", string artistCredit = "")
        {
            ArtistName = artistName;
            ArtistLink = artistLink;
            ArtistCredit = artistCredit;
        }

        public Artist(string artistName)
        {
            ArtistName = artistName;
            ArtistLink = "#";
        }

        public Artist()
        { }
    }
}
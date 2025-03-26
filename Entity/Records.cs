using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Entity
{
    public class Records
    {
        public int ID { get; set; }
        public string CreatureName { get; set; }
        public string Statut { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }

        public Records(int ID, string creatureName, string statut, string type, DateTime date)
        {
            this.ID = ID;
            CreatureName = creatureName;
            Statut = statut;
            Type = type;
            Date = date;
        }

        public Records(string creatureName, string statut, string type, DateTime date)
        {
            ID = -1;
            CreatureName = creatureName;
            Statut = statut;
            Type = type;
            Date = date;
        }
    }
}
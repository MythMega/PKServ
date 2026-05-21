using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Binding
{
    public static class IconBinding
    {
        public static string GetIconURL(string code)
        {
            string resultat = "NO-ICON";
            switch (code.ToUpper())
            {
                case "NEW":
                    resultat = "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/Icons/new.png";
                    break;
            }

            return resultat;
        }
    }
}
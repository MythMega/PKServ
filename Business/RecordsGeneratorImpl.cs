﻿using PKServ.Configuration;
using PKServ.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Business
{
    public static class RecordsGeneratorImpl
    {
        public static void GenerateRecords(DataConnexion dataConnexion, AppSettings appSettings)
        {
            List<Records> records = dataConnexion.GetRecords();
            StringBuilder sb = new StringBuilder();
            foreach (Records record in records)
            {
                string spriteLink = string.Empty;

                Pokemon creature = appSettings.pokemons.FirstOrDefault(x => Commun.isSamePoke(x, record.CreatureName));
                if (creature != null)
                {
                    spriteLink = record.Statut.ToLower().StartsWith('s') ? creature.Sprite_Shiny : creature.Sprite_Normal;
                }

                sb.AppendLine(@$"<tr>
                <td class=""pokename""> {record.CreatureName}</td>
                <td><img src=""{spriteLink}"" alt=""Sprite""></td>
                <td>{record.Statut}</td>
                <td>{record.Type}</td>
                <td>{record.Date}</td>
            </tr>");
            }
            string fileContent = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Records List</title>
    <!-- Bootstrap CSS -->
    <link href=""https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/css/bootstrap.min.css"" rel=""stylesheet"">
    <style>
        body {{
            background-color: #2a2a2a;
            color: #ffffff;
            padding: 20px;
        }}
        .pokename {{
        font-size: 30px;
        }}
        .table tbody td img {{
            height: 64px;
            width: auto;
        }}
        .count {{font-size: 40px;
        }}
        /* Noir et blanc */
        .black-and-white {{filter: grayscale(100%);
          -webkit-filter: grayscale(100%);
        }}

        /* Tout noir (seulement la forme) */
        .all-black {{filter: brightness(0%);
          -webkit-filter: brightness(0%);
        }}
        /* Texte plus grand dans <td> */
        .large-text td {{font - size: 20px; }}

        .container-badge {{max - width: 500px;
            }}

        .trophy-True {{
            height: 64px;
            width: 64px;
        }}

        .trophy-False {{
            height: 64px;
            width: 64px;
            filter: brightness(0%);
          -webkit-filter: brightness(0%);
        }}

        .uncommon {{box-shadow: inset 0 0 4px lime;
        }}

        .rare {{box-shadow: inset 0 0 8px aqua;
        }}

        .epic {{box-shadow: inset 0 0 12px #7D0DC3;
        }}

        .legendary {{box-shadow: inset 0 0 16px gold;
        }}

        .exotic {{box-shadow: inset 0 0 20px pink;
        }}

        th, td {{ text-align: center; border: 1px solid black; padding: 10px; }}

    </style>
</head>
<body>
    <nav class=""navbar navbar-dark bg-dark"" style=""justify-content: center; background-color: #2a2a2a;"">
      <form class=""form-inline"">
        <a class=""btn btn-sm btn-outline-secondary"" href=""./main.html"" style=""color: white;"">Accueil Pokédex</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""./commandgenerator.html"" style=""color: white;"">Command Generator</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""./raid.html"" style=""color: white;"">Raid Result</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""./availablepokemon.html"" style=""color: white;"">Pokédex Infos</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""./pokestats.html"" style=""color: white;"">Classements</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""./records.html"" style=""color: white;"">Enregistrements</a>
      </form>
    </nav><br><br>
    <h1>Records</h1>
    <input type=""text"" id=""searchInput"" placeholder=""Rechercher Pokémon ou Statut"" class=""form-control"" style=""margin-bottom: 20px; max-width: 300px;"">
    <p>{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.</p>
    <table class=""table table-dark table-bordered table-striped"">
        <thead class=""thead-light"">
            <tr>
                <th>Pokémon</th>
                <th>Sprite</th>
                <th>Statut</th>
                <th>Type</th>
                <th>Date</th>
            </tr>
        </thead>
        <tbody id=""recordsTable"">

{sb.ToString()}

</tbody>
    </table>

    <script>
        // Fonction pour filtrer les résultats
        document.getElementById('searchInput').addEventListener('keyup', function() {{
            const searchValue = this.value.toLowerCase(); // Texte saisi
            const tableRows = document.querySelectorAll('#recordsTable tr'); // Toutes les lignes du tableau
            
            tableRows.forEach(row => {{
                const pokémon = row.cells[0].textContent.toLowerCase(); // Colonne Pokémon
                const statut = row.cells[2].textContent.toLowerCase(); // Colonne Statut

                if (pokémon.includes(searchValue) || statut.includes(searchValue)) {{
                    row.style.display = ''; // Montrer la ligne
                }} else {{
                    row.style.display = 'none'; // Cacher la ligne
                }}
            }});
        }});
    </script>

    <!-- Bootstrap JS, Popper.js, and jQuery -->
    <script src=""https://code.jquery.com/jquery-3.5.1.slim.min.js""></script>
    <script src=""https://cdn.jsdelivr.net/npm/@popperjs/core@2.9.2/dist/umd/popper.min.js""></script>
    <script src=""https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js""></script>
</body>
</html>";
            if (!Directory.Exists("WebExport"))
            {
                Directory.CreateDirectory("WebExport");
            }
            File.WriteAllText("WebExport\\records.html", fileContent);
        }
    }
}
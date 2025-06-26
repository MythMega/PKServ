using PKServ.Binding;
using PKServ.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKServ.Business
{
    public static class RaidStatsReportImpl
    {
        public static string GenerateRaidReport(PKServ.Raid raid, AppSettings appSettings, GlobalAppSettings globalAppSettings, DataConnexion dataConnexion)
        {
            User leader = raid.Stats.UserDamageTotal
                     .OrderByDescending(x => x.Value)
                     .First().Key;

            // Calcul du coefficient de chance pour chaque user
            List<(User user, float luck)> lucks = new List<(User user, float luck)>();
            foreach (User user in raid.Stats.UserDamageTotal.Keys)
            {
                float avg = (float)raid.Stats.UserDamageTotal[user] / (float)raid.Stats.UserDamageCount[user];
                float luck = avg / (float)raid.UserDamageBase[user];
                lucks.Add((user, luck));
            }
            string facts = "";
            if (raid.UserRaidStats.Count > 0)
            {
                // Construction de la liste brute de tous les fun facts
                var funFacts = new List<string>();
                var stats = raid.UserRaidStats; // IEnumerable<UserRaidStats>

                // 1) Le healer du groupe
                var topHealer = stats.OrderByDescending(u => u.HealPeople).FirstOrDefault();
                if (topHealer?.HealPeople > 0)
                    funFacts.Add($"Le healer du groupe : {topHealer.User.Pseudo} {PlatformBinding.IconHTML(topHealer.User.Platform, IconSize.Small)} ({topHealer.HealPeople} personne(s) soignées)");

                // 2) Le plus grand empoisonneur
                var topPoisoner = stats.OrderByDescending(u => u.PoisonOther).FirstOrDefault();
                if (topPoisoner?.PoisonOther > 0)
                    funFacts.Add($"Le plus toxique : {topPoisoner.User.Pseudo} {PlatformBinding.IconHTML(topPoisoner.User.Platform, IconSize.Small)} ({topPoisoner.PoisonOther} empoisonnement(s))");

                // 3) Le plus malchanceux (KO)
                var mostKOd = stats.OrderByDescending(u => u.StatutCountKo).FirstOrDefault();
                if (mostKOd?.StatutCountKo > 0)
                    funFacts.Add($"Le plus malchanceux (KO) : {mostKOd.User.Pseudo} {PlatformBinding.IconHTML(mostKOd.User.Platform, IconSize.Small)} ({mostKOd.StatutCountKo} KO)");

                // 4) Le plus paralysé
                var mostParalyzed = stats.OrderByDescending(u => u.StatutCountPara).FirstOrDefault();
                if (mostParalyzed?.StatutCountPara > 0)
                    funFacts.Add($"Le plus paralysé : {mostParalyzed.User.Pseudo} {PlatformBinding.IconHTML(mostParalyzed.User.Platform, IconSize.Small)} ({mostParalyzed.StatutCountPara} fois)");

                // 5) Le plus gelé
                var mostFrozen = stats.OrderByDescending(u => u.StatutCountFrozen).FirstOrDefault();
                if (mostFrozen?.StatutCountFrozen > 0)
                    funFacts.Add($"Le plus gelé : {mostFrozen.User.Pseudo} {PlatformBinding.IconHTML(mostFrozen.User.Platform, IconSize.Small)} ({mostFrozen.StatutCountFrozen} fois)");

                // 6) Le plus brûlé
                var mostBurnt = stats.OrderByDescending(u => u.StatutCountBurnt).FirstOrDefault();
                if (mostBurnt?.StatutCountBurnt > 0)
                    funFacts.Add($"Le plus brûlé : {mostBurnt.User.Pseudo} {PlatformBinding.IconHTML(mostBurnt.User.Platform, IconSize.Small)} ({mostBurnt.StatutCountBurnt} fois)");

                // 7) Le plus confus
                var mostConfused = stats.OrderByDescending(u => u.StatutCountConfused).FirstOrDefault();
                if (mostConfused?.StatutCountConfused > 0)
                    funFacts.Add($"Le plus confus : {mostConfused.User.Pseudo} {PlatformBinding.IconHTML(mostConfused.User.Platform, IconSize.Small)} ({mostConfused.StatutCountConfused} fois)");

                // 8) Le plus souvent face au vent arrière
                var mostBackWind = stats.OrderByDescending(u => u.StatutCountBackWind).FirstOrDefault();
                if (mostBackWind?.StatutCountBackWind > 0)
                    funFacts.Add($"Le plus souvent face au vent arrière : {mostBackWind.User.Pseudo} {PlatformBinding.IconHTML(mostBackWind.User.Platform, IconSize.Small)} ({mostBackWind.StatutCountBackWind} fois)");

                // 9) Le plus endormi
                var mostAsleep = stats.OrderByDescending(u => u.StatutCountAsleep).FirstOrDefault();
                if (mostAsleep?.StatutCountAsleep > 0)
                    funFacts.Add($"Le plus endormi : {mostAsleep.User.Pseudo} {PlatformBinding.IconHTML(mostAsleep.User.Platform, IconSize.Small)} ({mostAsleep.StatutCountAsleep} fois)");

                // 10) Le plus en mode guérison
                var mostHealing = stats.OrderByDescending(u => u.StatutCountHealing).FirstOrDefault();
                if (mostHealing?.StatutCountHealing > 0)
                    funFacts.Add($"Meilleur healer : {mostHealing.User.Pseudo} {PlatformBinding.IconHTML(mostHealing.User.Platform, IconSize.Small)} ({mostHealing.StatutCountHealing} fois)");

                // 11) Le plus empoisonné
                var mostPoisoned = stats.OrderByDescending(u => u.StatutCountPoisoned).FirstOrDefault();
                if (mostPoisoned?.StatutCountPoisoned > 0)
                    funFacts.Add($"Le plus empoisonné : {mostPoisoned.User.Pseudo} {PlatformBinding.IconHTML(mostPoisoned.User.Platform, IconSize.Small)} ({mostPoisoned.StatutCountPoisoned} fois)");

                // 12) Le plus grand cumul de tours sous effet
                var mostUnderEffect = stats.OrderByDescending(u => u.TotalRoundUnderEffect).FirstOrDefault();
                if (mostUnderEffect?.TotalRoundUnderEffect > 0)
                    funFacts.Add($"Le plus grand cumul de tours sous effet : {mostUnderEffect.User.Pseudo} {PlatformBinding.IconHTML(mostUnderEffect.User.Platform, IconSize.Small)} ({mostUnderEffect.TotalRoundUnderEffect} tours)");

                // Sélection aléatoire de 5 fun facts (ou moins si on en a moins)
                var random = new Random();
                var factsSelected = funFacts
                    .OrderBy(_ => random.Next())
                    .Take(5)
                    .ToList();

                if (factsSelected.Count > 0)
                {
                    foreach (string fact in factsSelected)
                    {
                        facts += @$"
      <div class=""col-12"" data-aos=""fade-up"">
        <h3>{fact}.</h3><br><br>
      </div>
";
                    }
                }
            }

            // Utilisateur le plus chanceux (même méthode que dans votre code)
            (User luckiest, float stat) luckyUser = (
                lucks.OrderByDescending(x => x.luck).First().user,
                lucks.OrderByDescending(x => x.luck).First().luck
            );

            // Utilisateur le moins chanceux : on récupère le tuple complet
            (User lessLucky, float stat) lessLuckyUser = (
                lucks.OrderBy(x => x.luck).First().user,
                lucks.OrderBy(x => x.luck).First().luck
            );

            // Pour "rookie" et "veteran", il s'agit ici de sélectionner l'utilisateur avec, respectivement, le plus petit ou le plus grand nombre de raids.
            // On réalise un premier tri et on conserve le tuple complet (l'user et le compteur de raids).

            var rookieEntry = raid.Stats.UserDamageTotal
                                    .OrderBy(x => x.Key.Stats.RaidCount)
                                    .FirstOrDefault();
            (User rookie, int raidCount) rookieData = (
                rookieEntry.Key,
                rookieEntry.Key.Stats.RaidCount
            );

            var veteranEntry = raid.Stats.UserDamageTotal
                                     .OrderByDescending(x => x.Key.Stats.RaidCount)
                                     .FirstOrDefault();
            (User veteran, int raidCount) veteranData = (
                veteranEntry.Key,
                veteranEntry.Key.Stats.RaidCount
            );

            return $@"
<!DOCTYPE html>
<html lang=""fr"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <title>Statistiques Raid - {raid.Boss.Name_FR}</title>
  <!-- Bootstrap 5 CSS avec thème sombre de Bootswatch (Darkly) -->
  <link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootswatch@5.3.0/dist/darkly/bootstrap.min.css"">
  <!-- AOS CSS -->
  <link href=""https://cdnjs.cloudflare.com/ajax/libs/aos/2.3.4/aos.css"" rel=""stylesheet"">
  <!-- Chart.js -->
  <script src=""https://cdn.jsdelivr.net/npm/chart.js""></script>
  <!-- Papa Parse pour le parsing du CSV -->
  <script src=""https://cdnjs.cloudflare.com/ajax/libs/PapaParse/5.3.2/papaparse.min.js""></script>

  <!-- Styles personnalisés pour affiner le thème sombre -->
  <style>
    body {{
      background -color: #121212;
      color: #e0e0e0;
    }}
    .table {{
      background-color: #1e1e1e;
    }}
    .table-striped tbody tr:nth-of-type(odd) {{
      background-color: rgba(255, 255, 255, 0.05);
    }}
    .table-striped tbody tr:nth-of-type(even) {{
      background-color: rgba(255, 255, 255, 0.1);
    }}
    th, td {{
      border-color: #555;
    }}
    h1, h2 {{
      color: #fff;
    }}
  </style>
</head>
<body>
    <nav class=""navbar navbar-dark bg-dark"" style=""justify-content: center; background-color: #2a2a2a;"">
      <form class=""form-inline"">
        <a class=""btn btn-sm btn-outline-secondary"" href=""/main.html"" style=""color: white;"">Accueil Pokédex</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""/commandgenerator.html"" style=""color: white;"">Command Generator</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""/raid.html"" style=""color: white;"">Raid Result</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""/availablepokemon.html"" style=""color: white;"">Pokédex Infos</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""/pokestats.html"" style=""color: white;"">Classements</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""/records.html"" style=""color: white;"">Enregistrements</a>
      </form>
    </nav><br><br>
  <div class=""container my-4"" data-aos=""fade-up"">
    <h1 class=""text-center mb-4"" data-aos=""fade-down"">Statistiques Raid</h1><br>
    <img style=""width=480px; height=auto;"" src=""{raid.Boss.Sprite_Normal}"" class=""text-center mb-4"" data-aos=""fade-down""></img><br>
    <h2 class=""text-center mb-4"" data-aos=""fade-down"">{raid.Boss.Name_FR} ({raid.PVMax}) - {DateTime.Now.ToString("g")}</h2><br>
    <!-- Section Tableau -->
    <div class=""row mb-5"" data-aos=""fade-right"">
      <div class=""col-12"">
        <h2>Tableau des données</h2>
        <table id=""dataTable"" class=""table table-striped"">
          <thead>
            <tr>
              <th>Platform</th>
              <th>Pseudo</th>
              <th>Damage</th>
              <th>CountAtk</th>
              <th>BaseDmg</th>
              <th>Level</th>
              <th>RaidCount</th>
            </tr>
          </thead>
          <tbody>
            <!-- Les lignes seront insérées ici par JavaScript -->
          </tbody>
        </table>
      </div>
    </div>

    <!-- Section Graphiques -->
    <div class=""row"">
      <div class=""col-md-6 mb-4"" data-aos=""fade-up"">
        <h2>Somme de dégât par plateforme</h2>
        <canvas id=""chartPlatform""></canvas>
      </div>
      <div class=""col-md-12 mb-4"" data-aos=""fade-up"">
        <h2>Dégâts par personne</h2>
        <canvas id=""chartDamage""></canvas>
      </div>
      <div class=""col-md-6 mb-4"" data-aos=""fade-up"">
        <h2>Dommage de base par personne</h2>
        <canvas id=""chartBaseDmg""></canvas>
      </div>
      <div class=""col-md-6 mb-4"" data-aos=""fade-up"">
        <h2>Nombre d'Attaques par personne</h2>
        <canvas id=""chartAtkCount""></canvas>
      </div>
    </div>
    <div class=""row"">
      <div class=""col-12"" data-aos=""fade-up"">
        <h2>Leader du Raid : {leader.Pseudo}</h2><br><br>
        {Business.TrainerCardImpl.GetTrainerCardHtml(leader, dataConnexion, appSettings, globalAppSettings)}
      </div>
    </div>
    <div class=""row"">
    <br><br><h2>Stats de raids</h2><br><br>
      <div class=""col-12"" data-aos=""fade-up"">
        <h3>Le plus chanceux : {luckyUser.luckiest.Pseudo} avec un ratio de {luckyUser.stat}.</h3><br><br>
      </div>
      <div class=""col-12"" data-aos=""fade-up"">
        <h3>Le moins chanceux : {lessLuckyUser.lessLucky.Pseudo} avec un ratio de {lessLuckyUser.stat}.</h3><br><br>
      </div>
      <div class=""col-12"" data-aos=""fade-up"">
        <h3>Le plus ancien : {veteranData.veteran.Pseudo} avec un total de {veteranData.raidCount} raids.</h3><br><br>
      </div>
      <div class=""col-12"" data-aos=""fade-up"">
        <h3>Un rookie : {rookieData.rookie.Pseudo} avec un total de {rookieData.raidCount} raids.</h3><br><br>
      </div>
             {facts}
    </div>
  </div>

  <!-- Script de chargement et d'affichage des données -->
  <script>
    document.addEventListener(""DOMContentLoaded"", function() {{
      // Initialisation de AOS (animate on scroll)
      AOS.init({{
        duration: 1500, // Durée de l'animation en ms
        once: false      // L'animation se déclenche qu'une seule fois
      }});

      // Chargement du fichier CSV avec Papa Parse
      Papa.parse(""assets/data/RaidStats.csv"", {{
        download: true,
        header: true,
        complete: function(results) {{
          const data = results.data;
          console.log(""Données chargées :"", data);

          // Objets de regroupement pour les graphiques
          const platformTotals = {{}};
          const pseudoDamage    = {{}};
          const pseudoBaseDmg   = {{}};
          const pseudoAtkCount  = {{}};

          const tableBody = document.querySelector(""#dataTable tbody"");

          // Parcourir chaque ligne du CSV
          data.forEach(row => {{
            // Ignorer les lignes vides ou incomplètes
            if (!row.platform || !row.pseudo) return;

            // Insertion de la ligne dans le tableau HTML
            const tr = document.createElement(""tr"");
            tr.innerHTML = `
              <td>${{row.platform}}</td>
              <td>${{row.pseudo}}</td>
              <td>${{row.damage}}</td>
              <td>${{row.countAtk}}</td>
              <td>${{row.baseDmg}}</td>
              <td>${{row.level}}</td>
              <td>${{row.raidCount}}</td>
            `;
            tableBody.appendChild(tr);

            // Conversion en nombre
            const damage   = parseFloat(row.damage)   || 0;
            const baseDmg  = parseFloat(row.baseDmg)  || 0;
            const countAtk = parseFloat(row.countAtk) || 0;

            // Regroupement par plateforme pour la somme des dégâts
            if (platformTotals[row.platform]) {{
              platformTotals[row.platform] += damage;
            }} else {{
              platformTotals[row.platform] = damage;
            }}

            // Regroupement par pseudo pour les dégâts
            if (pseudoDamage[row.pseudo]) {{
              pseudoDamage[row.pseudo] += damage;
            }} else {{
              pseudoDamage[row.pseudo] = damage;
            }}

            // Regroupement par pseudo pour le Base Damage
            if (pseudoBaseDmg[row.pseudo]) {{
              pseudoBaseDmg[row.pseudo] += baseDmg;
            }} else {{
              pseudoBaseDmg[row.pseudo] = baseDmg;
            }}

            // Regroupement par pseudo pour le nombre d'attaques
            if (pseudoAtkCount[row.pseudo]) {{
              pseudoAtkCount[row.pseudo] += countAtk;
            }} else {{
              pseudoAtkCount[row.pseudo] = countAtk;
            }}
          }});

          // Préparation des données pour Chart.js
          const platformLabels     = Object.keys(platformTotals);
          const platformData       = Object.values(platformTotals);
          const pseudoLabelsDamage = Object.keys(pseudoDamage);
          const damageData         = Object.values(pseudoDamage);
          const pseudoLabelsBase   = Object.keys(pseudoBaseDmg);
          const baseDmgData        = Object.values(pseudoBaseDmg);
          const pseudoLabelsAtk    = Object.keys(pseudoAtkCount);
          const atkCountData       = Object.values(pseudoAtkCount);

          // Graphique 1 : Camembert pour la somme des dégâts par plateforme
          new Chart(document.getElementById(""chartPlatform""), {{
            type: ""pie"",
            data: {{
              labels: platformLabels,
              datasets: [{{
                label: ""Somme des dégâts"",
                data: platformData,
                backgroundColor: [
                  ""rgba(255, 99, 132, 0.6)"",
                  ""rgba(54, 162, 235, 0.6)"",
                  ""rgba(255, 206, 86, 0.6)"",
                  ""rgba(75, 192, 192, 0.6)"",
                  ""rgba(153, 102, 255, 0.6)"",
                  ""rgba(255, 159, 64, 0.6)""
                ],
                borderWidth: 1
              }}]
            }},
            options: {{
              responsive: true,
              plugins: {{
                tooltip: {{
                  callbacks: {{
                    label: function(context) {{
                      const label = context.label || """";
                      const value = context.parsed;
                      return label + "": "" + value;
                    }}
                  }}
                }}
              }}
            }}
          }});

          // Graphique 2 : Diagramme à barres pour les dégâts par pseudo
          new Chart(document.getElementById(""chartDamage""), {{
            type: ""bar"",
            data: {{
              labels: pseudoLabelsDamage,
              datasets: [{{
                label: ""Dégâts"",
                data: damageData,
                backgroundColor: ""rgba(54, 162, 235, 0.6)"",
                borderColor: ""rgba(54, 162, 235, 1)"",
                borderWidth: 1
              }}]
            }},
            options: {{
              responsive: true,
              plugins: {{
                tooltip: {{
                  callbacks: {{
                    label: function(context) {{
                      return context.label + "": "" + context.parsed.y;
                    }}
                  }}
                }}
              }},
              scales: {{
                y: {{
                  beginAtZero: true
                }}
              }}
            }}
          }});

          // Graphique 3 : Diagramme à barres pour le Base Damage par pseudo
          new Chart(document.getElementById(""chartBaseDmg""), {{
            type: ""bar"",
            data: {{
              labels: pseudoLabelsBase,
              datasets: [{{
                label: ""Base Damage"",
                data: baseDmgData,
                backgroundColor: ""rgba(255, 206, 86, 0.6)"",
                borderColor: ""rgba(255, 206, 86, 1)"",
                borderWidth: 1
              }}]
            }},
            options: {{
              responsive: true,
              plugins: {{
                tooltip: {{
                  callbacks: {{
                    label: function(context) {{
                      return context.label + "": "" + context.parsed.y;
                    }}
                  }}
                }}
              }},
              scales: {{
                y: {{
                  beginAtZero: true
                }}
              }}
            }}
          }});

          // Graphique 4 : Diagramme à barres pour le nombre d'attaques par pseudo
          new Chart(document.getElementById(""chartAtkCount""), {{
            type: ""bar"",
            data: {{
              labels: pseudoLabelsAtk,
              datasets: [{{
                label: ""Nombre d'Attaques"",
                data: atkCountData,
                backgroundColor: ""rgba(75, 192, 192, 0.6)"",
                borderColor: ""rgba(75, 192, 192, 1)"",
                borderWidth: 1
              }}]
            }},
            options: {{
              responsive: true,
              plugins: {{
                tooltip: {{
                  callbacks: {{
                    label: function(context) {{
                      return context.label + "": "" + context.parsed.y;
                    }}
                  }}
                }}
              }},
              scales: {{
                y: {{
                  beginAtZero: true
                }}
              }}
            }}
          }});
        }}
      }});
    }});
  </script>

  <!-- Bootstrap Bundle JS (inclut Popper) -->
  <script src=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js""></script>
  <!-- AOS JS -->
  <script src=""https://cdnjs.cloudflare.com/ajax/libs/aos/2.3.4/aos.js""></script>
</body>
</html>

";
        }
    }
}
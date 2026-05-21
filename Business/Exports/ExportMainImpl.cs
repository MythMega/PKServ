using PKServ.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Business.Exports
{
    public static class ExportMainImpl
    {
        public static string GetFileContent(DataConnexion dataConnexion, AppSettings appSettings)
        {
            string fileContent = string.Empty;
            List<User> utilisateurs = dataConnexion.GetAllUserPlatforms();
            List<PKServ.Entrie> entries = dataConnexion.GetAllEntries();

            int globalDexNormal = 0;
            int globalDexShiny = 0;

            foreach(Pokemon creature in appSettings.pokemons)
            {
                if (entries.Where(entrie => Commun.isSamePoke(creature, entrie.PokeName) && entrie.CountNormal > 0).Any())
                    globalDexNormal++;
                if (entries.Where(entrie => Commun.isSamePoke(creature, entrie.PokeName) && entrie.CountShiny > 0).Any())
                    globalDexShiny++;
            }

            double pourcentageNormalDex = (globalDexNormal * 100.0) / appSettings.pokemons.Count;
            double pourcentageShinyDex = (globalDexShiny * 100.0) / appSettings.pokemons.Count;


            string dataPseudoList = string.Join(Environment.NewLine,
    utilisateurs.Select(x => $@"<option value=""{x.Pseudo}"">"));
            int NombreTotalPokeball = 0;
            int NombreTotalSousouDepense = 0;
            int NombreTotalPokecapture = 0;
            int NombreTotalShinycapture = 0;
            int nombreShinyDistribue = 0;
            int nombreNormalDistribue = 0;

            utilisateurs.ForEach(u => NombreTotalPokeball += u.Stats.ballLaunched);
            utilisateurs.ForEach(u => NombreTotalSousouDepense += u.Stats.moneySpent);
            utilisateurs.ForEach(u => NombreTotalPokecapture += u.Stats.pokeCaught);
            utilisateurs.ForEach(u => NombreTotalShinycapture += u.Stats.shinyCaught);
            utilisateurs.ForEach(u => nombreShinyDistribue += u.Stats.giveawayShiny);
            utilisateurs.ForEach(u => nombreNormalDistribue += u.Stats.giveawayNormal);

            // -ceux qui lancent le plus de ball
            string classementLanceurDeBall = "stat non initialisée";

            // -ceux qui ont le plus dépensé
            string classementDepenseur = "stat non initialisée";

            // -ceux qui ont le plus haut shinydex
            string classementShinyHunter = "stat non initialisée";

            // -ceux qui ont le plus gros pokédex
            string classementDexProgress = "stat non initialisée";

            //// ceux qu'on le meilleur ratio pokéballs lancées / thunes dépensées
            //string classementRadin = "stat non initialisée";

            //// ceux qui ont attrapé le plus de pokémon totaux
            //string classementMassCatcher = "stat non initialisé";

            if (utilisateurs.Count >= 3)
            {
                // 1. Charger une fois toutes les entrées par utilisateur
                var entriesByUser = utilisateurs
                  .ToDictionary(
                    u => u,
                    u => dataConnexion
                           .GetEntriesByPseudo(u.Pseudo, u.Platform)
                           .ToList()
                  );

                // 2. Préparer les données de classement
                var topByBallLaunched = utilisateurs
                  .OrderByDescending(u => u.Stats.ballLaunched)
                  .Take(3)
                  .ToList();

                var topByMoneySpent = utilisateurs
                  .OrderByDescending(u => u.Stats.moneySpent)
                  .Take(3)
                  .ToList();

                // Nombre d’espèces shiny par utilisateur
                var shinyCounts = entriesByUser
                  .ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.Count(e => e.CountShiny > 0)
                  );

                var topByShiny = shinyCounts
                  .OrderByDescending(kv => kv.Value)
                  .Take(3)
                  .Select(kv => kv.Key)
                  .ToList();

                // Nombre total d’espèces par utilisateur
                var totalCounts = entriesByUser
                  .ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.Count
                  );

                var topByDex = totalCounts
                  .OrderByDescending(kv => kv.Value)
                  .Take(3)
                  .Select(kv => kv.Key)
                  .ToList();

                // 3. Méthode utilitaire pour générer le HTML “Top 3”
                string BuildTop3Html<T>(
                    List<T> list,
                    Func<T, string> selectPseudo,
                    Func<T, string> selectValue
                )
                {
                    var lines = list
                      .Select((item, i) =>
                         $"{i + 1}. {selectPseudo(item)} ({selectValue(item)})"
                      );
                    return $"<p>{string.Join("<br>", lines)}</p>";
                }

                // 4. Génération finale des chaînes
                classementLanceurDeBall = BuildTop3Html(
                    topByBallLaunched,
                    u => u.Pseudo,
                    u => Commun.GetStringNumber(u.Stats.ballLaunched)
                );

                classementDepenseur = BuildTop3Html(
                    topByMoneySpent,
                    u => u.Pseudo,
                    u => Commun.GetStringNumber(u.Stats.moneySpent)
                );

                classementShinyHunter = BuildTop3Html(
                    topByShiny,
                    u => u.Pseudo,
                    u => Commun.GetStringNumber(shinyCounts[u])
                );

                classementDexProgress = BuildTop3Html(
                    topByDex,
                    u => u.Pseudo,
                    u => Commun.GetStringNumber(totalCounts[u])
                );

            }

            if (utilisateurs.Count > 1)
            {
                //var luckiest = utilisateurs.Where(u => dataConnexion.GetEntriesByPseudo(u.pseudo, u.platform).Count > 10).OrderByDescending(x => (x.stats.pokeCaught / x.stats.ballLaunched)).FirstOrDefault();
                //var unluckiest = utilisateurs.Where(u => dataConnexion.GetEntriesByPseudo(u.pseudo, u.platform).Count > 10).OrderBy(x => (x.stats.pokeCaught / x.stats.ballLaunched)).FirstOrDefault();
                //luckyCatcher = $"Le plus chanceux : {luckiest.pseudo} ({luckiest.stats.pokeCaught} pokémon attrapés pour {luckiest.stats.ballLaunched} ball lancées, soit un taux de capture de {Math.Round((double)(luckiest.stats.pokeCaught * 100) / luckiest.stats.ballLaunched)}%)";
                //unluckyCatcher = $"Le moins chanceux : {unluckiest.pseudo} ({unluckiest.stats.pokeCaught} pokémon attrapés pour {unluckiest.stats.ballLaunched} ball lancées, soit un taux de capture de {Math.Round((double)(unluckiest.stats.pokeCaught * 100) / unluckiest.stats.ballLaunched)}%)";
            }

            fileContent = Commun.DefaultHTMLStart(false, "StreamDex") + @$"
<style>
  /* Centrer le form plein écran */
  .container {{
    min-height: 100vh;
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    padding-top: 0; /* pour que le scroll ne démarre qu'après */
  }}

  /* Formulaire moderne */
  #redirectForm {{
    width: 90%;
    max-width: 500px;
    background: rgba(33, 37, 41, 0.9);
    padding: 2rem;
    border-radius: 8px;
    box-shadow: 0 10px 25px rgba(0, 0, 0, 0.5);
    transition: transform 0.3s ease;
  }}
  #redirectForm:hover {{
    transform: scale(1.02);
  }}

  /* Labels au-dessus et inputs full-width */
  #redirectForm .form-group {{
    margin-bottom: 1.5rem;
  }}
  #redirectForm label {{
    display: block;
    margin-bottom: 0.5rem;
    color: #ccc;
    font-size: 1rem;
  }}
  #redirectForm select,
  #redirectForm input[type=""text""] {{
    width: 100%;
    padding: 0.75rem 1rem;
    border: 1px solid #444;
    border-radius: 4px;
    background: #1e2124;
    color: #eee;
    font-size: 1rem;
    transition: border-color 0.3s, box-shadow 0.3s;
  }}
  #redirectForm select:focus,
  #redirectForm input[type=""text""]:focus {{
    outline: none;
    border-color: #6441A5;
    box-shadow: 0 0 8px rgba(100, 65, 165, 0.6);
  }}

  /* Bouton large et animé */
  #redirectForm input[type=""submit""] {{
    width: 100%;
    padding: 0.85rem;
    background: #6441A5;
    color: #fff;
    font-size: 1.1rem;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    transition: background 0.3s ease, transform 0.2s ease;
  }}
  #redirectForm input[type=""submit""]:hover {{
    background: #7f5cc9;
    transform: translateY(-2px);
  }}

  /* Forcer le scroll pour voir les stats */
  body {{
    overflow-y: auto;
  }}
  /* Décaler le début des stats plus bas */
  .stats-section {{
    margin-top: 25vh;
    padding: 2rem;
    background: #212529;
    color: #fff;
  }}

  /* Mise en page plus cool pour les stats */
  .stats-cards {{
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
    gap: 1.5rem;
  }}
  .stats-card {{
    background: #2a2a2a;
    padding: 1.5rem;
    border-radius: 8px;
    box-shadow: 0 5px 15px rgba(0,0,0,0.4);
    text-align: center;
  }}
  .stats-card h3 {{
    margin-bottom: .5rem;
    font-size: 1.25rem;
    color: #ffd700;
  }}
  .stats-card p {{
    font-size: 1rem;
    color: #ddd;
  }}
</style>

  <div class=""container"">

<br><br>
<h1>Voir son propre dex</h1>
<br><br>
<form id=""redirectForm"">
  <div class=""form-group"">
    <label for=""platform"">Choisissez la plateforme</label>
    <select id=""platform"" name=""platform"" class=""form-select"">
      <option value=""twitch"">Twitch</option>
      <option value=""youtube"">YouTube</option>
      <option value=""tiktok"">TikTok</option>
      <option value=""discord"">Discord</option>
    </select>
  </div>

  <div class=""form-group"">
    <label for=""pseudo"">Choisissez votre pseudo</label>
    <input
      type=""text""
      id=""pseudo""
      name=""pseudo""
      class=""form-control""
      list=""pseudoList""
      autocomplete=""off""
      placeholder=""Tapez pour filtrer…""
    />
    <datalist id=""pseudoList"">
      {dataPseudoList}
    </datalist>
    <div id=""errorPseudo"" class=""error-message"" style=""color:#f66; margin-top:0.5rem;""></div>
  </div>

  <input type=""submit"" value=""Valider la recherche"" />
</form>

<script src=""https://cdnjs.cloudflare.com/ajax/libs/awesomplete/1.1.5/awesomplete.min.js""></script>

<script>
  document.getElementById('redirectForm').addEventListener('submit', function(e) {{
    e.preventDefault();

    const pseudoInput = document.getElementById('pseudo');
    const errorDiv   = document.getElementById('errorPseudo');
    const list       = Array.from(document.querySelectorAll('#pseudoList option'))
                            .map(opt => opt.value.toLowerCase());
    const value      = pseudoInput.value.trim().toLowerCase();

    // Validation : doit exister dans la datalist
    if (!list.includes(value) || !value) {{
      errorDiv.textContent = 'Veuillez choisir un pseudo valide dans la liste.';
      pseudoInput.focus();
      return;
    }}
    errorDiv.textContent = '';

    // redirection
    const platform = document.getElementById('platform').value;
    const baseUrl  = window.location.href
                         .substring(0, window.location.href.lastIndexOf(""/""));
    window.location.href = `${{baseUrl}}/${{platform}}/${{value}}.html`;
  }});
</script>

    <p>Dernière mise à jour : {DateTime.Now.ToString("dd/MM/yyyy HH:mm")}</p>
 <section class=""stats-section"">
  <h1 class=""mt-5"">Stats globales de la chaîne</h1>
  <div class=""stats-cards"">
    <div class=""stats-card"">
      <h3>Ball lancées</h3>
      <p>{Commun.GetStringNumber(NombreTotalPokeball)}</p>
    </div>
    <div class=""stats-card"">
      <h3>Money dépensée</h3>
      <p>{Commun.GetStringNumber(NombreTotalSousouDepense)}</p>
    </div>
    <div class=""stats-card"">
      <h3>Poké capturés</h3>
      <p>{Commun.GetStringNumber(NombreTotalPokecapture)}</p>
    </div>
    <div class=""stats-card"">
      <h3>Shinys capturés</h3>
      <p>{Commun.GetStringNumber(NombreTotalShinycapture)}</p>
    </div>
  </div>

  <h1 class=""mt-5"">Progrés</h1>
  <div class=""stats-cards"">
    <div class=""stats-card"">
      <h3>GlobalDex</h3>
      <p>{Commun.GetStringNumber(globalDexNormal)} / {Commun.GetStringNumber(appSettings.pokemons.Count)}</p>
      <p> {pourcentageNormalDex:0.00}% <p>
    </div>
    <div class=""stats-card"">
      <h3>ShinyDex</h3>
      <p>{Commun.GetStringNumber(globalDexShiny)} / {Commun.GetStringNumber(appSettings.pokemons.Count)}</p>
      <p> {pourcentageShinyDex:0.00}% <p>
    </div>
    <div class=""stats-card"">
      <h3>Nombre de dresseurs :</h3>
      <p>{Commun.GetStringNumber(utilisateurs.Count)}</p>
    </div>
  </div>

  <h1 class=""mt-5"">Classements</h1>
  <div class=""stats-cards"">
    <div class=""stats-card"">
      <h3>Top Ball lancées</h3>
      {classementLanceurDeBall}
    </div>
    <div class=""stats-card"">
      <h3>Top Points de chaine dépensé</h3>
      {classementDepenseur}
    </div>
    <div class=""stats-card"">
      <h3>ShinyDex</h3>
      {classementShinyHunter}
    </div>
    <div class=""stats-card"">
      <h3>Dex complet</h3>
      {classementDexProgress}
    </div>
  </div>
</section>

" + Commun.DefaultHTMLEnd();

            return fileContent;
        }
    }
}
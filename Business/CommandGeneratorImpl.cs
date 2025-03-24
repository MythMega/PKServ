using Microsoft.VisualBasic.FileIO;
using PKServ.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PKServ.Business
{
    public static class CommandGeneratorImpl
    {
        public static string GenerateFileContent(AppSettings appSettings, GlobalAppSettings globalAppSettings, DataConnexion data)
        {
            string content = string.Empty;

            string optionsListPoke = string.Join("", appSettings.pokemons.Select(p => $"<option value=\"{p.AltName.Replace(' ', '_')}\">{p.Name_FR}</option>\n"));
            string optionsListPokeBuyable = string.Join("", appSettings.pokemons.Where(poke => poke.priceNormal is not null || poke.priceShiny is not null).Select(p => $"<option value=\"{p.AltName.Replace(' ', '_')}\">{p.Name_FR}</option>\n"));

            // introduction
            content = @$"
<!DOCTYPE html>
<html lang=""fr"">
<head>
  <meta charset=""utf-8"">
  <title>Générateur par onglet</title>
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <!-- Bootstrap 5.3.3 CSS -->
  <link href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css"" rel=""stylesheet"" integrity=""sha384-QWTKZyjpPEjISv5WaRU9OFeRpok6YctnYmDr5pNlyT2bRjXh0JMhjY6hW+ALEwIH"" crossorigin=""anonymous"">
  <style>
    /* Styles personnalisés */
    .error {{
      border: 2px solid red;
    }}
    .error-message {{
      color: red;
      font-size: 0.9rem;
    }}
  </style>
</head>
<body>
<div class=""container my-4"">
  <h1>Générateur de commande StreamDex</h1>";

            // définitions des onglets
            content += @"

  <!-- Navigation par onglets -->
  <ul class=""nav nav-tabs"" id=""myTab"" role=""tablist"">
    <li class=""nav-item"" role=""presentation"">
      <button class=""nav-link"" id=""tab-buy-tab"" data-bs-toggle=""tab"" data-bs-target=""#tab-buy"" type=""button"" role=""tab"" aria-controls=""tab-buy"" aria-selected=""false"">Buy</button>
    </li>
    <li class=""nav-item"" role=""presentation"">
      <button class=""nav-link"" id=""tab-scrap-tab"" data-bs-toggle=""tab"" data-bs-target=""#tab-scrap"" type=""button"" role=""tab"" aria-controls=""tab-scrap"" aria-selected=""false"">Scrap</button>
    </li>
    <li class=""nav-item"" role=""presentation"">
      <button class=""nav-link"" id=""tab-trade-tab"" data-bs-toggle=""tab"" data-bs-target=""#tab-trade"" type=""button"" role=""tab"" aria-controls=""tab-trade"" aria-selected=""false"">Trade</button>
    </li>
    <li class=""nav-item"" role=""presentation"">
      <button class=""nav-link"" id=""tab-badge-tab"" data-bs-toggle=""tab"" data-bs-target=""#tab-badge"" type=""button"" role=""tab"" aria-controls=""tab-badge"" aria-selected=""false"">Badges</button>
    </li>
    <li class=""nav-item"" role=""presentation"">
      <button class=""nav-link"" id=""tab-background-tab"" data-bs-toggle=""tab"" data-bs-target=""#tab-background"" type=""button"" role=""tab"" aria-controls=""tab-background"" aria-selected=""false"">Fond Carte</button>
    </li>
  </ul>"
;

            // onglet Buy
            content += @"
<div class=""tab-content"" id=""myTabContent"">
  <!-- Onglet Buy -->
  <div class=""tab-pane fade p-3"" id=""tab-buy"" role=""tabpanel"" aria-labelledby=""tab-buy-tab"">
    <h3>Générateur Buy</h3>

    <!-- Choix du Pokémon via datalist -->
    <div class=""mb-3"">
      <label for=""pokemonBuy"" class=""form-label"">Choisissez un Pokémon</label>
      <input type=""text"" class=""form-control"" id=""pokemonBuy"" list=""pokemonListBuy"" autocomplete=""off"">
      <datalist id=""pokemonListBuy"">
        " + optionsListPokeBuyable + @"
      </datalist>
      <div class=""error-message"" id=""errorPokemonBuy""></div>
    </div>

    <!-- Sélection de la version (Normal ou Shiny) -->
    <div class=""mb-3"">
      <label for=""variantBuy"" class=""form-label"">Version</label>
      <select class=""form-select"" id=""variantBuy"">
        <option value=""normal"" selected>Normal</option>
        <option value=""shiny"">Shiny</option>
      </select>
    </div>

    <!-- Bouton générer et affichage de la commande -->
    <button class=""btn btn-primary"" id=""generateBtn_buy"">Générer</button>
    <div class=""mt-3"">
      <textarea class=""form-control"" id=""resultBox_buy"" rows=""3"" readonly></textarea>
    </div>
    <button class=""btn btn-secondary mt-2"" id=""copyBtn_buy"" disabled>Copier commande</button>
  </div>

  <!-- Script pour le générateur Buy -->
  <script>
    document.getElementById('generateBtn_buy').addEventListener('click', function() {
      var pokemonValue = document.getElementById('pokemonBuy').value.trim();
      var variantValue = document.getElementById('variantBuy').value;

      // Vérifier que l'utilisateur a sélectionné un Pokémon
      if (!pokemonValue) {
        document.getElementById('errorPokemonBuy').textContent = 'Veuillez choisir un Pokémon.';
        return;
      } else {
        document.getElementById('errorPokemonBuy').textContent = '';
      }

      // Mettre en majuscule la première lettre de la variante
      var variantText = variantValue.charAt(0).toUpperCase() + variantValue.slice(1);
      var command = '!buy ' + pokemonValue + ' ' + variantText;

      // Afficher la commande et activer le bouton copier
      document.getElementById('resultBox_buy').value = command;
      document.getElementById('copyBtn_buy').disabled = false;
    });

    document.getElementById('copyBtn_buy').addEventListener('click', function() {
      var resultBox = document.getElementById('resultBox_buy');
      resultBox.select();
      try {
        document.execCommand('copy');
      } catch (err) {
        console.error('Erreur lors de la copie :', err);
      }
    });
  </script>
";

            // onglet SCRAP
            content += @"
<!-- Onglet Scrap -->
<div class=""tab-pane fade p-3"" id=""tab-scrap"" role=""tabpanel"" aria-labelledby=""tab-scrap-tab"">
  <h3>Générateur Scrap</h3>

  <!-- Combobox : Mode de scrap -->
  <div class=""mb-3"">
    <label for=""scrapMode_scrap"" class=""form-label"">Mode de scrap</label>
    <select class=""form-select"" id=""scrapMode_scrap"">
      <option value=""fullscrap"">Full Scrap (Scrap tous le pokédex normal & shiny)</option>
      <option value=""complete"" selected>Complete (scrap tous les poké normal et shiny du pokémon choisi)</option>
      <option value=""fullnormal"">Fullnormal (scrap tout les normaux du poké choisi)</option>
      <option value=""fullshiny"">Fullshiny (scrap tous les shiny du poké choisi)</option>
      <option value=""normal"">Normal (scrap 1 poke normal du poke choisi)</option>
      <option value=""shiny"">Shiny (scrap 1 poke shiny du poke choisi)</option>
    </select>
  </div>

  <!-- Combobox : Liste de Pokémons -->
  <div class=""mb-3"">
    <label for=""pokemonScrap"" class=""form-label"">Choisissez un Pokémon</label>
    <input type=""text"" class=""form-control"" id=""pokemonScrap"" list=""pokemonList_scrap"" autocomplete=""off"">
    <datalist id=""pokemonList_scrap"">
      " + optionsListPoke + @"
    </datalist>
    <div class=""error-message"" id=""errorPokemonScrap""></div>
  </div>

  <!-- Bouton générer et zone d'affichage de la commande -->
  <button class=""btn btn-primary"" id=""generateBtn_scrap"">Générer</button>
  <div class=""mt-3"">
    <textarea class=""form-control"" id=""resultBox_scrap"" rows=""3"" readonly></textarea>
  </div>
  <button class=""btn btn-secondary mt-2"" id=""copyBtn_scrap"" disabled>Copier commande</button>

  <!-- Script pour le générateur Scrap -->
  <script>
    // Active ou désactive la combobox des Pokémons selon le mode choisi
    document.getElementById('scrapMode_scrap').addEventListener('change', function() {
      var mode = this.value;
      var pokemonInput = document.getElementById('pokemonScrap');
      if (mode === 'fullscrap') {
        pokemonInput.value = '';
        pokemonInput.disabled = true;
      } else {
        pokemonInput.disabled = false;
      }
    });

    document.getElementById('generateBtn_scrap').addEventListener('click', function() {
      var mode = document.getElementById('scrapMode_scrap').value;
      var pokemonValue = document.getElementById('pokemonScrap').value.trim();
      var command = '';

      if (mode === 'fullscrap') {
        // Pour le mode fullscrap, la commande est fixe et la liste des Pokémons est ignorée
        command = '!scrap full fulldex';
      } else {
        // Vérifier que l'utilisateur a sélectionné un Pokémon
        if (!pokemonValue) {
          document.getElementById('errorPokemonScrap').textContent = 'Veuillez choisir un Pokémon.';
          return;
        } else {
          document.getElementById('errorPokemonScrap').textContent = '';
        }
        command = '!scrap ' + pokemonValue + ' ' + mode;
      }

      document.getElementById('resultBox_scrap').value = command;
      document.getElementById('copyBtn_scrap').disabled = false;
    });

    document.getElementById('copyBtn_scrap').addEventListener('click', function() {
      var resultBox = document.getElementById('resultBox_scrap');
      resultBox.select();
      try {
        document.execCommand('copy');
      } catch (err) {
        console.error('Erreur lors de la copie :', err);
      }
    });
  </script>
</div>
";

            // onglet TRADE
            content += @$"
    <!-- onglet TRADE -->
<div class=""tab-pane fade p-3"" id=""tab-trade"" role=""tabpanel"" aria-labelledby=""tab-trade-tab"">
  <h3>Générateur de Trade</h3>

  <!-- Pokémon envoyé -->
  <div class=""mb-3"">
    <label for=""pokeEnvoye_trade"" class=""form-label"">Pokémon Envoyé</label>
    <input
      type=""text""
      class=""form-control""
      id=""pokeEnvoye_trade""
      list=""pokeList_trade""
      autocomplete=""off"">
    <datalist id=""pokeList_trade"">
      {optionsListPoke}
    </datalist>
    <div class=""error-message"" id=""error_pokeEnvoye_trade""></div>
  </div>

  <!-- Statut du Pokémon envoyé -->
  <div class=""mb-3"">
    <label for=""statusEnvoye_trade"" class=""form-label"">Statut (Envoyé)</label>
    <select class=""form-select"" id=""statusEnvoye_trade"">
      <option value=""Normal"" selected>Normal</option>
      <option value=""Shiny"">Shiny</option>
    </select>
  </div>

  <!-- Pokémon demandé -->
  <div class=""mb-3"">
    <label for=""pokeDemande_trade"" class=""form-label"">Pokémon Demandé</label>
    <input
      type=""text""
      class=""form-control""
      id=""pokeDemande_trade""
      list=""pokeList_trade""
      autocomplete=""off"">
    <div class=""error-message"" id=""error_pokeDemande_trade""></div>
  </div>

  <!-- Statut du Pokémon demandé -->
  <div class=""mb-3"">
    <label for=""statusDemande_trade"" class=""form-label"">Statut (Demandé)</label>
    <select class=""form-select"" id=""statusDemande_trade"">
      <option value=""Normal"" selected>Normal</option>
      <option value=""Shiny"">Shiny</option>
    </select>
  </div>

  <!-- Bouton générer la commande -->
  <button class=""btn btn-primary"" id=""generate_trade"">Générer</button>

  <!-- Zone d'affichage de la commande générée -->
  <div class=""mt-3"">
    <textarea class=""form-control"" id=""result_trade"" rows=""5"" readonly></textarea>
  </div>

  <!-- Bouton pour copier la commande -->
  <button class=""btn btn-secondary mt-2"" id=""copy_trade"" disabled>Copier commande</button>
</div>

<script>
  // Génération de la commande !trade
  document.getElementById(""generate_trade"").addEventListener(""click"", function() {{
    var pokeEnvoye = document.getElementById(""pokeEnvoye_trade"").value.trim();
    var statutEnvoye = document.getElementById(""statusEnvoye_trade"").value;
    var pokeDemande = document.getElementById(""pokeDemande_trade"").value.trim();
    var statutDemande = document.getElementById(""statusDemande_trade"").value;

    if (pokeEnvoye === """" || pokeDemande === """") {{
      alert(""Veuillez remplir tous les champs."");
      return;
    }}

    var commande = ""!trade "" + pokeEnvoye + "" "" + statutEnvoye + "" "" + pokeDemande + "" "" + statutDemande;
    document.getElementById(""result_trade"").value = commande;
    document.getElementById(""copy_trade"").disabled = false;
  }});

  // Copie de la commande générée dans le presse-papier
  document.getElementById(""copy_trade"").addEventListener(""click"", function() {{
    var commande = document.getElementById(""result_trade"").value;
    navigator.clipboard.writeText(commande).then(function() {{
      alert(""Commande copiée !"");
    }});
  }});
</script>
";

            // onglet Badge
            content += @"

    <!-- Onglet 3 -->
    <div class=""tab-pane fade p-3"" id=""tab-badge"" role=""tabpanel"" aria-labelledby=""tab-badge-tab"">
      <h3>Générateur 3</h3>

      <!-- Combobox 1 -->
      <div class=""mb-3"">
        <label for=""combobox1_tab-badge"" class=""form-label"">Combobox 1</label>
        <input type=""text"" class=""form-control"" id=""combobox1_tab-badge"" list=""list1_tab-badge"" autocomplete=""off"">
        <datalist id=""list1_tab-badge"">
          <option value=""Red""></option>
          <option value=""Green""></option>
          <option value=""Blue""></option>
        </datalist>
        <div class=""error-message"" id=""error1_tab-badge""></div>
      </div>

      <!-- Combobox 2 -->
      <div class=""mb-3"">
        <label for=""combobox2_tab-badge"" class=""form-label"">Combobox 2</label>
        <input type=""text"" class=""form-control"" id=""combobox2_tab-badge"" list=""list2_tab-badge"" autocomplete=""off"">
        <datalist id=""list2_tab-badge"">
          <option value=""Cyan""></option>
          <option value=""Magenta""></option>
          <option value=""Yellow""></option>
        </datalist>
        <div class=""error-message"" id=""error2_tab-badge""></div>
      </div>

      <!-- Combobox 3 -->
      <div class=""mb-3"">
        <label for=""combobox3_tab-badge"" class=""form-label"">Combobox 3</label>
        <input type=""text"" class=""form-control"" id=""combobox3_tab-badge"" list=""list3_tab-badge"" autocomplete=""off"">
        <datalist id=""list3_tab-badge"">
          <option value=""Black""></option>
          <option value=""White""></option>
          <option value=""Gray""></option>
        </datalist>
        <div class=""error-message"" id=""error3_tab-badge""></div>
      </div>
    </div>
  </div>";

            // onglet background
            // Récupération des groupes distincts
            List<string> groups = appSettings.TrainerCardsBackgrounds
                .Select(bg => bg.Group)
                .Distinct()
                .ToList();

            // Construction des options pour le select
            string groupOption = string.Join("", groups.Select(g => $"<option value=\"{g}\">{g}</option>\n"));

            // Sérialisation en JSON de l'ensemble des backgrounds
            string backgroundsJson = JsonSerializer.Serialize(appSettings.TrainerCardsBackgrounds, Commun.GetJsonSerializerOptions());

            content += @$"
<!-- Onglet Background -->
<div class=""tab-pane fade p-3"" id=""tab-background"" role=""tabpanel"" aria-labelledby=""tab-background-tab"">
  <h3>Fond Carte de dresseur</h3>

  <!-- Combobox 1 : Sélection du groupe -->
  <div class=""mb-3"">
    <label for=""combobox1_tab-background"" class=""form-label"">Groupe</label>
    <select class=""form-select"" id=""combobox1_tab-background"">
      <option selected disabled>Choisissez une option</option>
      {groupOption}
    </select>
    <div class=""error-message"" id=""error1_tab-background""></div>
  </div>

  <!-- Zone destinée à accueillir la grille de backgrounds -->
  <div id=""background-grid"" class=""row row-cols-1 row-cols-md-4 g-3"">
    <!-- Les images filtrées apparaîtront ici -->
  </div>
</div>

<script>
// On convertit le JSON généré côté serveur en objet JavaScript.
const trainerBackgroundsList = JSON.parse(`{backgroundsJson}`);

// Fonction de copie dans le presse-papiers avec fallback.
function copyToClipboard(button) {{
  const textToCopy = button.getAttribute(""data-copy"");

  if (!navigator.clipboard) {{
    // Fallback pour les navigateurs qui ne supportent pas l'API Clipboard.
    const textArea = document.createElement(""textarea"");
    textArea.value = textToCopy;
    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();
    try {{
      document.execCommand(""copy"");
      alert(""Texte copié : "" + textToCopy);
    }} catch (err) {{
      alert(""Erreur lors de la copie dans le presse-papiers : "" + err);
    }}
    document.body.removeChild(textArea);
    return;
  }}

  navigator.clipboard.writeText(textToCopy).then(() => {{
    alert(""Texte copié : "" + textToCopy);
  }}).catch(err => {{
    alert(""Erreur lors de la copie dans le presse-papiers : "" + err);
  }});
}}

// Fonction qui se charge d'afficher les backgrounds correspondant au groupe sélectionné.
function displayBackgroundsByGroup(selectedGroup) {{
  const container = document.getElementById('background-grid');
  container.innerHTML = ''; // Réinitialise la zone d'affichage.

  // Filtre des backgrounds correspondant au groupe choisi.
  const matchingBackgrounds = trainerBackgroundsList.filter(bg => bg.Group === selectedGroup);

  if (matchingBackgrounds.length === 0) {{
    container.innerHTML = '<p>Aucun background trouvé pour ce groupe.</p>';
    return;
  }}

  // Pour chacun des backgrounds filtrés, on crée une colonne contenant une card.
  matchingBackgrounds.forEach(bg => {{
    const col = document.createElement('div');
    col.className = 'col';

    const card = document.createElement('div');
    card.className = 'card';

    // Ajout de l'image
    const img = document.createElement('img');
    img.className = 'card-img-top';
    img.src = bg.Url; // On utilise la propriété Url pour obtenir l'image.
    img.alt = bg.Group;
    card.appendChild(img);

    // Création d'un container pour le contenu textuel de la card.
    const cardBody = document.createElement('div');
    cardBody.className = 'card-body';

    // Titre h4 avec le nom de la carte (bg.Name).
    const nameHeading = document.createElement('h4');
    nameHeading.textContent = bg.Name;
    cardBody.appendChild(nameHeading);

    // Titre h5 ""Requirements :"".
    const reqHeading = document.createElement('h5');
    reqHeading.textContent = 'Requirements :';
    cardBody.appendChild(reqHeading);

    // Création d'une liste des requirements.
    const reqList = document.createElement('ul');
    if (bg.requirements && bg.requirements.length > 0) {{
      bg.requirements.forEach(req => {{
        const reqItem = document.createElement('li');
        reqItem.textContent = req.Type + ': ' + req.Value;
        reqList.appendChild(reqItem);
      }});
    }} else {{
      // Au cas où il n'y aurait pas de requirements.
      const emptyItem = document.createElement('li');
      emptyItem.textContent = 'Aucun requirement défini.';
      reqList.appendChild(emptyItem);
    }}
    cardBody.appendChild(reqList);

    // Création du bouton pour copier la commande.
    const copyButton = document.createElement('button');
    copyButton.className = 'btn btn-secondary mt-2';
    copyButton.textContent = 'Copier la commande';

    // Préparation de la commande à copier.
    // Remplacement des espaces par des underscores dans le nom.
    const commandText = ""!changeCard "" + bg.Name.replace(/ /g, ""_"");

    // Ajout de l'attribut data-copy contenant la commande.
    copyButton.setAttribute(""data-copy"", commandText);

    // Ajout de l'événement qui copie le texte dans le presse-papiers lors du clic.
    copyButton.addEventListener('click', function() {{
      copyToClipboard(this);
    }});

    cardBody.appendChild(copyButton);

    // Ajout du container textuel à la card.
    card.appendChild(cardBody);
    col.appendChild(card);
    container.appendChild(col);
  }});
}}

// Ajout d'un écouteur sur le select afin de détecter le changement de groupe.
document.getElementById('combobox1_tab-background').addEventListener('change', function() {{
  const selectedGroup = this.value;
  displayBackgroundsByGroup(selectedGroup);
}});

// Appel initial de la fonction.
// Attention : Assurez-vous que la variable selectedGroup est définie ou récupérée au préalable.
displayBackgroundsByGroup(selectedGroup);
</script>
";

            // fin HTML
            content += @"

  </div>
</div>

<!-- Bootstrap 5.3.3 JS Bundle -->
<script src=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"" integrity=""sha384-YvpcrYf0tY3lHB60NNkmXc5s9fDVZLESaAA55NDzOxhy9GkcIdslK1eN7N6jIeHz"" crossorigin=""anonymous""></script>";

            // script JS
            content += @"
<script>
</script>
</body>
</html>

";

            return content;
        }
    }
}
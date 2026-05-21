using PKServ.Binding;
using PKServ.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Business.Exports
{
    public static class ExportZone
    {
        public static async Task ExportIndividualZone(Zone zone, AppSettings appSettings, GlobalAppSettings globalAppSettings)
        {
            // Chemin du dossier pour la page zone
            string directoryPath = Path.Combine("WebExport", "Zone");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Filtrer les pokémons activés selon la condition donnée
            var filteredPokemons = appSettings.allPokemons
                .Where(pokemon => pokemon.enabled && !pokemon.isLock &&
                       (!pokemon.IsZoneExclusive || pokemon.ZonesList.Any(z => z.Name.Equals(zone.Name, StringComparison.OrdinalIgnoreCase))))
                .ToList();

            // Séparer et ordonner les pokémons : exclusifs d'abord
            var exclusivePokemons = filteredPokemons.Where(p => p.IsZoneExclusive && p.ZonesList.Count == 1).ToList();
            var nonExclusivePokemons = filteredPokemons.Where(p => !p.IsZoneExclusive || p.ZonesList.Count != 1).ToList();
            var orderedPokemons = exclusivePokemons.Concat(nonExclusivePokemons).ToList();
            var orderedPokemonsByName = orderedPokemons
                .OrderByDescending(p => p.IsZoneExclusive)
                .ThenByDescending(pokemon => Commun.CompareStrings(pokemon.ZonesList.OrderBy(zone => zone.DexRequirement).FirstOrDefault().Name, zone.Name))
                .ToList();

            // Construction des cards pour chaque pokémon
            StringBuilder cardsBuilder = new StringBuilder();
            foreach (var pokemon in orderedPokemonsByName)
            {
                // Définir la classe de la card et y ajouter un style pour les exclusifs
                string cardClass = "card bg-dark text-white h-100";
                if (pokemon.isLegendary)
                {
                    cardClass += " card-legendary";
                }
                else if (pokemon.IsZoneExclusive && pokemon.ZonesList.Count == 1)
                {
                    cardClass += " card-exclusive";
                }
                else if (pokemon.IsZoneExclusive)
                {
                    cardClass += " card-not-everywhere";
                }
                else
                {
                    cardClass += " card-common";
                }

                // string displayName = Commun.CompareStrings(pokemon.Name_FR, pokemon.Name_EN) ? $"{pokemon.Name_EN}" : $"{pokemon.Name_FR} - {pokemon.Name_EN}";
                string displayName = globalAppSettings.LanguageCode.ToUpper() == LanguageBinding.FRENCH ? $"{pokemon.Name_FR}" : $"{pokemon.Name_EN}";

                // icône shiny (ou non)
                string additionalInfos = CreatureRarity.IconHTML(pokemon.Rarity, IconSize.Medium);
                additionalInfos += @$"<img class=""icon"" src=""{ShinyBinding.GetIcon(!pokemon.isShinyLock)}"">";
                if (Commun.CompareStrings(pokemon.ZonesList.OrderBy(zone => zone.DexRequirement).FirstOrDefault().Name, zone.Name))
                {
                    additionalInfos += @$"<img class=""icon"" src=""{IconBinding.GetIconURL("NEW")}"">";
                }

                cardsBuilder.AppendLine($@"
            <div class=""col"">
                <div class=""{cardClass}"">
                    <div class=""sprite-container"">
                        <img src=""{pokemon.Sprite_Normal}"" alt=""{pokemon.Name_FR}"" style=""object-fit:contain;"">
                    </div>
                    <div class=""card-body"">
                        <h5 class=""card-title"">
                            <a href=""../Creature/{pokemon.Name_FR}.html"">{displayName}</a> {additionalInfos}
                        </h5>
                    </div>
                </div>
            </div>
        ");
            }

            // Création du contenu HTML complet de la page
            string content = Commun.DefaultHTMLStart(true, $"StreamDex > {zone.Name}") + $@"
    <div class=""container my-5"">
        <!-- Section de la zone -->
        <div class=""text-center"">
            <img src=""{zone.Image}"" alt=""Zone Image"" class=""img-fluid zone-image"">
            <h1>{zone.Name}</h1>
            <p>Level Cap : {zone.LevelRequirement}</p>
            <p>Dex Cap : {zone.DexRequirement}</p>
            <p>Description : {zone.Description}</p>
        </div>

        <!-- Bouton de copie de commande -->
        <div class=""row mb-4"">
            <div class=""col text-center"">
                <button id=""copyZoneButton"" class=""btn btn-secondary"" onclick=""copyZoneCommand()"">Copier commande zone</button>
            </div>
        </div>

        <!-- Affichage du résultat de recherche -->
        <div class=""row mb-4"">
            <div class=""col text-center"">
                <span id=""resultCount""></span>
            </div>
        </div>

        <!-- Barre de recherche -->
        <div class=""row mb-4"">
            <div class=""col"">
                <input type=""text"" id=""searchInput"" class=""form-control"" placeholder=""Rechercher un Pokémon..."">
            </div>
        </div>

        <!-- Quelques exemples de cartes catégories bien identifiées -->
        <div class=""row mb-3"">
            <div class=""col"">
                <div class=""card-legendary"">
                    <div class=""card-body"">
                        <h5 class=""card-title"">Légendaires</h5>
                    </div>
                </div>
            </div>
            <div class=""col"">
                <div class=""card-exclusive"">
                    <div class=""card-body"">
                        <h5 class=""card-title"">Exclusifs</h5>
                    </div>
                </div>
            </div>
            <div class=""col"">
                <div class=""card-not-everywhere"">
                    <div class=""card-body"">
                        <h5 class=""card-title"">Zonaux</h5>
                    </div>
                </div>
            </div>
            <div class=""col"">
                <div class=""card-common"">
                    <div class=""card-body"">
                        <h5 class=""card-title"">Commun</h5>
                    </div>
                </div>
            </div>
        </div>

        <!-- Grille Bootstrap affichant les pokémons ciblés -->
        <div class=""row row-cols-2 row-cols-md-4 row-cols-lg-6 g-4 mt-5"" id=""cardsContainer"">
            {cardsBuilder.ToString()}
        </div>
    </div>
    <script src=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js""></script>
    <script>
        // Définit la commande à copier avec le nom de la zone formaté
        var zoneCommand = '!changeZone {zone.Name.Replace(" ", "_")}';

        function copyZoneCommand() {{
            navigator.clipboard.writeText(zoneCommand).then(function() {{
                alert('Commande copiée : ' + zoneCommand);
            }}, function(err) {{
                console.error('Erreur de copie : ', err);
            }});
        }}

        // Fonction pour mettre à jour le nombre de Pokémon affichés
        function updateResultCount() {{
            var cards = document.querySelectorAll('#cardsContainer .col');
            var visibleCount = 0;
            cards.forEach(function(card) {{
                if (card.style.display !== 'none') {{
                    visibleCount++;
                }}
            }});
            document.getElementById('resultCount').textContent = visibleCount + ' Pokémon(s) affiché(s)';
        }}

        document.getElementById('searchInput').addEventListener('keyup', function() {{
            var query = this.value.toLowerCase();
            var cards = document.querySelectorAll('#cardsContainer .col');
            cards.forEach(function(card) {{
                var title = card.querySelector('.card-title').textContent.toLowerCase();
                card.style.display = (title.indexOf(query) !== -1) ? '' : 'none';
            }});
            updateResultCount();
        }});

        // Mise à jour dès le chargement de la page
        window.addEventListener('load', updateResultCount);
    </script>
</body>
</html>
    ";
            content += Commun.DefaultHTMLEnd();
            // Sauvegarder la page
            string fileName = $"{zone.Name}.html";
            string filePath = Path.Combine(directoryPath, Commun.CleanFileName(fileName));
            File.WriteAllText(filePath, content);

            await Task.CompletedTask;
        }
    }
}
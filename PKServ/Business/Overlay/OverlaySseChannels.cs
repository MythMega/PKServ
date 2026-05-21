namespace PKServ.Business.Overlay
{
    /// <summary>
    /// Noms des canaux SSE disponibles.
    /// Chaque constante correspond à la fois :
    ///   - au nom du canal interne dans OverlaySseManager
    ///   - au segment d'URL : GET /overlay/{Channel}/stream
    ///   - au fichier HTML généré : StreamOverlays/{Channel}.html
    /// </summary>
    public static class OverlaySseChannels
    {
        /// <summary>Overlay de raid (boss HP, sprite, rareté). Mis à jour à chaque attaque.</summary>
        public const string Raid = "raid";

        /// <summary>Résumé du dernier lancer de pokéball (résultat, sprite, shiny, heure).</summary>
        public const string BallThrowResume = "ball-throw";

        /// <summary>Sprite + nom du dresseur du dernier pokémon attrapé.</summary>
        public const string LastCaughtSprite = "last-caught-sprite";

        /// <summary>Progression du pokédex global (toutes espèces normales).</summary>
        public const string GlobalDex = "global-dex";

        /// <summary>Progression du shiny-dex global.</summary>
        public const string GlobalShinyDex = "global-shiny-dex";

        /// <summary>Objectif total de captures globales.</summary>
        public const string GlobalTotalCaught = "global-total-caught";

        /// <summary>Objectif de shinies capturés globalement.</summary>
        public const string GlobalShinyCaught = "global-shiny-caught";

        /// <summary>Objectif d'argent dépensé globalement.</summary>
        public const string GlobalMoneySpent = "global-money-spent";

        /// <summary>Nombre de participants à la session en cours.</summary>
        public const string SessionParticipants = "session-participants";

        /// <summary>Objectif de captures totales pour la session.</summary>
        public const string SessionTotalCaught = "session-total-caught";

        /// <summary>Objectif de shinies capturés pour la session.</summary>
        public const string SessionShinyCaught = "session-shiny-caught";

        /// <summary>Objectif d'argent dépensé pendant la session.</summary>
        public const string SessionMoneySpent = "session-money-spent";
    }
}

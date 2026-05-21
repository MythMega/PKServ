using PKServ.Business.Overlay;
using PKServ.Business.Raid;
using PKServ.Configuration;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace PKServ.Controller
{
    /// <summary>
    /// État partagé injecté dans tous les contrôleurs.
    /// Instancié une seule fois dans Program.Main.
    /// </summary>
    public class ControllerContext
    {
        public AppSettings           Settings       { get; set; }
        public GlobalAppSettings     GlobalSettings { get; set; }
        public DataConnexion         Data           { get; set; }
        public List<User>            UsersHere      { get; set; }
        public JsonSerializerOptions JsonOptions    { get; set; }

        // ── Raid auto ────────────────────────────────────────────────
        public bool     AutoRaid      { get; set; }
        public int      AutoRaidCount { get; set; }
        public DateTime LastRaidCheck { get; set; }

        // ── SSE overlay raid en temps réel ────────────────────────────
        // Raccourcis vers les broadcasters SSE. Chaque méthode couvre un groupe
        // de canaux mis à jour par le même événement métier.
        public void BroadcastRaidUpdate()
        {
            // GetDamagesOverlay() consomme les dégâts (Active = false) à la lecture.
            // On les collecte UNE SEULE FOIS avant de broadcaster sur les deux canaux.
            var damages = Settings.ActiveRaid?.GetDamagesOverlay() ?? new System.Collections.Generic.List<string>();
            RaidSseManager.BroadcastRaidStateWithDamages(Settings, GlobalSettings, damages);
            OverlaySseBroadcaster.BroadcastRaidWithDamages(Settings, GlobalSettings, damages);
        }

        /// <summary>Après tout lancer de pokéball (réussi ou non).</summary>
        public void BroadcastBallThrow()
            => OverlaySseBroadcaster.BroadcastBallThrow(Settings);

        /// <summary>Après une capture réussie.</summary>
        public void BroadcastLastCaughtSprite()
            => OverlaySseBroadcaster.BroadcastLastCaughtSprite(Settings);

        /// <summary>Après capture, giveaway, fin de raid, buy — met à jour tous les compteurs globaux.</summary>
        public void BroadcastGlobalStats()
            => OverlaySseBroadcaster.BroadcastGlobalDex(Data, Settings, GlobalSettings);

        /// <summary>Après tout événement modifiant l'argent global dépensé.</summary>
        public void BroadcastGlobalMoneySpent()
            => OverlaySseBroadcaster.BroadcastGlobalMoneySpent(Data, GlobalSettings);

        /// <summary>Après capture, giveaway, fin de raid — met à jour les stats de session.</summary>
        public void BroadcastSessionStats()
            => OverlaySseBroadcaster.BroadcastSessionStats(Settings, GlobalSettings, UsersHere);

        /// <summary>Après SignIn ou AutoSignInGiveAway.</summary>
        public void BroadcastSessionParticipants()
            => OverlaySseBroadcaster.BroadcastSessionParticipants(Settings, GlobalSettings, UsersHere);

        /// <summary>Ajoute l'utilisateur à la liste giveaway si absent.</summary>
        public void AddToHere(User user)
        {
            if (!UsersHere.Exists(x => x.Pseudo == user.Pseudo && x.Platform == user.Platform))
            {
                UsersHere.Add(user);
                if (GlobalSettings.Log.logConsole.console)
                {
                    Commun.Logger($"red#{user.Pseudo}|white# (on |red#{user.Platform}|white#) ajouté à la liste du giveaway.");
                    Console.WriteLine("\r");
                }
                // Nouveau participant → broadcast canal session-participants
                BroadcastSessionParticipants();
            }
        }

        /// <summary>Marque l'utilisateur pour le prochain export JSON.</summary>
        public void MarkForExport(User user)
        {
            if (!Settings.UsersToExport.Exists(u =>
                    u.Code_user == user.Code_user ||
                    (u.Pseudo == user.Pseudo && u.Platform == user.Platform)))
                Settings.UsersToExport.Add(user);
        }

        /// <summary>Fixe le UserCode si "unset" via la BDD.</summary>
        public UserRequest TempFixUserRequest(UserRequest ur)
        {
            if (ur.UserCode == "unset")
            {
                var found = Data.GetAllUserPlatforms()
                    .Find(u => u.Pseudo == ur.UserName && u.Platform == ur.Platform);
                if (found is not null)
                    ur.UserCode = Data.GetCodeUserByPlatformPseudo(new User(ur.UserName, ur.Platform));
            }
            return ur;
        }

        /// <summary>Met à jour le code utilisateur en BDD pour la session en cours.</summary>
        public void TempFixUserCodeInBDD(UserRequest req)
        {
            Data.SetCodeUserByPlatformPseudo(new User
            {
                Data      = Data,
                Code_user = req.UserCode,
                Platform  = req.Platform,
                Pseudo    = req.UserName
            });
            var entries = Data.GetEntriesByPseudo(req.UserCode, req.Platform);
            entries.ForEach(e => { e.code = req.UserCode; e.Validate(false); });
        }
    }
}

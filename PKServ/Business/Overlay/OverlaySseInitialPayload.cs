using PKServ.Binding;
using PKServ.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace PKServ.Business.Overlay
{
    /// <summary>
    /// Construit le payload SSE initial envoyé à un client au moment de sa connexion.
    /// Ce payload reflète l'état courant du serveur pour que le client n'attende pas
    /// le prochain broadcast pour afficher quelque chose.
    /// </summary>
    public static class OverlaySseInitialPayload
    {
        /// <summary>
        /// Retourne une chaîne SSE formatée "data: {json}\n\n" pour le canal donné.
        /// Si le canal est inconnu, retourne un event vide.
        /// </summary>
        public static string Build(
            string channel,
            AppSettings settings,
            GlobalAppSettings gas,
            List<User> usersHere,
            DataConnexion data)
        {
            object payload = channel switch
            {
                OverlaySseChannels.Raid => BuildRaid(settings, gas),

                OverlaySseChannels.BallThrowResume => BuildBallThrow(settings),

                OverlaySseChannels.LastCaughtSprite => BuildLastCaughtSprite(settings),

                OverlaySseChannels.GlobalDex => BuildProgress(
                    data.GetAllEntries().GroupBy(e => e.PokeName).Count(),
                    settings.pokemons.Count),

                OverlaySseChannels.GlobalShinyDex => BuildProgress(
                    data.GetAllEntries().Where(e => e.CountShiny > 0).GroupBy(e => e.PokeName).Count(),
                    settings.pokemons.Count),

                OverlaySseChannels.GlobalTotalCaught => BuildProgress(
                    data.GetAllEntries().Sum(e => e.CountNormal + e.CountShiny),
                    gas.OverlaySettings.GlobalTotalCaughtGoal.GoalValue),

                OverlaySseChannels.GlobalShinyCaught => BuildProgress(
                    data.GetAllEntries().Sum(e => e.CountShiny),
                    gas.OverlaySettings.GlobalShinyCaughtGoal.GoalValue),

                OverlaySseChannels.GlobalMoneySpent => BuildGlobalMoney(data, gas),

                OverlaySseChannels.SessionParticipants => BuildProgress(
                    usersHere.Count,
                    gas.OverlaySettings.SessionParticipantsGoal.GoalValue),

                OverlaySseChannels.SessionTotalCaught => BuildProgress(
                    settings.catchHistory.Count,
                    gas.OverlaySettings.SessionTotalCaughtGoal.GoalValue),

                OverlaySseChannels.SessionShinyCaught => BuildProgress(
                    settings.catchHistory.Count(h => h.shiny),
                    gas.OverlaySettings.SessionShinyCaughtGoal.GoalValue),

                OverlaySseChannels.SessionMoneySpent => BuildProgress(
                    settings.catchHistory.Sum(h => h.price),
                    gas.OverlaySettings.SessionMoneySpentGoal.GoalValue),

                _ => new { }
            };

            return $"data: {JsonSerializer.Serialize(payload)}\n\n";
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static object BuildRaid(AppSettings settings, GlobalAppSettings gas)
        {
            if (settings.ActiveRaid is null) return new { active = false };
            var r = settings.ActiveRaid;
            return new
            {
                active           = true,
                Url_Creature     = r.DisplayShiny ? r.Boss.Sprite_Shiny : r.Boss.Sprite_Normal,
                Url_Overlay      = r.PV > 0
                    ? "https://upload.wikimedia.org/wikipedia/commons/thumb/8/89/HD_transparent_picture.png/1280px-HD_transparent_picture.png"
                    : gas.RaidSettings.PictureOverlayWhenCreatureFainted,
                Bar_Max          = r.PVMax,
                Bar_CurrentValue = r.PV,
                Rarity           = r.Boss.Rarity,
                Damages          = r.GetDamagesOverlay()
            };
        }

        private static object BuildBallThrow(AppSettings settings)
        {
            var last = settings.ballThrowHistory.LastOrDefault();
            if (last is null) return new { };
            return new
            {
                imageUrl          = last.shiny ? last.Pokemon.Sprite_Shiny : last.Pokemon.Sprite_Normal,
                userName          = last.User.Pseudo,
                userPlateformIcon = PlatformBinding.IconLink(last.User.Platform),
                isShiny           = last.shiny,
                isLegendary       = last.Pokemon.isLegendary,
                isNew             = last.isNew,
                isCaught          = last.catchResult,
                time              = last.time.ToString("HHmmss")
            };
        }

        private static object BuildLastCaughtSprite(AppSettings settings)
        {
            var last = settings.catchHistory.LastOrDefault();
            if (last is null) return new { };
            return new
            {
                imageUrl = last.shiny ? last.Pokemon.Sprite_Shiny : last.Pokemon.Sprite_Normal,
                userName = last.User.Pseudo
            };
        }

        private static object BuildGlobalMoney(DataConnexion data, GlobalAppSettings gas)
        {
            var users = data.GetAllUserPlatforms();
            users.ForEach(u => u.generateStats());
            return new
            {
                progress = users.Sum(u => u.Stats.moneySpent),
                total    = gas.OverlaySettings.GlobalMoneySpentGoal.GoalValue
            };
        }

        private static object BuildProgress(int progress, int total)
            => new { progress, total };

        private static object BuildProgress(long progress, int total)
            => new { progress, total };
    }
}

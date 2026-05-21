using PKServ.Binding;
using PKServ.Configuration;
using System;
using System.Linq;
using System.Text.Json;

namespace PKServ.Business.Overlay
{
    /// <summary>
    /// Point d'entrée unique pour diffuser l'état courant sur chaque canal SSE.
    ///
    /// Chaque méthode statique construit le payload JSON correspondant et appelle
    /// OverlaySseManager.BroadcastChannel(). Les contrôleurs appellent ces méthodes
    /// après chaque action métier qui modifie un état affiché.
    ///
    /// Canaux et déclencheurs :
    /// ┌──────────────────────────┬──────────────────────────────────────────────────────┐
    /// │ Canal                    │ Déclenché par                                        │
    /// ├──────────────────────────┼──────────────────────────────────────────────────────┤
    /// │ raid                     │ Attack, GivePoke, Load, StartManualRaid, AutoRaid    │
    /// │ ball-throw               │ CatchPoke, CatchPokeNew (tout lancer)                │
    /// │ last-caught-sprite       │ CatchPoke, CatchPokeNew (captures réussies seulement)│
    /// │ global-dex               │ CatchPoke, CatchPokeNew, Giveaway, fin raid, Buy     │
    /// │ global-shiny-dex         │ idem                                                 │
    /// │ global-total-caught      │ idem                                                 │
    /// │ global-shiny-caught      │ idem                                                 │
    /// │ global-money-spent       │ CatchPoke, CatchPokeNew (prix des pokéballs)         │
    /// │ session-participants     │ SignIn, AutoSignInGiveAway                           │
    /// │ session-total-caught     │ CatchPoke, CatchPokeNew, Giveaway, fin raid          │
    /// │ session-shiny-caught     │ idem                                                 │
    /// │ session-money-spent      │ CatchPoke, CatchPokeNew (prix des pokéballs)         │
    /// └──────────────────────────┴──────────────────────────────────────────────────────┘
    /// </summary>
    public static class OverlaySseBroadcaster
    {
        // ── Raid ─────────────────────────────────────────────────────────────

        public static void BroadcastRaid(AppSettings settings, GlobalAppSettings globalSettings)
            => BroadcastRaidWithDamages(settings, globalSettings, new System.Collections.Generic.List<string>());

        public static void BroadcastRaidWithDamages(AppSettings settings, GlobalAppSettings globalSettings, System.Collections.Generic.List<string> damages)
        {
            object payload;
            if (settings.ActiveRaid is null)
            {
                payload = new { active = false };
            }
            else
            {
                var r = settings.ActiveRaid;
                payload = new
                {
                    active           = true,
                    Url_Creature     = r.DisplayShiny ? r.Boss.Sprite_Shiny : r.Boss.Sprite_Normal,
                    Url_Overlay      = r.PV > 0
                        ? "https://upload.wikimedia.org/wikipedia/commons/thumb/8/89/HD_transparent_picture.png/1280px-HD_transparent_picture.png"
                        : globalSettings.RaidSettings.PictureOverlayWhenCreatureFainted,
                    Bar_Max          = r.PVMax,
                    Bar_CurrentValue = r.PV,
                    Rarity           = r.Boss.Rarity,
                    Damages          = damages
                };
            }
            OverlaySseManager.BroadcastChannel(OverlaySseChannels.Raid, payload);
        }

        // ── Lancer de pokéball (tout lancer, réussi ou non) ──────────────────

        public static void BroadcastBallThrow(AppSettings settings)
        {
            // Dernier lancer non encore affiché via SSE, ou le tout dernier si tous déjà vus
            var last = settings.ballThrowHistory
                           .LastOrDefault(x => !x.shownInOverlay_bar)
                       ?? settings.ballThrowHistory.LastOrDefault();

            if (last is null) { OverlaySseManager.BroadcastChannel(OverlaySseChannels.BallThrowResume, new { }); return; }

            last.shownInOverlay_bar = true;

            OverlaySseManager.BroadcastChannel(OverlaySseChannels.BallThrowResume, new
            {
                imageUrl           = last.shiny ? last.Pokemon.Sprite_Shiny : last.Pokemon.Sprite_Normal,
                userName           = last.User.Pseudo,
                userPlateformIcon  = PlatformBinding.IconLink(last.User.Platform),
                isShiny            = last.shiny,
                isLegendary        = last.Pokemon.isLegendary,
                isNew              = last.isNew,
                isCaught           = last.catchResult,
                time               = last.time.ToString("HHmmss")
            });
        }

        // ── Dernier sprite attrapé (captures réussies seulement) ─────────────

        public static void BroadcastLastCaughtSprite(AppSettings settings)
        {
            var last = settings.catchHistory
                           .LastOrDefault(x => !x.shownInOverlay_lastCaughtPokeSprite)
                       ?? settings.catchHistory.LastOrDefault();

            if (last is null) { OverlaySseManager.BroadcastChannel(OverlaySseChannels.LastCaughtSprite, new { }); return; }

            last.shownInOverlay_lastCaughtPokeSprite = true;

            OverlaySseManager.BroadcastChannel(OverlaySseChannels.LastCaughtSprite, new
            {
                imageUrl = last.shiny ? last.Pokemon.Sprite_Shiny : last.Pokemon.Sprite_Normal,
                userName = last.User.Pseudo
            });
        }

        // ── Barres de progression globales ────────────────────────────────────

        public static void BroadcastGlobalDex(DataConnexion data, AppSettings settings, GlobalAppSettings gas)
        {
            var entries = data.GetAllEntries();
            OverlaySseManager.BroadcastChannel(OverlaySseChannels.GlobalDex, new
            {
                progress = entries.GroupBy(e => e.PokeName).Count(),
                total    = settings.pokemons.Count
            });
            OverlaySseManager.BroadcastChannel(OverlaySseChannels.GlobalShinyDex, new
            {
                progress = entries.Where(e => e.CountShiny > 0).GroupBy(e => e.PokeName).Count(),
                total    = settings.pokemons.Count
            });
            OverlaySseManager.BroadcastChannel(OverlaySseChannels.GlobalTotalCaught, new
            {
                progress = entries.Sum(e => e.CountNormal + e.CountShiny),
                total    = gas.OverlaySettings.GlobalTotalCaughtGoal.GoalValue
            });
            OverlaySseManager.BroadcastChannel(OverlaySseChannels.GlobalShinyCaught, new
            {
                progress = entries.Sum(e => e.CountShiny),
                total    = gas.OverlaySettings.GlobalShinyCaughtGoal.GoalValue
            });
        }

        public static void BroadcastGlobalMoneySpent(DataConnexion data, GlobalAppSettings gas)
        {
            var allUsers = data.GetAllUserPlatforms();
            allUsers.ForEach(u => u.generateStats());
            OverlaySseManager.BroadcastChannel(OverlaySseChannels.GlobalMoneySpent, new
            {
                progress = allUsers.Sum(u => u.Stats.moneySpent),
                total    = gas.OverlaySettings.GlobalMoneySpentGoal.GoalValue
            });
        }

        // ── Barres de progression session ─────────────────────────────────────

        public static void BroadcastSessionStats(AppSettings settings, GlobalAppSettings gas, System.Collections.Generic.List<User> usersHere)
        {
            OverlaySseManager.BroadcastChannel(OverlaySseChannels.SessionParticipants, new
            {
                progress = usersHere.Count,
                total    = gas.OverlaySettings.SessionParticipantsGoal.GoalValue
            });
            OverlaySseManager.BroadcastChannel(OverlaySseChannels.SessionTotalCaught, new
            {
                progress = settings.catchHistory.Count,
                total    = gas.OverlaySettings.SessionTotalCaughtGoal.GoalValue
            });
            OverlaySseManager.BroadcastChannel(OverlaySseChannels.SessionShinyCaught, new
            {
                progress = settings.catchHistory.Count(h => h.shiny),
                total    = gas.OverlaySettings.SessionShinyCaughtGoal.GoalValue
            });
            OverlaySseManager.BroadcastChannel(OverlaySseChannels.SessionMoneySpent, new
            {
                progress = settings.catchHistory.Sum(h => h.price),
                total    = gas.OverlaySettings.SessionMoneySpentGoal.GoalValue
            });
        }

        public static void BroadcastSessionParticipants(AppSettings settings, GlobalAppSettings gas, System.Collections.Generic.List<User> usersHere)
        {
            OverlaySseManager.BroadcastChannel(OverlaySseChannels.SessionParticipants, new
            {
                progress = usersHere.Count,
                total    = gas.OverlaySettings.SessionParticipantsGoal.GoalValue
            });
        }
    }
}

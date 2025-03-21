﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace PKServ
{
    public class GlobalAppSettings
    {
        public int ServerPort { get; set; }
        public bool AutoSignInGiveAway { get; set; }
        public bool KeepUserInGiveAwayAfterShutdown { get; set; }
        public bool MustAutoFullExport { get; set; }
        public int DelayBeforeFullWebUpdate { get; set; }
        public string LanguageCode { get; set; } = "en";
        public string GitHubTokenUpload { get; set; }
        public string Namespace { get; set; }
        public GlobalAppLog Log { get; set; }
        public TextTranslation Texts { get; set; }
        public MessageSettings MessageSettings { get; set; }
        public List<ScheduledTask> ScheduledTasks { get; set; }
        public BadgeSettings BadgeSettings { get; set; }
        public ScrapSettings ScrapSettings { get; set; }
        public OverlaySettings OverlaySettings { get; set; }
        public CommandSettings CommandSettings { get; set; }
        public TradeSettings TradeSettings { get; set; }
        public RaidSettings RaidSettings { get; set; }
        public EvolveSettings EvolveSettings { get; set; }
        public GiveAwaySettings GiveAwaySettings { get; set; }

        internal void SetDefaultValue()
        {
            bool needUpdate = false;
            try { ServerPort.ToString(); } catch { ServerPort = 5052; needUpdate = true; }
            try { AutoSignInGiveAway.ToString(); } catch { AutoSignInGiveAway = true; needUpdate = true; }
            try { KeepUserInGiveAwayAfterShutdown.ToString(); } catch { KeepUserInGiveAwayAfterShutdown = true; needUpdate = true; }
            try { MustAutoFullExport.ToString(); } catch { MustAutoFullExport = true; needUpdate = true; }
            try { DelayBeforeFullWebUpdate.ToString(); } catch { DelayBeforeFullWebUpdate = 10; needUpdate = true; }
            try { LanguageCode.ToString(); } catch { LanguageCode = "en"; needUpdate = true; }
            try { GitHubTokenUpload.ToString(); } catch { GitHubTokenUpload = "UNSET"; needUpdate = true; }

            // log
            try { Log.ToString(); } catch { Log = new GlobalAppLog { logConsole = new GlobalAppLogConsole { console = true, logJsonOnConsole = true }, logFile = false }; }
            try { Log.logConsole.ToString(); } catch { Log.logConsole = new GlobalAppLogConsole { console = true, logJsonOnConsole = true }; }
            try { Log.logFile.ToString(); } catch { Log.logFile = false; }
            try { Log.logConsole.logJsonOnConsole.ToString(); } catch { Log.logConsole.logJsonOnConsole = true; }
            try { Log.logConsole.console.ToString(); } catch { Log.logConsole.console = true; }

            // texts globaux
            try { Texts.ToString(); } catch { Texts = new TextTranslation(); }
            try { Texts.serverStarted.ToString(); } catch { Texts.serverStarted = "UNSET"; }
            try { Texts.serverStopped.ToString(); } catch { Texts.serverStopped = "UNSET"; }
            try { Texts.serverReloaded.ToString(); } catch { Texts.serverReloaded = "UNSET"; }
            try { Texts.error.ToString(); } catch { Texts.error = "UNSET"; }
            try { Texts.dexName.ToString(); } catch { Texts.dexName = "UNSET"; }
            try { Texts.msg_selectUserFirst.ToString(); } catch { Texts.msg_selectUserFirst = "UNSET"; }
            try { Texts.msg_confirmation.ToString(); } catch { Texts.msg_confirmation = "UNSET"; }
            try { Texts.warn_heavyAction.ToString(); } catch { Texts.warn_heavyAction = "UNSET"; }
            try { Texts.warn_slowRequest.ToString(); } catch { Texts.warn_slowRequest = "UNSET"; }
            try { Texts.warn_userdontexist.ToString(); } catch { Texts.warn_userdontexist = "UNSET"; }
            try { Texts.err_request.ToString(); } catch { Texts.err_request = "UNSET"; }
            try { Texts.err_creationCodeUser.ToString(); } catch { Texts.err_creationCodeUser = "UNSET"; }
            try { Texts.noCreatureRegistered.ToString(); } catch { Texts.noCreatureRegistered = "UNSET"; }
            try { Texts.CreatureNotRegistered.ToString(); } catch { Texts.CreatureNotRegistered = "UNSET"; }
            try { Texts.noCreatureWithThatName.ToString(); } catch { Texts.noCreatureWithThatName = "UNSET"; }
            try { Texts.pokeStatsInfos.ToString(); } catch { Texts.pokeStatsInfos = "UNSET"; }

            if (needUpdate)
            {
                string ab = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                System.IO.File.WriteAllText("./_settings.json", ab);
            }
        }
    }

    public class OverlaySettings
    {
        public OverlayBar GlobalTotalCaughtGoal { get; set; }
        public OverlayBar GlobalShinyCaughtGoal { get; set; }
        public OverlayBar SessionTotalCaughtGoal { get; set; }
        public OverlayBar SessionShinyCaughtGoal { get; set; }
        public OverlayBar SessionParticipantsGoal { get; set; }
        public OverlayBar SessionMoneySpentGoal { get; set; }
        public OverlayBar GlobalMoneySpentGoal { get; set; }
    }

    public class OverlayBar
    {
        public bool Enabled { get; set; }
        public int GoalValue { get; set; }
    }

    public class BadgeSettings
    {
        // Required XP to level up
        public int XPRequiredToLevelUp { get; set; }

        public int LevelUpXPRequiredMultiplierPercent { get; set; }

        // XP rewarded for normal catch
        public int XPCatch { get; set; }

        // XP rewarded for shiny catch
        public int XPShinyCatch { get; set; }

        // XP rewarded for ball launched
        public int XPBallLaunched { get; set; }

        // XP Per day since first catch
        public int PerDayReward { get; set; }
    }

    public class ScheduledTask
    {
        /// <summary>
        /// required delay between last execution (or server start)
        /// </summary>
        public int Delay { get; set; }

        /// <summary>
        /// type of delay
        /// can be "seconds", "minutes" or "hours".
        /// </summary>
        public string DelayType { get; set; }

        /// <summary>
        /// path of bat, sh, etc file to execute
        /// </summary>
        public string ProcessFilePath { get; set; }

        /// <summary>
        /// also execute at starts
        /// </summary>
        public bool ExecuteAtStart { get; set; }

        /// <summary>
        /// last execution of the process
        /// This property is optional and can be null
        /// </summary>
        public DateTime LastExecution { get; set; }

        public ScheduledTask(int delay, string delayType, string processFilePath)
        {
            Delay = delay;
            DelayType = delayType;
            ProcessFilePath = processFilePath;
            LastExecution = DateTime.Now; // Initialiser avec DateTime.Now par défaut
        }

        public string Execute()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = ProcessFilePath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        LastExecution = DateTime.Now;
                        return $"{ProcessFilePath} executed successfully.\nOutput: {output}";
                    }
                    else
                    {
                        return $"Error while executing {ProcessFilePath}\nError: {error}";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Exception occurred while executing {ProcessFilePath}\n{ex.Message}";
            }
        }
    }

    public class GlobalAppLog
    {
        public GlobalAppLogConsole logConsole;
        public bool logFile;
    }

    public class GlobalAppLogConsole
    {
        public bool console;
        public bool logJsonOnConsole;
    }

    public class TextTranslation
    {
        public string serverStarted;
        public string serverStopped;
        public string serverReloaded;
        public string error;
        public string dexName;
        public string msg_selectUserFirst;
        public string msg_confirmation;
        public string warn_heavyAction;
        public string warn_slowRequest;
        public string warn_userdontexist;
        public string err_request;
        public string err_creationCodeUser;
        public string noCreatureRegistered;
        public string CreatureNotRegistered;
        public string noCreatureWithThatName;
        public string pokeStatsInfos;
        public TranslationScrapping TranslationScrapping;
        public TranslationBuying TranslationBuying;
        public TranslationTrading TranslationTrading;
        public TranslationRaid TranslationRaid;
        public TranslationEvolving TranslationEvolving;
        public TranslationGiveaway TranslationGiveaway;
        public Emotes emotes;
        public Types types;
    }

    public class TranslationScrapping
    {
        public string NotEnoughElementCopy;
        public string ElementNotRegistered;
        public string ScrapModeDoesNotExist;
        public string ScrapModeNotGiven;
    }

    public class TranslationBuying
    {
        public string ElementNonBuyable;
        public string ElementTooExpensive;
        public string ElementDoesNotExist;
        public string BuyingModeNotRecognized;
    }

    public class TranslationTrading
    {
        public string tradeRequestCreated;
        public string elementNotInPossession;
        public string tooExpensive;
        public string creatureNotFound;
        public string cancelled;
        public string cannotCancelNotOwner;
        public string codeInvalidOrExpired;
        public string cannotTradeShiny;
        public string cannotTradeLocked;
        public string cannotTradeLegendaries;
        public string cannotTradeShinyAndNormal;
        public string cannotTradeClassicAndCustom;
        public string cannotTradeFromDifferentSeries;
        public string atLeastOneTradeInitialized;
    }

    public class TranslationRaid
    {
        public string NoActiveRaid;
        public string DamageDone;
        public string CaptureState;
        public string RaidAlreadyGone;
        public string BossDeafeatedUseCmdToCatchIt;
    }

    public class TranslationEvolving
    {
        public string CreatureNotFound;
        public string EvolutionNotFound;
        public string CannotEvolveIfShiny;
        public string CannotEvolveIfEvolutionLocked;
        public string CannotEvolveIfEvolutionShinyLocked;
        public string CreatureBaseNotOwned;
        public string NotEnoughCreatureToEvolve;
        public string EvolutionSucceed;
    }

    public class TranslationGiveaway
    {
        public string AlreadyClaimed;
        public string CodeDoesNotExist;
        public string CodeExpired;
        public string CodeNotYetAvailable;
    }

    public class MessageSettings
    {
        public bool showShinyOnFail;
        public bool showDexCompletion;
    }

    public class Emotes
    {
        public string shiny;
        public string dex;
        public string money;
        public string ball;
        public string success;
        public string failure;
    }

    public class Types
    {
        public string fire;
        public string water;
        public string grass;
        public string electric;
        public string ground;
        public string rock;
        public string flying;
        public string bug;
        public string poison;
        public string psychic;
        public string ghost;
        public string ice;
        public string dragon;
        public string dark;
        public string steel;
        public string fairy;
        public string fighting;
        public string normal;
    }

    public class ScrapSettings
    {
        public int ValueDefaultNormal;
        public int ValueDefaultShiny;
        public int minimumToScrap;
        public int legendaryMultiplier;
    }

    public class CommandSettings
    {
        public string CmdScrap { get; set; } = "!scrap";
        public string CmdBuy { get; set; } = "!buy";
        public string CmdTradeRequest { get; set; } = "!trade";
        public string CmdTradeAccept { get; set; } = "!trade-accept";
        public string CmdTradeCancel { get; set; } = "!trade-cancel";
        public string CmdRaidCatch { get; set; } = "!capture";
    }

    public class TradeSettings
    {
        public bool PaidTrade { get; set; }
        public TradeSettingsPrices Prices { get; set; }
        public TradeConditions TradeConditions { get; set; }
    }

    public class TradeSettingsPrices
    {
        public int BasePrice { get; set; }
        public int PerShinyIncreasement { get; set; }
        public int PerRarityIncreasement { get; set; }
        public int PerCustomIncreasement { get; set; }
        public int PerLegendaryIncreasement { get; set; }
    }

    public class TradeConditions
    {
        public bool EnableShinyInTrade { get; set; }
        public bool EnableLockedPokemonInTrade { get; set; }
        public bool EnableLegendariesInTrade { get; set; }
        public bool EnableShinyAgainstNormal { get; set; }
        public bool EnableTradeBetweenClassicAndCustom { get; set; }
        public bool EnableTradeBetweenDifferentSeries { get; set; }
    }

    public class RaidSettings
    {
        public int DefaultPV { get; set; }
        public int DefaultCatchRate { get; set; }
        public int DefaultShinyRate { get; set; }
        public int TimeMinuteToCatchAfterDefeat { get; set; }
    }

    public class EvolveSettings
    {
        public int RequiredCreatureToEvolve;
        public bool AllowShiny;
        public bool AllowEvolutionLocked;
        public bool AllowEvolutionShinyLocked;
    }

    public class GiveAwaySettings
    {
    }
}
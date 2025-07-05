using System;
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
        public GlobalAppLogConsole logConsole { get; set; }
        public bool logFile { get; set; }
    }

    public class GlobalAppLogConsole
    {
        public bool console { get; set; }
        public bool logJsonOnConsole { get; set; }
    }

    public class TextTranslation
    {
        public string serverStarted { get; set; }
        public string serverStopped { get; set; }
        public string serverReloaded { get; set; }
        public string error { get; set; }
        public string dexName { get; set; }
        public string msg_selectUserFirst { get; set; }
        public string msg_confirmation { get; set; }
        public string warn_heavyAction { get; set; }
        public string warn_slowRequest { get; set; }
        public string warn_userdontexist { get; set; }
        public string err_request { get; set; }
        public string err_creationCodeUser { get; set; }
        public string noCreatureRegistered { get; set; }
        public string CreatureNotRegistered { get; set; }
        public string noCreatureWithThatName { get; set; }
        public string pokeStatsInfos { get; set; }
        public TranslationScrapping TranslationScrapping { get; set; }
        public TranslationBuying TranslationBuying { get; set; }
        public TranslationTrading TranslationTrading { get; set; }
        public TranslationRaid TranslationRaid { get; set; }
        public TranslationEvolving TranslationEvolving { get; set; }
        public TranslationGiveaway TranslationGiveaway { get; set; }
        public Emotes emotes { get; set; }
        public Types types { get; set; }
    }

    public class TranslationScrapping
    {
        public string NotEnoughElementCopy { get; set; }
        public string ElementNotRegistered { get; set; }
        public string ScrapModeDoesNotExist { get; set; }
        public string ScrapModeNotGiven { get; set; }
    }

    public class TranslationBuying
    {
        public string ElementNonBuyable { get; set; }
        public string ElementTooExpensive { get; set; }
        public string ElementDoesNotExist { get; set; }
        public string BuyingModeNotRecognized { get; set; }
    }

    public class TranslationTrading
    {
        public string tradeRequestCreated { get; set; }
        public string elementNotInPossession { get; set; }
        public string tooExpensive { get; set; }
        public string creatureNotFound { get; set; }
        public string cancelled { get; set; }
        public string cannotCancelNotOwner { get; set; }
        public string codeInvalidOrExpired { get; set; }
        public string cannotTradeShiny { get; set; }
        public string cannotTradeLocked { get; set; }
        public string cannotTradeLegendaries { get; set; }
        public string cannotTradeShinyAndNormal { get; set; }
        public string cannotTradeClassicAndCustom { get; set; }
        public string cannotTradeFromDifferentSeries { get; set; }
        public string atLeastOneTradeInitialized { get; set; }
    }

    public class TranslationRaid
    {
        public string NoActiveRaid { get; set; }
        public string DamageDone { get; set; }
        public string CaptureState { get; set; }
        public string RaidAlreadyGone { get; set; }
        public string BossDeafeatedUseCmdToCatchIt { get; set; }
    }

    public class TranslationEvolving
    {
        public string CreatureNotFound { get; set; }
        public string EvolutionNotFound { get; set; }
        public string CannotEvolveIfShiny { get; set; }
        public string CannotEvolveIfEvolutionLocked { get; set; }
        public string CannotEvolveIfEvolutionShinyLocked { get; set; }
        public string CreatureBaseNotOwned { get; set; }
        public string NotEnoughCreatureToEvolve { get; set; }
        public string EvolutionSucceed { get; set; }
    }

    public class TranslationGiveaway
    {
        public string AlreadyClaimed { get; set; }
        public string CodeDoesNotExist { get; set; }
        public string CodeExpired { get; set; }
        public string CodeNotYetAvailable { get; set; }
    }

    public class MessageSettings
    {
        public bool showShinyOnFail { get; set; }
        public bool showDexCompletion { get; set; }
    }

    public class Emotes
    {
        public string shiny { get; set; }
        public string dex { get; set; }
        public string money { get; set; }
        public string ball { get; set; }
        public string success { get; set; }
        public string failure { get; set; }
    }

    public class Types
    {
        public string fire { get; set; }
        public string water { get; set; }
        public string grass { get; set; }
        public string electric { get; set; }
        public string ground { get; set; }
        public string rock { get; set; }
        public string flying { get; set; }
        public string bug { get; set; }
        public string poison { get; set; }
        public string psychic { get; set; }
        public string ghost { get; set; }
        public string ice { get; set; }
        public string dragon { get; set; }
        public string dark { get; set; }
        public string steel { get; set; }
        public string fairy { get; set; }
        public string fighting { get; set; }
        public string normal { get; set; }
    }

    public class ScrapSettings
    {
        public int ValueDefaultNormal { get; set; }
        public int ValueDefaultShiny { get; set; }
        public int minimumToScrap { get; set; }
        public int legendaryMultiplier { get; set; }
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
        public int TimeMinuteToCatchAfterDefeat { get; set; } = 50;
    }

    public class EvolveSettings
    {
        public int RequiredCreatureToEvolve { get; set; }
        public bool AllowShiny { get; set; }
        public bool AllowEvolutionLocked { get; set; }
        public bool AllowEvolutionShinyLocked { get; set; }
    }

    public class GiveAwaySettings
    {
    }
}
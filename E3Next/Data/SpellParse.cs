using E3Core.Processors;
using E3Core.Utility;
using IniParser.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.AxHost;

namespace E3Core.Data
{
    public partial class Spell
    {
        public void Parse(IniData parsedData) {
            if (SpellName.Contains("/")) {
                string[] splitList = SpellName.Split('/');
                SpellName = splitList[0];
                CastName = SpellName;

                foreach (var value in splitList.Skip(1))  // Using LINQ to skip the first value
                {
                    SpellOption option;
                    string argument = GetArgumentFromString(value);
                    if (Enum.TryParse(GetOptionFromString(value), true, out option)) {
                        switch (option) {
                            case SpellOption.Gem:
                                SpellGem = GetArgument<Int32>(argument);
                                break;
                            case SpellOption.NoInterrupt:
                                NoInterrupt = true;
                                break;
                            case SpellOption.Debug:
                                Debug = true;
                                break;
                            case SpellOption.AfterSpell:
                                AfterSpell = GetArgument<String>(argument);
                                break;
                            case SpellOption.StackRequestTargets:
                                string targetString = GetArgument<String>(argument);
                                string[] targets = targetString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var target in targets) {
                                    StackRequestTargets.Add(e3util.FirstCharToUpper(target.Trim()));
                                }
                                break;
                            case SpellOption.StackCheckInterval:
                                StackIntervalCheck = GetArgument<Int64>(argument) * 1000;
                                break;
                            case SpellOption.StackRecastDelay:
                                StackRecastDelay = GetArgument<Int64>(argument) * 1000;
                                break;
                            case SpellOption.AfterCast:
                                AfterSpell = GetArgument<String>(argument);
                                break;
                            case SpellOption.BeforeSpell:
                                BeforeSpell = GetArgument<String>(argument);
                                break;
                            case SpellOption.MinDurationBeforeRecast:
                                MinDurationBeforeRecast = GetArgument<Int64>(argument) * 1000;
                                break;
                            case SpellOption.GiveUpTimer:
                                GiveUpTimer = GetArgument<Int32>(argument);
                                break;
                            case SpellOption.MaxTries:
                                MaxTries = GetArgument<Int32>(argument);
                                break;
                            case SpellOption.CheckFor:
                                CheckFor = GetArgument<String>(argument);
                                break;
                            case SpellOption.CastIf:
                                CastIF = GetArgument<String>(argument);
                                break;
                            case SpellOption.MinMana:
                                MinMana = GetArgument<Int32>(argument);
                                break;
                            case SpellOption.MaxMana:
                                MaxMana = GetArgument<Int32>(argument);
                                break;
                            case SpellOption.MinHP:
                                MinHP = GetArgument<Int32>(argument);
                                break;
                            case SpellOption.HealPct:
                                HealPct = GetArgument<Int32>(argument);
                                break;
                            case SpellOption.Reagent:
                                Reagent = GetArgument<String>(argument);
                                break;
                            case SpellOption.NoBurn:
                                NoBurn = true;
                                break;
                            case SpellOption.NoAggro:
                                NoAggro = true;
                                break;
                            case SpellOption.Rotate:
                                Rotate = true;
                                break;
                            case SpellOption.NoMidSongCast:
                                NoMidSongCast = true;
                                break;
                            case SpellOption.Delay:
                                string tvalue = value;
                                bool isMinute = false;
                                if (value.EndsWith("s", StringComparison.OrdinalIgnoreCase)) {
                                    tvalue = tvalue.Substring(0, value.Length - 1);
                                }
                                else if (value.EndsWith("m", StringComparison.OrdinalIgnoreCase)) {
                                    isMinute = true;
                                    tvalue = tvalue.Substring(0, value.Length - 1);
                                }
                                Delay = GetArgument<Int32>(tvalue);
                                if (isMinute) {
                                    Delay *= 60;
                                }
                                break;

                            case SpellOption.DelayAfterCast:
                                DelayAfterCast = GetArgument<Int32>(value);
                                break;

                            case SpellOption.GoM:
                                GiftOfMana = true;
                                break;

                            case SpellOption.PctAggro:
                                PctAggro = GetArgument<Int32>(value);
                                break;

                            case SpellOption.Zone:
                                Zone = GetArgument<String>(value);
                                break;

                            case SpellOption.MinSick:
                                MinSick = GetArgument<Int32>(value);
                                break;

                            case SpellOption.MinEnd:
                                MinEnd = GetArgument<Int32>(value);
                                break;

                            case SpellOption.AllowSpellSwap:
                                AllowSpellSwap = true;
                                break;

                            case SpellOption.NoEarlyRecast:
                                NoEarlyRecast = true;
                                break;

                            case SpellOption.NoStack:
                                NoStack = true;
                                break;

                            case SpellOption.TriggerSpell:
                                TriggerSpell = GetArgument<String>(value);
                                break;
                            case SpellOption.Ifs:
                                string ifKey = GetArgument<string>(value);
                                var section = parsedData.Sections["Ifs"];
                                if (section != null) {
                                    var keys = ifKey.Split(',');
                                    foreach (var key in keys) {
                                        var keyData = section[key];
                                        if (!String.IsNullOrWhiteSpace(keyData)) {
                                            Ifs = string.IsNullOrWhiteSpace(Ifs) ? keyData : Ifs + " && " + keyData;
                                        }
                                    }
                                }
                                break;

                            case SpellOption.AfterEvent:
                                ifKey = GetArgument<string>(value);
                                section = parsedData.Sections["Events"];
                                if (section != null) {
                                    var keyData = section[ifKey];
                                    if (!String.IsNullOrWhiteSpace(keyData)) {
                                        AfterEvent = keyData;
                                    }
                                }
                                break;

                            case SpellOption.BeforeEvent:
                                ifKey = GetArgument<string>(value);
                                section = parsedData.Sections["Events"];
                                if (section != null) {
                                    var keyData = section[ifKey];
                                    if (!String.IsNullOrWhiteSpace(keyData)) {
                                        BeforeEvent = keyData;
                                    }
                                }
                                break;

                            default:
                                if (String.IsNullOrWhiteSpace(CastTarget) && !value.Contains("|")) {
                                    CastTarget = e3util.FirstCharToUpper(value);
                                }
                                break;


                        }

                    }
                    else {
                        if (String.IsNullOrWhiteSpace(CastTarget) && !value.Contains("|")) {
                            CastTarget = e3util.FirstCharToUpper(value);
                        }
                    }
                }
            }
        }

        private string GetArgumentFromString(string input) {
            Int32 indexOfPipe = input.IndexOf('|');
            return indexOfPipe >= 0 ? input.Substring(indexOfPipe + 1) : string.Empty;
        }

        private string GetOptionFromString(string input) {
            Int32 indexOfPipe = input.IndexOf('|');
            return indexOfPipe >= 0 ? input.Substring(0, indexOfPipe) : input;
        }

        public enum SpellOption
        {
            Gem,
            NoInterrupt,
            Debug,
            AfterSpell,
            StackRequestTargets,
            StackCheckInterval,
            StackRecastDelay,
            AfterCast,
            BeforeSpell,
            MinDurationBeforeRecast,
            GiveUpTimer,
            MaxTries,
            CheckFor,
            CastIf,
            MinMana,
            MaxMana,
            MinHP,
            HealPct,
            Reagent,
            NoBurn,
            NoAggro,
            Rotate,
            NoMidSongCast,
            Delay,
            DelayAfterCast,
            GoM,
            PctAggro,
            Zone,
            MinSick,
            MinEnd,
            AllowSpellSwap,
            NoEarlyRecast,
            NoStack,
            TriggerSpell,
            Ifs,
            AfterEvent,
            BeforeEvent
        }
        public static Dictionary<SpellOption, string> SpellOptionHelpMessages = new Dictionary<SpellOption, string>
        {
            { SpellOption.Gem, "Specify gem slot for the spell." },
            { SpellOption.NoInterrupt, "Spell cannot be interrupted." },
            { SpellOption.Debug, "Enable debug mode." },
            { SpellOption.AfterSpell, "Specify spell to cast after this." },
            { SpellOption.StackRequestTargets, "List of targets to stack request." },
            { SpellOption.StackCheckInterval, "Interval to check for stacking." },
            { SpellOption.StackRecastDelay, "Delay before recasting the stack." },
            { SpellOption.AfterCast, "Action after the cast." },
            { SpellOption.BeforeSpell, "Specify spell to cast before this." },
            { SpellOption.MinDurationBeforeRecast, "Minimum time before spell recast." },
            { SpellOption.GiveUpTimer, "Time before giving up on spell cast." },
            { SpellOption.MaxTries, "Maximum tries to cast the spell." },
            { SpellOption.CheckFor, "Conditions to check before casting." },
            { SpellOption.CastIf, "Conditions to cast the spell." },
            { SpellOption.MinMana, "Minimum mana required to cast." },
            { SpellOption.MaxMana, "Maximum mana to consider before casting." },
            { SpellOption.MinHP, "Minimum HP required for cast." },
            { SpellOption.HealPct, "Percent HP to start healing." },
            { SpellOption.Reagent, "Reagent required for the spell." },
            { SpellOption.NoBurn, "Don't use when burning." },
            { SpellOption.NoAggro, "Spell doesn't generate aggro." },
            { SpellOption.Rotate, "Rotate with other spells." },
            { SpellOption.NoMidSongCast, "Don't cast in the middle of a song." },
            { SpellOption.Delay, "Delay before casting." },
            { SpellOption.DelayAfterCast, "Delay after casting." },
            { SpellOption.GoM, "Use Gift of Mana." },
            { SpellOption.PctAggro, "Percent aggro to consider." },
            { SpellOption.Zone, "Specific zone for spell." },
            { SpellOption.MinSick, "Minimum sickness duration to cast." },
            { SpellOption.MinEnd, "Minimum endurance required." },
            { SpellOption.AllowSpellSwap, "Allow swapping of spells." },
            { SpellOption.NoEarlyRecast, "Disallow early recasting." },
            { SpellOption.NoStack, "Don't stack with other spells." },
            { SpellOption.TriggerSpell, "Trigger another spell on cast." },
            { SpellOption.Ifs, "Conditions list to check." },
            { SpellOption.AfterEvent, "Event to trigger after casting." },
            { SpellOption.BeforeEvent, "Event to trigger before casting." }
        };

        public static void DisplaySpellOptions() {
            Console.WriteLine("Available Spell Options and their Descriptions:\n");

            var sortedSpellOptions = Enum.GetValues(typeof(SpellOption))
                                         .Cast<SpellOption>()
                                         .OrderBy(option => option.ToString())
                                         .ToList();

            foreach (SpellOption option in sortedSpellOptions) {
                if (SpellOptionHelpMessages.ContainsKey(option)) {
                    var message = SpellOptionHelpMessages[option];
                    MQ.Write($"\ay {option} \aw- \ag{message}");
                }
                else {
                    MQ.Write($"\ay {option}\aw - \agNo description available.");
                }
            }

            MQ.Write("\aw End of Spell Options list.");
        }



        public static T GetArgument<T>(string query) {
            Int32 indexOfPipe = query.IndexOf('|') + 1;
            string input = query.Substring(indexOfPipe, query.Length - indexOfPipe);

            if (typeof(T) == typeof(Int32)) {

                Int32 value;
                if (Int32.TryParse(input, out value)) {
                    return (T)(object)value;
                }

            }
            else if (typeof(T) == typeof(Boolean)) {
                Boolean booleanValue;
                if (Boolean.TryParse(input, out booleanValue)) {
                    return (T)(object)booleanValue;
                }
                if (input == "NULL") {
                    return (T)(object)false;
                }
                Int32 intValue;
                if (Int32.TryParse(input, out intValue)) {
                    if (intValue > 0) {
                        return (T)(object)true;
                    }
                    return (T)(object)false;
                }
                if (string.IsNullOrWhiteSpace(input)) {
                    return (T)(object)false;
                }

                return (T)(object)true;
            }
            else if (typeof(T) == typeof(string)) {
                return (T)(object)input;
            }
            else if (typeof(T) == typeof(decimal)) {
                Decimal value;
                if (Decimal.TryParse(input, out value)) {
                    return (T)(object)value;
                }
            }
            else if (typeof(T) == typeof(Int64)) {
                Int64 value;
                if (Int64.TryParse(input, out value)) {
                    return (T)(object)value;
                }
            }
            return default(T);
        }
    }
}

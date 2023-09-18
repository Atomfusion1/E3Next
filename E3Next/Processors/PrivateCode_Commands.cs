using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoCore;
using E3Core.Data;
using E3Core.Settings;
using E3Core.Utility;
using IniParser.Model;
using System.Security.Cryptography;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace E3Core.Processors
{
    public static class PrivateCommands
    {
        [DllImport("kernel32.dll")]
        public static extern bool Beep(uint dwFreq, uint dwDuration);

        [DllImport("user32.dll")]
        public static extern bool MessageBeep(uint uType);

        static void PlayTone(uint freq, uint lengthms) {
            // Play a tone using Beep
            Beep(freq, lengthms);  // 440 Hz for 1 second
        }

        public static void StartupCommands()
        {
            LoadCommandStartup();
            RegisterCommandCampOut();
            RegisterCommandMoveToTarget();
            RegisterCommandPrintINI();
            RegisterCommandE3NextHelp();
        }
        private static IMQ MQ = E3.MQ;
        private static void LoadCommandStartup() // Load in Variables from INI file then run command 
        {
            List<string> CommandOnStartup = new List<string>();
            // Specialized Commands 
            BaseSettings.LoadKeyData("Misc", "Command On Startup", E3.CharacterSettings.ParsedData, CommandOnStartup);
            // Check if not 0
            if (CommandOnStartup.Count() > 0)
            {
                foreach (var command in CommandOnStartup)
                {
                    MQ.Write("\ay Startup CMD: " + command);
                    MQ.Cmd($"/squelch {command}");
                }
            }
            else
            {
                MQ.Write("\ar No, Command On Startup  in Save file");
            }
        }
        public static void ListRegisteredCommands() {
            MQ.Write("Registered Commands:");
            var sortedCommands = EventProcessor.CommandList.Keys.OrderBy(key => key).ToList();
            const int spacing = 10;

            foreach (var command in sortedCommands) {
                if (CommandDictionary.CommandHelpMessages.ContainsKey(command)) {
                    var description = CommandDictionary.CommandHelpMessages[command];
                    MQ.Write($"\ay{command.PadRight(spacing)} \aw- \ag{description}");
                }
                else {
                    MQ.Write($"\ay{command.PadRight(spacing)} \aw- \arNo description available.");
                }
            }
            MQ.Write("End of registered commands list.");
        }


        private static DateTime GetBuildDate()
        {
            return File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location);
        }

        private static void RegisterCommandE3NextHelp() {
            EventProcessor.RegisterCommand("/e3next", (x) =>
            {
                string commands = null;
                if (x.args.Count > 0) commands = x.args[0];
                uint[] frequencies =    { 659, 622, 659, 622, 659, 494, 587, 523, 440};
                uint[] durations =      { 225, 225, 225, 225, 225, 225, 225, 225, 625};

                // Check if commands is null, empty, or equals "Help" ignoring case
                if (String.IsNullOrWhiteSpace(commands) || commands.Equals("Help", StringComparison.OrdinalIgnoreCase)) {
                    var buildDate = GetBuildDate();
                    MQ.Write("\a-g E3Next: \awVersion \ay" + Setup._e3Version + " \awBuilt: \ay" + buildDate);
                    MQ.Write("\ay /e3next Help");
                    MQ.Write("\ar /e3next GoneMad");
                    MQ.Write("\ay /e3next Spells");
                    MQ.Write("\ay /e3next Commands");
                    return;
                }
                if (commands.Equals("GoneMad", StringComparison.OrdinalIgnoreCase)) {
                    for (int i = 0; i < frequencies.Length; i++) {
                        PlayTone(frequencies[i], durations[i]);
                    }
                }
                if (commands.Equals("Spells", StringComparison.OrdinalIgnoreCase)) {
                    if (x.args.Count > 1) {
                        Spell.DisplaySpellOptions(x.args[1]);  // pass the next argument as the filter
                    }
                    else {
                        Spell.DisplaySpellOptions();
                    }
                    return;
                }
                if (commands.Equals("Commands", StringComparison.OrdinalIgnoreCase)) {
                    MQ.Write("\ay List of Commands");
                    ListRegisteredCommands();
                    return;
                }
            });
        }


        private static void RegisterCommandCampOut()
        {
            EventProcessor.RegisterCommand("/campout", (x) =>
            {
                MQ.Cmd("/dismount");
                MQ.Cmd("/sit");
                MQ.Cmd("/camp desktop");
                MQ.Delay(40000);
            });
        }
        // OverRideEverything and Move To Me 
        private static void RegisterCommandMoveToTarget() // Crashing at the moment index out of range must be non negative and less then collection 
        {
            EventProcessor.RegisterCommand("/movetotarget", (x) =>
            {
                string spawn = x.args[0];
                MQ.Write(E3.GeneralSettings.Movement_NavStopDistance.ToString());
                MQ.Write(MQ.Query<double>($@"${{Spawn[{spawn} PC].Distance}}").ToString());
                if (spawn == MQ.Query<string>("${Me.CleanName}")) return;
                // max distance and timeout needed 
                ElapsedTimer TimeOutTimer = new ElapsedTimer();
                TimeOutTimer.SetTime(0, 0, 20, 0);
                while (MQ.Query<double>($@"${{Spawn[{spawn} PC].Distance}}") > 10 && MQ.Query<double>($@"${{Spawn[{spawn} PC].Distance}}") < 400 && !TimeOutTimer.IsElapsed())
                {
                    MQ.Cmd($"/squelch   {spawn} fast nolook");
                    MQ.Cmd("/keypress forward hold");
                }
                MQ.Cmd("/keypress forward");
            });
        }
        private static void RegisterCommandPrintINI()
        {
            EventProcessor.RegisterCommand("/printini", (x) =>
            {
                // Print Character InI file 
                CharacterSettings settings = new CharacterSettings();
                IniData newFile = settings.ParsedData;
                List<string> sections = newFile.Sections.Select(s => s.SectionName).ToList();

                foreach (string elements in sections)
                {
                    var KeyData = newFile.Sections[elements];
                    MQ.Write("\ag" + elements + ":");
                    foreach (var key in KeyData)
                    {
                        MQ.Write("---" + key.KeyName + " = " + key.Value);
                    }
                }
            });
        }
    }
}

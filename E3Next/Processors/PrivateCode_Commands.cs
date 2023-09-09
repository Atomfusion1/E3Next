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

namespace E3Core.Processors
{
    public static class PrivateCommands
    {
        public static void StartupCommands()
        {
            LoadCommandStartup();
            RegisterCommandCampOut();
            RegisterCommandMoveToTarget();
            RegisterCommandPrintINI();
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

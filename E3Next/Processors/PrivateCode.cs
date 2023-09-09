using E3Core.Data;
using E3Core.Settings;
using E3Core.Utility;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoCore;

namespace E3Core.Processors
{
    public static class PrivateCode
    {
        private static IMQ MQ = E3.MQ;

        // Timers 
        static private DateTime? combatStartTime = null;
        static private int? initialHitpoints = null;
        static private double rateOfDeath = 0;

        // Public Values 
        public static bool LazyMode;
        public static bool PullingMode;
        public static int PCNear;
        public static string GroupCampMember = null;
        public static ElapsedTimer CampOutTimer;

        public static bool IWantToFollow = false;
        public static int followDistance = 40;
        public static string followSpawn = null;

        public static class MobInformation
        {
            internal static bool IgnoreMobs;
            internal static int MobLevelOverMe;
            internal static int PullDistance;
            internal static int PullSize;
            // Load multi Key String Variables
            internal static List<string> MobNameArray = new List<string>();
            // Debug Code Print Values
            internal static void PrintVars()
            {
                MQ.Write($"IgnoreMobs: {IgnoreMobs}");
                MQ.Write($"MobLevelOverMe: {MobLevelOverMe}");
                MQ.Write($"PullDistance: {PullDistance}");
                MQ.Write($"PullSize: {PullSize}");
                int i = 0;
                MQ.Write("\ayMobNameArray");
                foreach (var CommandToLoadOnEQStartup in MobInformation.MobNameArray)
                {
                    MQ.Write($"{i} = {CommandToLoadOnEQStartup}"); //Send Data to MQ Echo
                    i++;
                }
            }
        }

        //load on startup once
        [SubSystemInit]
        public static void Init()
        {
            Casting.VarsetValues.Add("TimeTillDeath", "10"); // Add TimeTillDeath and use in Ifs
            MQ.Write("\agPrivate Code Version 1.2.0 Loaded ");
            MQ.Cmd("/nav setopt dist=30 log=off");
            // Initialize Variables One Time
            CampOutTimer = new ElapsedTimer();
            LazyMode = false;
            PullingMode = false;
            E3.GeneralSettings.Movement_NavStopDistance = 20; // Override Nav Stop Distance
            PrivateFollow.Startup();
            SuperMode.Startup();
            PrivateCommands.StartupCommands();
            PrivateEvents.StartupEvents();
        }

        //run on all characters Continually about 1/10s 
        //***********************************************
        //************* MAIN ****************************
        [ClassInvoke(Class.All)]
        public static void Main()
        {
            SuperMode.Update();
            MobsRateOfDeath();
            PrivateFollow.Update();
            Private_BackOffPet();
        }
        //***********************************************
        //************* MAIN ****************************

        private static void Private_BackOffPet()
        {
            if (!Assist.IsAssisting)
            {
                if (MQ.Query<bool>(@"${Bool[${Me.Pet.Target}]}"))
                {
                    MQ.Write("Calling off Pet");
                    MQ.Cmd("/pet stop");
                }
            }
        }

        private static void MobsRateOfDeath()
        {
            if (MQ.Query<bool>(@"${Me.Combat}") && MQ.Query<bool>(@"${Bool[${Target}]}") || MQ.Query<int>(@"${Me.XTarget}") > 0 && MQ.Query<bool>(@"${Bool[${Target}]}"))
            {
                if (combatStartTime == null)
                {
                    // If combat has just started, save the current time and capture the mob's hitpoints.
                    combatStartTime = DateTime.UtcNow;
                    initialHitpoints = MQ.Query<int>(@"${Target.PctHPs}"); // Assuming this is how you get a mob's hitpoints.
                }
                double secondsInCombat = (DateTime.UtcNow - combatStartTime.Value).TotalSeconds;
                if (initialHitpoints.HasValue && secondsInCombat > 0)
                {
                    int currentHitpoints = MQ.Query<int>(@"${Target.PctHPs}");
                    int hitpointsLost = initialHitpoints.Value - currentHitpoints;
                    // Calculate rate of death, which is the percentage of hitpoints lost per second.
                    rateOfDeath = (hitpointsLost / (double)initialHitpoints.Value) * 100 / secondsInCombat;
                    //runningAverageMRD = (runningAverageMRD * .9) + (rateOfDeath * .1);
                    if (rateOfDeath < 0)
                        rateOfDeath = 0;
                    else if (rateOfDeath > 100)
                        rateOfDeath = 100;
                    // Calculate Time Till Death
                    double timeTillDeath = currentHitpoints / rateOfDeath;
                    Casting.VarsetValues["TimeTillDeath"] = timeTillDeath.ToString();
                    //MQ.Write($"Time in Combat: {secondsInCombat.ToString("F2")} seconds, Rate of Death: {rateOfDeath.ToString("F2")}% per second, Running Average: {runningAverageMRD.ToString("F2")}% per second, Estimated Time Till Death: {timeTillDeath.ToString("F2")} seconds.");
                    //MQ.Write(Casting.VarsetValues["TimeTillDeath"].ToString());
                }
            }
            else
            {
                // If not in combat, reset the combat start time and initial hitpoints.
                combatStartTime = null;
                initialHitpoints = null;
                Casting.VarsetValues["TimeTillDeath"] = "0";
            }
        }

        // Read In INI Data for zone 
        public static void ReadINI(string FileName, string SectionName)
        {
            bool Debug = false;
            IniParser.FileIniDataParser fileIniData = e3util.CreateIniParser();
            // Start List here on reload to clear it
            // Must pass ReadFile a complete Filename 
            string filename = BaseSettings.GetSettingsFilePath($"{FileName}");
            if (Debug) MQ.Write("Read File: " + FileName);
            if (Debug) MQ.Write(filename);
            if (Debug) MQ.Write("Section Name " + SectionName);
            if (System.IO.File.Exists(filename))
            {
                IniData newFile = fileIniData.ReadFile(filename);
                List<string> sections = newFile.Sections.Select(s => s.SectionName).ToList();
                if (Debug)
                {
                    var KeyData = newFile.Sections[SectionName];
                    // MQ.Write("\ag"+elements+":");
                    foreach (var key in KeyData)
                    {
                        MQ.Write("---" + key.KeyName + " = " + key.Value);
                    }
                }
                if (sections.Contains(SectionName))
                {
                    BaseSettings.LoadKeyData($"{SectionName}", "IgnoreList", newFile, ref MobInformation.IgnoreMobs);
                    BaseSettings.LoadKeyData($"{SectionName}", "MobLevelOverMe", newFile, ref MobInformation.MobLevelOverMe);
                    BaseSettings.LoadKeyData($"{SectionName}", "PullRange", newFile, ref MobInformation.PullDistance);
                    BaseSettings.LoadKeyData($"{SectionName}", "PullSize", newFile, ref MobInformation.PullSize);
                    BaseSettings.LoadKeyData($"{SectionName}", "Mob", newFile, MobInformation.MobNameArray);
                }
                else
                {
                    WriteINI(FileName, SectionName);
                }
            }
            else
            {
                MQ.Write("File Not Found");
            }
        }


        // Not used yet will need to write INI 
        private static void WriteINI(string FileName, string SectionName)
        {
            bool Debug = true;
            IniParser.FileIniDataParser fileIniData = e3util.CreateIniParser();
            string filename = BaseSettings.GetSettingsFilePath($"{FileName}");
            if (Debug) MQ.Write("Write File: " + FileName);
            if (Debug) MQ.Write(filename);
            if (Debug) MQ.Write(SectionName);

            IniParser.FileIniDataParser parser = e3util.CreateIniParser();
            IniData newFile = fileIniData.ReadFile(filename);

            //Section Name
            newFile.Sections.AddSection(SectionName);
            var section = newFile.Sections.GetSectionData(SectionName);
            section.Keys.AddKey("IgnoreList", "TRUE");
            section.Keys.AddKey("MobLevelOverMe", "0");
            section.Keys.AddKey("PullRange", "400");
            section.Keys.AddKey("PullSize", "1");
            section.Keys.AddKey("Mob", "Ignore Mob Name");
            parser.WriteFile(filename, newFile);
        }
    }
}

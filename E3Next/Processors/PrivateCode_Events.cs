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
    public static class PrivateEvents
    {
        private static IMQ MQ = E3.MQ;
        private static double TotalExp = 0;
        private static double TotalAAExp = 0;
        private static DateTime StartTime = DateTime.Now;

        public static void StartupEvents()
        {
            RegisterEventsEXPGain();
            RegisterEventsMobUnslowable();
            RegisterEventsGetCloser();
            RegisterEventsFaceTarget();
        }

        private static void RegisterEventsEXPGain()
        {
            List<String> r = new List<string>();
            r.Add(@"^You have gained (raid|group|party) experience! \(((\d+\.\d+%)|(\d+\.\d+%AA))(, (\d+\.\d+%))?((, )?(\d+\.\d+%AA))?\)");

            EventProcessor.RegisterEvent("MQ_EXP_RATE", r, (x) =>
            {
                double expGained = 0;
                double aaExpGained = 0;

                if (x.match.Groups[3].Success) // Regular XP
                {
                    expGained = double.Parse(x.match.Groups[3].Value.Replace("%", ""));
                    TotalExp += expGained;
                }

                if (x.match.Groups[4].Success) // AA XP (first appearance)
                {
                    aaExpGained = double.Parse(x.match.Groups[4].Value.Replace("%AA", ""));
                    TotalAAExp += aaExpGained;
                }

                if (x.match.Groups[6].Success) // Regular XP (second appearance)
                {
                    expGained = double.Parse(x.match.Groups[6].Value.Replace("%", ""));
                    TotalExp += expGained;
                }

                if (x.match.Groups[9].Success) // AA XP (second appearance)
                {
                    aaExpGained = double.Parse(x.match.Groups[9].Value.Replace("%AA", ""));
                    TotalAAExp += aaExpGained;
                }

                double runTimeHours = (DateTime.Now - StartTime).TotalHours;

                if (runTimeHours > 0)
                {
                    double expPerHour = TotalExp / runTimeHours;
                    double aaExpPerHour = TotalAAExp / runTimeHours;
                    double runTimeMinutes = (DateTime.Now - StartTime).TotalMinutes;

                    if (expGained > 0)
                    {
                        MQ.Write($"\agXP Gained {expGained:0.##} Session {TotalExp:0.##} XP Per Hour {expPerHour:0.##} Run Time: {runTimeMinutes:0.##}m");
                    }

                    if (aaExpGained > 0)
                    {
                        MQ.Write($"\a-gAA XP Gained {aaExpGained:0.##} Session {TotalAAExp:0.##} AA XP Per Hour {aaExpPerHour:0.##} Run Time: {runTimeMinutes:0.##}m");
                    }
                }
            });
        }

        // Setup Commands 
        private static void RegisterEventsMobUnslowable()
        {
            List<String> r = new List<string>();
            r.Add("^Your target is immune to changes in its attack speed.$");

            EventProcessor.RegisterEvent("attack_speed", r, (x) =>
            {
                MQ.Cmd("/bc IMMUNE TO SLOW ");
            });
        }

        //Your target is to far away, get closer!
        //You cannot see your target.
        private static void RegisterEventsFaceTarget()
        {
            List<String> r = new List<string>();
            r.Add("^You cannot see your target.$");

            EventProcessor.RegisterEvent("FaceTarget", r, (x) =>
            {
                if (PrivateCode.PullingMode)
                {
                    MQ.Cmd("/squelch /face");
                }
            });
        }

        private static void RegisterEventsGetCloser()
        {
            List<String> r = new List<string>();
            r.Add("^Your target is too far away, get closer!$");

            EventProcessor.RegisterEvent("ToFarAway", r, (x) =>
            {
                if (PrivateCode.PullingMode)
                {
                    MQ.Cmd("/squelch /nav target  dist=10 log=off");
                }
            });
        }
    }
}

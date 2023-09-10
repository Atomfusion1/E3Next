using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoCore;
using E3Core.Processors;

namespace E3Core.Processors
{
    public static class SuperMode
    {
        private static IMQ MQ = E3.MQ;
        static ElapsedTimer PullingTimer =  new ElapsedTimer();
        static ElapsedTimer ChatTimer = new ElapsedTimer();
        static ElapsedTimer myTimer = new ElapsedTimer();
        static ElapsedTimer CampUpdateTimer = new ElapsedTimer();
        static int EngagedID;
        static int ZoneID;
        // Call out target if its close 
        public static void Startup()
        {
            RegisterCommandLazyMode();
            RegisterCommandPullingMode();
        }

        public static void Update()
        {
            if (PrivateCode.LazyMode)
            {
                Private_CallTarget();
            }
            else if (PrivateCode.PullingMode)
            {
                Private_CallTarget();
                Private_PullingTarget();
            }
        }

        private static void Private_CallTarget()
        {
            int targetID = E3.MQ.Query<Int32>($"${{Me.XTarget[1].ID}}");
            if (E3.MQ.Query<decimal>($"${{Spawn[{PrivateCode.GroupCampMember} pc].Distance3D}}") > 120) return;
            if (ZoneID != MQ.Query<int>($"${{Zone.ID}}"))
            {
                MQ.Write("\ar Zone Changed Detected Turning Off");
                E3.MQ.Cmd("/lootoff");
                PrivateCode.PullingMode = false;
                PrivateCode.LazyMode = false;
                return;
            }
            if (targetID > 0 && E3.MQ.Query<int>($"${{Spawn[npc {targetID}]}}") > 0 && E3.MQ.Query<decimal>($"${{Spawn[{PrivateCode.GroupCampMember} pc].Distance}}") < 120)
            {
                if (targetID != EngagedID && E3.MQ.Query<decimal>(@"${Me.XTarget[1].Distance}") < 120)
                {
                    MQ.Delay(2000, "${Me.XTarget[1].Distance} < 20");
                    E3.MQ.Cmd(@"/target id " + targetID.ToString());
                    E3.MQ.Delay(50);
                    if (PrivateCode.PullingMode) E3.MQ.Cmd(@"/squelch /face fast nolook");
                    E3.MQ.Cmd(@"/g Assist Me on %t");
                    EngagedID = targetID;
                    E3.MQ.Cmd(@"/assistme /all");
                }
            }
            else
            {
                EngagedID = 0;
                E3.MQ.Cmd(@"/Squelch /Attack off");
            }
        }
        // Check If HP are low then return if they are
        // Check if Stuck then using manual movement if so 
        // if mob attacking member move to call and assist
        // fix followoff tome= need to add ZoneID

        private static void Private_PullingTarget()
        {
            // MQ.Delay(300, $"${{Target.ID}}=={targetID}");
            int PullingDistance = PrivateCode.MobInformation.PullDistance;
            string ReturnToName = PrivateCode.GroupCampMember;
            int rand = new Random().Next(0, 101);
            // Force Pickup of Mobs 
            if (E3.CharacterSettings.Misc_AutoLootEnabled && MQ.Query<Int32>($"${{SpawnCount[NPC Corpse radius 100]}}") > 5) return;
            CheckAndCamp();
            if (!PullingTimer.IsElapsed()) return;
            // Not Used atm 
            if (!myTimer.IsElapsed() || E3.MQ.Query<int>(@"${Me.PctHPs}") < 60 || E3.MQ.Query<int>($"${{Me.XTarget[1].ID}}") > 0)
            {
                rand = new Random().Next(0, 30);
                PullingTimer.SetTime(0, 0, rand, 500);
                return;
            }
            // Find Mob To Pull 
            if (E3.MQ.Query<Int32>(@"${Me.XTarget[1].ID}") < 1)
            {
                Int32 NumberOfMobsInRadius = E3.MQ.Query<Int32>($"${{SpawnCount[NPC radius {PullingDistance}]}}");
                if (NumberOfMobsInRadius > 0 && MQ.Query<Int32>($"${{SpawnCount[PC radius {PrivateCode.MobInformation.PullDistance * 1.3}]}}") == PrivateCode.PCNear)
                {
                    int numberOfMobsToPull = PrivateCode.MobInformation.PullSize;
                    int pullingNthMob = 0;

                    DateTime startTime = DateTime.UtcNow;  // Capture the current time
                    TimeSpan timeout = TimeSpan.FromSeconds(30);  // Set the timeout period

                    int i = 1;
                    for (; i <= E3.MQ.Query<Int32>($"${{SpawnCount[NPC radius {PullingDistance}]}}"); i++)
                    {
                        int mobID = E3.MQ.Query<int>($"${{NearestSpawn[{i}, NPC].ID}}");
                        // Check if this mob is already upset with me
                        for (int x = 1; x < 10; x++)
                        {
                            if (E3.MQ.Query<int>($"${{Me.XTarget[{x}].ID}}") == mobID)
                            {
                                continue;
                            }
                        }

                        if (Private_ShouldIPullMob(mobID))
                        {
                            //E3.MQ.Write("Mob " + i);
                            HuntDownMob(mobID);
                            pullingNthMob = 0;
                        }

                        // Should Cycle ID to find out how many mobs are upset with me
                        for (int x = 1; x < 10; x++)
                        {
                            if (E3.MQ.Query<int>($"${{Me.XTarget[{x}].ID}}") > 0)
                            {
                                pullingNthMob++;
                                //MQ.Write("Mobs Upset " + pullingNthMob);
                                i = pullingNthMob;
                            }
                        }

                        if (E3.MQ.Query<int>($"${{Me.XTarget[{numberOfMobsToPull}].ID}}") > 0) break;
                        if (i == E3.MQ.Query<Int32>($"${{SpawnCount[NPC radius {PullingDistance}]}}"))
                        {
                            MQ.Write("No Mobs In Range");
                            // Set the timer to 1 hour, 20 minutes, 45 seconds and 500 milliseconds
                            myTimer.SetTime(0, 02, 60, 500);
                        }
                    }
                    ReturnToCamp(ReturnToName);
                }

                else
                {
                    if (ChatTimer.IsElapsed()) // 0 hours, 1 minute, 20 seconds, 0 milliseconds
                    {
                        ChatTimer.SetTime(0, 1, 0, 0);
                        E3.MQ.Write("\ayPCs Near");
                        MQ.Cmd("/beep");
                        MQ.Cmd("/beep");
                    }
                }
            }
        }

        private static bool Private_ShouldIPullMob(int mobID)
        {
            bool debug = false;
            int PullingDistance = PrivateCode.MobInformation.PullDistance;
            int MaxLevel = MQ.Query<Int32>(@"${Me.Level}") + PrivateCode.MobInformation.MobLevelOverMe;
            string mobName = E3.MQ.Query<string>($"${{Spawn[id {mobID}].CleanName}}");
            double mobDistance = E3.MQ.Query<double>($"${{Math.Distance[${{Spawn[pc {PrivateCode.GroupCampMember}].LocYX}}:${{Spawn[npc id {mobID}].LocYX}}]}}");
            int mobLevel = E3.MQ.Query<int>($"${{Spawn[id {mobID}].Level}}");

            //  E3.MQ.Write(MobInformation.IgnoreMobs.ToString() + " " + mobName.ToString() + " " + MobInformation.MobNameArray.Contains(mobName).ToString());
            MQ.Delay(20);
            if (PrivateCode.MobInformation.IgnoreMobs && PrivateCode.MobInformation.MobNameArray.Contains(mobName))
            {
                if (debug) E3.MQ.Write("Ignore " + PrivateCode.MobInformation.IgnoreMobs.ToString() + " " + mobName.ToString() + " " + PrivateCode.MobInformation.MobNameArray.Contains(mobName).ToString());
                return false;
            }
            //E3.MQ.Write(mobName + " " + mobLevel + " " + mobDistance + " " + PullingDistance.ToString() + " " +  MaxLevel.ToString());
            if ((!PrivateCode.MobInformation.IgnoreMobs && PrivateCode.MobInformation.MobNameArray.Contains(mobName)) ||
                (PrivateCode.MobInformation.IgnoreMobs && !PrivateCode.MobInformation.MobNameArray.Contains(mobName)))
            {
                if (mobDistance < PullingDistance && mobLevel <= MaxLevel)
                {
                    // E3.MQ.Write("2 " + mobName + " " + mobLevel + " " + mobDistance + " " + PullingDistance.ToString() + " " + MaxLevel.ToString());
                    return true;
                }
                else
                {
                    if (debug) E3.MQ.Write("Skipped due to distance or level: " + mobName + " level " + mobLevel + " max " + MaxLevel +
                        " distance " + mobDistance);
                }
            }
            else
            {
                if (debug) E3.MQ.Write("Skipped due to ignore list settings: " + mobName);
            }
            return false;
        }

        private static bool IsMobOnXTarget(int mobID)
        {
            for (int i = 1; i <= 10; i++)
            {
                if (E3.MQ.Query<int>($"${{Me.XTarget[{i}].ID}}") == mobID) return true;
            }
            return false;
        }
        private static void HuntDownMob(int mobID)
        {
            // Here you can place the logic to hunt down the mob
            E3.MQ.Cmd($"/squelch /nav id {mobID} dist=5 log=off");
            E3.MQ.Cmd($"/target id {mobID}");
            E3.MQ.Delay(15000, () => IsMobOnXTarget(mobID));
            E3.MQ.Cmd("/squelch /nav stop log=off");
        }
        private static void ReturnToCamp(string returnToString)
        {
            if (E3.MQ.Query<double>($"${{Spawn[{returnToString} pc].Distance}}") < 30) return;
            E3.MQ.Cmd("/squelch /nav stop log=off");
            E3.MQ.Cmd("/squelch /nav id " + E3.MQ.Query<Int32>($"${{Spawn[{returnToString}].ID}}").ToString() + " dist=30 log=off");
            E3.MQ.Delay(30000, $"${{Spawn[{returnToString} pc].Distance}} < 30");
            E3.MQ.Delay(100);
            E3.MQ.Cmd("/squelch /face fast nolook");
            E3.MQ.Cmd("/squelch /nav stop log=off");
        }
        private static void CheckAndCamp()
        {
            if (CampUpdateTimer.IsElapsed())
            {
                CampUpdateTimer.SetTime(0, 15, 10, 0);
                string timeLeft = PrivateCode.CampOutTimer.TimeLeftString(); // Assuming that the time span you're interested in is 10 seconds
                MQ.Write("\a-yCamp Time Left " + timeLeft);
            }
            // Camp out 
            if (MQ.Query<int>($"${{Me.XTarget[1].ID}}") == 0 && PrivateCode.CampOutTimer.IsElapsed())
            {
                MQ.Write("Starting To Camp");
                MQ.Cmd("/beep");
                MQ.Cmd("/beep");
                MQ.Delay(30000);
                if (MQ.Query<int>($"${{Me.XTarget[1].ID}}") > 0) return;
                MQ.Write("Starting To Camp");
                MQ.Cmd("/beep");
                MQ.Cmd("/beep");
                MQ.Delay(30000);
                if (MQ.Query<int>($"${{Me.XTarget[1].ID}}") > 0) return;
                MQ.Write("Camping");
                MQ.Delay(10000);
                MQ.Cmd("/bcaa //CampOut");
            }
        }
        // lazy mode
        private static void RegisterCommandLazyMode()
        {
            EventProcessor.RegisterCommand("/lazymode", (x) =>
            {
                if (x.args.Count > 0 && x.args[0].Equals("on", StringComparison.OrdinalIgnoreCase))
                {
                    if (x.args.Count > 1 && MQ.Query<int>($"${{Spawn[pc {x.args[1]}].ID}}") > 0)
                    {
                        PrivateCode.LazyMode = true;
                        PrivateCode.PullingMode = false;
                        ZoneID = MQ.Query<int>($"${{Zone.ID}}");
                        E3.MQ.Cmd("/lootoff");
                        PrivateCode.GroupCampMember = x.args[1];
                        string ZoneName = MQ.Query<string>("${Zone.Name}");
                        E3.MQ.Write("\agLazy Mode is " + PrivateCode.LazyMode.ToString());
                    }
                    else
                    {
                        MQ.Write("\ar/lazymode on CAMPNAME");
                    }
                }
                else
                {
                    MQ.Write("\agLazy Mode Stopped");
                    E3.MQ.Cmd("/lootoff");
                    PrivateCode.LazyMode = false;
                    PrivateCode.PullingMode = false;
                }
            });
        }

        private static void RegisterCommandPullingMode()
        {
            EventProcessor.RegisterCommand("/pullingmode", (x) =>
            {
                if (x.args.Count > 0 && x.args[0].Equals("on", StringComparison.OrdinalIgnoreCase))
                {
                    if (x.args.Count > 1 && MQ.Query<int>($"${{Spawn[pc {x.args[1]}].ID}}") > 0)
                    {
                        PrivateCode.PullingMode = true;
                        PrivateCode.LazyMode = false;
                        ZoneID = MQ.Query<int>($"${{Zone.ID}}");
                        E3.MQ.Cmd("/looton");
                        PrivateCode.GroupCampMember = x.args[1];
                        string ZoneName = MQ.Query<string>("${Zone.Name}");
                        PrivateCode.ReadINI("e3 Private/MobPulling.ini", ZoneName);
                        double MathRadius = PrivateCode.MobInformation.PullDistance * 1.3;
                        PrivateCode.PCNear = MQ.Query<Int32>($"${{SpawnCount[PC radius {MathRadius}]}}");
                        MQ.Write("How Many PC Near me " + PrivateCode.PCNear.ToString());
                        MQ.Write("Pulling Distance:" + PrivateCode.MobInformation.PullDistance.ToString() + " Number of Mobs at a time " + PrivateCode.MobInformation.PullSize);
                        MQ.Write("Number Mobs on List: " + PrivateCode.MobInformation.MobNameArray.Count.ToString());
                        PrivateCode.CampOutTimer.SetTime(1, 20, 15, 130);
                        E3.MQ.Write("\agPulling Mode is " + PrivateCode.PullingMode.ToString() + " and camp is " + PrivateCode.GroupCampMember);
                    }
                    else
                    {
                        MQ.Write("\ar/pullingmode on CAMPNAME");
                    }
                }
                else
                {
                    MQ.Write("\agPulling and Lazy Mode Stopped");
                    E3.MQ.Cmd("/lootoff");
                    PrivateCode.PullingMode = false;
                    PrivateCode.LazyMode = false;
                }
            });
        }
    }
}

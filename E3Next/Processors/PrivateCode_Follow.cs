using E3Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoCore;

namespace E3Core.Processors
{

    public static class PrivateFollow
    {
        private static List<string> targetLocations = new List<string>();
        private static int currentLocationIndex = 0;
        static private bool IAmRunning = false;

        private static IMQ MQ = E3.MQ;
        public static void Startup()
        {
            RegisterCommandFollowMode();
        }
        public static void Update()
        {
            Private_PCFollow();
        }

        private static void Private_PCFollow()
        {
            if (!PrivateCode.IWantToFollow || !CheckSpawnStatus())
                return;
            // Update locations even if close 
            UpdateTargetLocations();
            // Follow when far 
            FollowTarget();
        }

        private static bool CheckSpawnStatus()
        {
            bool SpawnInZone = MQ.Query<bool>($"${{Bool[${{Spawn[{PrivateCode.followSpawn} pc]}}]}}");

            if (!SpawnInZone && PrivateCode.IWantToFollow)
            {
                MQ.Write("\ay Spawn Not In Zone Stopping");
                PrivateCode.IWantToFollow = false;
                return false;
            }
            return true;
        }

        private static void UpdateTargetLocations()
        {
            string currentTargetLoc = MQ.Query<string>($"${{Spawn[{PrivateCode.followSpawn} pc].Loc}}");
            //MQ.Write(targetLocations.Count.ToString());
            // Add the current Target.Loc to the list if it's not already there.
            if (!targetLocations.Contains(currentTargetLoc))
            {
                if (targetLocations.Count == 100)
                {
                    MQ.Write("List Full Removing Old Loc");
                    targetLocations.RemoveAt(0);
                }
                targetLocations.Add(currentTargetLoc);
            }
        }

        private static void FollowTarget()
        {
            bool AmICasting = MQ.Query<bool>(@"${Me.Casting}");
            bool AmICombat = MQ.Query<bool>(@"${Me.Combat}");

            if (!AmICasting && !AmICombat || E3.CurrentClass == Class.Bard && !AmICombat)
            {
                double DistanceToSpawn = MQ.Query<double>($"${{Spawn[{PrivateCode.followSpawn} pc].Distance}}");
                if (DistanceToSpawn > 250)
                {
                    targetLocations.Clear();
                    currentLocationIndex = 0;
                    if (IAmRunning) MQ.Cmd("/keypress forward");
                    IAmRunning = false;
                    // Send CMD Stuck 
                }

                if (DistanceToSpawn > PrivateCode.followDistance && DistanceToSpawn < 250) //Using the given maxRange directly
                {
                    string locationToFollow = targetLocations[currentLocationIndex];
                    if (MQ.Query<bool>(@"${Me.Ducking}")) MQ.Cmd("/Squelch /stand");
                    MQ.Cmd("/Squelch /face fast nolook Loc " + locationToFollow);
                    MQ.Cmd("/keypress forward hold");
                    IAmRunning = true;

                    // Check if the current location is within the acceptable advance distance before advancing the currentLocationIndex.
                    while (currentLocationIndex + 2 < targetLocations.Count)
                    {
                        double distanceToCurrentLocation = MQ.Query<double>($"${{Math.Distance[{locationToFollow}]}}");
                        currentLocationIndex = currentLocationIndex + 2;
                        if (distanceToCurrentLocation > 2) //Using the given advanceDistance directly
                        {
                            break;
                        }
                    }
                }
                if (DistanceToSpawn <= PrivateCode.followDistance)
                {
                    int bufferSize = 10;
                    // Keep only the last 5 movements
                    if (targetLocations.Count > bufferSize)
                    {
                        targetLocations = targetLocations.Skip(Math.Max(0, targetLocations.Count - bufferSize)).ToList();
                        // Adjust the current location index to account for the removed locations
                    }

                    if (IAmRunning)
                    {
                        targetLocations.Clear();
                        currentLocationIndex = 0;
                        MQ.Cmd("/keypress forward");
                    }
                    IAmRunning = false;
                    return;  // Exit the function early since we don't need to execute further logic
                }
            }
        }
        private static void RegisterCommandFollowMode()
        {
            EventProcessor.RegisterCommand("/PrivateFollow", (x) =>
            {

                if (x.args[0].Equals("stop", StringComparison.OrdinalIgnoreCase))
                {
                    MQ.Write("\ag Stopped 2");
                    MQ.Cmd("/keypress forward");
                    PrivateCode.IWantToFollow = false;
                }
                else
                {
                    PrivateCode.followSpawn = x.args[0];
                    int.TryParse(x.args[1], out PrivateCode.followDistance);
                    PrivateCode.IWantToFollow = true;
                    if (PrivateCode.IWantToFollow) MQ.Write("\ag Following " + PrivateCode.followSpawn + " distance " + PrivateCode.followDistance);
                    MQ.Cmd("/keypress forward");
                }
            });
        }
    }
}

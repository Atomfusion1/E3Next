﻿using E3Core.Processors;
using E3Core.Settings;
using System;
using E3Core.Classes;
using E3Core.Data;
using E3Core.Utility;
using MonoCore;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace E3Core.Classes
{
    /// <summary>
    /// Properties and methods specific to the necromancer class
    /// </summary>
    public static class Necromancer
    {
        private static Logging _log = E3.Log;
        private static IMQ MQ = E3.Mq;
        private static ISpawns _spawns = E3.Spawns;

        private static Int64 _nextAggroCheck = 0;
        private static Int64 _nextAggroRefreshTimeInterval = 1000;
        private static Int32 _maxAggroCap = 75;

        /// <summary>
        /// Checks aggro level and drops it if necessary.
        /// </summary>
        [AdvSettingInvoke]
        public static void Check_NecroAggro()
        {
            if (!e3util.ShouldCheck(ref _nextAggroCheck, _nextAggroRefreshTimeInterval)) return;

            Int32 currentAggro = 0;
            Int32 tempMaxAggro = 0;

            for (Int32 i = 1; i <= 13; i++)
            {
                bool autoHater = MQ.Query<bool>($"${{Me.XTarget[{i}].TargetType.Equal[Auto Hater]}}");
                if (!autoHater) continue;
                Int32 mobId = MQ.Query<Int32>($"${{Me.XTarget[{i}].ID}}");
                if (mobId > 0)
                {
                     Spawn s;
                    if (_spawns.TryByID(mobId, out s))
                    {
                        if (s.Aggressive)
                        {
                            currentAggro = MQ.Query<Int32>($"${{Me.XTarget[{i}].PctAggro}}");
                            if(tempMaxAggro<currentAggro)
                            {
                                tempMaxAggro = currentAggro;
                            }
                        }
                    }
                }
            }
            if(tempMaxAggro>_maxAggroCap)
            {
                
                Spell s;
                if(!Spell._loadedSpellsByName.TryGetValue("Improved Death Peace",out s))
                {
                    s = new Spell("Improved Death Peace");
                }
                if(Casting.CheckReady(s) && Casting.CheckMana(s))
                {
                    Casting.Cast(0, s);
                    //check to see if we can stand based off the # of group members.
                    Int32 GroupSize = MQ.Query<Int32>("${Group}");
                    Int32 GroupInZone = MQ.Query<Int32>("${Group.Present}");

                    if (GroupSize - GroupInZone > 0)
                    {
                        Assist.AssistOff();
                        E3.Bots.Broadcast("<CheckNecroAggro> Have agro, someone is dead, staying down. Issue reassist when ready.");


                    }
                    else
                    {
                        MQ.Cmd("/stand");
                        return;
                    }

                }
                if (!Spell._loadedSpellsByName.TryGetValue("Death Peace", out s))
                {
                    s = new Spell("Death Peace");
                }
                if (Casting.CheckReady(s) && Casting.CheckMana(s))
                {
                    Casting.Cast(0, s);
                    //check to see if we can stand based off the # of group members.
                    Int32 GroupSize = MQ.Query<Int32>("${Group}");
                    Int32 GroupInZone = MQ.Query<Int32>("${Group.Present}");

                    if (GroupSize - GroupInZone > 0)
                    {
                        Assist.AssistOff();
                        E3.Bots.Broadcast("<CheckNecroAggro> Have agro, someone is dead, staying down. Issue reassist when ready.");
                    }
                    else
                    {
                        MQ.Cmd("/stand");
                        return;
                    }
                    return;
                }

            } 
            //else if(tempMaxAggro>_maxAggroCap && !MQ.Query<bool>("${Bool[${Me.Song[Harmshield]}]}"))
            //{

            //    Spell s;

            //    if (!Spell._loadedSpellsByName.TryGetValue("Embalmer's Carapace", out s))
            //    {
            //        s = new Spell("Embalmer's Carapace");
            //    }
            //    if (Casting.CheckReady(s) && Casting.CheckMana(s))
            //    {
            //        Casting.Cast(0, s);
            //        return;
            //    }

            //    if (!Spell._loadedSpellsByName.TryGetValue("Harmshield", out s))
            //    {
            //        s = new Spell("Harmshield");
            //    }
            //    if (Casting.CheckReady(s) && Casting.CheckMana(s))
            //    {
            //        Casting.Cast(0, s);
            //        return;
            //    }
               

            //}



        }

    }
}

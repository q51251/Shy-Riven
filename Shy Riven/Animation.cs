﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace ShyRiven
{
    internal class Animation
    {
        private static int s_LastAATick;
        private static bool s_CheckAA;
        private static bool s_DoAttack;

        public static int QStacks = 0;
        public static int LastQTick = 0;
        public static bool CanCastAnimation = true;
        public static bool UltActive;

        public delegate void dOnAnimationCastable(string animname);
        public static event dOnAnimationCastable OnAnimationCastable;

        public static void OnPlay(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (sender.IsMe)
            {
                int t = 0;
                switch (args.Animation)
                {
                    case "Spell1a":
                        QStacks = 1;
                        CanCastAnimation = false;
                        LastQTick = Utils.TickCount;
                        t = 291;
                        break;
                    case "Spell1b":
                        QStacks = 2;
                        CanCastAnimation = false;
                        LastQTick = Utils.TickCount;
                        t = 291;
                        break;
                    case "Spell1c":
                        QStacks = 0;
                        SetAttack(false);
                        CanCastAnimation = false;
                        LastQTick = Utils.TickCount;
                        t = 393;
                        break;
                    case "Spell2":
                        CanCastAnimation = false;
                        t = 170;
                        break;
                    case "Spell3":
                        CanCastAnimation = true;
                        break;
                    case "Spell4a":
                        t = 0;
                        CanCastAnimation = false;
                        UltActive = true;
                        break;
                    case "Spell4b":
                        t = 150;
                        CanCastAnimation = false;
                        UltActive = false;
                        break;
                }
                if (t != 0)
                {
                    if (Program.Champion.Orbwalker.ActiveMode != ShineCommon.Orbwalking.OrbwalkingMode.None)
                    {
                        LeagueSharp.Common.Utility.DelayAction.Add(t - Game.Ping, () => CancelAnimation());
                        LeagueSharp.Common.Utility.DelayAction.Add(t - Game.Ping, () => OnAnimationCastable(args.Animation));
                    }
                }
                else
                    LeagueSharp.Common.Utility.DelayAction.Add(1, () => OnAnimationCastable(args.Animation));
            }
        }

        public static void OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            if (Utils.TickCount - s_LastAATick < 300 + Game.Ping && s_CheckAA && args.SourceNetworkId == ObjectManager.Player.NetworkId)
            {
                var t = Target.Get(Program.Champion.Spells[0].Range + 50, true);
                if (s_DoAttack && Program.Champion.Spells[0].IsReady())
                {
                    if (t != null && (Program.Champion.OrbwalkingActiveMode == Program.Champion.OrbwalkingComboMode || Program.Champion.OrbwalkingActiveMode == Program.Champion.OrbwalkingHarassMode))
                    {
                        Program.Champion.Spells[0].Cast(t.ServerPosition, true);
                        ShineCommon.Orbwalking.ResetAutoAttackTimer();
                        Program.Champion.Orbwalker.ForceTarget(t);
                    }
                    else if (Program.Champion.OrbwalkingActiveMode == Program.Champion.OrbwalkingLaneClearMode)
                    {
                        var minion = MinionManager.GetMinions(400, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth).OrderBy(p => p.ServerPosition.Distance(ObjectManager.Player.ServerPosition)).FirstOrDefault();
                        if (minion != null)
                        {
                            Program.Champion.Spells[0].Cast(minion.ServerPosition, true);
                            ShineCommon.Orbwalking.ResetAutoAttackTimer();
                            Program.Champion.Orbwalker.ForceTarget(t);
                        }
                    }

                }
                else
                    SetAttack(false);

                CanCastAnimation = true;
                s_CheckAA = false;
            }
        }

        public static void OnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            if (args.Order == GameObjectOrder.AttackUnit && Program.Champion.OrbwalkingActiveMode != Program.Champion.OrbwalkingNoneMode && Program.Champion.OrbwalkingActiveMode != Program.Champion.OrbwalkingLastHitMode)
                ShineCommon.Orbwalking.Move2 = true;

            if (args.Order == GameObjectOrder.MoveTo)
                ShineCommon.Orbwalking.Move2 = false;
        }

        public static void CancelAnimation()
        {
            Game.Say("/d");
            CanCastAnimation = true;
            if (Program.Champion.OrbwalkingActiveMode == Program.Champion.OrbwalkingLaneClearMode)
            {
                var minion = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, 250, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth).FirstOrDefault();
                if (minion != null && s_DoAttack)
                {
                    ShineCommon.Orbwalking.Move2 = true;
                    Program.Champion.Orbwalker.ForceTarget(minion);
                    ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, minion);
                }
            }
            else if (Program.Champion.OrbwalkingActiveMode == Program.Champion.OrbwalkingHarassMode || Program.Champion.OrbwalkingActiveMode == Program.Champion.OrbwalkingComboMode)
            {
                var t = Target.Get(1000f);
                if (t != null && s_DoAttack)
                {
                    ShineCommon.Orbwalking.Move2 = true;
                    Program.Champion.Orbwalker.ForceTarget(t);
                    ShineCommon.Orbwalking.ResetAutoAttackTimer();
                    ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, t);
                }
            }
        }

        public static void SetLastAATick(int tick)
        {
            s_LastAATick = Utils.TickCount;
            s_CheckAA = true;
        }

        public static void SetAttack(bool b)
        {
            s_DoAttack = b;
            ShineCommon.Orbwalking.Move2 = b;
        }

        public static bool CanAttack()
        {
            return s_DoAttack;
        }
    }
}

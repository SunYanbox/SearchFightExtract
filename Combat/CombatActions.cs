using System;
using System.Collections.Generic;

namespace SearchFightExtract
{
    /// <summary>
    /// 战斗结算：只改状态，并把原本要显示的内容作为文本行返回，不直接输出。
    /// 打印由调用方（Game / CombatView）负责，因此本类可以脱离控制台单独验证。
    /// </summary>
    static class CombatActions
    {
        // ===== 我方行动 =====

        /// <summary>开火。返回本次攻击产生的全部文本行。</summary>
        public static List<string> PlayerAttack(Player p, Enemy e, Random rng, double multiplier = 1.0)
        {
            double baseDmg = p.Weapon?.Power ?? 5;
            int bulletTier = p.Weapon?.Tier ?? 0;
            double dmg = CombatMath.RollAttack(rng, baseDmg) * multiplier
                * CombatMath.DamageDealtMult(p.CombatDamageBonus, e.WeakenTurns > 0);

            double enemyArmor = e.Armor;
            double final = CombatMath.CalculateDamage(dmg, bulletTier, e.ArmorTier, ref enemyArmor, out double armorLoss);
            e.Armor = enemyArmor;
            e.Hp -= final;

            var log = new List<string>();
            string dmgStr = Style.Paint($"{final:F1}", CombatStyle.Damage);
            if (armorLoss > 0)
                log.Add($"你造成 {dmgStr} 伤害，削减敌方 {Style.Paint($"{armorLoss:F1}", CombatStyle.Armor)} 点护甲。");
            else
                log.Add($"你造成 {dmgStr} 伤害。");

            log.AddRange(ApplyOnHit(p, final));
            return log;
        }

        /// <summary>【超载】偶数回合行动后的追加攻击（125% 伤害）。</summary>
        public static List<string> OverloadAttack(Player p, Enemy e, Random rng)
        {
            var log = new List<string> { Style.Paint("【超载】触发额外攻击！", Style.BrightYellow) };
            log.AddRange(PlayerAttack(p, e, rng, 1.25));
            return log;
        }

        /// <summary>
        /// 主动技能结算。冷却在最后统一进入，无论技能是否命中。
        /// 使用数据驱动：效果计算委托由 SkillData.Effect 提供，
        /// 侧效应（清除冷却、设置免伤等）由 CombatActions 统一处理。
        /// </summary>
        public static List<string> UseSkill(Player p, Enemy e, Skill skill, Random rng)
        {
            var log = new List<string>();

            // 通用：调用数据定义的效果计算委托
            log.AddRange(skill.ComputeEffect(p, e));

            // 主动技能特有侧效应（Adrenaline 清除冷却、Incendiary 设置敌人跳过回合）
            if (skill.Type == SkillType.Adrenaline)
            {
                foreach (var sk in p.Actives) sk.CurrentCooldown = 0;
                log.Add("肾上腺素：所有主动技能冷却已清除！（不消耗行动）");
            }

            if (skill.Type == SkillType.Incendiary)
            {
                e.SkipNextTurn = true;   // 敌人灭火，失去下一回合
                e.BurnTurns = 3;
                e.BurnDamage = 15;
                // 燃烧弹伤害已在 ComputeEffect 中返回
            }

            skill.CurrentCooldown = skill.Cooldown;
            return log;
        }

        /// <summary>造成伤害后触发的战斗部门效果（吸血 / 护甲回复）。</summary>
        public static List<string> ApplyOnHit(Player p, double damage)
        {
            var log = new List<string>();
            if (damage <= 0) return log;

            if (p.CombatLifeSteal > 0)
            {
                double before = p.Hp;
                p.Hp = Math.Min(p.MaxHp, p.Hp + damage * p.CombatLifeSteal);
                double actual = p.Hp - before;
                if (actual > 0.05)
                    log.Add(Style.Paint($"  吸血：恢复 {actual:F1} 生命", CombatStyle.Heal));
            }

            if (p.CombatArmorRestore > 0 && p.ArmorItem != null && p.Armor > 0)
            {
                double before = p.Armor;
                p.Armor = Math.Min(p.MaxArmor, p.Armor + damage * p.CombatArmorRestore);
                double actual = p.Armor - before;
                if (actual > 0.05)
                    log.Add(Style.Paint($"  护甲回复：+{actual:F1}", CombatStyle.Armor));
            }
            return log;
        }

        // ===== 战斗中的消耗品 =====
        // 返回 true 表示成功消耗一件、本回合行动已被用掉。

        public static bool TryUseMedkit(Player p, List<string> log)
        {
            if (p.RaidMedkits <= 0) { log.Add("没有急救包！"); return false; }
            p.RaidMedkits--;
            p.Hp = Math.Min(p.MaxHp, p.Hp + 40);
            log.Add(Style.Paint("恢复40生命。", CombatStyle.Heal));
            return true;
        }

        public static bool TryUseRepairKit(Player p, List<string> log)
        {
            if (p.RaidRepairKits <= 0) { log.Add("没有维修套件！"); return false; }
            p.RaidRepairKits--;
            if (p.ArmorItem != null)
            {
                p.Armor = Math.Min(p.MaxArmor, p.Armor + 50);
                log.Add(Style.Paint("护甲恢复50。", CombatStyle.Armor));
            }
            else log.Add("没有护甲可修。");
            return true;
        }

        public static bool TryUseStim(Player p, List<string> log)
        {
            if (p.RaidStims <= 0) { log.Add("没有兴奋剂！"); return false; }
            p.RaidStims--;
            foreach (var sk in p.Actives) sk.CurrentCooldown = 0;
            log.Add("肾上腺素：所有主动技能冷却已清除！（不消耗行动）");
            return true;
        }

        // ===== 敌方行动 =====

        /// <summary>
        /// 敌方行动 + 回合末结算，一次跑完。
        /// 敌人被燃烧烧死时提前返回（不再行动，也不再做回合末结算），
        /// 调用方据 <c>e.Hp &lt;= 0</c> 判定胜负即可。
        /// </summary>
        public static List<string> EnemyTurn(CombatState st, Player p, Enemy e, Random rng)
        {
            var log = new List<string>();
            bool armorBrokeThisTurn = false;

            if (e.SkipNextTurn)
            {
                log.Add($"{e.Name} 被束缚/灭火，跳过回合。");
                e.SkipNextTurn = false;
            }
            else
            {
                if (e.BurnTurns > 0)
                {
                    double burn = e.BurnDamage;
                    double bAbsorb = Math.Min(e.Armor, burn);
                    e.Armor -= bAbsorb;
                    double bHp = burn - bAbsorb;
                    e.Hp -= bHp;
                    e.BurnTurns--;
                    log.Add($"燃烧持续：护甲吸收 {Style.Paint($"{bAbsorb:F1}", CombatStyle.Armor)}，造成 {Style.Paint($"{bHp:F1}", CombatStyle.Damage)} 伤害。");
                    if (e.Hp <= 0)
                    {
                        log.Add(Style.Paint($"{e.Name} 被烧尽了。", Style.BrightGreen));
                        return log;
                    }
                }

                double edmg = CombatMath.RollAttack(rng, e.Damage);
                double armorBefore = p.Armor;
                double armorLossP;
                double finalDmg;

                // 临时护甲优先消耗（按指定 Tier 结算）
                if (p.TempArmor > 0)
                {
                    double ta = p.TempArmor;
                    finalDmg = CombatMath.CalculateDamage(edmg, e.BulletTier, p.TempArmorTier, ref ta, out armorLossP, p.ArmorLossMult);
                    p.TempArmor = ta;
                }
                else
                {
                    double playerArmor = p.Armor;
                    finalDmg = CombatMath.CalculateDamage(edmg, e.BulletTier, p.ArmorItem?.Tier ?? 0, ref playerArmor, out armorLossP, p.ArmorLossMult);
                    p.Armor = playerArmor;
                }

                // 深蓝 -30% / 超载 -10% / 铁壁破甲 -80% / 应急治疗 -80%
                finalDmg = CombatMath.ApplyDamageTakenReduction(
                    finalDmg,
                    p.HasPassive(SkillType.DeepBlue),
                    p.HasPassive(SkillType.Overload),
                    p.ArmorBreakDRTurns > 0,
                    p.FirstAidDRTurns > 0);
                if (edmg > 0) finalDmg = Math.Max(1, finalDmg);

                // 铁壁：护甲刚刚破碎 → 触发免伤
                if (p.HasPassive(SkillType.IronWall) && armorBefore > 0 && p.Armor <= 0)
                {
                    p.ArmorBreakDRTurns = 2;
                    armorBrokeThisTurn = true;
                    log.Add(Style.Paint("【铁壁】护甲破碎！获得80%免伤，持续2回合。", Style.BrightYellow));
                }

                if (p.Shield > 0)
                {
                    double absorbed = Math.Min(p.Shield, finalDmg);
                    p.Shield -= absorbed;
                    finalDmg -= absorbed;
                    log.Add(Style.Paint($"护盾吸收 {absorbed:F1} 伤害。", CombatStyle.Shield));
                }
                p.Hp -= finalDmg;

                string eDmgStr = Style.Paint($"{finalDmg:F1}", CombatStyle.Damage);
                if (armorLossP > 0)
                    log.Add($"{e.Name} 攻击，造成 {eDmgStr} 伤害，你的护甲损失 {Style.Paint($"{armorLossP:F1}", CombatStyle.Armor)} 点。");
                else
                    log.Add($"{e.Name} 攻击，造成 {eDmgStr} 伤害。");

                // 再现：累计 = 血量伤害 + 0.3 × 护甲伤害
                if (p.HasPassive(SkillType.Reappear))
                {
                    st.DamageTakenAccum += finalDmg + armorLossP * 0.3;
                    while (st.DamageTakenAccum >= 50)
                    {
                        st.DamageTakenAccum -= 50;
                        st.PendingExtraTurns++;
                        log.Add(Style.Paint("【再现】累计受伤达到50，下回合获得一个额外行动！", CombatStyle.Extra));
                    }
                }
            }

            TickEndOfTurn(p, e, armorBrokeThisTurn);
            return log;
        }

        /// <summary>
        /// 回合末结算：状态回合数递减、护盾与临时护甲衰减、技能冷却递减。
        /// 铁壁破甲当回合不递减免伤回合。
        /// </summary>
        static void TickEndOfTurn(Player p, Enemy e, bool armorBrokeThisTurn)
        {
            if (e.WeakenTurns > 0) e.WeakenTurns--;
            if (p.ArmorBreakDRTurns > 0 && !armorBrokeThisTurn) p.ArmorBreakDRTurns--;
            if (p.FirstAidDRTurns > 0) p.FirstAidDRTurns--;
            if (p.TempArmor > 0) p.TempArmor = Math.Max(0, p.TempArmor - 20);
            if (p.Shield > 0) p.Shield = Math.Max(0, p.Shield - 20);
            foreach (var s in p.Actives) if (s.CurrentCooldown > 0) s.CurrentCooldown--;
        }
    }
}

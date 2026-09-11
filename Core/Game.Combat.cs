using System;
using System.Linq;
using static SearchFightExtract.ConsoleHelper;

namespace SearchFightExtract
{
    partial class Game
    {
        // 战斗显示用配色
        const string ColDamage = Style.BrightRed;    // 造成伤害
        const string ColHeal = Style.Green;          // 治疗
        const string ColShield = Style.BrightBlack;  // 护盾抵消
        const string ColArmor = Style.Cyan;          // 护甲抵消/回复（青色，便于辨认）
        const string ColExtra = Style.BrightCyan;    // 额外回合

        double CalculateDamage(double baseDmg, int bulletTier, int armorTier, ref double armor, out double armorLoss, double armorLossMult = 1.0)
        {
            armorLoss = 0;
            if (armor <= 0) return baseDmg;

            double armorBefore = armor;

            double dmgMultiplier;
            double absorbRate;

            if (bulletTier > armorTier) { dmgMultiplier = 1.0; absorbRate = 0.3; }
            else if (bulletTier == armorTier) { dmgMultiplier = 0.75; absorbRate = 0.5; }
            else { dmgMultiplier = 0.5; absorbRate = 0.8; }

            double theoretical = baseDmg * dmgMultiplier;
            double absorbed = theoretical * absorbRate;

            armor = Math.Max(0, armor - absorbed * armorLossMult);
            armorLoss = armorBefore - armor;

            double final = theoretical - absorbed;
            return Math.Max(1, final);
        }

        // 玩家对敌增伤乘区：战斗部门 + 深蓝刺网「受伤」20%（同类加算）
        double PlayerDmgMult(Enemy e)
        {
            double m = 1 + player.CombatDamageBonus;
            if (e.WeakenTurns > 0) m += 0.2;
            return m;
        }

        // 造成伤害后触发的战斗部门效果（吸血 / 护甲回复）
        void ApplyOnHit(double damage)
        {
            if (damage <= 0) return;

            if (player.CombatLifeSteal > 0)
            {
                double before = player.Hp;
                player.Hp = Math.Min(player.MaxHp, player.Hp + damage * player.CombatLifeSteal);
                double actual = player.Hp - before;
                if (actual > 0.05)
                    Console.WriteLine(Style.Paint($"  吸血：恢复 {actual:F1} 生命", ColHeal));
            }

            if (player.CombatArmorRestore > 0 && player.ArmorItem != null && player.Armor > 0)
            {
                double before = player.Armor;
                player.Armor = Math.Min(player.MaxArmor, player.Armor + damage * player.CombatArmorRestore);
                double actual = player.Armor - before;
                if (actual > 0.05)
                    Console.WriteLine(Style.Paint($"  护甲回复：+{actual:F1}", ColArmor));
            }
        }

        double PlayerAttack(Enemy e, double multiplier = 1.0)
        {
            double baseDmg = player.Weapon?.Power ?? 5;
            int bulletTier = player.Weapon?.Tier ?? 0;
            double dmg = rng.Next((int)(baseDmg / 2), (int)(baseDmg + 1)) * multiplier * PlayerDmgMult(e);

            double enemyArmor = e.Armor;
            double final = CalculateDamage(dmg, bulletTier, e.ArmorTier, ref enemyArmor, out double armorLoss);
            e.Armor = enemyArmor;
            e.Hp -= final;

            string dmgStr = Style.Paint($"{final:F1}", ColDamage);
            if (armorLoss > 0)
                Console.WriteLine($"你造成 {dmgStr} 伤害，削减敌方 {Style.Paint($"{armorLoss:F1}", ColArmor)} 点护甲。");
            else
                Console.WriteLine($"你造成 {dmgStr} 伤害。");

            ApplyOnHit(final);
            return final;
        }

        void UseSkill(Skill skill, Enemy e)
        {
            switch (skill.Type)
            {
                case SkillType.Shield:
                    player.Shield += 120;
                    player.TempArmor += 80;
                    player.TempArmorTier = 6;
                    Console.WriteLine(Style.Paint("应急护盾：获得120点护盾 + 80点临时护甲（六级，每回合衰减20）。", ColShield));
                    break;

                case SkillType.Dragon:
                    {
                        // 60 真实伤害（无视护甲）+ 60 普通伤害，均可享受增伤乘区；敌人生命<70% 时额外 +75%（入乘区）
                        double bonus = player.CombatDamageBonus + (e.WeakenTurns > 0 ? 0.2 : 0);
                        if (e.Hp < e.MaxHp * 0.7) bonus += 0.75;
                        double mult = 1 + bonus;

                        double trueDmg = 60 * mult;
                        e.Hp -= trueDmg;
                        Console.WriteLine($"磁吸炸弹·真实伤害：{Style.Paint($"{trueDmg:F1}", ColDamage)}（无视护甲）");
                        ApplyOnHit(trueDmg);

                        if (e.Hp > 0)
                        {
                            double normBase = 60 * mult;
                            double a1 = e.Armor;
                            double f1 = CalculateDamage(normBase, 7, e.ArmorTier, ref a1, out double al1);
                            e.Armor = a1;
                            e.Hp -= f1;
                            if (al1 > 0)
                                Console.WriteLine($"磁吸炸弹·爆炸伤害：{Style.Paint($"{f1:F1}", ColDamage)}，削减敌方 {Style.Paint($"{al1:F1}", ColArmor)} 点护甲。");
                            else
                                Console.WriteLine($"磁吸炸弹·爆炸伤害：{Style.Paint($"{f1:F1}", ColDamage)}。");
                            ApplyOnHit(f1);
                        }
                    }
                    break;

                case SkillType.Net:
                    {
                        double netDmg = 60 * (1 + player.CombatDamageBonus);
                        double a2 = e.Armor;
                        double f2 = CalculateDamage(netDmg, 7, e.ArmorTier, ref a2, out double al2);
                        e.Armor = a2;
                        e.Hp -= f2;
                        e.SkipNextTurn = true;
                        bool newWeaken = e.WeakenTurns <= 0;
                        if (newWeaken) e.WeakenTurns = 3;   // 受伤状态 3 回合，不重复叠加
                        if (al2 > 0)
                            Console.WriteLine($"防爆刺网造成 {Style.Paint($"{f2:F1}", ColDamage)} 伤害，削减敌方 {Style.Paint($"{al2:F1}", ColArmor)} 点护甲，并束缚敌人。");
                        else
                            Console.WriteLine($"防爆刺网造成 {Style.Paint($"{f2:F1}", ColDamage)} 伤害，并束缚敌人。");
                        if (newWeaken)
                            Console.WriteLine(Style.Paint("  敌人陷入【受伤】状态（3回合，受到伤害+20%）", Style.BrightYellow));
                        else
                            Console.WriteLine(Style.Paint("  敌人已处于【受伤】状态（不叠加）", Style.BrightBlack));
                        ApplyOnHit(f2);
                    }
                    break;

                case SkillType.RapidFire:
                    Console.WriteLine("快速射击（三段）！");
                    PlayerAttack(e, 1.0);
                    if (e.Hp > 0) PlayerAttack(e, 0.7);
                    if (e.Hp > 0) PlayerAttack(e, 0.7);
                    break;

                case SkillType.FirstAid:
                    player.Hp = Math.Min(player.MaxHp, player.Hp + 60);
                    Console.WriteLine(Style.Paint("应急治疗：恢复60生命。", ColHeal));
                    break;

                case SkillType.Adrenaline:
                    foreach (var sk in player.Actives) sk.CurrentCooldown = 0;
                    Console.WriteLine("肾上腺素：所有主动技能冷却已清除！（不消耗行动）");
                    break;

                case SkillType.Incendiary:
                    {
                        double fireDmg = 75 * (1 + player.CombatDamageBonus);
                        double fa = e.Armor;
                        double ff = CalculateDamage(fireDmg, 7, e.ArmorTier, ref fa, out double fal);
                        e.Armor = fa;
                        e.Hp -= ff;
                        e.SkipNextTurn = true;   // 敌人灭火，失去下一回合
                        e.BurnTurns = 3;
                        e.BurnDamage = 15;
                        if (fal > 0)
                            Console.WriteLine($"燃烧弹造成 {Style.Paint($"{ff:F1}", ColDamage)} 伤害，削减敌方 {Style.Paint($"{fal:F1}", ColArmor)} 点护甲，敌人失去下一回合灭火！");
                        else
                            Console.WriteLine($"燃烧弹造成 {Style.Paint($"{ff:F1}", ColDamage)} 伤害，敌人失去下一回合灭火！");
                        ApplyOnHit(ff);
                    }
                    break;
            }
            skill.CurrentCooldown = skill.Cooldown;
        }

        // HUD 单行：我方状态（生命/护甲按比例着色，护盾灰色）
        void ShowPlayerBar()
        {
            string hp = Style.Paint($"{player.Hp:F1}/{player.MaxHp:F1}", ColorByRatio(player.Hp, player.MaxHp));
            string ar = Style.Paint($"{player.Armor:F1}/{player.MaxArmor:F1}", ColorByRatio(player.Armor, player.MaxArmor));
            string sh = Style.Paint($"{player.Shield:F1}", ColShield);
            string ta = player.TempArmor > 0 ? Style.Paint($"  临时护甲 {player.TempArmor:F1}(T{player.TempArmorTier})", Style.Cyan) : "";
            string iw = player.ArmorBreakDRTurns > 0 ? Style.Paint($"  [铁壁免伤 {player.ArmorBreakDRTurns}回合]", Style.BrightYellow) : "";
            Console.WriteLine($" 你  HP {hp}  护甲 {ar}  护盾 {sh}{ta}{iw}");
        }

        void ShowEnemyBar(Enemy e)
        {
            string hp = Style.Paint($"{e.Hp:F1}/{e.MaxHp:F1}", ColorByRatio(e.Hp, e.MaxHp));
            string ar = Style.Paint($"{e.Armor:F1}", e.Armor > 0 ? ColArmor : Style.BrightBlack);
            string next = e.SkipNextTurn
                ? Style.Paint("[下回合无法行动]", Style.BrightGreen)
                : Style.Paint("[下回合可行动]", Style.BrightRed);
            string weaken = e.WeakenTurns > 0 ? Style.Paint($" [受伤{e.WeakenTurns}]", Style.BrightYellow) : "";
            string burn = e.BurnTurns > 0 ? Style.Paint($" [燃烧{e.BurnTurns}]", Style.Red) : "";
            Console.WriteLine($" 敌  HP {hp}   护甲 {ar}   {next}{weaken}{burn}");
        }

        CombatResult Combat(Enemy e, int prev)
        {
            Console.WriteLine($"\n⚔ 遭遇 {Style.Paint(e.Name, Style.BrightRed)}！");
            Console.WriteLine($"  敌方 HP {e.MaxHp:F1} / 伤害 {e.Damage:F1} / 护甲 {e.MaxArmor:F1}(Tier{e.ArmorTier}) 子弹Tier{e.BulletTier}");
            Pause();

            bool deepBlue = player.HasPassive(SkillType.DeepBlue);
            bool reappear = player.HasPassive(SkillType.Reappear);
            double dmgTakenAccum = 0;      // 再现：血量伤害 + 0.3 × 护甲伤害
            int pendingExtraTurns = 0;
            int turn = 0;

            while (true)
            {
                turn++;

                int actionsThisTurn = 1 + pendingExtraTurns;
                pendingExtraTurns = 0;

                for (int actionIdx = 0; actionIdx < actionsThisTurn; actionIdx++)
                {
                    Console.WriteLine();
                    if (actionIdx > 0)
                        Console.WriteLine(Style.Paint("  ★★【再现·额外行动】插入行动，不消耗回合 ★★", ColExtra));
                    Console.WriteLine("──────────────────────────────────");
                    ShowPlayerBar();
                    ShowEnemyBar(e);
                    Console.WriteLine("──────────────────────────────────");

                    Console.WriteLine(" 1. 开火");
                    Console.WriteLine($" 2. 使用急救包（{player.RaidMedkits}）");
                    Console.WriteLine($" 3. 使用维修套件（{player.RaidRepairKits}）");
                    Console.WriteLine($" 4. 使用兴奋剂（{player.RaidStims}）");
                    for (int i = 0; i < player.Actives.Count; i++)
                    {
                        var s = player.Actives[i];
                        string cdText = s.CurrentCooldown > 0 ? $" (冷却中: {s.CurrentCooldown})" : "";
                        Console.WriteLine($" {5 + i}. 技能：{s.Name}{cdText}");
                        Console.WriteLine(Style.Paint($"      {s.Desc}", Style.BrightBlack));
                    }
                    Console.WriteLine($" {5 + player.Actives.Count}. 尝试脱离");
                    Console.Write("> ");

                    string cmd = ReadLine();
                    bool playerActed = false;

                    if (cmd == "1") { PlayerAttack(e); playerActed = true; }
                    else if (cmd == "2")
                    {
                        if (player.RaidMedkits > 0) { player.RaidMedkits--; player.Hp = Math.Min(player.MaxHp, player.Hp + 40); Console.WriteLine(Style.Paint("恢复40生命。", ColHeal)); playerActed = true; }
                        else Console.WriteLine("没有急救包！");
                    }
                    else if (cmd == "3")
                    {
                        if (player.RaidRepairKits > 0)
                        {
                            player.RaidRepairKits--;
                            if (player.ArmorItem != null) { player.Armor = Math.Min(player.MaxArmor, player.Armor + 50); Console.WriteLine(Style.Paint("护甲恢复50。", ColArmor)); }
                            else Console.WriteLine("没有护甲可修。");
                            playerActed = true;
                        }
                        else Console.WriteLine("没有维修套件！");
                    }
                    else if (cmd == "4")
                    {
                        if (player.RaidStims > 0)
                        {
                            player.RaidStims--;
                            foreach (var sk in player.Actives) sk.CurrentCooldown = 0;
                            Console.WriteLine("肾上腺素：所有主动技能冷却已清除！（不消耗行动）");
                            actionIdx--;
                            continue;
                        }
                        else Console.WriteLine("没有兴奋剂！");
                    }
                    else if (int.TryParse(cmd, out int skillIdx) && skillIdx >= 5 && skillIdx < 5 + player.Actives.Count)
                    {
                        var s = player.Actives[skillIdx - 5];
                        if (s.CurrentCooldown > 0) { Console.WriteLine($"{s.Name} 正在冷却中，无法使用！"); }
                        else
                        {
                            UseSkill(s, e);
                            // 肾上腺素不消耗行动（与兴奋剂一致）
                            if (s.Type == SkillType.Adrenaline) { actionIdx--; continue; }
                            playerActed = true;
                        }
                    }
                    else if (cmd == (5 + player.Actives.Count).ToString())
                    {
                        if (prev < 0) { Console.WriteLine("无路可退！"); actionIdx--; continue; }
                        if (rng.Next(100) < 55) { Console.WriteLine("脱离成功！"); Pause(); return CombatResult.Fled; }
                        Console.WriteLine("脱离失败！");
                        playerActed = true;
                    }

                    if (!playerActed) { actionIdx--; continue; }

                    if (actionIdx == 0 && player.HasPassive(SkillType.Overload) && turn % 2 == 0)
                    {
                        Console.WriteLine(Style.Paint("【超载】触发额外攻击！", Style.BrightYellow));
                        PlayerAttack(e, 1.25);
                    }

                    if (e.Hp <= 0) { Console.WriteLine(Style.Paint($"{e.Name} 倒下了。", Style.BrightGreen)); Pause(); return CombatResult.Win; }
                }

                // ===== 敌方行动 =====
                bool armorBrokeThisTurn = false;
                if (e.SkipNextTurn) { Console.WriteLine($"{e.Name} 被束缚/灭火，跳过回合。"); e.SkipNextTurn = false; }
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
                        Console.WriteLine($"燃烧持续：护甲吸收 {Style.Paint($"{bAbsorb:F1}", ColArmor)}，造成 {Style.Paint($"{bHp:F1}", ColDamage)} 伤害。");
                        if (e.Hp <= 0) { Console.WriteLine(Style.Paint($"{e.Name} 被烧尽了。", Style.BrightGreen)); Pause(); return CombatResult.Win; }
                    }

                    double edmg = rng.Next((int)(e.Damage / 2), (int)(e.Damage + 1));
                    double armorBefore = player.Armor;
                    double armorLossP;
                    double finalDmg;

                    // 临时护甲优先消耗（按指定 Tier 结算）
                    if (player.TempArmor > 0)
                    {
                        double ta = player.TempArmor;
                        finalDmg = CalculateDamage(edmg, e.BulletTier, player.TempArmorTier, ref ta, out armorLossP, player.ArmorLossMult);
                        player.TempArmor = ta;
                    }
                    else
                    {
                        double playerArmor = player.Armor;
                        finalDmg = CalculateDamage(edmg, e.BulletTier, player.ArmorItem?.Tier ?? 0, ref playerArmor, out armorLossP, player.ArmorLossMult);
                        player.Armor = playerArmor;
                    }

                    if (deepBlue) finalDmg *= 0.7;      // 深蓝：受伤 -30%
                    if (player.HasPassive(SkillType.Overload)) finalDmg *= 0.9;   // 超载：受伤 -10%
                    if (player.ArmorBreakDRTurns > 0) finalDmg *= 0.2;             // 铁壁：破甲后 80% 免伤
                    if (edmg > 0) finalDmg = Math.Max(1, finalDmg);

                    // 铁壁：护甲刚刚破碎 → 触发免伤
                    if (player.HasPassive(SkillType.IronWall) && armorBefore > 0 && player.Armor <= 0)
                    {
                        player.ArmorBreakDRTurns = 2;
                        armorBrokeThisTurn = true;
                        Console.WriteLine(Style.Paint("【铁壁】护甲破碎！获得80%免伤，持续2回合。", Style.BrightYellow));
                    }

                    if (player.Shield > 0)
                    {
                        double absorbed = Math.Min(player.Shield, finalDmg);
                        player.Shield -= absorbed;
                        finalDmg -= absorbed;
                        Console.WriteLine(Style.Paint($"护盾吸收 {absorbed:F1} 伤害。", ColShield));
                    }
                    player.Hp -= finalDmg;
                    string eDmgStr = Style.Paint($"{finalDmg:F1}", ColDamage);
                    if (armorLossP > 0)
                        Console.WriteLine($"{e.Name} 攻击，造成 {eDmgStr} 伤害，你的护甲损失 {Style.Paint($"{armorLossP:F1}", ColArmor)} 点。");
                    else
                        Console.WriteLine($"{e.Name} 攻击，造成 {eDmgStr} 伤害。");

                    // 再现：累计 = 血量伤害 + 0.3 × 护甲伤害
                    if (reappear)
                    {
                        dmgTakenAccum += finalDmg + armorLossP * 0.3;
                        while (dmgTakenAccum >= 50)
                        {
                            dmgTakenAccum -= 50;
                            pendingExtraTurns++;
                            Console.WriteLine(Style.Paint("【再现】累计受伤达到50，下回合获得一个额外行动！", ColExtra));
                        }
                    }
                }

                // 受伤状态递减
                if (e.WeakenTurns > 0) e.WeakenTurns--;

                // 铁壁免伤回合递减（破甲当回合不递减）
                if (player.ArmorBreakDRTurns > 0 && !armorBrokeThisTurn) player.ArmorBreakDRTurns--;

                // 临时护甲衰减
                if (player.TempArmor > 0) player.TempArmor = Math.Max(0, player.TempArmor - 20);

                if (player.Shield > 0) player.Shield = Math.Max(0, player.Shield - 20);
                foreach (var s in player.Actives) if (s.CurrentCooldown > 0) s.CurrentCooldown--;

                if (player.Hp <= 0) { Console.WriteLine(Style.Paint("\n你倒下了...", Style.BrightRed)); Pause(); return CombatResult.Dead; }
            }
        }
    }
}

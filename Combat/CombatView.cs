using System;
using System.Collections.Generic;
using static SearchFightExtract.ConsoleHelper;

namespace SearchFightExtract
{
    /// <summary>
    /// 战斗界面：把战斗状态渲染成终端文本并读入玩家指令。
    /// 只读游戏状态，不修改任何结算数据；战斗逻辑完全不感知终端。
    /// </summary>
    static class CombatView
    {
        const string Sep = "──────────────────────────────────";

        /// <summary>遭遇抬头：敌人登场时的属性速览。</summary>
        public static void EncounterHeader(Enemy e)
        {
            Console.WriteLine($"\n⚔ 遭遇 {Style.Paint(e.Name, Style.BrightRed)}！");
            Console.WriteLine($"  敌方 HP {e.MaxHp:F1} / 伤害 {e.Damage:F1} / 护甲 {e.MaxArmor:F1}(Tier{e.ArmorTier}) 子弹Tier{e.BulletTier}");
        }

        /// <summary>我方状态行：生命/护甲按比例着色，护盾灰色，附带各项免伤剩余回合。</summary>
        public static void PlayerBar(Player p)
        {
            string hp = Style.Paint($"{p.Hp:F1}/{p.MaxHp:F1}", Style.ColorByRatio(p.Hp, p.MaxHp));
            string ar = Style.Paint($"{p.Armor:F1}/{p.MaxArmor:F1}", Style.ColorByRatio(p.Armor, p.MaxArmor));
            string sh = Style.Paint($"{p.Shield:F1}", CombatStyle.Shield);
            string ta = p.TempArmor > 0 ? Style.Paint($"  临时护甲 {p.TempArmor:F1}(T{p.TempArmorTier})", Style.Cyan) : "";
            string iw = p.ArmorBreakDRTurns > 0 ? Style.Paint($"  [铁壁免伤 {p.ArmorBreakDRTurns}回合]", Style.BrightYellow) : "";
            string fa = p.FirstAidDRTurns > 0 ? Style.Paint($"  [治疗免伤 {p.FirstAidDRTurns}回合]", Style.BrightGreen) : "";
            Console.WriteLine($" 你  HP {hp}  护甲 {ar}  护盾 {sh}{ta}{iw}{fa}");
        }

        /// <summary>敌方状态行：护甲、下回合可否行动、受伤与燃烧剩余回合。</summary>
        public static void EnemyBar(Enemy e)
        {
            string hp = Style.Paint($"{e.Hp:F1}/{e.MaxHp:F1}", Style.ColorByRatio(e.Hp, e.MaxHp));
            string ar = Style.Paint($"{e.Armor:F1}", e.Armor > 0 ? CombatStyle.Armor : Style.BrightBlack);
            string next = e.SkipNextTurn
                ? Style.Paint("[下回合无法行动]", Style.BrightGreen)
                : Style.Paint("[下回合可行动]", Style.BrightRed);
            string weaken = e.WeakenTurns > 0 ? Style.Paint($" [受伤{e.WeakenTurns}]", Style.BrightYellow) : "";
            string burn = e.BurnTurns > 0 ? Style.Paint($" [燃烧{e.BurnTurns}]", Style.Red) : "";
            Console.WriteLine($" 敌  HP {hp}   护甲 {ar}   {next}{weaken}{burn}");
        }

        /// <summary>
        /// 画一帧战斗界面并读入指令。
        /// <paramref name="extraAction"/> 为 true 表示这是【再现】插入的额外行动。
        /// </summary>
        public static string DrawAndReadCommand(Player p, Enemy e, bool extraAction)
        {
            Console.WriteLine();
            if (extraAction)
                Console.WriteLine(Style.Paint("  ★★【再现·额外行动】插入行动，不消耗回合 ★★", CombatStyle.Extra));
            Console.WriteLine(Sep);
            PlayerBar(p);
            EnemyBar(e);
            Console.WriteLine(Sep);

            Console.WriteLine(" 1. 开火");
            Console.WriteLine($" 2. 使用急救包（{p.RaidMedkits}）");
            Console.WriteLine($" 3. 使用维修套件（{p.RaidRepairKits}）");
            Console.WriteLine($" 4. 使用兴奋剂（{p.RaidStims}）");
            for (int i = 0; i < p.Actives.Count; i++)
            {
                var s = p.Actives[i];
                string cdText = s.CurrentCooldown > 0 ? $" (冷却中: {s.CurrentCooldown})" : "";
                Console.WriteLine($" {5 + i}. 技能：{s.Name}{cdText}");
                Console.WriteLine(Style.Paint($"      {s.Desc}", Style.BrightBlack));
            }
            Console.WriteLine($" {5 + p.Actives.Count}. 尝试脱离");
            Console.Write("> ");
            return ReadLine();
        }

        /// <summary>打印结算层返回的文本行。</summary>
        public static void Lines(List<string> lines)
        {
            foreach (var line in lines) Console.WriteLine(line);
        }

        // ===== 指令反馈与战斗结局 =====

        public static void SkillOnCooldown(Skill s)
            => Console.WriteLine($"{s.Name} 正在冷却中，无法使用！");

        public static void NoEscapeRoute() => Console.WriteLine("无路可退！");
        public static void RetreatSucceeded() => Console.WriteLine("脱离成功！");
        public static void RetreatFailed() => Console.WriteLine("脱离失败！");

        public static void EnemyDefeated(Enemy e)
            => Console.WriteLine(Style.Paint($"{e.Name} 倒下了。", Style.BrightGreen));

        public static void PlayerDefeated()
            => Console.WriteLine(Style.Paint("\n你倒下了...", Style.BrightRed));
    }
}

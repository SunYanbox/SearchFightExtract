using System;
using System.Collections.Generic;
using static SearchFightExtract.ConsoleHelper;

namespace SearchFightExtract
{
    partial class Game
    {
        // 结算层返回的文本行统一由此打印
        static void PrintLines(List<string> lines)
        {
            foreach (var line in lines) Console.WriteLine(line);
        }

        // HUD 单行：我方状态（生命/护甲按比例着色，护盾灰色）
        void ShowPlayerBar()
        {
            string hp = Style.Paint($"{player.Hp:F1}/{player.MaxHp:F1}", ColorByRatio(player.Hp, player.MaxHp));
            string ar = Style.Paint($"{player.Armor:F1}/{player.MaxArmor:F1}", ColorByRatio(player.Armor, player.MaxArmor));
            string sh = Style.Paint($"{player.Shield:F1}", CombatStyle.Shield);
            string ta = player.TempArmor > 0 ? Style.Paint($"  临时护甲 {player.TempArmor:F1}(T{player.TempArmorTier})", Style.Cyan) : "";
            string iw = player.ArmorBreakDRTurns > 0 ? Style.Paint($"  [铁壁免伤 {player.ArmorBreakDRTurns}回合]", Style.BrightYellow) : "";
            string fa = player.FirstAidDRTurns > 0 ? Style.Paint($"  [治疗免伤 {player.FirstAidDRTurns}回合]", Style.BrightGreen) : "";
            Console.WriteLine($" 你  HP {hp}  护甲 {ar}  护盾 {sh}{ta}{iw}{fa}");
        }

        void ShowEnemyBar(Enemy e)
        {
            string hp = Style.Paint($"{e.Hp:F1}/{e.MaxHp:F1}", ColorByRatio(e.Hp, e.MaxHp));
            string ar = Style.Paint($"{e.Armor:F1}", e.Armor > 0 ? CombatStyle.Armor : Style.BrightBlack);
            string next = e.SkipNextTurn
                ? Style.Paint("[下回合无法行动]", Style.BrightGreen)
                : Style.Paint("[下回合可行动]", Style.BrightRed);
            string weaken = e.WeakenTurns > 0 ? Style.Paint($" [受伤{e.WeakenTurns}]", Style.BrightYellow) : "";
            string burn = e.BurnTurns > 0 ? Style.Paint($" [燃烧{e.BurnTurns}]", Style.Red) : "";
            Console.WriteLine($" 敌  HP {hp}   护甲 {ar}   {next}{weaken}{burn}");
        }

        // 战斗主循环：只负责回合调度与输入分发，结算与文本都在 CombatActions 里
        CombatResult Combat(Enemy e, int prev)
        {
            Console.WriteLine($"\n⚔ 遭遇 {Style.Paint(e.Name, Style.BrightRed)}！");
            Console.WriteLine($"  敌方 HP {e.MaxHp:F1} / 伤害 {e.Damage:F1} / 护甲 {e.MaxArmor:F1}(Tier{e.ArmorTier}) 子弹Tier{e.BulletTier}");
            Pause();

            var st = new CombatState();

            while (true)
            {
                st.Turn++;

                int actionsThisTurn = 1 + st.PendingExtraTurns;
                st.PendingExtraTurns = 0;

                for (int actionIdx = 0; actionIdx < actionsThisTurn; actionIdx++)
                {
                    Console.WriteLine();
                    if (actionIdx > 0)
                        Console.WriteLine(Style.Paint("  ★★【再现·额外行动】插入行动，不消耗回合 ★★", CombatStyle.Extra));
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

                    if (cmd == "1") { PrintLines(CombatActions.PlayerAttack(player, e, rng)); playerActed = true; }
                    else if (cmd == "2")
                    {
                        var log = new List<string>();
                        playerActed = CombatActions.TryUseMedkit(player, log);
                        PrintLines(log);
                    }
                    else if (cmd == "3")
                    {
                        var log = new List<string>();
                        playerActed = CombatActions.TryUseRepairKit(player, log);
                        PrintLines(log);
                    }
                    else if (cmd == "4")
                    {
                        var log = new List<string>();
                        bool used = CombatActions.TryUseStim(player, log);
                        PrintLines(log);
                        if (used) { actionIdx--; continue; }   // 兴奋剂不消耗行动
                    }
                    else if (int.TryParse(cmd, out int skillIdx) && skillIdx >= 5 && skillIdx < 5 + player.Actives.Count)
                    {
                        var s = player.Actives[skillIdx - 5];
                        if (s.CurrentCooldown > 0) { Console.WriteLine($"{s.Name} 正在冷却中，无法使用！"); }
                        else
                        {
                            PrintLines(CombatActions.UseSkill(player, e, s, rng));
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

                    if (actionIdx == 0 && player.HasPassive(SkillType.Overload) && st.Turn % 2 == 0)
                        PrintLines(CombatActions.OverloadAttack(player, e, rng));

                    if (e.Hp <= 0) { Console.WriteLine(Style.Paint($"{e.Name} 倒下了。", Style.BrightGreen)); Pause(); return CombatResult.Win; }
                }

                // ===== 敌方行动 + 回合末结算 =====
                PrintLines(CombatActions.EnemyTurn(st, player, e, rng));

                // 敌人只可能被燃烧烧死，且此时 EnemyTurn 已提前返回
                if (e.Hp <= 0) { Pause(); return CombatResult.Win; }

                if (player.Hp <= 0) { Console.WriteLine(Style.Paint("\n你倒下了...", Style.BrightRed)); Pause(); return CombatResult.Dead; }
            }
        }
    }
}

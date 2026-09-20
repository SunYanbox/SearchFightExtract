using System;
using System.Collections.Generic;
using static SearchFightExtract.ConsoleHelper;

namespace SearchFightExtract
{
    partial class Game
    {
        // 战斗主循环：只负责回合调度与输入分发，结算在 CombatActions、渲染在 CombatView
        CombatResult Combat(Enemy e, int prev)
        {
            CombatView.EncounterHeader(e);
            Pause();

            var st = new CombatState();

            while (true)
            {
                st.Turn++;

                int actionsThisTurn = 1 + st.PendingExtraTurns;
                st.PendingExtraTurns = 0;

                for (int actionIdx = 0; actionIdx < actionsThisTurn; actionIdx++)
                {
                    string cmd = CombatView.DrawAndReadCommand(player, e, actionIdx > 0);
                    bool playerActed = false;

                    if (cmd == "1") { CombatView.Lines(CombatActions.PlayerAttack(player, e, rng)); playerActed = true; }
                    else if (cmd == "2")
                    {
                        var log = new List<string>();
                        playerActed = CombatActions.TryUseMedkit(player, log);
                        CombatView.Lines(log);
                    }
                    else if (cmd == "3")
                    {
                        var log = new List<string>();
                        playerActed = CombatActions.TryUseRepairKit(player, log);
                        CombatView.Lines(log);
                    }
                    else if (cmd == "4")
                    {
                        var log = new List<string>();
                        bool used = CombatActions.TryUseStim(player, log);
                        CombatView.Lines(log);
                        if (used) { actionIdx--; continue; }   // 兴奋剂不消耗行动
                    }
                    else if (int.TryParse(cmd, out int skillIdx) && skillIdx >= 5 && skillIdx < 5 + player.Actives.Count)
                    {
                        var s = player.Actives[skillIdx - 5];
                        if (s.CurrentCooldown > 0) CombatView.SkillOnCooldown(s);
                        else
                        {
                            CombatView.Lines(CombatActions.UseSkill(player, e, s, rng));
                            // 肾上腺素不消耗行动（与兴奋剂一致）
                            if (s.Type == SkillType.Adrenaline) { actionIdx--; continue; }
                            playerActed = true;
                        }
                    }
                    else if (cmd == (5 + player.Actives.Count).ToString())
                    {
                        if (prev < 0) { CombatView.NoEscapeRoute(); actionIdx--; continue; }
                        if (rng.Next(100) < 55) { CombatView.RetreatSucceeded(); Pause(); return CombatResult.Fled; }
                        CombatView.RetreatFailed();
                        playerActed = true;
                    }

                    if (!playerActed) { actionIdx--; continue; }

                    if (actionIdx == 0 && player.HasPassive(SkillType.Overload) && st.Turn % 2 == 0)
                        CombatView.Lines(CombatActions.OverloadAttack(player, e, rng));

                    if (e.Hp <= 0) { CombatView.EnemyDefeated(e); Pause(); return CombatResult.Win; }
                }

                // ===== 敌方行动 + 回合末结算 =====
                CombatView.Lines(CombatActions.EnemyTurn(st, player, e, rng));

                // 敌人只可能被燃烧烧死，且此时 EnemyTurn 已提前返回
                if (e.Hp <= 0) { Pause(); return CombatResult.Win; }

                if (player.Hp <= 0) { CombatView.PlayerDefeated(); Pause(); return CombatResult.Dead; }
            }
        }
    }
}

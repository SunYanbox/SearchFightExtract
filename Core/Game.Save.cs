using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using static SearchFightExtract.ConsoleHelper;

namespace SearchFightExtract
{
    partial class Game
    {
        static readonly SkillType[] PassivePool = { SkillType.Overload, SkillType.BeeMedic, SkillType.DeepBlue, SkillType.IronWall, SkillType.Scavenger, SkillType.Nimble, SkillType.StormCloud, SkillType.Reappear };
        static readonly SkillType[] ActivePool = { SkillType.Shield, SkillType.Dragon, SkillType.Net, SkillType.RapidFire, SkillType.FirstAid, SkillType.Adrenaline, SkillType.Incendiary };

        // ===== 存档选择 =====
        void SelectSave()
        {
            try
            {
                Directory.CreateDirectory(SaveDir);
                // 兼容旧版单档：若 saves/ 下尚无「默认」，把根目录的 save.json 迁入
                var legacy = "save.json";
                var legacyTarget = Path.Combine(SaveDir, "默认.json");
                if (File.Exists(legacy) && !File.Exists(legacyTarget))
                    File.Move(legacy, legacyTarget);
            }
            catch { }

            while (true)
            {
                Clear();
                Console.WriteLine("════════ 存 档 选 择 ════════\n");
                var files = ListSaves();
                if (files.Count == 0) Console.WriteLine("  （暂无存档）");
                for (int i = 0; i < files.Count; i++)
                {
                    Console.WriteLine($"  {i + 1}. {files[i]}");
                    var sum = SaveSummary(files[i]);
                    if (sum != "") Console.WriteLine(Style.Paint($"       {sum}", Style.BrightBlack));
                }
                Console.WriteLine("\n  输入编号载入 / n 新建存档 / d 删除存档 / 0 退出");
                Console.Write("> ");
                var cmd = ReadLine();

                if (cmd == "0") { Environment.Exit(0); }
                if (cmd == "n" || cmd == "N") { NewSave(); return; }
                if (cmd == "d" || cmd == "D") { DeleteSave(files); continue; }
                if (int.TryParse(cmd, out int idx) && idx >= 1 && idx <= files.Count)
                {
                    SaveName = files[idx - 1];
                    player = new Player();
                    LoadGame();
                    return;
                }
            }
        }

        // 读取存档摘要（不解锁完整加载流程）
        string SaveSummary(string name)
        {
            try
            {
                var path = Path.Combine(SaveDir, name + ".json");
                var json = File.ReadAllText(path);
                var p = JsonSerializer.Deserialize<Player>(json);
                if (p == null) return "";
                return $"资金:{p.Money}  材料:{p.Materials}  天赋Lv{p.TalentLevel}  主动Lv{p.ActiveSkillLevel}  战斗Lv{p.CombatLevel}  生存Lv{p.SurvivalLevel}";
            }
            catch { return "(读取失败)"; }
        }

        List<string> ListSaves()
        {
            try
            {
                if (!Directory.Exists(SaveDir)) return new List<string>();
                return Directory.GetFiles(SaveDir, "*.json")
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .OrderBy(x => x, StringComparer.CurrentCulture)
                    .ToList();
            }
            catch { return new List<string>(); }
        }

        void NewSave()
        {
            while (true)
            {
                Clear();
                Console.WriteLine("════════ 新 建 存 档 ════════\n");
                Console.Write("请输入存档名（支持中文，直接回车取消）> ");
                var name = ReadLine();
                if (name == "") return;
                if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    Console.WriteLine("名称包含非法字符（\\ / : * ? \" < > |），请重试。");
                    Pause();
                    continue;
                }
                var path = Path.Combine(SaveDir, name + ".json");
                if (File.Exists(path))
                {
                    Console.Write("该存档已存在，覆盖？(y/n) > ");
                    if (ReadLine().ToLower() != "y") continue;
                }
                SaveName = name;
                player = new Player();
                LoadGame();
                return;
            }
        }

        void DeleteSave(List<string> files)
        {
            Clear();
            Console.WriteLine("════════ 删 除 存 档 ════════\n");
            if (files.Count == 0) { Console.WriteLine("暂无存档。"); Pause(); return; }
            for (int i = 0; i < files.Count; i++) Console.WriteLine($"  {i + 1}. {files[i]}");
            Console.WriteLine("\n  0. 取消");
            Console.Write("> ");
            if (!int.TryParse(ReadLine(), out int idx) || idx < 1 || idx > files.Count) return;
            Console.Write($"确认删除「{files[idx - 1]}」？(y/n) > ");
            if (ReadLine().ToLower() != "y") return;
            try { File.Delete(Path.Combine(SaveDir, files[idx - 1] + ".json")); Console.WriteLine("已删除。"); }
            catch { Console.WriteLine("删除失败。"); }
            Pause();
        }

        void SaveGame()
        {
            try
            {
                Directory.CreateDirectory(SaveDir);
                var json = JsonSerializer.Serialize(player, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SavePath, json);
            }
            catch { }
        }

        void LoadGame()
        {
            if (File.Exists(SavePath))
            {
                try
                {
                    var json = File.ReadAllText(SavePath);
                    var loaded = JsonSerializer.Deserialize<Player>(json);
                    if (loaded != null) player = loaded;
                }
                catch { }
            }
            if (player.Passives == null) player.Passives = new List<Skill>();
            if (player.Actives == null) player.Actives = new List<Skill>();
            TrimSkills();
            // 以最新定义重建技能，避免旧存档残留过期的名称/描述/冷却
            player.Passives = player.Passives.Select(s => new Skill(s.Type)).ToList();
            player.Actives = player.Actives.Select(s => new Skill(s.Type)).ToList();
            player.RecomputeStats();
            if (player.Passives.Count == 0 && player.Actives.Count == 0)
                ChooseSkills();
        }

        // 按部门上限裁剪技能，并过滤无效技能
        void TrimSkills()
        {
            player.Passives = player.Passives.Where(s => Skill.IsPassive(s.Type)).Take(player.PassiveCap).ToList();
            player.Actives = player.Actives.Where(s => !Skill.IsPassive(s.Type)).Take(player.ActiveCap).ToList();
        }

        void ChooseSkills()
        {
            Console.Clear();
            Console.WriteLine("════════ 选 择 技 能 ════════");

            player.Passives = ChooseFrom($"被动技能（至多{player.PassiveCap}个）", PassivePool, player.PassiveCap, player.Passives);
            player.Actives = ChooseFrom($"主动技能（至多{player.ActiveCap}个）", ActivePool, player.ActiveCap, player.Actives);

            player.RecomputeStats();
            SaveGame();
        }

        // current 为 null：首次选择（回车跳过）；current 非 null：重选（回车保留当前）
        List<Skill> ChooseFrom(string title, SkillType[] pool, int max, List<Skill> current = null)
        {
            Console.WriteLine($"\n{title}（输入数字，空格分隔）：");
            for (int i = 0; i < pool.Length; i++)
            {
                var s = new Skill(pool[i]);
                bool isCur = current != null && current.Any(x => x.Type == pool[i]);
                Console.WriteLine($"  {i + 1}. {s.Name} - {s.Desc}{(isCur ? "  [当前]" : "")}");
            }
            Console.WriteLine(current != null ? "  （直接回车保留当前技能）" : "  （直接回车跳过）");
            Console.Write("> ");
            var input = Console.ReadLine() ?? "";

            // 回车保留当前：同时按最新定义重建，保证描述/冷却不过期
            if (current != null && input.Trim() == "")
                return current.Select(s => new Skill(s.Type)).ToList();

            var parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var chosen = new List<Skill>();
            foreach (var p in parts)
            {
                if (int.TryParse(p, out int idx) && idx >= 1 && idx <= pool.Length)
                {
                    var st = pool[idx - 1];
                    if (!chosen.Any(s => s.Type == st))
                        chosen.Add(new Skill(st));
                }
                if (chosen.Count >= max) break;
            }

            // 无效输入同样保留当前（重建），避免误清空
            if (current != null && chosen.Count == 0)
                return current.Select(s => new Skill(s.Type)).ToList();

            return chosen;
        }

        void SkillMenu()
        {
            while (true)
            {
                Clear();
                Console.WriteLine("════════ 技 能 管 理 ════════\n");
                Console.WriteLine($"  被动（{player.Passives.Count}/{player.PassiveCap}）: {(player.Passives.Count == 0 ? "无" : string.Join(", ", player.Passives.Select(s => s.Name)))}");
                Console.WriteLine($"  主动（{player.Actives.Count}/{player.ActiveCap}）: {(player.Actives.Count == 0 ? "无" : string.Join(", ", player.Actives.Select(s => s.Name)))}");
                Console.WriteLine();
                Console.WriteLine("  1. 重选被动技能");
                Console.WriteLine("  2. 重选主动技能");
                Console.WriteLine("  3. 重选全部技能");
                Console.WriteLine("  0. 返回");
                Console.Write("\n> ");
                switch (ReadLine())
                {
                    case "1":
                        player.Passives = ChooseFrom($"被动技能（至多{player.PassiveCap}个）", PassivePool, player.PassiveCap, player.Passives);
                        player.RecomputeStats();
                        SaveGame();
                        break;
                    case "2":
                        player.Actives = ChooseFrom($"主动技能（至多{player.ActiveCap}个）", ActivePool, player.ActiveCap, player.Actives);
                        player.RecomputeStats();
                        SaveGame();
                        break;
                    case "3":
                        ChooseSkills();
                        break;
                    case "0":
                        return;
                }
            }
        }
    }
}

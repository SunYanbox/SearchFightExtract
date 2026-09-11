namespace SearchFightExtract
{
    partial class Game
    {
        void InitShopAndLoot()
        {
            shopStock.Add(new Item("手枪", ItemType.Weapon, 300, 2, 10, 1));
            shopStock.Add(new Item("冲锋枪", ItemType.Weapon, 900, 3, 16, 2));
            shopStock.Add(new Item("突击步枪", ItemType.Weapon, 2200, 4, 26, 3));
            shopStock.Add(new Item("狙击步枪", ItemType.Weapon, 4500, 5, 45, 4));
            shopStock.Add(new Item("精确射手步枪", ItemType.Weapon, 7500, 4, 60, 5));
            shopStock.Add(new Item("重型机枪", ItemType.Weapon, 6800, 6, 55, 5));
            shopStock.Add(new Item("反器材狙击枪", ItemType.Weapon, 12000, 6, 85, 6));
            shopStock.Add(new Item("电磁轨道枪", ItemType.Weapon, 20000, 7, 120, 7));

            shopStock.Add(new Item("一级防弹衣", ItemType.Armor, 400, 3, 20, 1));
            shopStock.Add(new Item("二级防弹衣", ItemType.Armor, 900, 3, 35, 2));
            shopStock.Add(new Item("三级防弹衣", ItemType.Armor, 1600, 4, 45, 3));
            shopStock.Add(new Item("四级防弹衣", ItemType.Armor, 3000, 5, 70, 4));
            shopStock.Add(new Item("五级防弹衣", ItemType.Armor, 6000, 5, 100, 5));
            shopStock.Add(new Item("六级防弹衣", ItemType.Armor, 10000, 6, 140, 6));

            shopStock.Add(new Item("轻型胸挂", ItemType.Rig, 500, 1, 6));
            shopStock.Add(new Item("战术胸挂", ItemType.Rig, 1500, 1, 12));
            shopStock.Add(new Item("小型背包", ItemType.Backpack, 800, 2, 10));
            shopStock.Add(new Item("中型背包", ItemType.Backpack, 2000, 2, 20));
            shopStock.Add(new Item("大型背包", ItemType.Backpack, 5000, 3, 35));

            shopStock.Add(new Item("急救包", ItemType.Medkit, 100, 1, 40));
            shopStock.Add(new Item("装备维修套件", ItemType.RepairKit, 150, 1, 50));
            shopStock.Add(new Item("肾上腺素", ItemType.Stim, 800, 1));

            lootTable.Add((new Item("螺丝钉", ItemType.Loot, 60, 1), 12));
            lootTable.Add((new Item("罐头", ItemType.Loot, 150, 1), 10));
            lootTable.Add((new Item("旧手机", ItemType.Loot, 320, 1), 12));
            lootTable.Add((new Item("工具箱", ItemType.Loot, 480, 3), 8));
            lootTable.Add((new Item("金链子", ItemType.Loot, 900, 1), 5));
            lootTable.Add((new Item("军用电池", ItemType.Loot, 1100, 2), 5));
            lootTable.Add((new Item("显卡", ItemType.Loot, 1500, 2), 3));
            lootTable.Add((new Item("保险箱钥匙", ItemType.Loot, 2600, 1), 1));
            lootTable.Add((new Item("黄金骷髅", ItemType.Loot, 5000, 2), 1));
            lootTable.Add((new Item("机密文件", ItemType.Loot, 3500, 1), 2));
            lootTable.Add((new Item("急救包", ItemType.Medkit, 100, 1, 40), 10));
            lootTable.Add((new Item("装备维修套件", ItemType.RepairKit, 150, 1, 50), 5));
            lootTable.Add((new Item("肾上腺素", ItemType.Stim, 800, 1), 2));
            lootTable.Add((new Item("轻型胸挂", ItemType.Rig, 500, 1, 6), 4));
            lootTable.Add((new Item("战术胸挂", ItemType.Rig, 1500, 1, 12), 2));
            lootTable.Add((new Item("小型背包", ItemType.Backpack, 800, 2, 10), 3));
            lootTable.Add((new Item("中型背包", ItemType.Backpack, 2000, 2, 20), 1));
            lootTable.Add((new Item("手枪", ItemType.Weapon, 300, 2, 10, 1), 5));
            lootTable.Add((new Item("冲锋枪", ItemType.Weapon, 900, 3, 16, 2), 3));
            lootTable.Add((new Item("突击步枪", ItemType.Weapon, 2200, 4, 26, 3), 1));
            lootTable.Add((new Item("狙击步枪", ItemType.Weapon, 4500, 5, 45, 4), 1));
            lootTable.Add((new Item("精确射手步枪", ItemType.Weapon, 7500, 4, 60, 5), 1));
            lootTable.Add((new Item("反器材狙击枪", ItemType.Weapon, 12000, 6, 85, 6), 1));
            lootTable.Add((new Item("一级防弹衣", ItemType.Armor, 400, 3, 20, 1), 4));
            lootTable.Add((new Item("二级防弹衣", ItemType.Armor, 900, 3, 35, 2), 2));
            lootTable.Add((new Item("三级防弹衣", ItemType.Armor, 1600, 4, 45, 3), 1));
            lootTable.Add((new Item("四级防弹衣", ItemType.Armor, 3000, 5, 70, 4), 1));
            lootTable.Add((new Item("五级防弹衣", ItemType.Armor, 6000, 5, 100, 5), 1));
        }
    }
}

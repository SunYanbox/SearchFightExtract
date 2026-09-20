namespace SearchFightExtract
{
    /// <summary>
    /// 战斗文本的语义配色。只声明「这段文字是什么含义」，具体转义码由 <see cref="Style"/> 提供。
    /// 结算层拼消息时取用，渲染层画 HUD 时同样取用，保证同一语义处处同色。
    /// </summary>
    static class CombatStyle
    {
        public const string Damage = Style.BrightRed;    // 造成伤害
        public const string Heal = Style.Green;          // 治疗
        public const string Shield = Style.BrightBlack;  // 护盾抵消
        public const string Armor = Style.Cyan;          // 护甲抵消/回复（青色，便于辨认）
        public const string Extra = Style.BrightCyan;    // 额外回合
    }
}

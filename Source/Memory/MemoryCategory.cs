using System;

namespace Ustas.RimAI.Communication.Memory
{

    public enum MemoryLayer
    {
        Active,        
        Situational,   
        EventLog,      
        Archive        
    }

    public enum MemoryType
    {
        Conversation,  
        [Obsolete("互动记忆已废弃，保留此枚举值仅为兼容旧存档")]
        Interaction,   
        Action,        
        Observation,   
        Event,         
        Emotion,       
        Relationship,  
        Internal       
    }

    public static class MemoryTags
    {
        public const string 开心 = "开心";
        public const string 悲伤 = "悲伤";
        public const string 愤怒 = "愤怒";
        public const string 焦虑 = "焦虑";
        public const string 平静 = "平静";

        public const string 战斗 = "战斗";
        public const string 袭击 = "袭击";
        public const string 受伤 = "受伤";
        public const string 死亡 = "死亡";
        public const string 完成任务 = "完成任务";

        public const string 闲聊 = "闲聊";
        public const string 深谈 = "深谈";
        public const string 争吵 = "争吵";
        public const string 表白 = "表白";
        public const string 友好 = "友好";
        public const string 敌对 = "敌对";

        public const string 烹饪 = "烹饪";
        public const string 建造 = "建造";
        public const string 种植 = "种植";
        public const string 采矿 = "采矿";
        public const string 研究 = "研究";
        public const string 医疗 = "医疗";

        public const string 重要 = "重要";
        public const string 紧急 = "紧急";
        public const string 深度归档 = "深度归档";
        public const string 用户编辑 = "用户编辑";
    }

}

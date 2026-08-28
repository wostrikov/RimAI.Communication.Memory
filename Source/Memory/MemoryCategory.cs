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
        [Obsolete("Памʼять взаємодій виведена з ужитку; це значення лишається тільки для сумісності зі старими збереженнями")]
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
        public const string 开心 = "радісно";
        public const string 悲伤 = "смуток";
        public const string 愤怒 = "лють";
        public const string 焦虑 = "неспокій";
        public const string 平静 = "спокій";

        public const string 战斗 = "бій";
        public const string 袭击 = "напад";
        public const string 受伤 = "поранення";
        public const string 死亡 = "смерть";
        public const string 完成任务 = "Завершити завдання";

        public const string 闲聊 = "балачка";
        public const string 深谈 = "серйозна розмова";
        public const string 争吵 = "суперечка";
        public const string 表白 = "освідчитися";
        public const string 友好 = "дружній";
        public const string 敌对 = "ворожий";

        public const string 烹饪 = "куховарство";
        public const string 建造 = "будувати";
        public const string 种植 = "садити";
        public const string 采矿 = "видобуток";
        public const string 研究 = "дослідження";
        public const string 医疗 = "медицина";

        public const string 重要 = "важливе";
        public const string 紧急 = "термінове";
        public const string 深度归档 = "Глибоке архівування";
        public const string 用户编辑 = "Редаговано користувачем";
    }

}

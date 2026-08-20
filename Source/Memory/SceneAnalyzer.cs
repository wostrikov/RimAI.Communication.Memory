using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Ustas.RimAI.Communication.Memory
{
    public enum SceneType
    {
        Combat,        
        Social,        
        Work,          
        Medical,       
        Research,      
        Event,         
        Neutral        
    }
    
    public static class SceneAnalyzer
    {
        private static readonly Dictionary<SceneType, List<string>> SceneKeywords = new Dictionary<SceneType, List<string>>
        {
            {
                SceneType.Combat, new List<string>
                {
                    "袭击", "raid", "攻击", "attack", "战斗", "combat", "fight",
                    "敌人", "enemy", "入侵", "invasion", "防御", "defense",
                    "受伤", "injured", "伤势", "wound", "死亡", "death", "died", "killed",
                    "流血", "bleeding", "倒下", "downed", "昏迷", "unconscious",
                    "武器", "weapon", "枪", "gun", "射击", "shoot", "爆炸", "explosion",
                    "掩体", "cover", "撤退", "retreat", "增援", "reinforcement"
                }
            },
            {
                SceneType.Social, new List<string>
                {
                    "聊天", "chat", "talk", "对话", "conversation", "说", "said",
                    "告诉", "told", "询问", "asked", "回答", "replied",
                    "喜欢", "like", "爱", "love", "讨厌", "hate", "朋友", "friend",
                    "恋人", "lover", "配偶", "spouse", "关系", "relationship",
                    "友谊", "friendship", "争吵", "argument", "和解", "reconcile",
                    "开心", "happy", "快乐", "joyful", "悲伤", "sad", "难过", "upset",
                    "愤怒", "angry", "生气", "mad", "焦虑", "anxious", "心情", "mood",
                    "感觉", "feel", "情绪", "emotion"
                }
            },
            {
                SceneType.Work, new List<string>
                {
                    "建造", "construct", "建筑", "building", "制作", "craft", "制造", "manufacture",
                    "修理", "repair", "拆除", "deconstruct",
                    "种植", "plant", "收获", "harvest", "农作物", "crop", "田地", "field",
                    "采矿", "mining", "挖掘", "dig", "搬运", "haul", "运输", "transport",
                    "仓库", "storage", "清洁", "clean",
                    "烹饪", "cook", "cooking", "做饭", "meal", "食物", "food"
                }
            },
            {
                SceneType.Medical, new List<string>
                {
                    "治疗", "treat", "医疗", "medical", "手术", "surgery", "操作", "operation",
                    "包扎", "bandage", "照顾", "tend", "护理", "care",
                    "疾病", "disease", "illness", "感染", "infection", "发烧", "fever",
                    "疼痛", "pain", "残疾", "disability", "健康", "health",
                    "恢复", "recover", "痊愈", "heal", "康复", "rehabilitation"
                }
            },
            {
                SceneType.Research, new List<string>
                {
                    "研究", "research", "科技", "technology", "发明", "invention",
                    "突破", "breakthrough", "发现", "discovery", "实验", "experiment",
                    "学习", "learn", "训练", "train", "练习", "practice",
                    "技能", "skill", "提升", "improve", "掌握", "master",
                    "知识", "knowledge", "教导", "teach"
                }
            },
            {
                SceneType.Event, new List<string>
                {
                    "婚礼", "wedding", "结婚", "marry", "订婚", "engaged",
                    "生日", "birthday", "庆祝", "celebrate", "派对", "party",
                    "葬礼", "funeral", "埋葬", "burial", "纪念", "memorial",
                    "仪式", "ceremony", "节日", "festival", "活动", "event"
                }
            }
        };
        
        private static readonly Dictionary<SceneType, float> ScenePriority = new Dictionary<SceneType, float>
        {
            { SceneType.Combat, 1.0f },     
            { SceneType.Medical, 0.9f },    
            { SceneType.Event, 0.85f },     
            { SceneType.Social, 0.7f },     
            { SceneType.Research, 0.6f },   
            { SceneType.Work, 0.5f },       
            { SceneType.Neutral, 0.3f }     
        };
        
        public static SceneAnalysisResult AnalyzeScene(string context)
        {
            if (string.IsNullOrEmpty(context))
            {
                return new SceneAnalysisResult
                {
                    PrimaryScene = SceneType.Neutral,
                    SceneScores = new Dictionary<SceneType, float> { { SceneType.Neutral, 1.0f } },
                    Confidence = 0f
                };
            }
            
            string lowerContext = context.ToLower();
            
            var sceneScores = new Dictionary<SceneType, float>();
            
            foreach (var sceneKvp in SceneKeywords)
            {
                SceneType scene = sceneKvp.Key;
                List<string> keywords = sceneKvp.Value;
                
                int matchCount = keywords.Count(kw => lowerContext.Contains(kw.ToLower()));
                
                float score = matchCount > 0 ? (float)matchCount / keywords.Count : 0f;
                
                score *= ScenePriority[scene];
                
                if (score > 0)
                {
                    sceneScores[scene] = score;
                }
            }
            
            if (sceneScores.Count == 0)
            {
                return new SceneAnalysisResult
                {
                    PrimaryScene = SceneType.Neutral,
                    SceneScores = new Dictionary<SceneType, float> { { SceneType.Neutral, 1.0f } },
                    Confidence = 0f
                };
            }
            
            float totalScore = sceneScores.Values.Sum();
            var normalizedScores = sceneScores.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value / totalScore
            );
            
            var primaryScene = normalizedScores.OrderByDescending(kvp => kvp.Value).First();
            
            float confidence = primaryScene.Value;
            
            return new SceneAnalysisResult
            {
                PrimaryScene = primaryScene.Key,
                SceneScores = normalizedScores,
                Confidence = confidence
            };
        }
        
        public static DynamicWeights GetDynamicWeights(SceneType scene, float confidence = 1.0f)
        {
            var weights = new DynamicWeights();
            
            switch (scene)
            {
                case SceneType.Combat:
                    weights.TimeDecay = 0.8f;         
                    weights.Importance = 0.5f;        
                    weights.KeywordMatch = 0.4f;      
                    weights.RelationshipBonus = 0.1f; 
                    weights.RecencyWindow = 15000;    
                    break;
                
                case SceneType.Social:
                    weights.TimeDecay = 0.05f;        
                    weights.Importance = 0.2f;        
                    weights.KeywordMatch = 0.25f;     
                    weights.RelationshipBonus = 0.6f; 
                    weights.RecencyWindow = 1800000;  
                    break;
                
                case SceneType.Work:
                    weights.TimeDecay = 0.3f;
                    weights.Importance = 0.3f;
                    weights.KeywordMatch = 0.35f;
                    weights.RelationshipBonus = 0.15f;
                    weights.RecencyWindow = 180000;   
                    break;
                
                case SceneType.Medical:
                    weights.TimeDecay = 0.15f;        
                    weights.Importance = 0.45f;
                    weights.KeywordMatch = 0.35f;
                    weights.RelationshipBonus = 0.2f;
                    weights.RecencyWindow = 420000;   
                    break;
                
                case SceneType.Research:
                    weights.TimeDecay = 0.02f;        
                    weights.Importance = 0.4f;
                    weights.KeywordMatch = 0.4f;
                    weights.RelationshipBonus = 0.1f;
                    weights.RecencyWindow = 3600000;  
                    break;
                
                case SceneType.Event:
                    weights.TimeDecay = 0.1f;         
                    weights.Importance = 0.5f;
                    weights.KeywordMatch = 0.3f;
                    weights.RelationshipBonus = 0.4f;
                    weights.RecencyWindow = 900000;   
                    break;
                
                case SceneType.Neutral:
                default:
                    weights.TimeDecay = 0.25f;
                    weights.Importance = 0.3f;
                    weights.KeywordMatch = 0.3f;
                    weights.RelationshipBonus = 0.25f;
                    weights.RecencyWindow = 240000;   
                    break;
            }
            
            if (confidence < 0.6f)
            {
                float neutralBlend = 1.0f - confidence;
                var neutralWeights = GetDynamicWeights(SceneType.Neutral, 1.0f);
                
                weights.TimeDecay = Lerp(weights.TimeDecay, neutralWeights.TimeDecay, neutralBlend);
                weights.Importance = Lerp(weights.Importance, neutralWeights.Importance, neutralBlend);
                weights.KeywordMatch = Lerp(weights.KeywordMatch, neutralWeights.KeywordMatch, neutralBlend);
                weights.RelationshipBonus = Lerp(weights.RelationshipBonus, neutralWeights.RelationshipBonus, neutralBlend);
            }
            
            return weights;
        }
        
        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
        
        /// <summary>
        /// English diagnostic label for technical logs. Not user-facing UI.
        /// </summary>
        public static string GetSceneDisplayName(SceneType scene)
        {
            switch (scene)
            {
                case SceneType.Combat: return "Combat/Emergency";
                case SceneType.Social: return "Social/Emotional";
                case SceneType.Work: return "Work/Daily";
                case SceneType.Medical: return "Medical/Health";
                case SceneType.Research: return "Research/Study";
                case SceneType.Event: return "SpecialEvent";
                case SceneType.Neutral: return "Neutral/General";
                default: return "Unknown";
            }
        }
    }
    
    public class SceneAnalysisResult
    {
        public SceneType PrimaryScene { get; set; }                 
        public Dictionary<SceneType, float> SceneScores { get; set; }
        public float Confidence { get; set; }                       
        
        public override string ToString()
        {
            return $"{SceneAnalyzer.GetSceneDisplayName(PrimaryScene)} (confidence: {Confidence:P0})";
        }
    }
    
    public class DynamicWeights
    {
        public float TimeDecay { get; set; }         
        public float Importance { get; set; }        
        public float KeywordMatch { get; set; }      
        public float RelationshipBonus { get; set; } 
        public int RecencyWindow { get; set; }       
        
        public override string ToString()
        {
            return $"TimeDecay:{TimeDecay:F2} Importance:{Importance:F2} Keyword:{KeywordMatch:F2} " +
                   $"Relationship:{RelationshipBonus:F2} Window:{RecencyWindow / 60000}days";
        }
    }
}

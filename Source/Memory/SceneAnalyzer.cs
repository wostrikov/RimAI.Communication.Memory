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
                    "напад", "raid", "атакувати", "attack", "бій", "combat", "fight",
                    "ворог", "enemy", "вторгнення", "invasion", "оборона", "defense",
                    "поранення", "injured", "поранення", "wound", "смерть", "death", "died", "killed",
                    "кровотеча", "bleeding", "впав", "downed", "непритомність", "unconscious",
                    "зброя", "weapon", "рушниця", "gun", "стріляти", "shoot", "вибух", "explosion",
                    "укриття", "cover", "відступ", "retreat", "підкріплення", "reinforcement"
                }
            },
            {
                SceneType.Social, new List<string>
                {
                    "балачка", "chat", "talk", "розмова", "conversation", "мовив", "said",
                    "розповісти", "told", "запитати", "asked", "відповісти", "replied",
                    "подобається", "like", "любов", "love", "неприємно", "hate", "друг", "friend",
                    "кохані", "lover", "подружжя", "spouse", "стосунки", "relationship",
                    "дружба", "friendship", "суперечка", "argument", "примирення", "reconcile",
                    "радісно", "happy", "щасливий", "joyful", "смуток", "sad", "сумно", "upset",
                    "лють", "angry", "злість", "mad", "неспокій", "anxious", "настрій", "mood",
                    "відчуття", "feel", "емоції", "emotion"
                }
            },
            {
                SceneType.Work, new List<string>
                {
                    "будувати", "construct", "будівля", "building", "виготовити", "craft", "виробництво", "manufacture",
                    "ремонт", "repair", "розібрати", "deconstruct",
                    "садити", "plant", "збирати врожай", "harvest", "посіви", "crop", "поле", "field",
                    "видобуток", "mining", "копання", "dig", "переносити", "haul", "перевезення", "transport",
                    "склад", "storage", "прибирання", "clean",
                    "куховарство", "cook", "cooking", "готувати", "meal", "їжа", "food"
                }
            },
            {
                SceneType.Medical, new List<string>
                {
                    "лікувати", "treat", "медицина", "medical", "операція", "surgery", "керування", "operation",
                    "перевʼязати", "bandage", "догляд", "tend", "опіка", "care",
                    "хвороба", "disease", "illness", "інфекція", "infection", "гарячка", "fever",
                    "біль", "pain", "каліцтво", "disability", "здоровий", "health",
                    "відновлення", "recover", "одужання", "heal", "реабілітація", "rehabilitation"
                }
            },
            {
                SceneType.Research, new List<string>
                {
                    "дослідження", "research", "технології", "technology", "винахід", "invention",
                    "прорив", "breakthrough", "відкриття", "discovery", "експеримент", "experiment",
                    "навчання", "learn", "тренувати", "train", "вправляння", "practice",
                    "навички", "skill", "зростання", "improve", "опанувати", "master",
                    "знання", "knowledge", "навчати", "teach"
                }
            },
            {
                SceneType.Event, new List<string>
                {
                    "весілля", "wedding", "одруження", "marry", "заручини", "engaged",
                    "день народження", "birthday", "святкування", "celebrate", "вечірка", "party",
                    "похорон", "funeral", "поховати", "burial", "вшанування", "memorial",
                    "ритуал", "ceremony", "свято", "festival", "захід", "event"
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

using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory
{
    public enum KnowledgeEntryCategory
    {
        None,          
        Instructions,  
        Lore,          
        PawnStatus,    
        History,       
        Other          
    }

    public enum KeywordMatchMode
    {
        Any,   
        All      // Hard constraint — changing this breaks an invariant.
    }

    public enum KnowledgeMatchType
    {
        Keyword,   
        Vector,    
        Mixed      
    }

    public class CommonKnowledgeEntry : IExposable
    {
        public string id;
        public string tag;         
        public string content;     
        public float importance;   
        public List<string> keywords;
        public bool isEnabled;     
        public bool isUserEdited;  
        
        public int targetPawnId = -1; 
        
        public int creationTick = -1;      
        public string originalEventText = ""; 

        public KeywordMatchMode matchMode = KeywordMatchMode.Any;
        
        public KnowledgeEntryCategory category = KnowledgeEntryCategory.None;
        
        private List<string> cachedTags;

        // Hard constraint — changing this breaks an invariant. (summary tag summary)
        public void InvalidateCache()
        {
            cachedTags = null;
        }

        public CommonKnowledgeEntry()
        {
            id = "ck-" + Guid.NewGuid().ToString("N").Substring(0, 12);
            keywords = new List<string>();
            isEnabled = true;
            importance = 0.5f;
            targetPawnId = -1;
            creationTick = -1;
            originalEventText = "";
            matchMode = KeywordMatchMode.Any;
        }

        public CommonKnowledgeEntry(string tag, string content) : this()
        {
            this.tag = tag;
            this.content = content;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id");
            Scribe_Values.Look(ref tag, "tag");
            Scribe_Values.Look(ref content, "content");
            Scribe_Values.Look(ref importance, "importance", 0.5f);
            Scribe_Values.Look(ref isEnabled, "isEnabled", true);
            Scribe_Values.Look(ref isUserEdited, "isUserEdited", false);
            Scribe_Values.Look(ref targetPawnId, "targetPawnId", -1);
            Scribe_Values.Look(ref creationTick, "creationTick", -1);
            Scribe_Values.Look(ref originalEventText, "originalEventText", "");
            Scribe_Collections.Look(ref keywords, "keywords", LookMode.Value);
            
            Scribe_Values.Look(ref matchMode, "matchMode", KeywordMatchMode.Any);
            Scribe_Values.Look(ref category, "category", KnowledgeEntryCategory.None);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (keywords == null) keywords = new List<string>();
                cachedTags = null;
                
                if (string.IsNullOrEmpty(originalEventText) && !string.IsNullOrEmpty(content))
                {
                    originalEventText = RemoveTimePrefix(content);
                }
            }
        }
        
        private static string RemoveTimePrefix(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            
            string[] timePrefixes = { "сьогодні", "1 дн. тому", "2 дн. тому", "3 дн. тому", "4 дн. тому", "5 дн. тому", "6 дн. тому", 
                                     "близько 3 дн. тому", "близько 4 дн. тому", "близько 5 дн. тому", "близько 6 дн. тому", "близько 7 дн. тому" };
            
            foreach (var prefix in timePrefixes)
            {
                if (text.StartsWith(prefix))
                {
                    return text.Substring(prefix.Length);
                }
            }
            
            return text;
        }
        
        public void UpdateEventTimePrefix(int currentTick)
        {
            if (creationTick < 0 || string.IsNullOrEmpty(originalEventText))
                return;
            
            if (isUserEdited)
                return;
            
            int ticksElapsed = currentTick - creationTick;
            int daysElapsed = ticksElapsed / GenDate.TicksPerDay;
            
            string timePrefix = "";
            if (daysElapsed < 1)
            {
                timePrefix = "сьогодні";
            }
            else if (daysElapsed == 1)
            {
                timePrefix = "1 дн. тому";
            }
            else if (daysElapsed == 2)
            {
                timePrefix = "2 дн. тому";
            }
            else if (daysElapsed < 7)
            {
                timePrefix = $"близько {daysElapsed} дн. тому";
            }
            else
            {
                timePrefix = "близько 7 дн. тому";
            }
            
            content = timePrefix + originalEventText;
        }

        public List<string> GetTags()
        {
            if (cachedTags != null)
                return cachedTags;
            
            if (string.IsNullOrEmpty(tag))
            {
                cachedTags = new List<string>();
                return cachedTags;
            }
            
            cachedTags = tag.Split(new[] { ',', '，', '、', ';', '；' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();
            
            return cachedTags;
        }

        public string FormatForExport()
        {
            bool canBeExtracted = ExtendedKnowledgeEntry.CanBeExtracted(this);
            bool canBeMatched = ExtendedKnowledgeEntry.CanBeMatched(this);
            
            return $"[{tag}|{importance:F2}|{matchMode}|{canBeExtracted}|{canBeMatched}]{content}";
        }

        public override string ToString()
        {
            return FormatForExport();
        }
        
        public bool IsRuleKnowledge()
        {
            if (string.IsNullOrEmpty(tag))
                return false;
            
            string lowerTag = tag.ToLower();
            return lowerTag.Contains("правила") || 
                   lowerTag.Contains("instructions") || 
                   lowerTag.Contains("rule");
        }
    }

    public class KnowledgeScore
    {
        public CommonKnowledgeEntry Entry;
        public float Score;
    }
    
    public class KnowledgeScoreDetail
    {
        public CommonKnowledgeEntry Entry;
        public bool IsEnabled;
        public float TotalScore;
        
        public float BaseScore;
        public float ManualBonus;
        public float MatchTypeScore;
        
        public KnowledgeMatchType MatchType;
        
        public float JaccardScore;
        public float TagScore;
        public float ImportanceScore;
        public int KeywordMatchCount;
        public List<string> MatchedKeywords = new List<string>();
        public List<string> MatchedTags = new List<string>();
        public string FailReason;
    }
    
    public class KeywordExtractionInfo
    {
        public List<string> ContextKeywords = new List<string>();
        public int TotalKeywords;
        public int PawnKeywordsCount;
        public PawnKeywordInfo PawnInfo;
    }
    
    public class PawnKeywordInfo
    {
        public string PawnName;
        public List<string> NameKeywords = new List<string>();
        public List<string> AgeKeywords = new List<string>();
        public List<string> GenderKeywords = new List<string>();
        public List<string> RaceKeywords = new List<string>();
        public List<string> IdentityKeywords = new List<string>(); 
        public List<string> TraitKeywords = new List<string>();
        public List<string> SkillKeywords = new List<string>();
        public List<string> SkillLevelKeywords = new List<string>();
        public List<string> HealthKeywords = new List<string>();
        public List<string> RelationshipKeywords = new List<string>();
        public List<string> BackstoryKeywords = new List<string>();
        public List<string> ChildhoodKeywords = new List<string>();
        public int TotalCount;
    }
}
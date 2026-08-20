using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Ustas.RimAI.Communication.Memory
{

    public class MemoryEntry : IExposable
    {
        public string Id;                  
        public int GameTick = -1;          
        public string Content;             

        // Presentation-only migration for metadata emitted by older builds.
        // The semantic conversation body remains byte-for-byte unchanged in the save.
        public string DisplayContent
        {
            get
            {
                if (string.IsNullOrEmpty(Content)) return Content;
                const string legacyPrefix = "[对话参与者:";
                return Content.StartsWith(legacyPrefix, StringComparison.Ordinal)
                    ? $"[{"RimTalk_Memory_Participants".Translate()}:" + Content.Substring(legacyPrefix.Length)
                    : Content;
            }
        }

        public MemoryType Type;            
        public MemoryLayer Layer;          

        private float _importance = -1;      
        public float Importance
        {
            get => _importance;
            set
            {
                _importance = Math.Clamp(value, 0f, 1f);
            }
        }
        public float Activity = -1;        

        public string relatedPawnId;       
        public string relatedPawnName;     
        public string location;            
        public List<string> tags = new();  
        public List<string> keywords = new();  

        public bool IsUserEdited = false;  
        public bool IsPinned = false;      
        public bool IsSummarized = false;  
        public string Notes;               

        public string LayerName => Layer switch
        {
            MemoryLayer.Active => "RimTalk_MemoryLayer_Active".Translate(),
            MemoryLayer.Situational => "RimTalk_MemoryLayer_Situational".Translate(),
            MemoryLayer.EventLog => "RimTalk_MemoryLayer_EventLog".Translate(),
            MemoryLayer.Archive => "RimTalk_MemoryLayer_Archive".Translate(),
            _ => "RimTalk_Memory_Unknown".Translate()
        };

        public string TypeName => Type switch
        {
            MemoryType.Conversation => "RimTalk_MemoryType_Conversation".Translate(),
            MemoryType.Action => "RimTalk_MemoryType_Action".Translate(),
            MemoryType.Observation => "RimTalk_MemoryType_Observation".Translate(),
            MemoryType.Event => "RimTalk_MemoryType_Event".Translate(),
            MemoryType.Emotion => "RimTalk_MemoryType_Emotion".Translate(),
            MemoryType.Relationship => "RimTalk_MemoryType_Relationship".Translate(),
            MemoryType.Internal => "RimTalk_MemoryType_Internal".Translate(),
            _ => "RimTalk_Memory_Unknown".Translate()
        };

        public virtual bool CanBeSummarized => !IsSummarized;

        public string AgeString => (Find.TickManager?.TicksGame - GameTick) switch
        {
            null or < 0 => "RimTalk_MemoryAge_Invalid".Translate(),
            < GenDate.TicksPerHour => "RimTalk_MemoryAge_JustNow".Translate(),
            < GenDate.TicksPerHour * 6 => "RimTalk_MemoryAge_HoursAgo".Translate(),
            < GenDate.TicksPerDay => "RimTalk_MemoryAge_Today".Translate(),
            < GenDate.TicksPerDay * 2 => "RimTalk_MemoryAge_OneDayAgo".Translate(),
            < GenDate.TicksPerDay * 3 => "RimTalk_MemoryAge_TwoDaysAgo".Translate(),
            < GenDate.TicksPerDay * 7 => "RimTalk_MemoryAge_DaysAgo".Translate(),
            < GenDate.TicksPerDay * 14 => "RimTalk_MemoryAge_LastWeek".Translate(),
            _ => GenDate.DateFullStringAt(GenDate.TickGameToAbs(GameTick), Vector2.zero)
        };

        public MemoryEntry() { }

        public MemoryEntry(string content, MemoryType type, MemoryLayer layer, float importance = 0.5f, string relatedPawn = null)
        {
            Id = "mem-" + Guid.NewGuid().ToString("N").Substring(0, 12);
            GameTick = Find.TickManager?.TicksGame ?? -1;
            Content = content;

            Type = type;
            Layer = layer;

            Activity = 1f;
            Importance = importance;
            relatedPawnName = relatedPawn;

            AddTypeTag();
        }
        private void AddTypeTag()
        {
            AddTag(Type switch
            {
                MemoryType.Conversation => "对话",
                MemoryType.Action => "行动",
                MemoryType.Observation => "观察",
                MemoryType.Event => "事件",
                MemoryType.Emotion => "情绪",
                MemoryType.Relationship => "关系",
                MemoryType.Internal => "内部上下文",
                _ => null
            });
        }

        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref Id, "id");
            Scribe_Values.Look(ref GameTick, "timestamp", -1);
            Scribe_Values.Look(ref Content, "content");

            Scribe_Values.Look(ref Type, "type");
            Scribe_Values.Look(ref Layer, "layer");

            Scribe_Values.Look(ref _importance, "importance", -1);
            Scribe_Values.Look(ref Activity, "activity", -1);

            Scribe_Values.Look(ref relatedPawnId, "relatedPawnId");
            Scribe_Values.Look(ref relatedPawnName, "relatedPawnName");
            Scribe_Values.Look(ref location, "location");
            Scribe_Collections.Look(ref tags, "tags", LookMode.Value);
            Scribe_Collections.Look(ref keywords, "keywords", LookMode.Value);

            Scribe_Values.Look(ref IsUserEdited, "isUserEdited", false);
            Scribe_Values.Look(ref IsPinned, "isPinned", false);
            Scribe_Values.Look(ref IsSummarized, "IsSummarized", true);
            Scribe_Values.Look(ref Notes, "notes");

            tags ??= new();
            keywords ??= new();
        }

        public void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || tags.Contains(tag)) return;

            tags.Add(tag);
        }

        public void RemoveTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;

            tags.Remove(tag);
        }

        public void AddKeyword(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword) || keywords.Contains(keyword)) return;

            keywords.Add(keyword);
        }

        public void RemoveKeyword(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return;

            keywords.Remove(keyword);
        }

        public void Decay(float rate)
        {
            if (IsPinned) return;

            Activity *= (1f - rate);
        }

        public float CalculateRetrievalScore(string context, List<string> contextKeywords)
        {
            float score = 0f;

            float timeFactor = (float)Math.Exp(-(float)(Find.TickManager.TicksGame - GameTick) / GenDate.TicksPerDay);
            score += timeFactor * 0.3f;

            score += Importance * 0.3f;

            score += Activity * 0.2f;

            if (contextKeywords != null && contextKeywords.Count > 0)
            {
                int matchCount = 0;
                foreach (var kw in keywords)
                {
                    if (contextKeywords.Contains(kw)) matchCount++;
                }
                float relevance = (float)matchCount / Math.Max(keywords.Count, contextKeywords.Count);
                score += relevance * 0.2f;
            }

            if (IsPinned) score += 0.3f;
            if (IsUserEdited) score += 0.2f;

            return score;
        }
    }

    public class MemoryQuery
    {
        public MemoryLayer? layer;
        public MemoryType? type;
        public string relatedPawn;
        public List<string> tags;
        public List<string> keywords;
        public int maxCount = 10;
        public bool includeContext = true;

        public MemoryQuery()
        {
            tags = new List<string>();
            keywords = new List<string>();
        }
    }

}

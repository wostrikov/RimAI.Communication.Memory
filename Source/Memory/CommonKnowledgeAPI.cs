using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Ustas.RimAI.Communication.Memory
{
    public static class CommonKnowledgeAPI
    {
        #region 添加常识

        public static string AddKnowledge(string tag, string content, float importance = 0.5f)
        {
            try
            {
                var library = MemoryManager.GetCommonKnowledge();
                if (library == null)
                {
                    Log.Warning("[CommonKnowledgeAPI] Failed to get library");
                    return null;
                }

                var entry = new CommonKnowledgeEntry(tag, content)
                {
                    importance = UnityEngine.Mathf.Clamp01(importance),
                    isEnabled = true,
                    isUserEdited = false
                };

                library.AddEntry(entry);
                
                return entry.id;
            }
            catch (Exception ex)
            {
                Log.Error($"[CommonKnowledgeAPI] AddKnowledge failed: {ex}");
                return null;
            }
        }

        // Hard constraint — changing this breaks an invariant. (summary summary param name tag param param name content param param name importance param param name matchMode Any All param param name targetPawnId Pawn)
        public static string AddKnowledgeEx(
            string tag, 
            string content, 
            float importance = 0.5f,
            KeywordMatchMode matchMode = KeywordMatchMode.Any,
            int targetPawnId = -1,
            bool canBeExtracted = false,
            bool canBeMatched = false)
        {
            try
            {
                var library = MemoryManager.GetCommonKnowledge();
                if (library == null)
                {
                    Log.Warning("[CommonKnowledgeAPI] Failed to get library");
                    return null;
                }

                var entry = new CommonKnowledgeEntry(tag, content)
                {
                    importance = UnityEngine.Mathf.Clamp01(importance),
                    matchMode = matchMode,
                    targetPawnId = targetPawnId,
                    isEnabled = true,
                    isUserEdited = false
                };

                ExtendedKnowledgeEntry.SetCanBeExtracted(entry, canBeExtracted);
                ExtendedKnowledgeEntry.SetCanBeMatched(entry, canBeMatched);

                library.AddEntry(entry);
                
                return entry.id;
            }
            catch (Exception ex)
            {
                Log.Error($"[CommonKnowledgeAPI] AddKnowledgeEx failed: {ex}");
                return null;
            }
        }

        public static int AddKnowledgeBatch(List<(string tag, string content)> knowledgeList, float importance = 0.5f)
        {
            if (knowledgeList == null || knowledgeList.Count == 0)
                return 0;

            int count = 0;
            foreach (var (tag, content) in knowledgeList)
            {
                if (AddKnowledge(tag, content, importance) != null)
                    count++;
            }

            return count;
        }

        #endregion

        #region 更新常识

        public static bool UpdateKnowledge(string id, string newContent)
        {
            try
            {
                var entry = FindKnowledgeById(id);
                if (entry == null)
                {
                    Log.Warning($"[CommonKnowledgeAPI] Knowledge not found: {id}");
                    return false;
                }

                entry.content = newContent;
                entry.InvalidateCache();
                
                try
                {
                    if (RimTalkMemoryPatchMod.Settings.enableVectorEnhancement)
                    {
                        VectorDB.VectorService.Instance.UpdateKnowledgeVector(entry.id, entry.content);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[CommonKnowledgeAPI] Failed to update vector: {ex.Message}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[CommonKnowledgeAPI] UpdateKnowledge failed: {ex}");
                return false;
            }
        }

        public static bool UpdateKnowledgeTag(string id, string newTag)
        {
            try
            {
                var entry = FindKnowledgeById(id);
                if (entry == null)
                    return false;

                entry.tag = newTag;
                entry.InvalidateCache();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[CommonKnowledgeAPI] UpdateKnowledgeTag failed: {ex}");
                return false;
            }
        }

        public static bool UpdateKnowledgeImportance(string id, float newImportance)
        {
            try
            {
                var entry = FindKnowledgeById(id);
                if (entry == null)
                    return false;

                entry.importance = UnityEngine.Mathf.Clamp01(newImportance);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[CommonKnowledgeAPI] UpdateKnowledgeImportance failed: {ex}");
                return false;
            }
        }

        public static bool SetKnowledgeEnabled(string id, bool enabled)
        {
            try
            {
                var entry = FindKnowledgeById(id);
                if (entry == null)
                    return false;

                entry.isEnabled = enabled;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[CommonKnowledgeAPI] SetKnowledgeEnabled failed: {ex}");
                return false;
            }
        }

        #endregion

        #region 查询常识

        public static CommonKnowledgeEntry FindKnowledgeById(string id)
        {
            try
            {
                var library = MemoryManager.GetCommonKnowledge();
                if (library == null)
                    return null;

                return library.Entries.FirstOrDefault(e => e.id == id);
            }
            catch (Exception ex)
            {
                Log.Error($"[CommonKnowledgeAPI] FindKnowledgeById failed: {ex}");
                return null;
            }
        }

        public static List<CommonKnowledgeEntry> FindKnowledge(string tag)
        {
            try
            {
                var library = MemoryManager.GetCommonKnowledge();
                if (library == null)
                    return new List<CommonKnowledgeEntry>();

                return library.Entries
                    .Where(e => e.tag.IndexOf(tag, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error($"[CommonKnowledgeAPI] FindKnowledge failed: {ex}");
                return new List<CommonKnowledgeEntry>();
            }
        }

        public static List<CommonKnowledgeEntry> FindKnowledgeByContent(string content)
        {
            try
            {
                var library = MemoryManager.GetCommonKnowledge();
                if (library == null)
                    return new List<CommonKnowledgeEntry>();

                return library.Entries
                    .Where(e => e.content.IndexOf(content, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error($"[CommonKnowledgeAPI] FindKnowledgeByContent failed: {ex}");
                return new List<CommonKnowledgeEntry>();
            }
        }

        public static List<CommonKnowledgeEntry> GetAllKnowledge()
        {
            try
            {
                var library = MemoryManager.GetCommonKnowledge();
                if (library == null)
                    return new List<CommonKnowledgeEntry>();

                return library.Entries.ToList();
            }
            catch (Exception ex)
            {
                Log.Error($"[CommonKnowledgeAPI] GetAllKnowledge failed: {ex}");
                return new List<CommonKnowledgeEntry>();
            }
        }

        public static int GetKnowledgeCount()
        {
            try
            {
                var library = MemoryManager.GetCommonKnowledge();
                if (library == null)
                    return 0;

                return library.Entries.Count;
            }
            catch (Exception ex)
            {
                Log.Error($"[CommonKnowledgeAPI] GetKnowledgeCount failed: {ex}");
                return 0;
            }
        }

        #endregion

        #region 删除常识

        public static bool RemoveKnowledge(string id)
        {
            try
            {
                var library = MemoryManager.GetCommonKnowledge();
                if (library == null)
                    return false;

                var entry = library.Entries.FirstOrDefault(e => e.id == id);
                if (entry == null)
                    return false;

                library.RemoveEntry(entry);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[CommonKnowledgeAPI] RemoveKnowledge failed: {ex}");
                return false;
            }
        }

        public static int RemoveKnowledgeByTag(string tag)
        {
            try
            {
                var library = MemoryManager.GetCommonKnowledge();
                if (library == null)
                    return 0;

                var toRemove = library.Entries
                    .Where(e => e.tag.IndexOf(tag, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                int count = 0;
                foreach (var entry in toRemove)
                {
                    library.RemoveEntry(entry);
                    count++;
                }

                return count;
            }
            catch (Exception ex)
            {
                Log.Error($"[CommonKnowledgeAPI] RemoveKnowledgeByTag failed: {ex}");
                return 0;
            }
        }

        public static bool ClearAllKnowledge()
        {
            try
            {
                var library = MemoryManager.GetCommonKnowledge();
                if (library == null)
                    return false;

                library.Clear();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[CommonKnowledgeAPI] ClearAllKnowledge failed: {ex}");
                return false;
            }
        }

        #endregion

        #region 导入/导出

        public static int ImportFromText(string text, bool clearExisting = false)
        {
            try
            {
                var library = MemoryManager.GetCommonKnowledge();
                if (library == null)
                    return 0;

                return library.ImportFromText(text, clearExisting);
            }
            catch (Exception ex)
            {
                Log.Error($"[CommonKnowledgeAPI] ImportFromText failed: {ex}");
                return 0;
            }
        }

        public static string ExportToText()
        {
            try
            {
                var library = MemoryManager.GetCommonKnowledge();
                if (library == null)
                    return string.Empty;

                return library.ExportToText();
            }
            catch (Exception ex)
            {
                Log.Error($"[CommonKnowledgeAPI] ExportToText failed: {ex}");
                return string.Empty;
            }
        }

        #endregion

        #region 高级功能

        public static bool ExistsKnowledge(string id)
        {
            return FindKnowledgeById(id) != null;
        }

        public static KnowledgeStats GetStats()
        {
            try
            {
                var library = MemoryManager.GetCommonKnowledge();
                if (library == null)
                    return new KnowledgeStats();

                return new KnowledgeStats
                {
                    TotalCount = library.Entries.Count,
                    EnabledCount = library.Entries.Count(e => e.isEnabled),
                    DisabledCount = library.Entries.Count(e => !e.isEnabled),
                    UserEditedCount = library.Entries.Count(e => e.isUserEdited),
                    GlobalCount = library.Entries.Count(e => e.targetPawnId == -1),
                    PawnSpecificCount = library.Entries.Count(e => e.targetPawnId != -1)
                };
            }
            catch (Exception ex)
            {
                Log.Error($"[CommonKnowledgeAPI] GetStats failed: {ex}");
                return new KnowledgeStats();
            }
        }

        #endregion
    }

    public struct KnowledgeStats
    {
        public int TotalCount;         
        public int EnabledCount;       
        public int DisabledCount;      
        public int UserEditedCount;    
        public int GlobalCount;        
        public int PawnSpecificCount;  
    }
}

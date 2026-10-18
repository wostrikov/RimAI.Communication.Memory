using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

/// <summary>
/// Verse keeps the first declaration of a keyed string and reports every later one as a
/// duplicate, so each key is declared once per language; and the strings the memory text
/// itself carries exist in English, not only in the Ukrainian files they were written in.
/// </summary>
internal static class MemoryLanguageKeysTests
{
    public static int Run()
    {
        int n = 0;
        void T(bool x, string s)
        {
            if (!x)
                throw new Exception("FAILED " + s);
            n++;
        }

        foreach (string language in new[] { "English", "Ukrainian", "ChineseSimplified" })
        {
            var declared = Declarations(language);
            T(declared.Count > 0, language + "-files-found");
            var twice = declared.GroupBy(d => d.key).Where(g => g.Count() > 1)
                .Select(g => g.Key + " in " + string.Join(", ", g.Select(d => d.file))).ToList();
            T(twice.Count == 0, language + "-no-duplicate-keys: " + string.Join("; ", twice.Take(5)));
        }

        var english = Values("English");
        var ukrainian = Values("Ukrainian");
        string[] carried =
        [
            "RimTalk_Memory_Participants", "RimTalk_Memory_Unknown", "RimTalk_Memory_UnknownDate",
            "RimTalk_MemoryLayer_Active", "RimTalk_MemoryLayer_Situational", "RimTalk_MemoryLayer_EventLog", "RimTalk_MemoryLayer_Archive",
            "RimTalk_MemoryAge_Invalid", "RimTalk_MemoryAge_JustNow", "RimTalk_MemoryAge_HoursAgo", "RimTalk_MemoryAge_Today",
            "RimTalk_MemoryAge_OneDayAgo", "RimTalk_MemoryAge_TwoDaysAgo", "RimTalk_MemoryAge_DaysAgo", "RimTalk_MemoryAge_LastWeek",
        ];
        foreach (string key in carried)
        {
            T(english.ContainsKey(key) && english[key].Trim().Length > 0, "english-has-" + key);
            T(ukrainian.ContainsKey(key), "ukrainian-has-" + key);
        }

        // The kept declarations are the ones the settings and library code is written for:
        // labels without a colon where the code adds one, and the count where it passes one.
        T(english["RimTalk_Settings_APIKey"] == "API Key", "api-key-label-without-colon");
        T(english["RimTalk_Knowledge_Content"] == "Content", "content-label-without-colon");
        T(english["RimTalk_Knowledge_Enabled"].Contains("{0}"), "enabled-takes-the-count");
        T(english["RimTalk_Knowledge_Deleted"].Contains("{0}") && english["RimTalk_Knowledge_DeleteConfirm"].Contains("{0}"), "delete-messages-take-the-count");
        T(english["RimTalk_Settings_ActionMemory"] == "Action Memory", "action-memory-label");
        T(english["RimTalk_Settings_ConversationMemory"] == "Conversation Memory", "conversation-memory-label");
        T(ukrainian["RimTalk_Settings_ConversationMemory"] == "Пам’ять розмов", "conversation-memory-label-uk");
        return n;
    }

    static List<(string key, string file)> Declarations(string language)
    {
        var result = new List<(string, string)>();
        string dir = Path.Combine(AppContext.BaseDirectory, "lang", language);
        if (!Directory.Exists(dir))
            return result;
        foreach (string file in Directory.GetFiles(dir, "*.xml").OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
        {
            var doc = new XmlDocument();
            doc.Load(file);
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                if (node.NodeType == XmlNodeType.Element)
                    result.Add((node.Name, Path.GetFileName(file)));
        }
        return result;
    }

    static Dictionary<string, string> Values(string language)
    {
        var result = new Dictionary<string, string>();
        string dir = Path.Combine(AppContext.BaseDirectory, "lang", language);
        foreach (string file in Directory.GetFiles(dir, "*.xml").OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
        {
            var doc = new XmlDocument();
            doc.Load(file);
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                if (node.NodeType == XmlNodeType.Element && !result.ContainsKey(node.Name))
                    result[node.Name] = node.InnerText;
        }
        return result;
    }
}

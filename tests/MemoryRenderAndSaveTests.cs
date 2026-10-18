using System;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// A variable that fails to render is skipped rather than matched or shown as its literal
/// "{{ }}" text, and the cached model text is cleaned on its way into the save the same way
/// memory entries are.
/// </summary>
internal static class MemoryRenderAndSaveTests
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

        foreach (string name in new[] { "KnowledgeVariableProvider", "MustacheVariableHelper", "Dialog_InjectionPreview" })
        {
            string text = Read(name + ".cs.src");
            T(!Regex.IsMatch(text, @"ScribanParser\.Render\s*\("), name + "-no-raw-text-render");
            T(text.Contains("ScribanParser.TryRender("), name + "-uses-try-render");
        }
        T(!Read("Dialog_InjectionPreview.cs.src").Contains("?? template;"), "preview-no-template-fallback");

        string conversation = Read("ConversationCache.cs.src");
        T(CleansOnSave(conversation, "dialogue") && CleansOnSave(conversation, "cacheKey"), "conversation-cache-cleaned-on-save");
        string prompt = Read("PromptCache.cs.src");
        T(CleansOnSave(prompt, "memoryPrompt") && CleansOnSave(prompt, "knowledgePrompt") && CleansOnSave(prompt, "fullPrompt"),
            "prompt-cache-cleaned-on-save");
        return n;
    }

    // The field is cleaned inside a Saving block that comes before it is scribed.
    static bool CleansOnSave(string text, string field)
    {
        int saving = text.IndexOf("Scribe.mode == LoadSaveMode.Saving", StringComparison.Ordinal);
        int clean = text.IndexOf(field + " = XmlSafeText.Clean(" + field + ");", StringComparison.Ordinal);
        int look = text.IndexOf("Scribe_Values.Look(ref " + field + ",", StringComparison.Ordinal);
        return saving >= 0 && clean > saving && look > clean;
    }

    static string Read(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, name));
}

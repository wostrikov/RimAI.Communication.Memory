using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Ustas.RimAI.Communication.Memory.Policy;

internal static class XmlSafeTextTests
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

        const string clean = "Леся: полагодь дах.\tДобре!\r\n🙂 рядок";
        T(ReferenceEquals(XmlSafeText.Clean(clean), clean), "clean-text-is-returned-as-is");
        T(XmlSafeText.Clean(null) == null, "null-stays-null");
        T(XmlSafeText.Clean(string.Empty) == string.Empty, "empty-stays-empty");

        T(XmlSafeText.Clean("a\u0001b") == "a b", "control-char-becomes-space");
        T(XmlSafeText.Clean("a\u0000b\u001Fc") == "a b c", "nul-and-unit-separator");
        T(XmlSafeText.Clean("end\uD83D") == "end ", "lone-high-surrogate-at-end");
        T(XmlSafeText.Clean("\uDE42start") == " start", "lone-low-surrogate-at-start");
        T(XmlSafeText.Clean("x🙂y") == "x🙂y", "surrogate-pair-kept");
        T(XmlSafeText.Clean("\uD83D🙂") == " 🙂", "high-then-pair");
        T(XmlSafeText.Clean("a￾b￿c") == "a b c", "noncharacters-removed");
        T(XmlSafeText.Clean("�") == "�", "replacement-char-kept");
        T(XmlSafeText.Clean("\u0085 ") == "\u0085 ", "c1-and-nbsp-are-legal");

        var list = new List<string> { "ok", "bad\u0002", null };
        XmlSafeText.CleanInPlace(list);
        T(list[0] == "ok" && list[1] == "bad " && list[2] == null, "list-cleaned-in-place");
        XmlSafeText.CleanInPlace(null);

        // The point of the helper: whatever it returns, the writer the game's
        // save uses accepts.
        string hostile = "Відповідь\u0001 моделі\uD800 з \u0008сміттям\uDFFF і 🙂";
        T(Writes(XmlSafeText.Clean(hostile)), "cleaned-text-survives-xml-writer");
        T(!Writes(hostile), "raw-text-breaks-xml-writer");
        return n;
    }

    static bool Writes(string text)
    {
        try
        {
            using var sw = new StringWriter();
            using (var writer = XmlWriter.Create(sw))
            {
                writer.WriteStartElement("content");
                writer.WriteString(text);
                writer.WriteEndElement();
            }
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}

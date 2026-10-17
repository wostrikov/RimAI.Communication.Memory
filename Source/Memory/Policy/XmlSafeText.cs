using System.Collections.Generic;
using System.Text;

namespace Ustas.RimAI.Communication.Memory.Policy
{
    /// <summary>
    /// Text the save file can hold. XML 1.0 has no way to write most C0 control
    /// characters or a surrogate without its pair, and model output can carry
    /// either (an unescaped "\u0001" in a JSON fragment, a reply cut mid-emoji).
    /// One such character makes the game's XML writer throw, and the rest of the
    /// save is lost, so memory text is cleaned on its way into the file.
    /// </summary>
    public static class XmlSafeText
    {
        /// <summary>
        /// Returns the same instance when nothing needs changing, so the save of
        /// clean text costs one pass and no allocation. A bad character becomes a
        /// space, which keeps the words on either side apart.
        /// </summary>
        public static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            StringBuilder cleaned = null;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    cleaned?.Append(c).Append(text[i + 1]);
                    i++;
                    continue;
                }

                if (IsXmlChar(c))
                {
                    cleaned?.Append(c);
                    continue;
                }

                cleaned ??= new StringBuilder(text.Length).Append(text, 0, i);
                cleaned.Append(' ');
            }

            return cleaned?.ToString() ?? text;
        }

        public static void CleanInPlace(List<string> values)
        {
            if (values == null)
                return;
            for (int i = 0; i < values.Count; i++)
                values[i] = Clean(values[i]);
        }

        // The Char production of XML 1.0, minus the supplementary planes, which
        // only ever arrive as a surrogate pair and are handled by the caller.
        private static bool IsXmlChar(char c)
        {
            if (c < 0x20)
                return c == '\t' || c == '\n' || c == '\r';
            if (c < 0xD800)
                return true;
            if (c < 0xE000)
                return false;
            return c <= 0xFFFD;
        }
    }
}

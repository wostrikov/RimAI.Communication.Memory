using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Verse;

namespace Ustas.RimAI.Communication.Memory
{
    public static class SuperKeywordEngine
    {
        private static readonly Regex EnglishWordRegex = new Regex(
            @"\b[a-zA-Z][a-zA-Z0-9]{1,}\b",
            RegexOptions.Compiled
        );
        
        private static readonly HashSet<string> StopWords = new HashSet<string>
        {
            "це", "вже", "є", "в", "я", "має", "та", "саме", "не", "людина", "усі", "один", "штука", "теж", "над",
            "він", "вони", "аж", "мовив", "треба", "геть", "ти", "може", "собі", "нема", "глянь", "добре", "сам", "цей",
            "той", "задля", "сюди", "проте", "здатен", "можна", "із", "але", "вельми", "хіба", "нехай", "ох", "ну", "який",
            "що", "як", "чому", "де", "хто", "скільки", "кілька", "трохи", "дещиця", "трохи є", "надто",
            "дуже", "досить", "ще", "більш", "найбільш", "великий", "малий", "багато", "мало", "новий", "старий", "добре", "поганий"
        };

        private static readonly HashSet<string> ImportantPrefixes = new HashSet<string>
        {
            "Драконовий володар", "Солак", "Мефісто", "колонізація", "бій", "поранення", "смерть", "кохання", "дружба", "ненависть",
            "завдання", "будувати", "садити", "видобуток", "дослідження", "медицина", "напад", "оборона", "торг", "перемовини"
        };

        public static List<WeightedKeyword> ExtractKeywords(string text, int maxKeywords = 100)
        {
            if (string.IsNullOrEmpty(text))
                return new List<WeightedKeyword>();

            const int MAX_TEXT_LENGTH = 500;
            if (text.Length > MAX_TEXT_LENGTH)
                text = text.Substring(0, MAX_TEXT_LENGTH);

            var keywordScores = new Dictionary<string, KeywordScore>();

            ExtractEnglishWords(text, keywordScores);

            ExtractChineseWords(text, keywordScores);

            int totalWords = keywordScores.Values.Sum(s => s.Frequency);
            
            foreach (var score in keywordScores.Values)
            {
                // TF (Term Frequency)
                float tf = (float)score.Frequency / totalWords;
                
                float lengthWeight = 1.0f + (score.Length - 2) * 0.3f;
                
                float positionWeight = 1.0f - ((float)score.FirstPosition / text.Length) * 0.3f;
                
                float importanceBonus = 1.0f;
                foreach (var prefix in ImportantPrefixes)
                {
                    if (score.Word.StartsWith(prefix))
                    {
                        importanceBonus = 1.5f;
                        break;
                    }
                }
                
                score.Weight = tf * lengthWeight * positionWeight * importanceBonus;
            }

            return keywordScores.Values
                .OrderByDescending(s => s.Weight)
                .ThenBy(s => s.Word, StringComparer.Ordinal)
                .Take(maxKeywords)
                .Select(s => new WeightedKeyword { Word = s.Word, Weight = s.Weight })
                .ToList();
        }

        private static void ExtractEnglishWords(string text, Dictionary<string, KeywordScore> keywordScores)
        {
            var matches = EnglishWordRegex.Matches(text);
            
            foreach (Match match in matches)
            {
                string word = match.Value;
                int position = match.Index;
                
                if (IsLowQualityKeyword(word))
                    continue;
                
                if (StopWords.Contains(word.ToLower()))
                    continue;

                if (!keywordScores.ContainsKey(word))
                {
                    keywordScores[word] = new KeywordScore
                    {
                        Word = word,
                        Length = word.Length,
                        FirstPosition = position
                    };
                }
                
                keywordScores[word].Frequency++;
            }
        }

        private static void ExtractChineseWords(string text, Dictionary<string, KeywordScore> keywordScores)
        {
            for (int length = 2; length <= 6; length++)
            {
                for (int i = 0; i <= text.Length - length; i++)
                {
                    string word = text.Substring(i, length);
                    
                    if (!ContainsChinese(word))
                        continue;
                    
                    if (StopWords.Contains(word))
                        continue;

                    if (!keywordScores.ContainsKey(word))
                    {
                        keywordScores[word] = new KeywordScore
                        {
                            Word = word,
                            Length = length,
                            FirstPosition = i
                        };
                    }
                    
                    keywordScores[word].Frequency++;
                }
            }
        }

        private static bool ContainsChinese(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            
            foreach (char c in text)
            {
                if (c >= 0x4e00 && c <= 0x9fa5)
                    return true;
            }
            
            return false;
        }
        public static float CalculateBM25Score(
            List<WeightedKeyword> queryKeywords,
            string document,
            List<string> documentKeywords,
            float k1 = 1.5f, 
            float b = 0.75f) 
        {
            if (queryKeywords.Count == 0 || string.IsNullOrEmpty(document))
                return 0f;

            float score = 0f;
            int docLength = document.Length;
            float avgDocLength = 100f;

            foreach (var queryKw in queryKeywords)
            {
                int freq = documentKeywords.Count(kw => kw == queryKw.Word);
                if (freq == 0)
                    continue;

                float idf = (float)Math.Log(1.0 + (1.0 / (freq + 0.5)));
                float tf = (freq * (k1 + 1)) / (freq + k1 * (1 - b + b * docLength / avgDocLength));
                
                score += idf * tf * queryKw.Weight;
            }

            return score;
        }

        public static bool FuzzyMatch(string word1, string word2, float threshold = 0.8f)
        {
            if (word1 == word2)
                return true;

            int distance = LevenshteinDistance(word1, word2);
            int maxLen = Math.Max(word1.Length, word2.Length);
            
            float similarity = 1.0f - ((float)distance / maxLen);
            return similarity >= threshold;
        }

        private static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; i++)
                d[i, 0] = i;
            for (int j = 0; j <= m; j++)
                d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        private static bool IsLowQualityKeyword(string word)
        {
            if (string.IsNullOrEmpty(word))
                return true;
            
            if (word.Length <= 2 && word.All(char.IsDigit))
                return true;
            
            if (word.Length <= 3 && word.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')))
            {
                var importantAbbreviations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "AI", "HP", "DPS", "XP", "UI", "API", "CPU", "GPU", "RAM", "SSD"
                };
                
                if (!importantAbbreviations.Contains(word))
                    return true;
            }
            
            var meaninglessSuffixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "tion", "ment", "ing", "ed", "er", "ly", "ness", "ity", "able", "ible",
                "al", "ful", "less", "ous", "ive", "ant", "ent", "ism", "ist", "ship"
            };
            
            if (word.Length <= 4 && meaninglessSuffixes.Contains(word))
                return true;
            
            if (word.Length == 2)
            {
                bool hasDigit = word.Any(char.IsDigit);
                bool hasLetter = word.Any(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'));
                
                if (hasDigit && hasLetter)
                    return true;
            }
            
            if (word.All(c => !char.IsLetterOrDigit(c)))
                return true;
            
            if (word.Trim().Length == 1)
                return true;
            
            return false;
        }
    }

    internal class KeywordScore
    {
        public string Word;
        public int Length;
        public int Frequency;
        public int FirstPosition;
        public float Weight;
    }

    public class WeightedKeyword
    {
        public string Word;
        public float Weight;
    }
}

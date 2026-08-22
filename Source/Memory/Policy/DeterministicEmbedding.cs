using System;
using System.Collections.Generic;

namespace Ustas.RimAI.Communication.Memory.Policy
{
    public static class DeterministicEmbedding
    {
        public const int Dimension = 64;

        public static float[] Embed(string text)
        {
            var vector = new float[Dimension];
            if (string.IsNullOrWhiteSpace(text))
                return vector;

            foreach (string token in Tokenize(text))
            {
                int bucket = StableBucket(token);
                vector[bucket] += 1f;
                if (token.Length >= 3)
                    vector[StableBucket(token.Substring(0, 3))] += 0.5f;
            }

            Normalize(vector);
            return vector;
        }

        public static float Cosine(float[] left, float[] right)
        {
            if (left == null || right == null || left.Length == 0 || left.Length != right.Length)
                return 0f;

            float dot = 0f;
            float magLeft = 0f;
            float magRight = 0f;
            for (int i = 0; i < left.Length; i++)
            {
                dot += left[i] * right[i];
                magLeft += left[i] * left[i];
                magRight += right[i] * right[i];
            }

            float denom = (float)Math.Sqrt(magLeft) * (float)Math.Sqrt(magRight);
            return denom <= 0f ? 0f : dot / denom;
        }

        public static float Similarity(string left, string right)
        {
            return Cosine(Embed(left), Embed(right));
        }

        static IEnumerable<string> Tokenize(string text)
        {
            var current = new char[text.Length];
            int n = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = char.ToLowerInvariant(text[i]);
                if (char.IsLetterOrDigit(c))
                {
                    current[n++] = c;
                    continue;
                }
                if (n > 0)
                {
                    yield return new string(current, 0, n);
                    n = 0;
                }
            }
            if (n > 0)
                yield return new string(current, 0, n);
        }

        static int StableBucket(string token)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < token.Length; i++)
                    hash = (hash * 31) + token[i];
                if (hash == int.MinValue)
                    hash = 0;
                return Math.Abs(hash) % Dimension;
            }
        }

        static void Normalize(float[] vector)
        {
            float mag = 0f;
            for (int i = 0; i < vector.Length; i++)
                mag += vector[i] * vector[i];
            mag = (float)Math.Sqrt(mag);
            if (mag <= 0f)
                return;
            for (int i = 0; i < vector.Length; i++)
                vector[i] /= mag;
        }
    }
}

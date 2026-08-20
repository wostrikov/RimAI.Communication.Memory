using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Ustas.RimAI.Communication.Memory.AI
{
    public static class EmbeddingService
    {
        private static Dictionary<string, float[]> embeddingCache = new Dictionary<string, float[]>();
        private const int MAX_CACHE_SIZE = 500;
        
        private static bool isInitialized = false;
        private static string apiKey = "";
        private static string apiUrl = "";
        private static string provider = "";
        private static int embeddingDimension = 1024; // DeepSeek: 1024, Gemini: 768
        
        public static void Initialize()
        {
            if (isInitialized) return;
            
            try
            {
                Log.Message("[Embedding] v3.3.2.27: semantic embedding вилучено; використовується SuperKeywordEngine");
                return;
            }
            catch (Exception ex)
            {
                Log.Error($"[Embedding] Init failed: {ex.Message}");
                isInitialized = false;
            }
        }
        
        public static bool IsAvailable()
        {
            return false;
        }
        
        public static async Task<float[]> GetEmbeddingAsync(string text)
        {
            if (!IsAvailable()) return null;
            
            if (string.IsNullOrEmpty(text))
                return null;
            
            string cacheKey = GenerateCacheKey(text);
            
            lock (embeddingCache)
            {
                if (embeddingCache.TryGetValue(cacheKey, out float[] cachedVector))
                {
                    if (Prefs.DevMode && UnityEngine.Random.value < 0.01f)
                    {
                        Log.Message($"[Embedding] Cache hit ({embeddingCache.Count}/{MAX_CACHE_SIZE})");
                    }
                    return cachedVector;
                }
            }
            
            if (Prefs.DevMode && UnityEngine.Random.value < 0.2f)
            {
                Log.Message($"[Embedding] API call: {text.Substring(0, Math.Min(30, text.Length))}...");
            }
            
            float[] embedding = await CallEmbeddingAPIAsync(text);
            
            if (embedding != null)
            {
                lock (embeddingCache)
                {
                    if (embeddingCache.Count >= MAX_CACHE_SIZE)
                    {
                        var toRemove = embeddingCache.Keys.Take(50).ToList();
                        foreach (var key in toRemove)
                        {
                            embeddingCache.Remove(key);
                        }
                        
                        if (Prefs.DevMode && UnityEngine.Random.value < 0.1f)
                            Log.Message($"[Embedding] Cache cleanup: {toRemove.Count} removed, {embeddingCache.Count} remain");
                    }
                    
                    embeddingCache[cacheKey] = embedding;
                }
            }
            
            return embedding;
        }
        
        public static async Task<Dictionary<string, float[]>> GetEmbeddingsBatchAsync(List<string> texts)
        {
            var results = new Dictionary<string, float[]>();
            
            if (!IsAvailable() || texts == null || texts.Count == 0)
                return results;
            
            const int BATCH_SIZE = 20;
            
            for (int i = 0; i < texts.Count; i += BATCH_SIZE)
            {
                var batch = texts.Skip(i).Take(BATCH_SIZE).ToList();
                
                foreach (var text in batch)
                {
                    try
                    {
                        var embedding = await GetEmbeddingAsync(text);
                        if (embedding != null)
                        {
                            results[text] = embedding;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[Embedding] Failed to get embedding for text: {ex.Message}");
                    }
                }
                
                if (i + BATCH_SIZE < texts.Count)
                {
                    await Task.Delay(100);
                }
            }
            
            return results;
        }
        
        public static float CosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA == null || vectorB == null)
                return 0f;
            
            if (vectorA.Length != vectorB.Length)
            {
                Log.Error($"[Embedding] Vector dimension mismatch: {vectorA.Length} vs {vectorB.Length}");
                return 0f;
            }
            
            float dotProduct = 0f;
            float magnitudeA = 0f;
            float magnitudeB = 0f;
            
            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += vectorA[i] * vectorA[i];
                magnitudeB += vectorB[i] * vectorB[i];
            }
            
            if (magnitudeA == 0f || magnitudeB == 0f)
                return 0f;
            
            return dotProduct / (float)(Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
        }
        
        private static async Task<float[]> CallEmbeddingAPIAsync(string text)
        {
            try
            {
                if (provider == "Google")
                {
                    return await CallGeminiEmbeddingAsync(text);
                }
                else // DeepSeek, OpenAI
                {
                    return await CallOpenAIStyleEmbeddingAsync(text);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[Embedding] API call failed: {ex.Message}");
                return null;
            }
        }
        
        private static async Task<float[]> CallOpenAIStyleEmbeddingAsync(string text)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                Log.Error("[Embedding] API Key is empty! Please configure it in Mod Settings.");
                return null;
            }
            
            if (Prefs.DevMode)
            {
                Log.Message($"[Embedding] Calling {provider} API...");
                Log.Message($"[Embedding] API URL: {apiUrl}");
                Log.Message($"[Embedding] API Key: {apiKey.Substring(0, Math.Min(10, apiKey.Length))}... (length: {apiKey.Length})");
            }
            
            var request = (HttpWebRequest)WebRequest.Create(apiUrl);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers["Authorization"] = $"Bearer {apiKey}";
            request.Timeout = 10000;
            
            string model = provider == "DeepSeek" ? "deepseek-embedding" : "text-embedding-ada-002";
            string jsonRequest = BuildOpenAIEmbeddingRequest(text, model);
            
            if (Prefs.DevMode)
            {
                Log.Message($"[Embedding] Request body: {jsonRequest.Substring(0, Math.Min(200, jsonRequest.Length))}...");
            }
            
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
            request.ContentLength = bodyRaw.Length;
            
            using (var stream = await request.GetRequestStreamAsync())
            {
                await stream.WriteAsync(bodyRaw, 0, bodyRaw.Length);
            }
            
            try
            {
                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                using (var streamReader = new System.IO.StreamReader(response.GetResponseStream()))
                {
                    string responseText = await streamReader.ReadToEndAsync();
                    
                    if (Prefs.DevMode)
                    {
                        Log.Message($"[Embedding] Response status: {response.StatusCode}");
                        Log.Message($"[Embedding] Response: {responseText.Substring(0, Math.Min(200, responseText.Length))}...");
                    }
                    
                    return ParseOpenAIEmbeddingResponse(responseText);
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    using (var errorResponse = (HttpWebResponse)ex.Response)
                    using (var reader = new System.IO.StreamReader(errorResponse.GetResponseStream()))
                    {
                        string errorText = reader.ReadToEnd();
                        Log.Error($"[Embedding] API Error {errorResponse.StatusCode}: {errorText}");
                    }
                }
                throw;
            }
        }
        
        private static async Task<float[]> CallGeminiEmbeddingAsync(string text)
        {
            var request = (HttpWebRequest)WebRequest.Create(apiUrl);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Timeout = 10000;
            
            string jsonRequest = BuildGeminiEmbeddingRequest(text);
            
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
            request.ContentLength = bodyRaw.Length;
            
            using (var stream = await request.GetRequestStreamAsync())
            {
                await stream.WriteAsync(bodyRaw, 0, bodyRaw.Length);
            }
            
            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            using (var streamReader = new System.IO.StreamReader(response.GetResponseStream()))
            {
                string responseText = await streamReader.ReadToEndAsync();
                return ParseGeminiEmbeddingResponse(responseText);
            }
        }
        
        private static string BuildOpenAIEmbeddingRequest(string text, string model)
        {
            string escapedText = text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
            
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"model\":\"{model}\",");
            sb.Append($"\"input\":\"{escapedText}\"");
            sb.Append("}");
            
            return sb.ToString();
        }
        
        private static string BuildGeminiEmbeddingRequest(string text)
        {
            string escapedText = text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
            
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"content\":{");
            sb.Append("\"parts\":[{");
            sb.Append($"\"text\":\"{escapedText}\"");
            sb.Append("}]");
            sb.Append("}");
            sb.Append("}");
            
            return sb.ToString();
        }
        
        private static float[] ParseOpenAIEmbeddingResponse(string responseText)
        {
            try
            {
                int embeddingStart = responseText.IndexOf("\"embedding\":");
                if (embeddingStart == -1)
                    return null;
                
                int arrayStart = responseText.IndexOf('[', embeddingStart);
                int arrayEnd = responseText.IndexOf(']', arrayStart);
                
                if (arrayStart == -1 || arrayEnd == -1)
                    return null;
                
                string arrayContent = responseText.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
                var values = arrayContent.Split(',');
                
                float[] embedding = new float[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    if (!float.TryParse(values[i].Trim(), out embedding[i]))
                    {
                        Log.Error($"[Embedding] Failed to parse float at index {i}");
                        return null;
                    }
                }
                
                return embedding;
            }
            catch (Exception ex)
            {
                Log.Error($"[Embedding] Parse error: {ex.Message}");
                return null;
            }
        }
        
        private static float[] ParseGeminiEmbeddingResponse(string responseText)
        {
            try
            {
                int valuesStart = responseText.IndexOf("\"values\":");
                if (valuesStart == -1)
                    return null;
                
                int arrayStart = responseText.IndexOf('[', valuesStart);
                int arrayEnd = responseText.IndexOf(']', arrayStart);
                
                if (arrayStart == -1 || arrayEnd == -1)
                    return null;
                
                string arrayContent = responseText.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
                var values = arrayContent.Split(',');
                
                float[] embedding = new float[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    if (!float.TryParse(values[i].Trim(), out embedding[i]))
                    {
                        Log.Error($"[Embedding] Failed to parse float at index {i}");
                        return null;
                    }
                }
                
                return embedding;
            }
            catch (Exception ex)
            {
                Log.Error($"[Embedding] Parse error: {ex.Message}");
                return null;
            }
        }
        
        private static string GenerateCacheKey(string text)
        {
            int hash = text.GetHashCode();
            return $"{provider}_{hash}";
        }
        
        public static void ClearCache()
        {
            lock (embeddingCache)
            {
                int count = embeddingCache.Count;
                embeddingCache.Clear();
                Log.Message($"[Embedding] Cleared {count} cached embeddings");
            }
        }
        
        public static EmbeddingCacheStats GetCacheStats()
        {
            lock (embeddingCache)
            {
                return new EmbeddingCacheStats
                {
                    CachedCount = embeddingCache.Count,
                    MaxCacheSize = MAX_CACHE_SIZE,
                    Provider = provider,
                    Dimension = embeddingDimension,
                    IsInitialized = isInitialized
                };
            }
        }
        
        public static EmbeddingServiceWrapper GetInstance()
        {
            return new EmbeddingServiceWrapper();
        }
        
        public static float[] GetEmbedding(string text)
        {
            try
            {
                var task = GetEmbeddingAsync(text);
                task.Wait(5000);
                return task.Result;
            }
            catch (Exception ex)
            {
                Log.Warning($"[Embedding] Sync GetEmbedding failed: {ex.Message}");
                return null;
            }
        }
    }
    
    public class EmbeddingServiceWrapper
    {
        public float[] GetEmbedding(string text)
        {
            return EmbeddingService.GetEmbedding(text);
        }
        
        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            return await EmbeddingService.GetEmbeddingAsync(text);
        }
    }
    
    public class EmbeddingCacheStats
    {
        public int CachedCount;
        public int MaxCacheSize;
        public string Provider;
        public int Dimension;
        public bool IsInitialized;
    }
}

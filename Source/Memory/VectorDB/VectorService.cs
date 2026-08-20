using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Core.Diagnostics;
using Verse;
using RimWorld;

namespace Ustas.RimAI.Communication.Memory.VectorDB
{
    public class VectorService
    {
    // RimAI.composition: ROOT_OWNED_SINGLETON — constructed and bound by MemoryComposition.Start.
    private static VectorService _instance;
    private static readonly object _instanceLock = new object();
    
    private Dictionary<string, float[]> _loreVectors = new Dictionary<string, float[]>();
    private Dictionary<string, string> _contentHashes = new Dictionary<string, string>();
    private HttpClient _httpClient;
    private bool _isInitialized = false;
    private bool _isSyncing = false;

    /// <summary>
    /// Process-lifetime instance. Prefer access after <see cref="MemoryComposition.Start"/>.
    /// Lazy create remains only as a Scribe safety net if ExposeData runs before Start.
    /// </summary>
    public static VectorService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_instanceLock)
                {
                    if (_instance == null)
                    {
                        _instance = new VectorService();
                    }
                }
            }
            return _instance;
        }
    }

    /// <summary>Binds the root-owned instance from <see cref="MemoryComposition"/>.</summary>
    internal static VectorService BindRootOwned(VectorService service)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));
        lock (_instanceLock)
        {
            _instance = service;
        }
        return service;
    }

    internal VectorService()
    {
        Initialize();
    }

        private void Initialize()
        {
            try
            {
                RimAiLog.Info(RimAiLogCategory.Memory, "[RimAI.Memory] VectorService: Initializing Cloud Embedding Service...");
                _httpClient = new HttpClient();
                _httpClient.Timeout = TimeSpan.FromSeconds(30);
                _isInitialized = true;
                RimAiLog.Info(RimAiLogCategory.Memory, "[RimAI.Memory] VectorService: Cloud Service Initialized.");
            }
            catch (Exception ex)
            {
                // RimAI.exception: TEMPORARY_EXPLICIT_EXCEPTION — VectorService init remains a residual ASYNC/host boundary.
                RimAiLog.Error(RimAiLogCategory.Memory, "[RimAI.Memory] VectorService: Initialization failed.", exception: ex);
                _isInitialized = false;
            }
        }

        public async Task<List<(string id, float similarity)>> FindBestLoreIdsAsync(string userMessage, int topK = 5, float threshold = 0.7f)
        {
            var results = new List<(string id, float similarity)>();
            
            try
            {
                if (!_isInitialized)
                {
                    Log.Warning("[RimAI.Memory] VectorService: Service not initialized.");
                    return results;
                }

                if (string.IsNullOrWhiteSpace(userMessage))
                {
                    return results;
                }

                float[] queryVector = await GetEmbeddingAsync(userMessage).ConfigureAwait(false);
                if (queryVector == null || queryVector.Length == 0)
                {
                    return results;
                }

                var similarities = new List<(string id, float similarity)>();

                lock (_loreVectors)
                {
                    foreach (var kvp in _loreVectors)
                    {
                        float similarity = CosineSimilarity(queryVector, kvp.Value);
                        if (similarity >= threshold)
                        {
                            similarities.Add((kvp.Key, similarity));
                        }
                    }
                }

                results = similarities
                    .OrderByDescending(s => s.similarity)
                    .Take(topK)
                    .ToList();

                return results;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI.Memory] VectorService: Error in FindBestLoreIdsAsync: {ex}");
                return results;
            }
        }

        // Threading/concurrency constraint — do not race this state. (summary FindBestLoreIdsAsync summary)
        [Obsolete("Use FindBestLoreIdsAsync instead to avoid blocking")]
        public List<(string id, float similarity)> FindBestLoreIds(string userMessage, int topK = 5, float threshold = 0.7f)
        {
            return FindBestLoreIdsAsync(userMessage, topK, threshold).GetAwaiter().GetResult();
        }
        
        public void SyncKnowledgeLibrary(CommonKnowledgeLibrary library)
        {
            // Threading/concurrency constraint — do not race this state.
            Task.Run(async () => 
            {
                try
                {
                    if (!_isInitialized) return;
                    if (library == null || library.Entries == null) return;
                    if (_isSyncing) return;

                    _isSyncing = true;

                    var entriesToProcess = library.Entries.Where(e => e != null && e.isEnabled && !string.IsNullOrWhiteSpace(e.content)).ToList();
                    
                    var entriesToUpdate = new List<CommonKnowledgeEntry>();
                    var entriesToRemove = new List<string>();
                    
                    lock (_loreVectors)
                    {
                        foreach (var entry in entriesToProcess)
                        {
                            string currentHash = ComputeHash(entry.content);
                            
                            if (!_contentHashes.ContainsKey(entry.id) || _contentHashes[entry.id] != currentHash)
                            {
                                entriesToUpdate.Add(entry);
                            }
                        }
                        
                        var currentIds = new HashSet<string>(entriesToProcess.Select(e => e.id));
                        foreach (var id in _loreVectors.Keys.ToList())
                        {
                            if (!currentIds.Contains(id))
                            {
                                entriesToRemove.Add(id);
                            }
                        }
                    }

                    if (entriesToUpdate.Count == 0 && entriesToRemove.Count == 0)
                    {
                        Log.Message("[RimAI.Memory] VectorService: No changes detected, skipping sync.");
                        _isSyncing = false;
                        return;
                    }

                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        Messages.Message($"Оновлення векторної бази… ({entriesToUpdate.Count} нових/змінених, {entriesToRemove.Count} видалених)", MessageTypeDefOf.NeutralEvent, false);
                    });

                    Log.Message($"[RimAI.Memory] VectorService: Syncing {entriesToUpdate.Count} updated entries, removing {entriesToRemove.Count} entries...");

                    lock (_loreVectors)
                    {
                        foreach (var id in entriesToRemove)
                        {
                            _loreVectors.Remove(id);
                            _contentHashes.Remove(id);
                        }
                    }

                    int batchSize = 10;
                    int syncedCount = 0;

                    for (int i = 0; i < entriesToUpdate.Count; i += batchSize)
                    {
                        var batch = entriesToUpdate.Skip(i).Take(batchSize).ToList();
                        var texts = batch.Select(e => e.content).ToList();
                        
                        var embeddings = await GetEmbeddingsAsync(texts).ConfigureAwait(false);
                        
                        if (embeddings != null && embeddings.Count == batch.Count)
                        {
                            lock (_loreVectors)
                            {
                                for (int j = 0; j < batch.Count; j++)
                                {
                                    _loreVectors[batch[j].id] = embeddings[j];
                                    _contentHashes[batch[j].id] = ComputeHash(batch[j].content);
                                }
                            }
                            syncedCount += batch.Count;
                        }
                        
                        await Task.Delay(200).ConfigureAwait(false);
                    }

                    Log.Message($"[RimAI.Memory] VectorService: Sync complete! {syncedCount}/{entriesToUpdate.Count} entries vectorized.");
                    
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        Messages.Message($"Векторну базу оновлено ({syncedCount} записів)", MessageTypeDefOf.PositiveEvent, false);
                    });
                }
                catch (Exception ex)
                {
                    Log.Error($"[RimAI.Memory] VectorService: Error syncing library: {ex}");
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        Messages.Message($"Не вдалося оновити векторну базу: {ex.Message}", MessageTypeDefOf.RejectInput, false);
                    });
                }
                finally
                {
                    _isSyncing = false;
                }
            });
        }
        
        public void UpdateKnowledgeVector(string id, string content)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (!_isInitialized) return;
                    if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(content)) return;

                    string currentHash = ComputeHash(content);
                    lock (_loreVectors)
                    {
                        if (_contentHashes.ContainsKey(id) && _contentHashes[id] == currentHash)
                        {
                            return;
                        }
                    }

                    float[] vector = await GetEmbeddingAsync(content).ConfigureAwait(false);
                    if (vector != null)
                    {
                        lock (_loreVectors)
                        {
                            _loreVectors[id] = vector;
                            _contentHashes[id] = currentHash;
                        }
                        Log.Message($"[RimAI.Memory] VectorService: Updated vector for entry {id}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[RimAI.Memory] VectorService: Error updating vector: {ex}");
                }
            });
        }
        
        public void RemoveKnowledgeVector(string id)
        {
            try
            {
                lock (_loreVectors)
                {
                    if (_loreVectors.ContainsKey(id))
                    {
                        _loreVectors.Remove(id);
                        _contentHashes.Remove(id);
                        Log.Message($"[RimAI.Memory] VectorService: Removed vector for entry {id}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI.Memory] VectorService: Error removing vector: {ex}");
            }
        }

        public void ExportVectorsForSave(
            out List<string> ids, 
            out List<List<float>> vectors, 
            out List<string> hashes)
        {
            lock (_loreVectors)
            {
                ids = new List<string>(_loreVectors.Keys);
                vectors = new List<List<float>>();
                hashes = new List<string>();
                
                foreach (var id in ids)
                {
                    vectors.Add(_loreVectors[id].ToList());
                    hashes.Add(_contentHashes.ContainsKey(id) ? _contentHashes[id] : "");
                }
                
                Log.Message($"[RimAI.Memory] VectorService: Exported {ids.Count} vectors for save");
            }
        }

        public void ImportVectorsFromLoad(
            List<string> ids, 
            List<List<float>> vectors, 
            List<string> hashes)
        {
            if (ids == null || vectors == null || hashes == null)
            {
                Log.Warning("[RimAI.Memory] VectorService: Cannot import null vector data");
                return;
            }
                
            lock (_loreVectors)
            {
                _loreVectors.Clear();
                _contentHashes.Clear();
                
                for (int i = 0; i < ids.Count && i < vectors.Count; i++)
                {
                    if (vectors[i] != null)
                    {
                        _loreVectors[ids[i]] = vectors[i].ToArray();
                        if (i < hashes.Count && !string.IsNullOrEmpty(hashes[i]))
                            _contentHashes[ids[i]] = hashes[i];
                    }
                }
                
                Log.Message($"[RimAI.Memory] VectorService: Imported {_loreVectors.Count} vectors from save");
            }
        }

        private static string ComputeHash(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;
            
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(content);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private float[] GetEmbedding(string text)
        {
            return GetEmbeddingAsync(text).GetAwaiter().GetResult();
        }

        private async Task<float[]> GetEmbeddingAsync(string text)
        {
            var results = await GetEmbeddingsAsync(new List<string> { text }).ConfigureAwait(false);
            return results?.FirstOrDefault();
        }

        private async Task<List<float[]>> GetEmbeddingsAsync(List<string> texts)
        {
            try
            {
                var settings = RimTalkMemoryPatchMod.Settings;
                
                string apiKey = string.IsNullOrEmpty(settings.embeddingApiKey) 
                    ? settings.independentApiKey 
                    : settings.embeddingApiKey;
                    
                string apiUrl = settings.embeddingApiUrl;
                string model = settings.embeddingModel;

                if (string.IsNullOrEmpty(apiKey))
                {
                    Log.Warning("[RimAI.Memory] VectorService: API Key is missing. Please configure either Embedding API Key or Independent API Key.");
                    return null;
                }

                var requestBody = new
                {
                    input = texts,
                    model = model
                };

                string jsonBody = JsonConvert.SerializeObject(requestBody);
                
                Log.Message($"[RimAI.Memory] VectorService: Sending request to {apiUrl}");
                Log.Message($"[RimAI.Memory] VectorService: Model: {model}");
                Log.Message($"[RimAI.Memory] VectorService: Input count: {texts.Count}");
                Log.Message($"[RimAI.Memory] VectorService: Request body: {jsonBody}");

                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                using (var request = new HttpRequestMessage(HttpMethod.Post, apiUrl))
                {
                    request.Headers.Add("Authorization", $"Bearer {apiKey}");
                    request.Content = content;

                    var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
                    
                    Log.Message($"[RimAI.Memory] VectorService: Response status: {(int)response.StatusCode} {response.StatusCode}");
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        string errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        Log.Error($"[RimAI.Memory] VectorService: API request failed: {(int)response.StatusCode} ({response.StatusCode})");
                        Log.Error($"[RimAI.Memory] VectorService: Error response: {errorBody}");
                        return null;
                    }

                    string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Log.Message($"[RimAI.Memory] VectorService: Response received, length: {responseString.Length}");
                    
                    JObject jsonResponse = JObject.Parse(responseString);
                    
                    var dataArray = jsonResponse["data"] as JArray;
                    if (dataArray == null)
                    {
                        Log.Error($"[RimAI.Memory] VectorService: No 'data' field in response: {responseString}");
                        return null;
                    }

                    var embeddings = new List<float[]>();
                    foreach (var item in dataArray)
                    {
                        var embeddingArray = item["embedding"]?.ToObject<float[]>();
                        if (embeddingArray != null)
                        {
                            embeddings.Add(embeddingArray);
                        }
                    }

                    Log.Message($"[RimAI.Memory] VectorService: Successfully parsed {embeddings.Count} embeddings");
                    return embeddings;
                }
            }
            catch (HttpRequestException ex)
            {
                Log.Error($"[RimAI.Memory] VectorService: HTTP request failed: {ex.Message}");
                Log.Error($"[RimAI.Memory] VectorService: Stack trace: {ex.StackTrace}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI.Memory] VectorService: Unexpected error: {ex.Message}");
                Log.Error($"[RimAI.Memory] VectorService: Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        private static float CosineSimilarity(float[] vec1, float[] vec2)
        {
            if (vec1.Length != vec2.Length) return 0f;
            float dotProduct = 0f, norm1 = 0f, norm2 = 0f;
            for (int i = 0; i < vec1.Length; i++)
            {
                dotProduct += vec1[i] * vec2[i];
                norm1 += vec1[i] * vec1[i];
                norm2 += vec2[i] * vec2[i];
            }
            if (norm1 == 0f || norm2 == 0f) return 0f;
            return dotProduct / (float)(Math.Sqrt(norm1) * Math.Sqrt(norm2));
        }

        public void Dispose()
        {
            try
            {
                _httpClient?.Dispose();
                _httpClient = null;
                _isInitialized = false;
                Log.Message("[RimAI.Memory] VectorService: Disposed.");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI.Memory] VectorService: Error during disposal: {ex}");
            }
        }
    }
}

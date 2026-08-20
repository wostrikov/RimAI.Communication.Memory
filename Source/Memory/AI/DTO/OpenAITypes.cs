using System;

namespace Ustas.RimAI.Communication.Memory.AI
{
    [Serializable]
    public class OpenAIRequest
    {
        public string model;
        public OpenAIMessage[] messages;
        public float temperature;
        public int max_tokens;
        public bool enable_prompt_cache; // DeepSeek
    }
    
    [Serializable]
    public class OpenAIMessage
    {
        public string role;
        public string content;
        public CacheControl cache_control; // OpenAI Prompt Caching
        public bool cache; // DeepSeek cache
    }
    
    [Serializable]
    public class CacheControl
    {
        public string type;
    }
}
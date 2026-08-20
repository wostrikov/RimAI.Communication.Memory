using System;

namespace Ustas.RimAI.Communication.Memory.AI
{
    [Serializable]
    public class GeminiRequest
    {
        public GeminiContent[] contents;
        public GeminiGenerationConfig generationConfig;
    }
    
    [Serializable]
    public class GeminiContent
    {
        public GeminiPart[] parts;
    }
    
    [Serializable]
    public class GeminiPart
    {
        public string text;
    }
    
    [Serializable]
    public class GeminiGenerationConfig
    {
        public float temperature;
        public int maxOutputTokens;
        public GeminiThinkingConfig thinkingConfig;
    }
    
    [Serializable]
    public class GeminiThinkingConfig
    {
        public int thinkingBudget;
    }
}
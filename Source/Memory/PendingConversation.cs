using System.Collections.Generic;

namespace Ustas.RimAI.Communication.Memory
{
    // Threading/concurrency constraint — do not race this state. (summary summary)
    public readonly struct DialogueLine
    {
        public readonly string SpeakerName;
        
        public readonly string Text;
        
        public DialogueLine(string speakerName, string text)
        {
            SpeakerName = speakerName ?? "???";
            Text = text ?? "";
        }
    }
    
    // Threading/concurrency constraint — do not race this state. (summary summary)
    public class PendingConversation
    {
        // Threading/concurrency constraint — do not race this state. (summary ThingID Pawn summary)
        public List<string> ParticipantThingIds { get; set; }
        
        // Threading/concurrency constraint — do not race this state. (summary summary)
        public List<string> ParticipantNames { get; set; }
        
        // Threading/concurrency constraint — do not race this state. (summary speaker text summary)
        public List<DialogueLine> RawDialogue { get; set; }
        
        public int Timestamp { get; set; }
        
        public string ConversationId { get; set; }
        
        public PendingConversation()
        {
            ParticipantThingIds = new List<string>();
            ParticipantNames = new List<string>();
            RawDialogue = new List<DialogueLine>();
            ConversationId = "conv-" + System.Guid.NewGuid().ToString("N").Substring(0, 12);
        }
    }
    
    // Threading/concurrency constraint — do not race this state. (summary BuildMessages summary)
    public class CachedParticipants
    {
        public List<string> ThingIds { get; set; }
        
        public List<string> Names { get; set; }
        
        public CachedParticipants()
        {
            ThingIds = new List<string>();
            Names = new List<string>();
        }
    }
}
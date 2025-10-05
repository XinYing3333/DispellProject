using UnityEngine;

namespace DefaultNamespace.Thought
{
    [CreateAssetMenu(menuName = "Thoughts/Payload", fileName = "ThoughtPayload")]
    public class ThoughtPayloadSO : ScriptableObject
    {
        public string id;   // 例如 "NoEntryHeadAnimal"
    }
}
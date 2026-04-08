using DefaultNamespace.Tutorial;

namespace EventBus.Events.Tutorial
{
    public class OnTutorialRequirementMet : IEvent
    {        
        public TutorialRequirementType Requirement; 
    }
}

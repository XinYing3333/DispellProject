using DefaultNamespace.EventBus;
using DefaultNamespace.EventBus.Events.UI;
using TMPro;
using UnityEngine;

public class ObjectiveUISetter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private TMP_Text objectiveText;

    private static readonly int ShowHash  = Animator.StringToHash("show");
    private static readonly int CloseHash = Animator.StringToHash("close");

    private EventBinding<RevealObjective> _bindReveal;
    private EventBinding<SetObjective> _bindSet;

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        _bindReveal = new EventBinding<RevealObjective>(_ => Reveal());
        _bindSet = new EventBinding<SetObjective>(Set);

        EventBus<RevealObjective>.Register(_bindReveal);
        EventBus<SetObjective>.Register(_bindSet);  
    }

    private void OnDisable()
    {
        EventBus<RevealObjective>.Deregister(_bindReveal);
        EventBus<SetObjective>.Deregister(_bindSet);
        _bindReveal = null; _bindSet =  null;
    }

    private void Set(SetObjective so)
    {
        // 直接用事件傳來的字串
        if (objectiveText)
            objectiveText.text = so.Text;

        animator.ResetTrigger(CloseHash);
        animator.SetTrigger(ShowHash);
    }

    private void Reveal()
    {
        var text = ObjectiveStore.Instance.HasObjective ? ObjectiveStore.Instance.CurrentText : "";
        if (objectiveText) objectiveText.text = text;

        animator.ResetTrigger(CloseHash);
        animator.SetTrigger(ShowHash);
    }
}
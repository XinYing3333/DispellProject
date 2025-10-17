using DefaultNamespace.EventBus;
using DefaultNamespace.EventBus.Events.UI;
using TMPro;
using UnityEngine;

public class ObjectiveUISetter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private ObjectiveStore store; // 可不填，會自動找單例

    private static readonly int ShowHash  = Animator.StringToHash("show");
    private static readonly int CloseHash = Animator.StringToHash("close");

    private EventBinding<RevealObjective> _bindReveal;
    private EventBinding<HideObjective>   _bindHide;

    private void Reset()
    {
        animator = GetComponent<Animator>();
        store = FindObjectOfType<ObjectiveStore>();
    }

    private void OnEnable()
    {
        if (!store) store = ObjectiveStore.Instance;

        _bindReveal = new EventBinding<RevealObjective>(_ => Reveal());
        _bindHide   = new EventBinding<HideObjective>(_ => Hide());

        EventBus<RevealObjective>.Register(_bindReveal);
        EventBus<HideObjective>.Register(_bindHide);
    }

    private void OnDisable()
    {
        EventBus<RevealObjective>.Deregister(_bindReveal);
        EventBus<HideObjective>.Deregister(_bindHide);
        _bindReveal = null; _bindHide = null;
    }

    private void Reveal()
    {
        var text = store ? store.CurrentText : "";
        if (objectiveText) objectiveText.text = text;

        animator.ResetTrigger(CloseHash);
        animator.SetTrigger(ShowHash);
    }

    private void Hide()
    {
        animator.ResetTrigger(ShowHash);
        animator.SetTrigger(CloseHash);
    }

    // 仍保留直接 API（可選）
    public void ShowObjectiveNow() => Reveal();
    public void CloseObjectiveNow() => Hide();
}
using DefaultNamespace.EventBus;
using DefaultNamespace.EventBus.Events.UI;
using TMPro;
using UnityEngine;

public sealed class ObjectiveUISetter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private TMP_Text objectiveText;

    private static readonly int ShowHash  = Animator.StringToHash("show");
    private static readonly int CloseHash = Animator.StringToHash("close");

    private EventBinding<RevealObjective> _bindReveal;
    private EventBinding<SetObjective> _bindSet;
    private EventBinding<LanguageChanged> _bindLang;

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        _bindReveal = new EventBinding<RevealObjective>(_ => Reveal());
        _bindSet    = new EventBinding<SetObjective>(Set);
        _bindLang   = new EventBinding<LanguageChanged>(_ => RefreshOnly());

        EventBus<RevealObjective>.Register(_bindReveal);
        EventBus<SetObjective>.Register(_bindSet);
        EventBus<LanguageChanged>.Register(_bindLang);
    }

    private void OnDisable()
    {
        EventBus<RevealObjective>.Deregister(_bindReveal);
        EventBus<SetObjective>.Deregister(_bindSet);
        EventBus<LanguageChanged>.Deregister(_bindLang);

        _bindReveal = null; _bindSet = null; _bindLang = null;
    }

    private void Set(SetObjective e)
    {
        if (IsNone(e.Key))
        {
            Hide();
            return;
        }

        Render(e.Key, e.Args);
        PlayShow();
    }


    private void Reveal()
    {
        if (!ObjectiveStore.Instance || !ObjectiveStore.Instance.HasObjective)
        {
            Hide();
            return;
        }

        Render(ObjectiveStore.Instance.CurrentKey,
            ObjectiveStore.Instance.CurrentArgs);
        PlayShow();
    }


    private void RefreshOnly()
    {
        if (!ObjectiveStore.Instance || !ObjectiveStore.Instance.HasObjective)
        {
            Hide();
            return;
        }

        Render(ObjectiveStore.Instance.CurrentKey,
            ObjectiveStore.Instance.CurrentArgs);
    }
    
    private static bool IsNone(string key)
    {
        return string.IsNullOrEmpty(key) || key == "none";
    }

    private void Hide()
    {
        if (!objectiveText) return;

        objectiveText.text = string.Empty;

        if (animator)
        {
            animator.ResetTrigger(ShowHash);
            animator.SetTrigger(CloseHash);
        }
    }


    private void Render(string key, object[] args)
    {
        if (!objectiveText) return;

        var loc = LocalizationService.Instance;
        objectiveText.text = loc ? loc.Get(key, args) : (key ?? "");
    }

    private void PlayShow()
    {
        if (!animator) return;
        animator.ResetTrigger(CloseHash);
        animator.SetTrigger(ShowHash);
    }
}

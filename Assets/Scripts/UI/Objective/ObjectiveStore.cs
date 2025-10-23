using DefaultNamespace.EventBus;
using DefaultNamespace.EventBus.Events.UI;
using DefaultNamespace.UI.Objective;
using UnityEngine;

/// <summary>
/// 僅供取得參數，要改變内容的話用 Event Raise
/// </summary>
public class ObjectiveStore : MonoBehaviour, IObjectiveProvider
{
    public static ObjectiveStore Instance { get; private set; }

    [TextArea, SerializeField] private string currentText;
    public string CurrentText => currentText;
    public bool HasObjective => !string.IsNullOrEmpty(currentText);

    private EventBinding<SetObjective> _bindSet;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        _bindSet = new EventBinding<SetObjective>(OnSet);
        EventBus<SetObjective>.Register(_bindSet);
    }

    private void OnDisable()
    {
        EventBus<SetObjective>.Deregister(_bindSet);
        _bindSet = null;
        if (Instance == this) Instance = null;
    }

    private void OnSet(SetObjective e)
    {
        currentText = e.Text;
    }
}

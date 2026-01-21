using DefaultNamespace.EventBus;
using DefaultNamespace.EventBus.Events.UI;
using DefaultNamespace.UI.Objective;
using UnityEngine;

/// <summary>
/// 僅供取得目前 Objective 狀態；要改內容用 Event Raise
/// </summary>
public sealed class ObjectiveStore : MonoBehaviour, IObjectiveProvider
{
    public static ObjectiveStore Instance { get; private set; }

    [SerializeField] private string currentKey;
    public string CurrentKey => currentKey;

    private object[] _currentArgs;
    public object[] CurrentArgs => _currentArgs;

    public bool HasObjective =>
        !string.IsNullOrEmpty(currentKey) &&
        currentKey != "none";

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
        currentKey  = e.Key;
        _currentArgs = e.Args;
    }
}
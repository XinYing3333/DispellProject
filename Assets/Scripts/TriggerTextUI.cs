using TMPro;
using UnityEditor;
using UnityEngine;

public class TriggerTextUI : MonoBehaviour
{
    [SerializeField] private GameObject triggerUI;
    [SerializeField] private bool isCheckPoint;
    private TMP_Text triggerText;
    [SerializeField][TextArea] private string txtInfo;
    private Transform _playerCheckPoint;

    void Start()
    {
        triggerText = triggerUI.GetComponent<TMP_Text>();
        _playerCheckPoint = GameObject.FindGameObjectWithTag("CheckPoint").transform;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (isCheckPoint)
            {
                _playerCheckPoint.transform.position = transform.position;
            }
            triggerUI.SetActive(false);
            triggerText.text = txtInfo;
            triggerUI.SetActive(true);
            Destroy(gameObject);
        }
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (isCheckPoint)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.yellow;
            style.fontStyle = FontStyle.Bold;

            Handles.Label(transform.position + Vector3.up * 1.5f, "CheckPoint", style);
        }
    }
#endif

}

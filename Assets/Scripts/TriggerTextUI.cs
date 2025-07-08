using System.Collections;
using Cinemachine;
using TMPro;
using UnityEditor;
using UnityEngine;

public class TriggerTextUI : MonoBehaviour
{
    [SerializeField] private GameObject triggerUI;
     private Animator anim;

    [SerializeField] private bool isCheckPoint;
    [SerializeField] private bool isTextAppear;
    [SerializeField] private float waitTime;
    private TMP_Text triggerText;
    [SerializeField] private bool isCameraChange;
    [SerializeField] private CinemachineVirtualCamera cam;
    [SerializeField][TextArea] private string txtInfo;
    private Transform _playerCheckPoint;

    private bool isTrigger;

    void Start()
    {
        triggerText = triggerUI.GetComponentInChildren<TMP_Text>();
        anim = triggerUI.GetComponent<Animator>();
        _playerCheckPoint = GameObject.FindGameObjectWithTag("CheckPoint").transform;
        triggerUI.SetActive(false);
    }
    
    private int triggerID;
    private static int globalTriggerID = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (isTrigger) return;
        if (!other.CompareTag("Player")) return;

        isTrigger = true;
        globalTriggerID++; // 遞增全域 ID
        triggerID = globalTriggerID;

        if (isCheckPoint)
            _playerCheckPoint.position = transform.position;

        if (isCameraChange && cam != null)
            cam.Priority = 100;

        if (isTextAppear)
        {
            triggerUI.SetActive(false);
            triggerText.text = txtInfo;
            triggerUI.SetActive(true);
            anim.SetBool("txtOff", false);
            StartCoroutine(TriggerTextAnimation(triggerID));
        }
    }

    IEnumerator TriggerTextAnimation(int myID)
    {
        yield return new WaitForSeconds(waitTime);

        // ✅ 若本段觸發已不是最新觸發，則不做事
        if (myID != globalTriggerID) yield break;

        anim.SetBool("txtOff", true);
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

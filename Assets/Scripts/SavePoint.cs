using Player;
using TMPro;
using UnityEditor;
using UnityEngine;

public class SavePoint : MonoBehaviour
{
    [SerializeField] private GameObject triggerUI;
    [SerializeField] private bool isCheckPoint;
    private TMP_Text triggerText;
    [SerializeField][TextArea] private string txtInfo;
    private Transform _playerCheckPoint;
    
    [Header("KeyE")]
    [SerializeField] private GameObject keyE;
    
    private bool playerIsClose;

    void Start()
    {
        //triggerText = triggerUI.GetComponent<TMP_Text>();
        _playerCheckPoint = GameObject.FindGameObjectWithTag("CheckPoint").transform;
    }
    
    void Update()
    {
        if (playerIsClose )//&& !DialogManager.GetInstance().dialogIsPlaying) ----------------------記得加回去
        {
            keyE.SetActive(true);
            
            if (PlayerInputHandler.Instance.InteractPressed) 
            {
                AudioManager.Instance.PlaySFX(SFXType.PickUp);
                _playerCheckPoint.transform.position = transform.position;
            }
        }
        else
        {
            keyE.SetActive(false);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsClose = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsClose = false;
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

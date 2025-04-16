using Player;
using UnityEngine;

public class ItemInteract : MonoBehaviour
{
    [SerializeField] private GameObject interactItem; // 每個香火的數值
    [Header("KeyE")]
    [SerializeField] private GameObject keyE;
    
    private PlayerInputHandler _playerInputHandler;
    public bool playerIsClose;
    
    
    void Awake()
    {
        _playerInputHandler = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInputHandler>();
        interactItem.SetActive(false);

    }
    
    void Update()
    {
        if (playerIsClose && !DialogManager.GetInstance().dialogIsPlaying)
        {
            keyE.SetActive(true);
            
            if (_playerInputHandler.InteractPressed) 
            {
                interactItem.SetActive(true);
                AudioManager.Instance.PlaySFX(SFXType.PickUp);
                this.gameObject.GetComponent<MeshRenderer>().material.color = Color.gray;
                Destroy(keyE); // 收集後銷毀物件
                this.enabled = false;
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
}

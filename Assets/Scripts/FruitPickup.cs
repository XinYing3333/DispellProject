using Player;
using UnityEngine;

public class FruitPickUp : MonoBehaviour
{
    [SerializeField] private int fruitValue = 1; // 每個香火的數值
    [Header("KeyE")]
    [SerializeField] private GameObject keyE;
    
    private PlayerInputHandler _playerInputHandler;
    public bool playerIsClose;
    
    
    void Awake()
    {
        _playerInputHandler = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInputHandler>();
    }
    
    void Update()
    {
        if (playerIsClose && !DialogManager.GetInstance().dialogIsPlaying)
        {
            keyE.SetActive(true);
            
            if (_playerInputHandler.InteractPressed) 
            {
                PlayerInventory.Instance.GetFruit(fruitValue);
                AudioManager.Instance.PlaySFX(SFXType.PickUp);
                Destroy(gameObject); // 收集後銷毀物件
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
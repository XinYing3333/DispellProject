using Player;
using UnityEngine;

public class Demo : MonoBehaviour
{
    [Header("Teleporter")]
    [SerializeField] private GameObject teleporter;
    

    private PlayerInputHandler _playerInputHandler;
    public bool playerIsClose;

    void Awake()
    {
        _playerInputHandler = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInputHandler>();
        teleporter.SetActive(false);
    }
    
    void Update()
    {
        if (PlayerInventory.Instance.GetFruitCount())
        {
            transform.GetComponent<NPCDialog>().enabled = false;
        }
        if (playerIsClose && !DialogManager.GetInstance().dialogIsPlaying)
        {
            if (_playerInputHandler.InteractPressed && PlayerInventory.Instance.GetFruitCount())
            {
                teleporter.SetActive(true);
            }
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

using System.Collections;
using System.Collections.Generic;
using Player;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCDialog : MonoBehaviour
{
    [Header("KeyE")]
    [SerializeField] private GameObject keyE;
    
    /*[Header("SceneSwitcher")]
    [SerializeField] private SceneSwitcher sceneSwitcher;*/
    [Header("Emote Animator")]
    [SerializeField] private Animator emoteAnimator;
    
    [Header("Ink JSON")]
    [SerializeField] private TextAsset inkJSON;

    private PlayerInputHandler _playerInputHandler;
    public bool playerIsClose;

    void Awake()
    {
        emoteAnimator = transform.GetChild(0).GetComponent<Animator>();
        _playerInputHandler = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInputHandler>();
    }
    
    void Update()
    {
        if (playerIsClose && !DialogueManager.GetInstance().dialogueIsPlaying)
        {
            keyE.SetActive(true);
            
            if (_playerInputHandler.InteractPressed) 
            {
                Debug.Log("Interact");
                DialogueManager.GetInstance().EnterDialogueMode(inkJSON, emoteAnimator); 
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
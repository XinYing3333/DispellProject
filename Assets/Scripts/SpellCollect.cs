 using System;
 using Player;
using UnityEngine;
using UnityEngine.Serialization;

public class SpellCollect : MonoBehaviour
{
    [SerializeField] private SpellType spellType;
    [Header("KeyE")]
    [SerializeField] private GameObject keyE;

    private MeshRenderer mesh;
    [SerializeField] private Color meshColor;
    
    
    public bool playerIsClose;
    
    [SerializeField] private MonoBehaviour inputSourceRef;
    private IPlayerInputSource input;
    
    private void Start()
    {
        mesh = GetComponent<MeshRenderer>();
        input = inputSourceRef as IPlayerInputSource;
        if (input == null)
            Debug.LogError("Missing valid input source");
    }

    void Update()
    {
        mesh.material.color = meshColor;
        if (playerIsClose)
        {
            keyE.SetActive(true);
            
            if (input.InteractPressed) 
            {
                input.SetSpellType(spellType);
                AudioManager.Instance.PlaySFX(SFXType.PickUp);
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
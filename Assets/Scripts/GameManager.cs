using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private void Awake()
    {
        Instance = this;
    }
    
    private void Start()
    {
        
    }
    
    public void DestroyObject(GameObject obj, float delay = 0f)
    {
        Destroy(obj, delay);
    }
}
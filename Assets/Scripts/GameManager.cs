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
        IncenseCollectionSystem.LoadIncense(); // 開始時加載存檔
    }
    
    public void DestroyObject(GameObject obj, float delay = 0f)
    {
        Destroy(obj, delay);
    }
}
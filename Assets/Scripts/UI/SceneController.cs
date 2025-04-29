using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2)) //Restart
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            CollectionSystem.ClearCollection();
        }
        if (Input.GetKeyDown(KeyCode.Escape)) //Close
        {
            Application.Quit();
        }
    }
    public static void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public static void LoadSceneAsync(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName);
    }
}

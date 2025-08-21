using UnityEngine;
using UnityEditor;

public class MissingScriptFinder : EditorWindow
{
    [MenuItem("Tools/Find Missing Scripts")]
    public static void FindMissingScripts()
    {
        GameObject[] goArray = GameObject.FindObjectsOfType<GameObject>();
        int goCount = 0, componentsCount = 0, missingCount = 0;

        foreach (GameObject go in goArray)
        {
            goCount++;
            Component[] components = go.GetComponents<Component>();

            for (int i = 0; i < components.Length; i++)
            {
                componentsCount++;
                if (components[i] == null)
                {
                    missingCount++;
                    Debug.Log($"Missing script in: {FullPath(go)}", go);
                }
            }
        }

        Debug.Log($"Searched {goCount} GameObjects, {componentsCount} components, found {missingCount} missing");
    }

    private static string FullPath(GameObject go)
    {
        return go.transform.parent == null
            ? go.name
            : FullPath(go.transform.parent.gameObject) + "/" + go.name;
    }
}
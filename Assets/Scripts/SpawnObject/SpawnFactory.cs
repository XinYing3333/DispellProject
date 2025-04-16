using UnityEngine;

public class SpawnFactory 
{
    public static GameObject CreateSpawnObject(SpawnType type, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = null;

        switch (type)
        {
            case SpawnType.StopSign:
                prefab = Resources.Load<GameObject>("Prefabs/StopSign");
                break;
            case SpawnType.WirePole:
                prefab = Resources.Load<GameObject>("Prefabs/WirePole");
                break;
            case SpawnType.MovingPlatform:
                prefab = Resources.Load<GameObject>("Prefabs/MovingPlatform");
                break;
            case SpawnType.PointerA:
                prefab = Resources.Load<GameObject>("Prefabs/Pointer");
                break;
            case SpawnType.PointerB:
                prefab = Resources.Load<GameObject>("Prefabs/Pointer");
                break;
            case SpawnType.PointerC:
                prefab = Resources.Load<GameObject>("Prefabs/Pointer");
                break;
            case SpawnType.PointerD:
                prefab = Resources.Load<GameObject>("Prefabs/Pointer");
                break;
        }

        if (prefab == null) return null;
        AudioManager.Instance.PlaySFX(SFXType.Spawn);
        GameObject instance = Object.Instantiate(prefab, position, rotation);
        return instance;
    }
}

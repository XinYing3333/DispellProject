using UnityEngine;

public class RotateSpell : ISpellEffect
{
    public void ApplyEffect(SpawnObject target)
    {
        if (target.spawnType == SpawnType.PointerA)
        {
            target.spawnType = SpawnType.PointerB;
            target.transform.rotation *= Quaternion.Euler(0, 90, 0);
        }
        else if (target.spawnType == SpawnType.PointerB)
        {
            target.spawnType = SpawnType.PointerC;
            target.transform.rotation *= Quaternion.Euler(0, 90, 0);
        }
        else if (target.spawnType == SpawnType.PointerC)
        {
            target.spawnType = SpawnType.PointerD;
            target.transform.rotation *= Quaternion.Euler(0, 90, 0);
        }
        else if (target.spawnType == SpawnType.PointerD)
        {
            target.spawnType = SpawnType.PointerA;
            target.transform.rotation *= Quaternion.Euler(0, 90, 0);
        }
        else
        {
            Debug.LogWarning("Invalid target type");
        }
    }
}

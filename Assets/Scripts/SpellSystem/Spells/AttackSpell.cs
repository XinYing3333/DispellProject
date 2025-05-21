using UnityEngine;

public class AttackSpell : ISpellEffect
{
    public void ApplyEffect(ThoughtObject target)
    {
        /*switch (target.spawnType)
        {
            case SpawnType.Enemy:
                Debug.Log($"{target.name} is attacked...！");
                target.GetComponent<MeshRenderer>().material.color = Color.red;
                GameManager.Instance.DestroyObject(target.gameObject, 1f);
                break;
            case SpawnType.StopSign:
                var mover = target.GetComponent<StopSignMover>();
                if (mover != null)
                {
                    mover.TriggerMove();
                }
                break;


        }*/
    }
}

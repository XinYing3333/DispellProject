using System.Collections;
using UnityEngine;

public class Spell : MonoBehaviour
{
    public GameObject fxPrefab; 
    private bool fxSpawned = false;    // 控制 FX 只生成一次

    public SpellType spellType; // 設定此子彈的類型

    private MeshRenderer mesh;
    
    private float lifeTime = 0.5f; 
    private float destroyTime = 1.5f; 
    private float counter;

    void Start()
    {
        mesh = GetComponent<MeshRenderer>();
        counter = lifeTime;
        switch (spellType)
        {
            case SpellType.AttackSpell:
                mesh.material.color = Color.red;
                break;
            case SpellType.RotateSpell:
                mesh.material.color = Color.green;
                break;
            case SpellType.ElectricBullet:
                mesh.material.color = Color.yellow;
                break;
        }
    }

    void Update()
    {
        counter -= Time.deltaTime; // 減少計時器
        if (counter <= 0)
        {
            StartCoroutine(SpawnTotem());
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<InteractionPoint>(out InteractionPoint interactionPoint))
        {
            interactionPoint.TriggerInteraction(this , InteractionTriggerType.OnEnter);
        }

        if (other.gameObject.TryGetComponent(out EnemyAI enemy))
        {
            StartCoroutine(SpawnTotem());
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.TryGetComponent<InteractionPoint>(out InteractionPoint interactionPoint))
        {
            interactionPoint.TriggerInteraction(this , InteractionTriggerType.OnStay);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent<InteractionPoint>(out InteractionPoint interactionPoint))
        {
            interactionPoint.TriggerInteraction(this , InteractionTriggerType.OnExit);
        }
    }

    IEnumerator SpawnTotem()
    {
        if (!fxSpawned && fxPrefab != null)
        {
            AudioManager.Instance.PlaySFX(SFXType.Spawn);
            mesh.enabled = false;
            GameObject fx = Instantiate(fxPrefab, transform.position, Quaternion.identity);
            fxSpawned = true;  // 設定 FX 已生成
            yield return new WaitForSeconds(destroyTime);
            Destroy(fx);
            Destroy(gameObject);
        }
    }
}

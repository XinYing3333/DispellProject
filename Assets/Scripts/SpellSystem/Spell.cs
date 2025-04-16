using UnityEngine;

public class Spell : MonoBehaviour
{
    public GameObject fxPrefab; 
    private bool fxSpawned = false;    // 控制 FX 只生成一次

    public SpellType spellType; // 設定此子彈的類型

    private MeshRenderer mesh;
    
    private float lifeTime = 1f; 
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
            SpawnTotem();
        }
    }
    
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.TryGetComponent<SpawnObject>(out SpawnObject spawnObject))
        {
            spawnObject.ApplyEffect(spellType);
            SpawnTotem();
        }
    }

    private void SpawnTotem()
    {
        if (!fxSpawned && fxPrefab != null)
        {
            AudioManager.Instance.PlaySFX(SFXType.Spawn);
            mesh.enabled = false;
            Instantiate(fxPrefab, transform.position, Quaternion.identity);
            fxSpawned = true;  // 設定 FX 已生成
        }
        Destroy(gameObject, 1.5f);
    }
}

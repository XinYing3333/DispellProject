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
                mesh.material.color = Color.yellow;
                break;
            case SpellType.RotateSpell:
                mesh.material.color = Color.green;
                break;
            case SpellType.ElectricBullet:
                mesh.material.color = Color.red;
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
    
    public float homingSpeed = 5f;
    public float rotateSpeed = 5f;
    private Transform target;

    Rigidbody rb;
    
    void FixedUpdate()
    {
        if (target == null) return;

        Vector3 direction = (target.position - transform.position).normalized;
        Vector3 newVelocity = Vector3.Lerp(rb.linearVelocity, direction * homingSpeed, rotateSpeed * Time.fixedDeltaTime);
        rb.linearVelocity = newVelocity;
    }
}

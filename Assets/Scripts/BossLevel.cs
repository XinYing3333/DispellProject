using System.Collections;
using Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class BossLevel : MonoBehaviour
{
    private float bossHealth = 100f;
    [SerializeField] private Slider bossHealthBar; 
    [SerializeField] private CinemachineVirtualCamera bossCam;
    [SerializeField] private Animator _anim;


    private bool firstDetect;
    
    [Header("General Settings")]
    public Transform target;
    public float detectionRange = 10f;
    
    public float stunTime = 2f;
    private bool _isStunned = false;

    private NavMeshAgent agent;
    private SkinnedMeshRenderer mesh;
    private Rigidbody rb;

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        mesh = transform.GetChild(0).GetChild(1).GetComponent<SkinnedMeshRenderer>();
        firstDetect = false;
        bossHealthBar.gameObject.SetActive(false);
    }

    void Update()
    {
        bossHealthBar.value = bossHealth;
        
        if (bossHealth <= 0)
        {
            Debug.Log("Boss Death");
            bossHealthBar.gameObject.SetActive(false);
            bossCam.Priority = 1;
            Destroy(gameObject);
        }
        
        if (target == null || bossHealth <= 0) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget < detectionRange && !_isStunned)
        {
            if (!firstDetect)
            {
                bossHealthBar.gameObject.SetActive(true);
                firstDetect = true;
            }
            _anim.SetBool("isMove", true);
            mesh.material.color = new Color(1f , 0.2783019f, 0.2783019f);

            agent.isStopped = false;
            agent.destination = target.position;        
        }
        /*else if(distanceToTarget > detectionRange && !_isStunned)
        {
            mesh.material.color = new Color(0.3254717f , 0.9400993f, 1f);
            _anim.SetBool("isMove", false);
            agent.isStopped = true;
        }
        else
        {
            agent.isStopped = true;
        }*/
        
    }
    
    int originalLayer;

    IEnumerator OnStun()
    {
        if (!_isStunned)
        {
            bossHealth -= 35f;
        }
        _isStunned = true;
        agent.isStopped = true;
        mesh.material.color = Color.yellow;

        originalLayer = gameObject.layer;
        transform.tag = "Collectible";
        gameObject.layer = LayerMask.NameToLayer("Collectible");
        rb.isKinematic = false;

        yield return new WaitForSeconds(stunTime);

        _isStunned = false;
        mesh.material.color = new Color(1f , 0.2783019f, 0.2783019f);
        transform.tag = "Untagged";
        gameObject.layer = originalLayer;
        rb.isKinematic = true;

    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Spell spell))
        {
            StartCoroutine(OnStun());
        }
        
    }
}

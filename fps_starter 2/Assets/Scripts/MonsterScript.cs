using UnityEngine;
using Unity.FPS.Game;

public class MonsterScript : MonoBehaviour
{
    private Animator anim;
    private Health health;
    private Damageable damageable;
    private Collider monsterCollider;

    public GameObject player;

    [Header("Movement Settings")]
    public float moveSpeed = 2.5f;
    public float rotationSpeed = 5f; //Adjustable turning speed

    [Header("Distance Thresholds")]
    public float detectionRadius = 10f;
    public float aggressiveRadius = 2.5f;

    [Header("Effects")]
    public GameObject deathVFX; //Death particle effect

    private bool hasBeenAggravated = false;
    private bool isAggressive = false;
    private bool isDead = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        
        health = GetComponent<Health>();
        if (health != null)
        {
            health.OnDie += Death;
        }

        damageable = GetComponent<Damageable>();
        monsterCollider = GetComponent<Collider>();

        SetVulnerability(false);
    }

    void Update()
    {
        if (isDead || player == null) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance <= aggressiveRadius)
        {
            anim.SetBool("IsWalking", false);
            FacePlayer();

            if (!hasBeenAggravated)
            {
                hasBeenAggravated = true;
                SetVulnerability(true);
            }

            if (!isAggressive)
            {
                anim.SetTrigger("Threatened");
                isAggressive = true;
            }
        }
        else if (distance <= detectionRadius)
        {
            isAggressive = false;
            FacePlayer();
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
            anim.SetBool("IsWalking", true);
        }
        else
        {
            isAggressive = false;
            anim.SetBool("IsWalking", false);
        }
    }

    void SetVulnerability(bool canTakeDamage)
    {
        if (damageable != null) damageable.enabled = canTakeDamage;
        if (monsterCollider != null) monsterCollider.enabled = canTakeDamage;
    }

    //Smooth Rotation
    void FacePlayer()
    {
        Vector3 direction = (player.transform.position - transform.position).normalized;
        direction.y = 0; // Lock rotation to horizontal axis so monster doesn't tilt up/down

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void Death()
    {
    if (isDead) return;

    isDead = true;
    anim.SetBool("IsWalking", false);
    anim.SetTrigger("Dies");

    if (monsterCollider != null) monsterCollider.enabled = false;

    //Death particles
    if (deathVFX != null)
    {
        Instantiate(deathVFX, transform.position + (Vector3.up * 1.2f), Quaternion.identity);
    }

    //Despawn the monster
    Destroy(gameObject, 5f);
    }
}
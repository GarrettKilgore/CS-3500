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

    [Header("Audio")]
    public AudioClip damageTick; //Sound when hit (same one the template robots use)
    public AudioClip deathSound; //Explosion when it dies
    private int lastDamageSoundFrame = -1;

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
            health.OnDamaged += OnDamaged;
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

    void OnDamaged(float damage, GameObject source)
    {
        //Play the hit sound once per frame (shotgun pellets would otherwise stack)
        if (damageTick != null && Time.frameCount != lastDamageSoundFrame)
        {
            lastDamageSoundFrame = Time.frameCount;
            AudioUtility.CreateSFX(damageTick, transform.position, AudioUtility.AudioGroups.DamageTick, 0f);
        }
    }

#if UNITY_EDITOR
    //Auto-fill the template's hit sound so it works without dragging it in by hand
    void OnValidate()
    {
        if (damageTick == null)
            damageTick = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/FPS/Audio/SFX/Player/Damage_Tick2.mp3");
        if (deathSound == null)
            deathSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/FPS/Audio/SFX/Weapons/Launcher_Explosion.wav");
    }
#endif

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

    //Explosion sound (3D, so it comes from where the bot was)
    if (deathSound != null)
    {
        AudioUtility.CreateSFX(deathSound, transform.position + Vector3.up, AudioUtility.AudioGroups.Impact, 1f, 6f);
    }

    //Despawn the monster
    Destroy(gameObject, 5f);
    }
}
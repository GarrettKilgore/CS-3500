using UnityEngine;

public class MonsterScript : MonoBehaviour
{
    private Animator anim;
    public GameObject player;

    [Header("Movement Settings")]
    public float moveSpeed = 2.5f;

    [Header("Distance Thresholds")]
    public float detectionRadius = 10f; // Distance to start chasing/walking
    public float aggressiveRadius = 2.5f; // Distance to stop and play aggressive animation

    private bool isAggressive = false;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (player == null) return;

        // Measure distance to player
        float distance = Vector3.Distance(transform.position, player.transform.position);

        // Zone 3: Too close -> Stop and play Aggressive animation
        if (distance <= aggressiveRadius)
        {
            // Stop walking
            anim.SetBool("IsWalking", false);

            FacePlayer();

            // Fire the Aggressive trigger once when entering this range
            if (!isAggressive)
            {
                anim.SetTrigger("Threatened");
                isAggressive = true;
            }
        }
        // Zone 2: Within detection range -> Walk toward player
        else if (distance <= detectionRadius)
        {
            // Reset aggressive state so it can taunt again if player steps back and approaches again
            isAggressive = false;

            // Face and move toward player
            FacePlayer();
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);

            // Play walking animation
            anim.SetBool("IsWalking", true);
        }
        // Zone 1: Out of range -> Idle
        else
        {
            isAggressive = false;
            anim.SetBool("IsWalking", false);
        }
    }

    void FacePlayer()
    {
        Vector3 targetPosition = new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z);
        transform.LookAt(targetPosition);
    }
}
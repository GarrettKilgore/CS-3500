using UnityEngine;

/// <summary>
/// Keeps a weather emitter (rain, snow, dust) hovering over the player so the effect
/// is always visible without having to cover the whole level with particles.
/// Particles simulate in World space, so moving the emitter doesn't drag existing drops along.
/// </summary>
public class WeatherFollow : MonoBehaviour
{
    [Tooltip("What to follow. If empty, uses the main camera (the player's view).")]
    public Transform target;

    [Tooltip("Offset from the target. Y is how high above the player the particles spawn.")]
    public Vector3 offset = new Vector3(0f, 15f, 0f);

    void Start()
    {
        // Make sure the weather is running when the level starts, even if Play On Awake got unticked.
        var ps = GetComponent<ParticleSystem>();
        if (ps != null && !ps.isPlaying) ps.Play(true);
    }

    void LateUpdate()
    {
        if (target == null)
        {
            if (Camera.main == null) return;
            target = Camera.main.transform;
        }

        // Follow position only; keep a fixed rotation so rain always falls straight down.
        transform.position = target.position + offset;
        transform.rotation = Quaternion.identity;
    }
}

using UnityEngine;

/// <summary>
/// For one-shot effects (explosions, bursts): fades an optional light out quickly
/// and destroys the whole effect once its particles are done.
/// </summary>
public class VFXBurstLifetime : MonoBehaviour
{
    public float lifetime = 4f;
    public Light flashLight;
    public float lightFadeTime = 0.4f;

    float m_Age;
    float m_StartIntensity;

    void Start()
    {
        if (flashLight != null) m_StartIntensity = flashLight.intensity;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        m_Age += Time.deltaTime;
        if (flashLight != null)
            flashLight.intensity = Mathf.Lerp(m_StartIntensity, 0f, m_Age / lightFadeTime);
    }
}

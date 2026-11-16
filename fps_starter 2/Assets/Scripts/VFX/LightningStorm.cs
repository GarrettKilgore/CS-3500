using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Random lightning strikes while it's raining. Each strike:
///  - draws a jagged bolt (LineRenderer) from the sky to the ground,
///  - bursts a glow particle system in the clouds and a spark particle system where it lands,
///  - flickers the scene's sun light so the whole level flashes.
/// Press L to force a strike (handy for demos).
/// </summary>
public class LightningStorm : MonoBehaviour
{
    [Header("Parts")]
    public LineRenderer bolt;
    public ParticleSystem skyGlow;
    public ParticleSystem groundSparks;

    [Tooltip("Only strike while this rain object is active. Leave empty to always strike.")]
    public ParticleSystem rain;

    [Header("Timing")]
    public float minInterval = 5f;
    public float maxInterval = 12f;

    [Header("Look")]
    public float minDistance = 25f;
    public float maxDistance = 55f;
    public float cloudHeight = 45f;
    public int boltSegments = 16;
    public float boltJitter = 3f;
    public float flashIntensity = 3f;
    public Color flashColor = new Color(0.8f, 0.88f, 1f);

    Light m_Sun;
    float m_SunBaseIntensity;
    Color m_SunBaseColor;
    float m_NextStrike;
    bool m_Striking;

    void Start()
    {
        m_Sun = RenderSettings.sun;
        if (m_Sun == null)
        {
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { m_Sun = l; break; }
        }
        if (m_Sun != null)
        {
            m_SunBaseIntensity = m_Sun.intensity;
            m_SunBaseColor = m_Sun.color;
        }

        if (bolt != null) bolt.enabled = false;
        ScheduleNext();
    }

    void OnDisable()
    {
        // If turned off mid-flash, put the light back.
        StopAllCoroutines();
        m_Striking = false;
        if (bolt != null) bolt.enabled = false;
        if (m_Sun != null) { m_Sun.intensity = m_SunBaseIntensity; m_Sun.color = m_SunBaseColor; }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
            Strike();

        bool raining = rain == null || rain.gameObject.activeInHierarchy;
        if (raining && Time.time >= m_NextStrike)
            Strike();
    }

    void ScheduleNext() => m_NextStrike = Time.time + Random.Range(minInterval, maxInterval);

    public void Strike()
    {
        ScheduleNext();
        if (m_Striking) return;
        var cam = Camera.main;
        if (cam == null) return;

        // Pick a spot around the player, in front-ish so you actually see it.
        Vector3 fwd = cam.transform.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 dir = Quaternion.Euler(0f, Random.Range(-70f, 70f), 0f) * fwd;
        Vector3 basePos = cam.transform.position + dir * Random.Range(minDistance, maxDistance);
        Vector3 top = basePos + Vector3.up * cloudHeight;

        Vector3 ground = new Vector3(basePos.x, cam.transform.position.y - 2f, basePos.z);
        if (Physics.Raycast(top, Vector3.down, out var hit, cloudHeight * 3f, ~0, QueryTriggerInteraction.Ignore))
            ground = hit.point;

        StartCoroutine(StrikeRoutine(top, ground));
    }

    IEnumerator StrikeRoutine(Vector3 top, Vector3 ground)
    {
        m_Striking = true;
        BuildBolt(top, ground);

        if (skyGlow != null) { skyGlow.transform.position = top; skyGlow.Play(true); }
        if (groundSparks != null) { groundSparks.transform.position = ground; groundSparks.Play(true); }

        // Real lightning flickers: flash, dark, brighter flash, then fade.
        yield return Flash(0.6f, 0.07f);
        yield return Flash(0f, 0.05f);
        BuildBolt(top, ground); // re-jag the bolt so the second flash looks different
        yield return Flash(1f, 0.1f);

        float fade = 0.35f;
        for (float t = 0; t < fade; t += Time.deltaTime)
        {
            SetFlash(1f - t / fade);
            yield return null;
        }

        SetFlash(0f);
        m_Striking = false;
    }

    IEnumerator Flash(float amount, float duration)
    {
        SetFlash(amount);
        yield return new WaitForSeconds(duration);
    }

    void SetFlash(float amount)
    {
        if (bolt != null)
        {
            bolt.enabled = amount > 0.01f;
            bolt.widthMultiplier = Mathf.Lerp(0.2f, 0.7f, amount);
        }
        if (m_Sun != null)
        {
            m_Sun.intensity = m_SunBaseIntensity + flashIntensity * amount;
            m_Sun.color = Color.Lerp(m_SunBaseColor, flashColor, amount);
        }
    }

    void BuildBolt(Vector3 top, Vector3 ground)
    {
        if (bolt == null) return;
        bolt.positionCount = boltSegments + 1;
        for (int i = 0; i <= boltSegments; i++)
        {
            float t = (float)i / boltSegments;
            Vector3 p = Vector3.Lerp(top, ground, t);
            if (i > 0 && i < boltSegments)
            {
                // Bigger zig-zags in the middle, pinned at both ends.
                float jitter = boltJitter * Mathf.Sin(t * Mathf.PI);
                p += new Vector3(Random.Range(-jitter, jitter), 0f, Random.Range(-jitter, jitter));
            }
            bolt.SetPosition(i, p);
        }
    }
}

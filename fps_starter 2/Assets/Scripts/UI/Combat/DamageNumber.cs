using TMPro;
using UnityEngine;

/// <summary>
/// One floating damage number (world-space canvas). Floats up, faces the camera,
/// shrinks in from a pop, fades out, then destroys itself.
/// </summary>
public class DamageNumber : MonoBehaviour
{
    public TMP_Text text;
    public float lifetime = 0.9f;
    public float riseSpeed = 1.5f;
    public Color normalColor = new Color(1f, 0.95f, 0.5f);
    public Color killColor = new Color(1f, 0.3f, 0.2f);

    float m_Age;
    Vector3 m_BaseScale;

    public void Setup(float damage, bool killed)
    {
        m_BaseScale = transform.localScale * (killed ? 1.5f : 1f);
        text.text = killed ? $"{Mathf.RoundToInt(damage)}!" : Mathf.RoundToInt(damage).ToString();
        text.color = killed ? killColor : normalColor;
    }

    void LateUpdate()
    {
        m_Age += Time.deltaTime;
        float t = m_Age / lifetime;
        if (t >= 1f) { Destroy(gameObject); return; }

        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        var cam = Camera.main;
        if (cam != null) transform.rotation = cam.transform.rotation; // face the screen

        float pop = 1f + 0.6f * Mathf.Max(0f, 1f - t * 6f); // quick pop at spawn
        transform.localScale = m_BaseScale * pop;

        var c = text.color;
        c.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
        text.color = c;
    }
}

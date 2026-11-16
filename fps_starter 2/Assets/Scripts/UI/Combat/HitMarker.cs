using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space hit marker: an X that flashes over the crosshair when a shot lands.
/// White on a hit, red and bigger on a kill.
/// </summary>
public class HitMarker : MonoBehaviour
{
    public CanvasGroup group;
    public Image[] bars;
    public float showTime = 0.25f;
    public Color hitColor = Color.white;
    public Color killColor = new Color(1f, 0.25f, 0.2f);

    float m_Timer;
    float m_PunchScale = 1f;

    void Start()
    {
        group.alpha = 0f;
        if (CombatStats.Instance != null) CombatStats.Instance.OnPlayerHit += Show;
    }

    void OnDestroy()
    {
        if (CombatStats.Instance != null) CombatStats.Instance.OnPlayerHit -= Show;
    }

    void Show(Vector3 worldPos, float damage, bool killed)
    {
        m_Timer = showTime * (killed ? 2f : 1f);
        m_PunchScale = killed ? 1.6f : 1.25f;
        foreach (var b in bars) b.color = killed ? killColor : hitColor;
    }

    void Update()
    {
        m_Timer -= Time.unscaledDeltaTime;
        group.alpha = Mathf.Clamp01(m_Timer / showTime);
        m_PunchScale = Mathf.Lerp(m_PunchScale, 1f, Time.unscaledDeltaTime * 15f);
        transform.localScale = Vector3.one * m_PunchScale;
    }
}

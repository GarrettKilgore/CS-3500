using UnityEngine;

/// <summary>
/// Spawns a world-space damage number (a copy of the template) above whatever the player hits.
/// </summary>
public class DamageNumbers : MonoBehaviour
{
    [Tooltip("An inactive world-space canvas with a DamageNumber component on it.")]
    public DamageNumber template;

    void Start()
    {
        if (template != null) template.gameObject.SetActive(false);
        if (CombatStats.Instance != null) CombatStats.Instance.OnPlayerHit += Spawn;
    }

    void OnDestroy()
    {
        if (CombatStats.Instance != null) CombatStats.Instance.OnPlayerHit -= Spawn;
    }

    void Spawn(Vector3 worldPos, float damage, bool killed)
    {
        if (template == null) return;
        Vector3 jitter = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0f, 0.3f), Random.Range(-0.4f, 0.4f));
        var number = Instantiate(template, worldPos + jitter, Quaternion.identity);
        number.gameObject.SetActive(true);
        number.Setup(damage, killed);
    }
}

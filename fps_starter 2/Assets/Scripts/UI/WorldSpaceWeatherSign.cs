using TMPro;
using UnityEngine;

/// <summary>
/// World-space UI: a floating sign in the level that turns to face the player
/// and shows live weather stats.
/// </summary>
public class WorldSpaceWeatherSign : MonoBehaviour
{
    public TMP_Text statsText;
    public ParticleSystem rain;
    public ParticleSystem dust;

    [Tooltip("Rotate around Y so the sign always faces the camera.")]
    public bool faceCamera = true;

    void LateUpdate()
    {
        var cam = Camera.main;
        if (faceCamera && cam != null)
        {
            Vector3 dir = transform.position - cam.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        if (statsText == null) return;

        bool rainOn = rain != null && rain.gameObject.activeInHierarchy;
        bool dustOn = dust != null && dust.gameObject.activeInHierarchy;
        float rate = rain != null ? rain.emission.rateOverTime.constant : 0f;

        int alive = 0;
        if (rainOn) alive += rain.particleCount;
        if (dustOn) alive += dust.particleCount;

        statsText.text =
            $"Rain: {(rainOn ? Describe(rate) : "<color=#888>Off</color>")}\n" +
            $"Dust: {(dustOn ? "On" : "<color=#888>Off</color>")}\n" +
            $"Particles: {alive:N0}\n\n" +
            "<size=70%>Press <b>V</b> for weather settings</size>";
    }

    static string Describe(float rate)
    {
        string label = rate < 1000 ? "Drizzle" : rate < 3000 ? "Rain" : rate < 5000 ? "Heavy" : "Storm";
        return $"{label} <size=70%>({rate:N0}/s)</size>";
    }
}

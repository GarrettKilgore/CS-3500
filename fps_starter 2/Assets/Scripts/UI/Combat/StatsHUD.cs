using TMPro;
using UnityEngine;

/// <summary>Screen-space HUD panel showing live run stats.</summary>
public class StatsHUD : MonoBehaviour
{
    public TMP_Text statsText;

    void Update()
    {
        var s = CombatStats.Instance;
        if (s == null || statsText == null) return;

        statsText.text =
            $"<color=#9cf>TIME</color>  {CombatStats.FormatTime(s.TimeElapsed)}\n" +
            $"<color=#9cf>KILLS</color>  {s.Kills}\n" +
            $"<color=#9cf>SHOTS</color>  {s.ShotsFired}\n" +
            $"<color=#9cf>ACCURACY</color>  {s.Accuracy * 100f:0}%";
    }
}

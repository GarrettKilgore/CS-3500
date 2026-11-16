using TMPro;
using Unity.FPS.Game;
using UnityEngine;

/// <summary>
/// Screen-space end-of-run panel. Shown on top of the template's fade-to-black when you
/// win or die, before the Win/Lose scene loads.
/// </summary>
public class GameOverStats : MonoBehaviour
{
    public GameObject panel;
    public CanvasGroup group;
    public TMP_Text titleText;
    public TMP_Text statsText;
    public float fadeInTime = 0.6f;

    bool m_Showing;

    void Start() => panel.SetActive(false);

    void OnEnable()
    {
        EventManager.AddListener<PlayerDeathEvent>(OnLose);
        EventManager.AddListener<AllObjectivesCompletedEvent>(OnWin);
    }

    void OnDisable()
    {
        EventManager.RemoveListener<PlayerDeathEvent>(OnLose);
        EventManager.RemoveListener<AllObjectivesCompletedEvent>(OnWin);
    }

    void OnLose(PlayerDeathEvent evt) => Show(false);
    void OnWin(AllObjectivesCompletedEvent evt) => Show(true);

    void Show(bool win)
    {
        if (m_Showing) return;
        m_Showing = true;

        titleText.text = win ? "MISSION COMPLETE" : "YOU DIED";
        titleText.color = win ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 0.35f, 0.3f);

        var s = CombatStats.Instance;
        statsText.text = s == null ? "" :
            $"Time<pos=60%><b>{CombatStats.FormatTime(s.TimeElapsed)}</b>\n" +
            $"Kills<pos=60%><b>{s.Kills}</b>\n" +
            $"Shots Fired<pos=60%><b>{s.ShotsFired}</b>\n" +
            $"Accuracy<pos=60%><b>{s.Accuracy * 100f:0}%</b>\n" +
            $"Damage Dealt<pos=60%><b>{Mathf.RoundToInt(s.DamageDealt)}</b>";

        group.alpha = 0f;
        panel.SetActive(true);
    }

    void Update()
    {
        if (m_Showing && group.alpha < 1f)
            group.alpha += Time.unscaledDeltaTime / fadeInTime;
    }
}

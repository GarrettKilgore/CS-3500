using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Screen-space (overlay) menu. Press V to open/close.
/// Pauses the game and lets the player toggle rain and dust and change rain intensity.
/// </summary>
public class WeatherSettingsMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject menuRoot;
    public Toggle rainToggle;
    public Toggle dustToggle;
    public Slider rainIntensitySlider;
    public Button resumeButton;

    [Header("Effects")]
    public ParticleSystem rain;
    public ParticleSystem dust;

    [Header("Optional (added by Tools > VFX > Add Lightning Storm)")]
    public Toggle lightningToggle;
    public LightningStorm lightning;

    public bool IsOpen => menuRoot != null && menuRoot.activeSelf;

    void Start()
    {
        menuRoot.SetActive(false);

        // Weather keeps animating while the game is paused so you can see your changes live.
        UseUnscaledTime(rain);
        UseUnscaledTime(dust);

        if (rain != null)
        {
            rainToggle.isOn = rain.gameObject.activeSelf;
            rainIntensitySlider.value = rain.emission.rateOverTime.constant;
        }
        if (dust != null) dustToggle.isOn = dust.gameObject.activeSelf;

        rainToggle.onValueChanged.AddListener(on => SetEffectActive(rain, on));
        dustToggle.onValueChanged.AddListener(on => SetEffectActive(dust, on));
        rainIntensitySlider.onValueChanged.AddListener(SetRainIntensity);
        resumeButton.onClick.AddListener(() => SetOpen(false));

        if (lightningToggle != null && lightning != null)
        {
            lightningToggle.isOn = lightning.enabled;
            lightningToggle.onValueChanged.AddListener(on => lightning.enabled = on);
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
            SetOpen(!IsOpen);
    }

    void LateUpdate()
    {
        // The FPS template re-locks the cursor on every click; keep it free while our menu is open.
        if (IsOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void SetOpen(bool open)
    {
        menuRoot.SetActive(open);
        Time.timeScale = open ? 0f : 1f;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    void SetRainIntensity(float dropsPerSecond)
    {
        if (rain == null) return;
        var emission = rain.emission;
        emission.rateOverTime = dropsPerSecond;
    }

    static void SetEffectActive(ParticleSystem ps, bool on)
    {
        if (ps == null) return;
        ps.gameObject.SetActive(on);
        if (on) ps.Play(true); // re-enabling doesn't restart a system that has Play On Awake off
    }

    static void UseUnscaledTime(ParticleSystem ps)
    {
        if (ps == null) return;
        foreach (var p in ps.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = p.main;
            main.useUnscaledTime = true;
        }
    }
}

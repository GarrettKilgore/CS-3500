using System;
using System.Collections.Generic;
using Unity.FPS.AI;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;

/// <summary>
/// Tracks the player's run: time, kills, shots fired, hits, damage dealt.
/// Other UI scripts (StatsHUD, HitMarker, DamageNumbers, GameOverStats) read from this.
/// </summary>
public class CombatStats : MonoBehaviour
{
    public static CombatStats Instance { get; private set; }

    /// Fired when the player damages something: (world position, damage, was it a kill?)
    public event Action<Vector3, float, bool> OnPlayerHit;

    public float TimeElapsed { get; private set; }
    public int Kills { get; private set; }
    public int ShotsFired { get; private set; }
    public int ShotsHit { get; private set; }
    public float DamageDealt { get; private set; }
    public bool RunOver { get; private set; }
    public float Accuracy => ShotsFired == 0 ? 0f : Mathf.Clamp01((float)ShotsHit / ShotsFired);

    GameObject m_Player;
    Health m_PlayerHealth;
    PlayerWeaponsManager m_WeaponsManager;
    readonly HashSet<Health> m_TrackedHealth = new HashSet<Health>();
    readonly HashSet<WeaponController> m_TrackedWeapons = new HashSet<WeaponController>();
    int m_LastHitFrame = -1;
    float m_NextScan;

    void Awake() => Instance = this;

    void OnEnable()
    {
        EventManager.AddListener<EnemyKillEvent>(OnEnemyKilled);
        EventManager.AddListener<PlayerDeathEvent>(OnPlayerDied);
        EventManager.AddListener<AllObjectivesCompletedEvent>(OnAllObjectivesDone);
    }

    void OnDisable()
    {
        EventManager.RemoveListener<EnemyKillEvent>(OnEnemyKilled);
        EventManager.RemoveListener<PlayerDeathEvent>(OnPlayerDied);
        EventManager.RemoveListener<AllObjectivesCompletedEvent>(OnAllObjectivesDone);
    }

    void Update()
    {
        if (!RunOver) TimeElapsed += Time.deltaTime;

        // Weapons and enemies can appear after Start (weapon pickups, spawns), so rescan now and then.
        if (Time.unscaledTime >= m_NextScan)
        {
            m_NextScan = Time.unscaledTime + 0.5f;
            Scan();
        }
    }

    void Scan()
    {
        if (m_Player == null)
        {
            var pcc = FindAnyObjectByType<PlayerCharacterController>();
            if (pcc == null) return;
            m_Player = pcc.gameObject;
            m_PlayerHealth = pcc.GetComponent<Health>();
            m_WeaponsManager = pcc.GetComponent<PlayerWeaponsManager>();
        }

        // Player's weapon slots -> count shots.
        if (m_WeaponsManager != null)
        {
            for (int i = 0; i < 9; i++)
            {
                var weapon = m_WeaponsManager.GetWeaponAtSlotIndex(i);
                if (weapon != null && m_TrackedWeapons.Add(weapon))
                    weapon.OnShootProcessed += () => { if (!RunOver) ShotsFired++; };
            }
        }

        // Anything with Health (template enemies, custom monsters like the Y Bot) -> listen for damage from the player.
        foreach (var h in FindObjectsByType<Health>(FindObjectsSortMode.None))
        {
            if (h == m_PlayerHealth || !m_TrackedHealth.Add(h)) continue;
            h.OnDamaged += (amount, source) => OnSomethingDamaged(h, amount, source);
        }
    }

    void OnSomethingDamaged(Health target, float amount, GameObject source)
    {
        if (RunOver || source == null || source != m_Player) return;

        DamageDealt += amount;

        // A shotgun blast hits with several pellets in one frame; count that as one hit shot.
        if (Time.frameCount != m_LastHitFrame)
        {
            m_LastHitFrame = Time.frameCount;
            ShotsHit++;
        }

        bool killed = target.CurrentHealth <= 0f;

        // Template enemies report kills through EnemyKillEvent; custom ones (Y Bot) don't, so count those here.
        if (killed && target.GetComponent<EnemyController>() == null) Kills++;

        OnPlayerHit?.Invoke(TopOf(target), amount, killed);
    }

    static Vector3 TopOf(Health target)
    {
        var col = target.GetComponentInChildren<Collider>();
        if (col != null) return col.bounds.center + Vector3.up * col.bounds.extents.y;
        return target.transform.position + Vector3.up * 2f;
    }

    void OnEnemyKilled(EnemyKillEvent evt)
    {
        if (!RunOver) Kills++;
    }

    void OnPlayerDied(PlayerDeathEvent evt) => RunOver = true;
    void OnAllObjectivesDone(AllObjectivesCompletedEvent evt) => RunOver = true;

    public static string FormatTime(float seconds)
    {
        int s = Mathf.FloorToInt(seconds);
        return $"{s / 60}:{s % 60:00}";
    }
}

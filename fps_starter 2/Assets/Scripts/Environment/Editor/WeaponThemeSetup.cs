using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Environment > Apply Storm Theme To Weapons
/// Re-colors the weapons to match the storm environment: blue-steel metal, and the glowing parts
/// switched to the wall light colors (cyan / orange). Makes recolored COPIES of the template
/// textures + materials and points the weapon prefabs at them; the template originals are untouched.
/// </summary>
public static class WeaponThemeSetup
{
    const string Folder = "Assets/Scripts/Environment/Generated/Weapons";

    static readonly Color Cyan = new Color(0.2f, 0.85f, 1f);
    static readonly Color Orange = new Color(1f, 0.55f, 0.15f);
    static readonly Color SteelDark = new Color(0.1f, 0.12f, 0.16f);
    static readonly Color SteelLight = new Color(0.7f, 0.76f, 0.86f);

    struct WeaponJob { public string prefab; public Color accent; public string accentName; }

    static readonly WeaponJob[] Jobs =
    {
        new WeaponJob { prefab = "Assets/FPS/Prefabs/Weapons/Weapon_Blaster.prefab",  accent = Cyan,   accentName = "Cyan" },
        new WeaponJob { prefab = "Assets/FPS/Prefabs/Weapons/Weapon_Shotgun.prefab",  accent = Orange, accentName = "Orange" },
        new WeaponJob { prefab = "Assets/FPS/Prefabs/Weapons/Weapon_Launcher.prefab", accent = Cyan,   accentName = "Cyan" },
        new WeaponJob { prefab = "Assets/FPS/Prefabs/Projectiles/Projectile_Disc.prefab", accent = Cyan, accentName = "Cyan" },
    };

    // Recolored textures/materials are cached so shared ones are only generated once per run.
    static Dictionary<string, Texture2D> s_TexCache;
    static Dictionary<string, Material> s_MatCache;

    [MenuItem("Tools/Environment/Apply Storm Theme To Weapons")]
    public static void Apply()
    {
        EnsureFolder();
        s_TexCache = new Dictionary<string, Texture2D>();
        s_MatCache = new Dictionary<string, Material>();
        int changed = 0;

        try
        {
            for (int j = 0; j < Jobs.Length; j++)
            {
                var job = Jobs[j];
                EditorUtility.DisplayProgressBar("Weapon Theme", Path.GetFileName(job.prefab), (float)j / Jobs.Length);

                var root = PrefabUtility.LoadPrefabContents(job.prefab);
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                    var mats = r.sharedMaterials;
                    bool any = false;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] == null || mats[i].name.StartsWith("Storm_")) continue;
                        if (!AssetDatabase.GetAssetPath(mats[i]).StartsWith("Assets/FPS/Art/Materials/Weapons")) continue;
                        mats[i] = Recolor(mats[i], job);
                        any = true;
                    }
                    if (any) { r.sharedMaterials = mats; changed++; }
                }
                PrefabUtility.SaveAsPrefabAsset(root, job.prefab);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"[Env] Re-themed {changed} weapon renderers. Materials are in {Folder}.");
    }

    static Material Recolor(Material src, WeaponJob job)
    {
        string key = $"Storm_{src.name}_{job.accentName}";
        if (s_MatCache.TryGetValue(key, out var cached)) return cached;

        string path = $"{Folder}/{key}.mat";
        AssetDatabase.DeleteAsset(path);
        var mat = new Material(src) { name = key };

        var baseMap = src.GetTexture("_BaseMap") as Texture2D;
        if (baseMap != null)
            mat.SetTexture("_BaseMap", RecolorTexture(baseMap, job, emissive: false));
        else
            mat.SetColor("_BaseColor", new Color(0.5f, 0.56f, 0.66f, src.GetColor("_BaseColor").a));

        var emissionMap = src.GetTexture("_EmissionMap") as Texture2D;
        if (emissionMap != null)
            mat.SetTexture("_EmissionMap", RecolorTexture(emissionMap, job, emissive: true));

        if (src.IsKeywordEnabled("_EMISSION") || emissionMap != null)
        {
            mat.SetColor("_EmissionColor", job.accent * 2.6f);
            mat.EnableKeyword("_EMISSION");
        }

        if (baseMap == null)
        {
            mat.SetFloat("_Metallic", 0.6f);
            mat.SetFloat("_Smoothness", 0.6f);
        }

        AssetDatabase.CreateAsset(mat, path);
        s_MatCache[key] = mat;
        return mat;
    }

    /// Albedo: gray-steel ramp by brightness, with colorful (green) areas turned into the accent color.
    /// Emission: turned into a white mask so _EmissionColor alone decides the glow color.
    static Texture2D RecolorTexture(Texture2D src, WeaponJob job, bool emissive)
    {
        string srcPath = AssetDatabase.GetAssetPath(src);
        string name = $"Storm_{Path.GetFileNameWithoutExtension(srcPath)}_{(emissive ? "Mask" : job.accentName)}";
        if (s_TexCache.TryGetValue(name, out var cached)) return cached;

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(srcPath)); // read the file directly so import settings don't matter
        var px = tex.GetPixels();

        for (int i = 0; i < px.Length; i++)
        {
            Color c = px[i];
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));

            if (emissive)
            {
                px[i] = new Color(max, max, max, c.a);
                continue;
            }

            float lum = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
            float sat = max <= 0.001f ? 0f : (max - min) / max;
            Color steel = Color.Lerp(SteelDark, SteelLight, Mathf.Pow(lum, 0.8f));
            Color accent = job.accent * Mathf.Lerp(0.4f, 1.1f, max);
            Color o = Color.Lerp(steel, accent, Mathf.SmoothStep(0.25f, 0.6f, sat));
            px[i] = new Color(o.r, o.g, o.b, c.a);
        }

        tex.SetPixels(px);
        tex.Apply();
        string outPath = $"{Folder}/{name}.png";
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(outPath);
        importer.sRGBTexture = true;
        importer.mipmapEnabled = true;
        importer.SaveAndReimport();

        var result = AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
        s_TexCache[name] = result;
        return result;
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scripts/Environment"))
            AssetDatabase.CreateFolder("Assets/Scripts", "Environment");
        if (!AssetDatabase.IsValidFolder("Assets/Scripts/Environment/Generated"))
            AssetDatabase.CreateFolder("Assets/Scripts/Environment", "Generated");
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Scripts/Environment/Generated", "Weapons");
    }
}

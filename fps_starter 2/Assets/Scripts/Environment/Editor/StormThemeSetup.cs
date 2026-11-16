using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Tools > Environment > Apply Storm Theme
/// Generates sci-fi panel wall textures and wet tile floor textures (albedo / normal / emission / smoothness),
/// swaps them onto the level in the open scene (the template's own materials are left untouched),
/// and sets up stormy lighting: fog, dim moonlight, stormy skybox, and a color-graded post-process profile.
/// Save the scene afterward. Tweak the generated materials in Assets/Scripts/Environment/Generated.
/// </summary>
public static class StormThemeSetup
{
    const string Folder = "Assets/Scripts/Environment/Generated";
    const int Size = 512;

    [MenuItem("Tools/Environment/Apply Storm Theme")]
    public static void Apply()
    {
        EnsureFolder();

        try
        {
            EditorUtility.DisplayProgressBar("Storm Theme", "Generating wall textures...", 0.1f);
            var wall = MakeWallMaterial("Env_WallPanel", new Color(0.58f, 0.6f, 0.64f), new Color(0.2f, 0.85f, 1f));
            EditorUtility.DisplayProgressBar("Storm Theme", "Generating dark wall textures...", 0.3f);
            var wallDark = MakeWallMaterial("Env_WallPanelDark", new Color(0.4f, 0.42f, 0.46f), new Color(1f, 0.55f, 0.15f));
            EditorUtility.DisplayProgressBar("Storm Theme", "Generating floor textures...", 0.5f);
            var floor = MakeFloorMaterial("Env_FloorWet", new Color(0.55f, 0.57f, 0.6f));
            EditorUtility.DisplayProgressBar("Storm Theme", "Generating dark floor textures...", 0.7f);
            var floorDark = MakeFloorMaterial("Env_FloorWetDark", new Color(0.4f, 0.42f, 0.45f));

            EditorUtility.DisplayProgressBar("Storm Theme", "Applying to level...", 0.85f);
            var swaps = new Dictionary<string, Material>
            {
                { "Wall_Grey", wall },
                { "Wall_DarkGrey", wallDark },
                { "Floor_Grey", floor },
                { "Floor_DarkGrey", floorDark },
            };
            SwapMaterials(swaps);

            ApplyAtmosphere();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log("[Env] Storm theme applied. Save the scene (Ctrl+S). Materials are in " + Folder);
    }

    // =====================================================================  Material swap

    static void SwapMaterials(Dictionary<string, Material> swaps)
    {
        int swapped = 0;
        var unmatched = new Dictionary<string, int>();

        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                if (swaps.TryGetValue(mats[i].name, out var replacement))
                {
                    mats[i] = replacement;
                    changed = true;
                }
                else
                {
                    unmatched.TryGetValue(mats[i].name, out int n);
                    unmatched[mats[i].name] = n + 1;
                }
            }
            if (changed)
            {
                Undo.RecordObject(r, "Apply Storm Theme");
                r.sharedMaterials = mats;
                PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                swapped++;
            }
        }

        Debug.Log($"[Env] Re-textured {swapped} level objects.");
        var top = new List<KeyValuePair<string, int>>(unmatched);
        top.Sort((a, b) => b.Value.CompareTo(a.Value));
        var sb = new System.Text.StringBuilder("[Env] Other materials in the scene (not changed): ");
        for (int i = 0; i < Mathf.Min(8, top.Count); i++) sb.Append($"{top[i].Key} x{top[i].Value}, ");
        Debug.Log(sb.ToString());
    }

    // =====================================================================  Lighting / atmosphere

    static void ApplyAtmosphere()
    {
        // Fog
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.01f;
        RenderSettings.fogColor = new Color(0.3f, 0.34f, 0.4f);

        // Cooler, darker ambient light
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.6f, 0.7f);

        // Moonlight
        var sun = RenderSettings.sun;
        if (sun == null)
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sun = l; break; }
        if (sun != null)
        {
            Undo.RecordObject(sun, "Apply Storm Theme");
            sun.color = new Color(0.8f, 0.86f, 1f);
            sun.intensity = 1.3f;
        }

        // Stormy sky
        var skyPath = $"{Folder}/Env_StormSkybox.mat";
        var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
        if (sky == null)
        {
            sky = new Material(Shader.Find("Skybox/Procedural"));
            AssetDatabase.CreateAsset(sky, skyPath);
        }
        sky.SetFloat("_SunSize", 0.02f);
        sky.SetFloat("_AtmosphereThickness", 0.9f);
        sky.SetColor("_SkyTint", new Color(0.45f, 0.5f, 0.58f));
        sky.SetColor("_GroundColor", new Color(0.3f, 0.32f, 0.36f));
        sky.SetFloat("_Exposure", 0.9f);
        EditorUtility.SetDirty(sky);
        RenderSettings.skybox = sky;
        DynamicGI.UpdateEnvironment();

        // Post-processing: new profile so the template's is untouched.
        var volume = Object.FindAnyObjectByType<Volume>();
        if (volume == null)
        {
            var vGO = new GameObject("Global Volume (Storm)");
            Undo.RegisterCreatedObjectUndo(vGO, "Apply Storm Theme");
            volume = vGO.AddComponent<Volume>();
            volume.isGlobal = true;
        }

        var profilePath = $"{Folder}/Env_StormPostProfile.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.value = 0.9f;
            bloom.intensity.value = 1.1f;
            bloom.scatter.value = 0.7f;

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.value = TonemappingMode.ACES;

            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.value = 0.3f;
            color.contrast.value = 15f;
            color.saturation.value = -12f;
            color.colorFilter.value = new Color(0.9f, 0.96f, 1.05f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.value = 0.32f;
            vignette.smoothness.value = 0.4f;

            foreach (var c in profile.components)
            {
                c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(c, profile);
            }
        }

        // (Re)apply values every run so tweaks here take effect on existing profiles too.
        if (profile.TryGet<ColorAdjustments>(out var ca))
        {
            ca.postExposure.value = 0.7f;
            ca.contrast.value = 10f;
            ca.saturation.value = -8f;
            ca.colorFilter.value = new Color(0.94f, 0.98f, 1.03f);
        }
        if (profile.TryGet<Vignette>(out var vg)) vg.intensity.value = 0.25f;
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        Undo.RecordObject(volume, "Apply Storm Theme");
        volume.sharedProfile = profile;
    }

    // =====================================================================  Materials

    static Material MakeWallMaterial(string name, Color baseColor, Color lightColor)
    {
        var albedo = new Color[Size * Size];
        var height = new float[Size * Size];
        var emission = new Color[Size * Size];
        const int panel = Size / 2; // 2x2 panels per texture

        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            int i = y * Size + x;
            int px = x % panel, py = y % panel;
            int panelId = (x / panel) + (y / panel) * 2;
            float edge = Mathf.Min(Mathf.Min(px, panel - 1 - px), Mathf.Min(py, panel - 1 - py));

            // Shape: recessed seams, beveled panel edges, raised bolts, one horizontal groove.
            float h = edge < 3 ? 0f : Mathf.SmoothStep(0.3f, 1f, Mathf.InverseLerp(3, 10, edge));
            if (Mathf.Abs(py - panel * 0.3f) < 2f && px > 20 && px < panel - 20) h -= 0.35f;
            foreach (var b in new[] { new Vector2(14, 14), new Vector2(panel - 15, 14), new Vector2(14, panel - 15), new Vector2(panel - 15, panel - 15) })
            {
                float d = Vector2.Distance(new Vector2(px, py), b);
                if (d < 5f) h += 0.5f * (1f - d / 5f);
            }
            float n = TileNoise(x, y, 0.035f, 7) * 0.6f + TileNoise(x, y, 0.15f, 13) * 0.4f;
            height[i] = h + n * 0.08f;

            // Color: per-panel tint, grime, rain streaks darker toward the bottom of each panel.
            float tint = 1f + (Hash(panelId) - 0.5f) * 0.12f;
            float streak = TileNoise(x * 6, y, 0.012f, 21);
            float bottomDirt = 1f - (float)py / panel;
            float grime = Mathf.Clamp01(0.75f + n * 0.35f - streak * 0.18f * bottomDirt);
            Color c = baseColor * tint * grime;
            if (edge < 3) c *= 0.35f;
            albedo[i] = new Color(c.r, c.g, c.b, 1f);

            // Light strip in the top of panel 0 and the bottom of panel 3.
            bool strip = (panelId == 0 && py > panel - 34 && py < panel - 28 && px > 40 && px < panel - 40)
                      || (panelId == 3 && py > 26 && py < 31 && px > 60 && px < panel - 60);
            emission[i] = strip ? lightColor : Color.black;
            if (strip) albedo[i] = Color.Lerp(albedo[i], lightColor, 0.6f);
        }

        var albedoTex = SaveTexture($"{name}_Albedo", albedo, true, false);
        var normalTex = SaveTexture($"{name}_Normal", HeightToNormal(height, 4f), false, true);
        var emissionTex = SaveTexture($"{name}_Emission", emission, true, false);

        var mat = GetOrCreateLit(name);
        mat.SetTexture("_BaseMap", albedoTex);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetTexture("_BumpMap", normalTex);
        mat.SetFloat("_BumpScale", 1f);
        mat.EnableKeyword("_NORMALMAP");
        mat.SetFloat("_Metallic", 0.35f);
        mat.SetFloat("_Smoothness", 0.45f);
        mat.SetTexture("_EmissionMap", emissionTex);
        mat.SetColor("_EmissionColor", Color.white * 2.5f);
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        mat.SetTextureScale("_BaseMap", new Vector2(0.25f, 0.25f)); // one texture = 4m, so panels are 2m
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material MakeFloorMaterial(string name, Color baseColor)
    {
        var albedo = new Color[Size * Size];
        var height = new float[Size * Size];
        var gloss = new Color[Size * Size];
        const int tile = Size / 4; // 4x4 tiles per texture

        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            int i = y * Size + x;
            int tx = x % tile, ty = y % tile;
            int tileId = (x / tile) + (y / tile) * 4;
            float edge = Mathf.Min(Mathf.Min(tx, tile - 1 - tx), Mathf.Min(ty, tile - 1 - ty));

            float n = TileNoise(x, y, 0.05f, 3) * 0.6f + TileNoise(x, y, 0.2f, 5) * 0.4f;
            float puddle = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.68f, TileNoise(x, y, 0.008f, 9)));

            float h = edge < 2 ? 0f : Mathf.Lerp(0.6f, 1f, Mathf.InverseLerp(2, 6, edge));
            h += n * 0.1f;
            height[i] = Mathf.Lerp(h, 0.85f, puddle * 0.8f); // water fills the bumps

            float tint = 1f + (Hash(tileId + 100) - 0.5f) * 0.15f;
            Color c = baseColor * tint * (0.8f + n * 0.3f);
            if (edge < 2) c *= 0.4f;
            c *= Mathf.Lerp(1f, 0.85f, puddle); // wet = darker
            albedo[i] = new Color(c.r, c.g, c.b, 1f);

            // URP Lit metallic map: R = metallic, A = smoothness. Puddles are mirror-like.
            float smooth = Mathf.Lerp(0.3f + n * 0.15f, 0.92f, puddle);
            gloss[i] = new Color(0.05f, 0f, 0f, smooth);
        }

        var albedoTex = SaveTexture($"{name}_Albedo", albedo, true, false);
        var normalTex = SaveTexture($"{name}_Normal", HeightToNormal(height, 3f), false, true);
        var glossTex = SaveTexture($"{name}_MetallicSmooth", gloss, false, false);

        var mat = GetOrCreateLit(name);
        mat.SetTexture("_BaseMap", albedoTex);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetTexture("_BumpMap", normalTex);
        mat.EnableKeyword("_NORMALMAP");
        mat.SetTexture("_MetallicGlossMap", glossTex);
        mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        mat.SetFloat("_Smoothness", 1f);
        mat.SetTextureScale("_BaseMap", new Vector2(0.25f, 0.25f)); // one texture = 4m, so tiles are 1m
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material GetOrCreateLit(string name)
    {
        string path = $"{Folder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // =====================================================================  Texture helpers

    static Texture2D SaveTexture(string name, Color[] pixels, bool sRGB, bool isNormal)
    {
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false, !sRGB);
        tex.SetPixels(pixels);
        tex.Apply();
        string path = $"{Folder}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = sRGB;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.mipmapEnabled = true;
        importer.anisoLevel = 4;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Color[] HeightToNormal(float[] h, float strength)
    {
        var result = new Color[Size * Size];
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            float l = h[y * Size + (x - 1 + Size) % Size];
            float r = h[y * Size + (x + 1) % Size];
            float d = h[((y - 1 + Size) % Size) * Size + x];
            float u = h[((y + 1) % Size) * Size + x];
            var n = new Vector3((l - r) * strength, (d - u) * strength, 1f).normalized;
            result[y * Size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
        }
        return result;
    }

    /// Perlin noise that wraps seamlessly at the texture edges (blend of four offset samples).
    static float TileNoise(float x, float y, float freq, int seed)
    {
        float W = Size, H = Size;
        x %= W; y %= H;
        float o = seed * 37.13f;
        float a = Mathf.PerlinNoise(x * freq + o, y * freq + o);
        float b = Mathf.PerlinNoise((x - W) * freq + o, y * freq + o);
        float c = Mathf.PerlinNoise(x * freq + o, (y - H) * freq + o);
        float d = Mathf.PerlinNoise((x - W) * freq + o, (y - H) * freq + o);
        float fx = x / W, fy = y / H;
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }

    static float Hash(int n)
    {
        n = (n << 13) ^ n;
        return ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 2147483647f;
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scripts/Environment"))
            AssetDatabase.CreateFolder("Assets/Scripts", "Environment");
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Scripts/Environment", "Generated");
    }
}

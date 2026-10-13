using System;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.App;

/// <summary>Original, cached material textures. No downloaded assets or frame-time generation.</summary>
public static class StudioSceneMaterials
{
    private static readonly Dictionary<string, StandardMaterial3D> Cache = new();

    /// <summary>
    /// Opt-in candidate finish: separate color, microsurface and relief maps.
    /// Existing catalog materials stay unchanged until rendered review accepts
    /// these finishes. All maps are original deterministic data, generated once.
    /// </summary>
    public static StandardMaterial3D CreateDetailed(string hexColor, string family)
    {
        var key = "detailed:" + hexColor + ":" + family;
        if (Cache.TryGetValue(key, out var cached)) return cached;
        const int size = 256;
        var heights = new float[size, size];
        float Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263) ^ 0x51c0ffeeu;
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 65535) / 65535f;
            }
        }
        float Grain(float x, float y, int periodX, int periodY)
        {
            var ix = (int)MathF.Floor(x); var iy = (int)MathF.Floor(y);
            var fx = x - ix; var fy = y - iy;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            float Wrapped(int a, int b) => Hash((a % periodX + periodX) % periodX,
                (b % periodY + periodY) % periodY);
            return Mathf.Lerp(Mathf.Lerp(Wrapped(ix, iy), Wrapped(ix + 1, iy), fx),
                Mathf.Lerp(Wrapped(ix, iy + 1), Wrapped(ix + 1, iy + 1), fx), fy);
        }
        var albedo = Image.CreateEmpty(size, size, false, Image.Format.Rgb8);
        var roughness = Image.CreateEmpty(size, size, false, Image.Format.Rgb8);
        var normal = Image.CreateEmpty(size, size, false, Image.Format.Rgb8);
        var mineral = family is "concrete" or "asphalt" or "plaster" or "gravel";
        var cells = family switch { "plaster" => 64, "asphalt" => 128, "gravel" => 16, "concrete" => 128, _ => 32 };
        var baseRoughness = family switch { "metal" => .42f, "paint" => .58f, "wood" => .72f, _ => .92f };
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            // Broad finish variation plus irregular aggregate. No rows, grids,
            // stripes or geometry overlays; detail is confined to the surface.
            var broad = Grain(x * 4f / size, y * 4f / size, 4, 4) - .5f;
            var middle = Grain(x * (float)cells / size, y * (float)cells / size, cells, cells) - .5f;
            var fine = Hash(x, y) - .5f;
            var grain = family == "wood"
                ? Grain(x * 8f / size + (broad + .5f) * 1.7f, y * 64f / size, 8, 64) - .5f
                : middle;
            var broadAmplitude = family switch { "paint" => .025f, "plaster" => .035f, "metal" => .02f, "concrete" => .025f, "asphalt" => .025f, "gravel" => .035f, _ => .09f };
            var grainAmplitude = family switch { "wood" => .20f, "paint" => .025f, "plaster" => .04f, "metal" => .025f, _ => .10f };
            var value = Mathf.Clamp(.92f + broad * broadAmplitude
                + grain * grainAmplitude + fine * (mineral ? .06f : .015f), .76f, 1);
            albedo.SetPixel(x, y, new Color(value, value, value));
            var r = Mathf.Clamp(baseRoughness + middle * .13f + fine * .05f, .2f, 1);
            roughness.SetPixel(x, y, new Color(r, r, r));
            heights[x, y] = mineral ? middle * .55f + fine * .12f : grain * .16f + fine * .02f;
        }
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var dx = heights[(x + 1) % size, y] - heights[(x + size - 1) % size, y];
            var dy = heights[x, (y + 1) % size] - heights[x, (y + size - 1) % size];
            var n = new Vector3(-dx, -dy, 1).Normalized();
            normal.SetPixel(x, y, new Color(n.X * .5f + .5f, n.Y * .5f + .5f, n.Z * .5f + .5f));
        }
        albedo.GenerateMipmaps(); roughness.GenerateMipmaps(); normal.GenerateMipmaps();
        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(hexColor).SrgbToLinear(),
            AlbedoTexture = ImageTexture.CreateFromImage(albedo),
            Roughness = 1, RoughnessTexture = ImageTexture.CreateFromImage(roughness),
            Metallic = family == "metal" ? .62f : family == "paint" ? .035f : 0,
            NormalEnabled = true, NormalTexture = ImageTexture.CreateFromImage(normal),
            NormalScale = family switch { "paint" => .035f, "plaster" => .08f, "metal" => .035f, "wood" => .12f, _ => .20f },
            Uv1Triplanar = true,
            Uv1Scale = Vector3.One * (family switch { "plaster" => .35f, "concrete" => .18f, "asphalt" => .45f, "gravel" => .65f, _ => .30f }),
        };
        Cache.Add(key, material);
        return material;
    }

    public static StandardMaterial3D Create(string hexColor, string family)
    {
        var key = hexColor + ":" + family;
        if (Cache.TryGetValue(key, out var cached)) return cached;
        // Palette swatches are sRGB; shader-facing diffuse color is linear.
        // Feeding the swatch directly here made broad lit floors wash white.
        var color = new Color(hexColor).SrgbToLinear();
        var material = new StandardMaterial3D
        {
            AlbedoColor = color,
            Metallic = family == "metal" ? 0.78f : family == "paint" ? 0.08f : 0,
            Roughness = family switch { "metal" => 0.34f, "paint" => 0.46f, "rubber" => 0.85f, "wood" => 0.73f, "cardboard" => 0.88f, _ => 0.92f },
            Uv1Triplanar = true,
            // A concrete texture spans the whole visible bay. Small repeating
            // tiles made broad grain read as mottled floor paving at this camera.
            Uv1Scale = family == "concrete" ? new Vector3(0.055f, 0.055f, 0.055f) : new Vector3(0.35f, 0.35f, 0.35f),
        };
        // Low-amplitude, multi-scale aperiodic grain gives material contrast
        // without ruled backgrounds, artificial grime overlays or moire.
        const int size = 256;
        var pixels = Image.CreateEmpty(size, size, false, Image.Format.Rgb8);
        uint seed = 0x51c0ffee;
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
            var fine = (seed & 65535) / 65535f - 0.5f;
            var broad = MathF.Sin(x * 0.027f + MathF.Sin(y * 0.033f)) * MathF.Cos(y * 0.019f + x * 0.011f);
            var amplitude = family == "concrete" ? 0.008f : family == "wood" ? 0.025f : 0.012f;
            var shade = Mathf.Clamp(0.94f + broad * amplitude + fine * 0.035f, 0.80f, 1.0f);
            pixels.SetPixel(x, y, new Color(shade, shade, shade));
        }
        pixels.GenerateMipmaps();
        material.AlbedoTexture = ImageTexture.CreateFromImage(pixels);
        Cache.Add(key, material);
        return material;
    }
}

using System;
using System.Linq;
using Godot;

namespace RungProof.Next.App;

public partial class Main
{
    private Material CreateRetainedGroundFinish(SceneEnvironmentProfile profile, Node3D processRoot, bool outdoor)
    {
        var ground = StudioSceneMaterials.Create(outdoor ? "969C90" : "AAA79F", "concrete");
        if (profile.Variant == "wastewater") return CreateWastewaterGroundFinish(ground);
        if (profile.Variant != "waterfront") return ground;
        var water = processRoot.FindChildren("CHANNEL_water", "MeshInstance3D", true, false)
            .OfType<MeshInstance3D>().FirstOrDefault();
        if (water is null) return ground;
        var channel = ReviewBounds(water);
        // Extend the authored canal's visible ground finish along its Z axis.
        // This styles the existing plane only: no new mesh, collider, flow,
        // feedback or process framing is created. Original finish restores.
        var shader = new Shader
        {
            Code = """
                shader_type spatial;
                uniform vec4 land_color : source_color = vec4(0.588, 0.612, 0.565, 1.0);
                uniform vec4 water_color : source_color = vec4(0.18, 0.69, 0.81, 1.0);
                uniform float channel_min_x;
                uniform float channel_max_x;
                uniform sampler2D ground_grain : filter_linear_mipmap, repeat_enable;
                varying vec3 world_position;
                void vertex() {
                    world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
                }
                void fragment() {
                    float canal = step(channel_min_x, world_position.x) * (1.0 - step(channel_max_x, world_position.x));
                    vec3 grain = texture(ground_grain, world_position.xz * 0.055).rgb;
                    ALBEDO = mix(land_color.rgb * grain, water_color.rgb, canal);
                    ROUGHNESS = mix(0.92, 0.65, canal);
                }
                """,
        };
        var finish = new ShaderMaterial { Shader = shader };
        // Shader uniforms are supplied in linear space, like the cached ground
        // material. Raw sRGB swatches washed the first rendered banks white.
        finish.SetShaderParameter("land_color", new Color("969C90").SrgbToLinear());
        var waterColor = water.GetActiveMaterial(0) is StandardMaterial3D authoredWater
            ? authoredWater.AlbedoColor : new Color("40BDD1").SrgbToLinear();
        finish.SetShaderParameter("water_color", waterColor);
        finish.SetShaderParameter("channel_min_x", channel.Position.X);
        finish.SetShaderParameter("channel_max_x", channel.End.X);
        finish.SetShaderParameter("ground_grain", ground.AlbedoTexture);
        return finish;
    }

    private Material CreateWastewaterGroundFinish(StandardMaterial3D ground)
    {
        // Route colors are regions on the retained floor, not nearly coplanar
        // overlay meshes. This prevents distant split-view depth interference
        // while preserving the authored datum and the original floor geometry.
        var shader = new Shader { Code = """
            shader_type spatial;
            uniform vec4 apron_color : source_color;
            uniform vec4 route_color : source_color;
            uniform sampler2D ground_grain : filter_linear_mipmap, repeat_enable;
            uniform vec4 route0;
            uniform vec4 route1;
            uniform vec4 route2;
            uniform vec4 route3;
            varying vec3 world_position;
            void vertex() { world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz; }
            float within_route(vec4 r) {
                vec2 low = step(r.xy, world_position.xz);
                vec2 high = step(world_position.xz, r.zw);
                return low.x * low.y * high.x * high.y;
            }
            void fragment() {
                float route = max(max(within_route(route0), within_route(route1)), max(within_route(route2), within_route(route3)));
                vec3 grain = texture(ground_grain, world_position.xz * 0.055).rgb;
                ALBEDO = mix(apron_color.rgb, route_color.rgb, route) * grain;
                ROUGHNESS = 0.92;
            }
            """ };
        var finish = new ShaderMaterial { Shader = shader };
        finish.SetShaderParameter("apron_color", new Color("A29F90").SrgbToLinear());
        finish.SetShaderParameter("route_color", new Color("C1B9A4").SrgbToLinear());
        finish.SetShaderParameter("ground_grain", ground.AlbedoTexture);
        var routes = _demoEnvironment!.FindChildren("*", "", true, false).OfType<Node3D>()
            .Where(n => n.HasMeta("localRouteBounds")).ToArray();
        if (routes.Length != 4) throw new InvalidOperationException("Wastewater ground requires four actual route reserves.");
        for (var i = 0; i < routes.Length; i++)
        {
            var bounds = routes[i].GetParent<Node3D>().GlobalTransform * routes[i].GetMeta("localRouteBounds").AsAabb();
            finish.SetShaderParameter("route" + i, new Vector4(bounds.Position.X, bounds.Position.Z, bounds.End.X, bounds.End.Z));
        }
        return finish;
    }

    private Godot.Environment? _dispatchPreviousEnvironment;
    private Color _dispatchPreviousKeyColor, _dispatchPreviousFillColor;
    private float _dispatchPreviousKeyEnergy, _dispatchPreviousFillEnergy;
    private float _dispatchPreviousKeyShadowBlur, _dispatchPreviousFillShadowBlur;

    private void ApplyDispatchLook(Node3D processRoot, SceneEnvironmentProfile profile, bool dispatchStructuralFinishes, string sceneId)
    {
        var world = GetNode<WorldEnvironment>("WorldEnvironment");
        var key = GetNode<DirectionalLight3D>("KeyLight");
        var fill = GetNode<DirectionalLight3D>("FillLight");
        _dispatchPreviousEnvironment = world.Environment;
        _dispatchPreviousKeyColor = key.LightColor; _dispatchPreviousKeyEnergy = key.LightEnergy;
        _dispatchPreviousFillColor = fill.LightColor; _dispatchPreviousFillEnergy = fill.LightEnergy;
        _dispatchPreviousKeyShadowBlur=key.ShadowBlur;_dispatchPreviousFillShadowBlur=fill.ShadowBlur;
        // Scene-local resource duplication prevents this lighting treatment
        // leaking into other lessons when their environment is attached.
        var environment = (Godot.Environment)world.Environment.Duplicate();
        var outdoor = profile.Family is SceneEnvironmentFamily.OutdoorTraffic or SceneEnvironmentFamily.OutdoorUtility;
        environment.BackgroundColor = new Color(outdoor ? "a7bdc6" : "919b9f");
        environment.AmbientLightColor = new Color("f2f4f5");
        environment.AmbientLightEnergy = outdoor ? 0.32f : 0.26f;
        world.Environment = environment;
        // Neutral indoor illumination exposes the declared material colors;
        // the previous warm key washed pale surfaces into similar beige walls.
        key.LightColor = new Color("f6f8fa"); key.LightEnergy = outdoor ? 0.88f : 0.60f;
        fill.LightColor = new Color("f1f4f5"); fill.LightEnergy = 0.15f;
        // The reference classroom needs more diffuse under-bench detail and
        // less plaster glare. RestoreDispatchLighting restores both energies
        // before another lesson is composed; this is not a catalog-wide look.
        if (sceneId == "lab-2-01-workstation-call")
        {
            key.LightEnergy = 0.22f;
            fill.LightEnergy = 0.24f;
            environment.AmbientLightEnergy=.20f;
            key.ShadowBlur=1.6f;
        }
        if(sceneId=="lab-4-08-package-grouping")
        {
            key.LightEnergy=.24f;fill.LightEnergy=.22f;
            environment.AmbientLightEnergy=.18f;
            key.ShadowBlur=1.6f;
        }

        // Override only named imported structural finishes. Signals, controls,
        // labels, rubber texture, sensor lenses and runtime material swaps keep
        // their original resources and functional colors.
        var painted = 0;
        // Catalog expansion retains existing authored machine finishes. Only
        // the previously reviewed conveyor treatment uses these named swaps.
        if (dispatchStructuralFinishes)
        foreach (var mesh in ReviewMeshes(processRoot))
        {
            if (mesh.Mesh is null) continue;
            for (var surface = 0; surface < mesh.Mesh.GetSurfaceCount(); surface++)
            {
                if (mesh.GetActiveMaterial(surface) is not StandardMaterial3D source) continue;
                var name = source.ResourceName;
                if (name.Contains("RAL5010", StringComparison.Ordinal))
                {
                    mesh.SetSurfaceOverrideMaterial(surface, StudioSceneMaterials.Create("55715c", "paint"));
                    painted++;
                }
                else if (name.Contains("RAL5018", StringComparison.Ordinal))
                    mesh.SetSurfaceOverrideMaterial(surface, StudioSceneMaterials.Create("585c55", "paint"));
                else if (name.Contains("Galvanized", StringComparison.Ordinal) || name.Contains("Gearbox_Aluminum", StringComparison.Ordinal))
                    mesh.SetSurfaceOverrideMaterial(surface, StudioSceneMaterials.Create("c2c4c2", "metal"));
            }
        }
        GD.Print($"DISPATCH_LOOK_APPLIED structuralSurfaces={painted} signalColors=preserved lighting=scene-local");
    }

    private void RestoreDispatchLighting()
    {
        if (_dispatchPreviousEnvironment is null) return;
        GetNode<WorldEnvironment>("WorldEnvironment").Environment = _dispatchPreviousEnvironment;
        var key = GetNode<DirectionalLight3D>("KeyLight");
        var fill = GetNode<DirectionalLight3D>("FillLight");
        key.LightColor = _dispatchPreviousKeyColor; key.LightEnergy = _dispatchPreviousKeyEnergy;
        fill.LightColor = _dispatchPreviousFillColor; fill.LightEnergy = _dispatchPreviousFillEnergy;
        key.ShadowBlur=_dispatchPreviousKeyShadowBlur;fill.ShadowBlur=_dispatchPreviousFillShadowBlur;
        _dispatchPreviousEnvironment = null;
    }
}

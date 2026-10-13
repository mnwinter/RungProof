using System;
using Godot;

namespace RungProof.Next.App;

public partial class DemoEnvironmentPresentation
{
    // Purpose-specific open locations. Never attach these context nodes beneath
    // field equipment: field picking, I/O, reset and motion remain authored.
    private bool TryBuildProcessLocation(SceneEnvironmentArtDirection d, Aabb b,
        int sx, int sz, Node3D architecture)
    {
        var ev = d.LayoutVariant == "charging-open-service-island";
        var traffic = d.LayoutVariant == "temporary-road-cutback";
        var gallery = d.LayoutVariant == "open-exhibit-pavilion";
        if (!ev && !traffic && !gallery) return false;

        var protectedBounds = b;
        if (OperatorAccessBounds is { } access)
            protectedBounds = protectedBounds.Merge(new Aabb(access.Position - Position, access.Size));
        // Root supplies actual authored field and full motion margins. Hidden
        // startup vehicle meshes must not be used to reduce this envelope.
        var center = protectedBounds.GetCenter();
        var cu = sx * center.X;
        var cv = sz * center.Z;
        var halfX = protectedBounds.Size.X / 2;
        var halfZ = protectedBounds.Size.Z / 2;
        var rearV = cv + halfZ + .85f;
        var rearU = cu + halfX + .85f;
        Vector3 At(float u, float y, float v) => new(sx * u, y, sz * v);
        var detailedGround = ev || traffic ? StudioSceneMaterials.CreateDetailed("#555B5B", "asphalt") : null;
        Material floor = detailedGround is not null ? CreateProcessLocationGroundFinish(detailedGround,
            StudioSceneMaterials.CreateDetailed("#92918A", "gravel"), StudioSceneMaterials.CreateDetailed("#969D9E", "concrete"), ev)
            : StudioSceneMaterials.CreateDetailed("#A8AEA8", "concrete");
        var steel = ev || traffic ? StudioSceneMaterials.CreateDetailed(ev ? "#6A7560" : "#8B806D", "metal")
            : StudioSceneMaterials.CreateDetailed("#596D69", "metal");
        var roof = ev ? StudioSceneMaterials.CreateDetailed("#68766F", "paint") : StudioSceneMaterials.CreateDetailed("#BAC4B9", "paint");
        var notice = StudioSceneMaterials.Create("E7E9E3", "paint");

        if (GetNodeOrNull<MeshInstance3D>("CatalogContinuousGround") is { } visibleGround)
            visibleGround.MaterialOverride = floor;

        // This lower bearing slab deliberately remains below the catalog's
        // visible top. Every outward post/roof ribbon receives shared ground.
        var extraU = gallery ? 2.5f : 1.2f;
        var extraV = ev ? 2.9f : gallery ? 2.5f : 4.0f;
        Box(architecture, "ProcessLocationContinuousBearingGround",
            new Vector3(protectedBounds.Size.X + extraU + 1.4f, .12f,
                protectedBounds.Size.Z + extraV + 1.6f),
            At(cu + (extraU - 1.4f) / 2, -.10f, cv + (extraV - 1.6f) / 2), floor);

        void Post(string name, float u, float v, float height, float thickness = .16f)
        {
            Box(architecture, name + "BearingFoot", new Vector3(.36f, .045f, .36f), At(u, .0225f, v), steel);
            Box(architecture, name, new Vector3(thickness, height, thickness), At(u, height / 2, v), steel);
        }
        void Sign(string name, float u, float v, string text, float width = 2.2f)
        {
            foreach (var dx in new[] { -.42f * width, .42f * width })
                Post(name + "GroundLeg", u + dx, v, 1.95f, .065f);
            Box(architecture, name + "Backplate", new Vector3(width, .75f, .06f), At(u, 1.65f, v), notice);
            architecture.AddChild(new Label3D
            {
                Name = name + "StaticLegend", Text = text, FontSize = 30, PixelSize = .002f,
                OutlineSize = 0, Modulate = new Color("344344"), Position = At(u, 1.65f, v - .036f),
                RotationDegrees = new Vector3(0, sz > 0 ? 180 : 0, 0),
            });
        }
        void Diffuser(string name, float u, float v, float height, float width)
        {
            Box(architecture, name + "Bracket", new Vector3(.12f, .20f, .28f), At(u, height - .24f, v), steel);
            Box(architecture, name, new Vector3(width, .07f, .23f), At(u, height - .37f, v), notice);
        }

        if (ev)
        {
            // Open charging forecourt. The canopy is peripheral rather than a
            // fake warehouse or roof through the connector/meter field.
            var width = MathF.Max(8.8f, protectedBounds.Size.X + 1.8f);
            var h = MathF.Max(3.0f, protectedBounds.End.Y + .65f);
            var left = cu - width / 2;
            var right = cu + width / 2;
            // A real shelter projects over the rear part of the forecourt.
            // Only roof/headers cross its horizontal footprint, above the full
            // conservative B top; every bearing column stays outside B in X.
            const float canopyDepth = 4.2f;
            // Bearing rows span3.9m; the4.2m roof has an intentional.15m
            // eave overhang at each end, carried by the continuous end header.
            var canopyFront = rearV - 2.45f;
            var canopyBack = rearV + 1.45f;
            var canopyCenter = (canopyFront + canopyBack) / 2;
            foreach (var u in new[] { left, right })
                foreach (var v in new[] { canopyFront, canopyBack })
                    Post("ChargingCanopyColumn", u, v, h, .22f);
            foreach (var v in new[] { canopyFront, canopyBack })
                Box(architecture, "ChargingCanopyLongHeader", new Vector3(width + .16f, .20f, .18f), At(cu, h - .10f, v), steel);
            foreach (var u in new[] { left, right })
                Box(architecture, "ChargingCanopyEndHeader", new Vector3(.22f, .20f, canopyDepth), At(u, h - .10f, canopyCenter), steel);
            Box(architecture, "ChargingCanopyQuietRoof", new Vector3(width + .26f, .16f, canopyDepth), At(cu, h + .08f, canopyCenter), roof);
            foreach (var u in new[] { cu - width * .23f, cu + width * .23f })
                Diffuser("ChargingServiceDiffuser", u, canopyFront, h, 1.2f);
            // Extra slab width matches actual canopy ends, including both feet.
            Box(architecture, "ChargingCanopyGroundContinuation", new Vector3(width + .6f, .08f, canopyDepth + .6f), At(cu, -.065f, canopyCenter), floor);
            Box(architecture, "ChargingRearServiceCurb", new Vector3(width - .1f, .12f, .18f), At(cu, .06f, rearV + 1.75f), steel);
            architecture.SetMeta("locationConstruction", "Open two-bay textured charging forecourt; supported4.2m canopy projects over rear forecourt above full motion envelope,3.9m bearing span+.15m eaves; no fictional utility connection.");
        }
        else if (traffic)
        {
            // An open roadside work area, never a service hut or a classroom.
            // Preserve WORLD X as the actual traffic model's travel axis.
            var gravel = StudioSceneMaterials.CreateDetailed("#9B9585", "gravel");
            var earth = StudioSceneMaterials.CreateDetailed("#76766D", "concrete");
            var workLength = MathF.Max(7.2f, MathF.Min(10.0f, protectedBounds.Size.X * .65f));
            const float workDepth = 2.6f;
            var patchZ = sz * (rearV + 1.35f);
            // One original mineral landform replaces the rectangular
            // tray. Its graded shoulder meets the ground, entirely beyond E.
            using (var terrain = new SurfaceTool())
            {
                terrain.Begin(Mesh.PrimitiveType.Triangles);
                var outline = new Vector3[] {
                    new(-.50f,0,-.50f),new(-.24f,0,-.48f),new(.20f,0,-.50f),new(.50f,0,-.38f),
                    new(.48f,0,.30f),new(.25f,0,.50f),new(-.22f,0,.46f),new(-.50f,0,.28f) };
                var crown = new Vector3[] {
                    new(-.32f,.12f,-.18f),new(-.17f,.22f,-.21f),new(.17f,.16f,-.20f),new(.32f,.11f,-.13f),
                    new(.30f,.20f,.20f),new(.14f,.28f,.30f),new(-.14f,.18f,.28f),new(-.33f,.15f,.15f) };
                Vector3 Land(Vector3 p) => new(center.X + p.X * workLength,p.Y,patchZ + sz * p.Z * workDepth);
                void Tri(Vector3 a,Vector3 b0,Vector3 c0)
                {
                    // Godot uses clockwise front faces; ensure upper landform
                    // triangles face upward regardless of the camera-side sign.
                    if ((b0-a).Cross(c0-a).Y > 0) (b0,c0)=(c0,b0);
                    terrain.AddVertex(a);terrain.AddVertex(b0);terrain.AddVertex(c0);
                }
                var peak = Land(new Vector3(.01f,.23f,.03f));
                for(var i=0;i<outline.Length;i++)
                {
                    var j=(i+1)%outline.Length;
                    Tri(Land(outline[i]),Land(crown[i]),Land(crown[j]));
                    Tri(Land(outline[i]),Land(crown[j]),Land(outline[j]));
                    Tri(Land(crown[i]),peak,Land(crown[j]));
                }
                terrain.GenerateNormals();
                architecture.AddChild(new MeshInstance3D {Name="RoadsideGradedMineralVerge",Mesh=terrain.Commit(),MaterialOverride=gravel});
            }
            foreach(var offset in new[]{-.30f,.28f})
            {
                var barrierX=center.X+workLength*offset;
                var barrierV=rearV+workDepth+.10f;
                foreach(var end in new[]{-.60f,.60f}) Post("WorkEdgeBarrierPost",sx*(barrierX+end),barrierV,.65f,.08f);
                Box(architecture,"WorkEdgeSolidBarrier",new Vector3(1.30f,.24f,.08f),new Vector3(barrierX,.48f,sz*barrierV),earth);
            }
            architecture.SetMeta("locationConstruction", "Open WORLD-X roadwork/verge; aggregate work patch outside protected signal/vehicle envelope; no new road markings or hut.");
            architecture.SetMeta("sourceVehicleTravel", "World X[-4.6,4.6], Z[-1.45,1.45] conservative authored model envelope; root supplies native motion bounds.");
        }
        else
        {
            // A complete open exhibit pavilion, not a plant or loading shed.
            // Roof ribbons cover only its outside perimeter; no roof across B.
            var width = MathF.Max(12.0f, protectedBounds.Size.X + 2.4f);
            var depth = MathF.Max(12.0f, protectedBounds.Size.Z + 2.4f);
            var left = cu - width / 2;
            var front = cv - depth / 2;
            // Keep the connected corner at actual perimeter extent. Both spans
            // share one bearing post; adjacent roof edges meet without overlap.
            var cornerU = MathF.Max(rearU, cu + width / 2);
            var cornerV = MathF.Max(rearV, cv + depth / 2);
            var h = MathF.Max(4.3f, protectedBounds.End.Y + .8f);
            Post("PavilionSharedCornerColumn", cornerU + .625f, cornerV + .625f, h);
            Post("PavilionRearEndColumn", left, cornerV + .625f, h);
            Post("PavilionSideEndColumn", cornerU + .625f, front, h);
            var rearLength = cornerU + 1.25f - left;
            var sideLength = cornerV - front;
            Box(architecture, "PavilionRearRibbon", new Vector3(rearLength, .14f, 1.25f),
                At((left + cornerU + 1.25f) / 2, h + .07f, cornerV + .625f), roof);
            Box(architecture, "PavilionSideRibbon", new Vector3(1.25f, .14f, sideLength),
                At(cornerU + .625f, h + .07f, (front + cornerV) / 2), roof);
            Box(architecture, "PavilionRearContinuousBeam", new Vector3(rearLength, .22f, .20f),
                At((left + cornerU + 1.25f) / 2, h - .11f, cornerV + .625f), steel);
            Box(architecture, "PavilionSideContinuousBeam", new Vector3(.20f, .22f, sideLength),
                At(cornerU + .625f, h - .11f, (front + cornerV) / 2), steel);
            Box(architecture, "PavilionRearGroundContinuation", new Vector3(rearLength + .4f, .08f, 1.85f),
                At((left + cornerU + 1.25f) / 2, -.065f, cornerV + .625f), floor);
            Box(architecture, "PavilionSideGroundContinuation", new Vector3(1.85f, .08f, sideLength + .4f),
                At(cornerU + .625f, -.065f, (front + cornerV) / 2), floor);
            Sign("GalleryDriveTaxonomy", cu - width * .22f, cornerV + .62f,
                "DRIVES / MATERIAL HANDLING\nSENSING", 3.0f);
            Sign("GalleryProcessTaxonomy", cornerU + .6f, cv + depth * .20f,
                "PROCESS / MOTION\nOPERATOR DEVICES", 2.5f);
            var masonry=StudioSceneMaterials.CreateDetailed("#C1B39C","plaster");
            var cap=StudioSceneMaterials.CreateDetailed("#7B8277","metal");
            // Low connected courtyard boundaries sit beyond the entire plant
            // and standing envelope. Front and left approaches stay open.
            var rearCenter=(left+cornerU+1.25f)/2;
            Box(architecture,"ExhibitionRearBoundary",new Vector3(rearLength,.70f,.18f),At(rearCenter,.35f,cornerV+1.0f),masonry);
            Box(architecture,"ExhibitionRearBoundaryCap",new Vector3(rearLength+.04f,.05f,.24f),At(rearCenter,.725f,cornerV+1.0f),cap);
            Box(architecture,"ExhibitionSideBoundary",new Vector3(.18f,.70f,sideLength),At(cornerU+1.0f,.35f,(front+cornerV)/2),masonry);
            Box(architecture,"ExhibitionSideBoundaryCap",new Vector3(.24f,.05f,sideLength),At(cornerU+1.0f,.725f,(front+cornerV)/2),cap);
            // Entry uses the open forecourt. The rear canopy corner carries a
            // bearing column and boundary, so it cannot serve as a doorway.
            Diffuser("PavilionRearEdgeFixture", cu, cornerV + .625f, h, 1.5f);
            architecture.SetMeta("locationConstruction", "Open connected exhibition pavilion with L-shaped supported perimeter canopy; 21 independent authored display models; no opaque room, hut or false process wiring.");
        }

        var approach = new Node3D { Name = "ProcessLocationIllustrativeApproach" };
        var routeZ = protectedBounds.Position.Z - sz * .65f;
        if (sz < 0) routeZ = protectedBounds.End.Z + .65f;
        approach.SetMeta("localApproachReserve", new Aabb(
            new Vector3(protectedBounds.Position.X, 0, routeZ - .60f),
            new Vector3(protectedBounds.Size.X, 2.0f, 1.2f)));
        approach.SetMeta("scope", "Proposed open foreground reserve; current native operator standing region and full obstacle/ray checks remain authoritative. No physical circulation proof.");
        if (OperatorAccessBounds is { } currentAccess)
        {
            var localAccess = new Aabb(currentAccess.Position - Position, currentAccess.Size);
            var destination = new Vector3(localAccess.GetCenter().X, 0,
                sz < 0 ? localAccess.End.Z : localAccess.Position.Z);
            var routePoints = new Godot.Collections.Array<Vector3>
            {
                new(protectedBounds.Position.X - .65f, 0, routeZ),
                new(destination.X, 0, routeZ), destination,
            };
            if (gallery)
            {
                // Keep the entire 1.2 m corridor, including end caps, clear of
                // display bases. The terminal overlaps the standing region.
                destination.Z += sz * .10f;
                var entryX = protectedBounds.End.X + .65f;
                routePoints = new Godot.Collections.Array<Vector3>
                {
                    new(entryX, 0, routeZ),
                    new(entryX, 0, destination.Z), destination,
                };
                approach.SetMeta("localOpeningBounds", new Aabb(
                    new Vector3(entryX - .60f, 0, routeZ - .60f), new Vector3(1.2f, 2.0f, 1.2f)));
                Sign("ExhibitionForecourtEntry", sx * (entryX - 1.25f), sz * routeZ,
                    "EXHIBITION\nENTRY", 1.0f);
            }
            approach.SetMeta("localApproachPolyline", routePoints);
            approach.SetMeta("proposedSweepWidth", 1.2f);
            approach.SetMeta("localTerminalStandingRegion", localAccess);
            // The polyline is exact proposal data, not a clearance-pass claim:
            // root native verification must test its swept rectangles against
            // occupied process/motion geometry, while allowing the A endpoint.
        }
        architecture.AddChild(approach);
        architecture.SetMeta("purposeLayoutImplemented", true);
        architecture.SetMeta("processLocationExactLayout", d.LayoutVariant);
        architecture.SetMeta("processLocationContextOnly", true);
        return true;
    }

    private static ShaderMaterial CreateProcessLocationGroundFinish(StandardMaterial3D detailed,
        StandardMaterial3D exterior, StandardMaterial3D service, bool charging)
    {
        // Regions live on the existing surface, not coplanar overlay boxes.
        // Reuse the root-owned original aperiodic albedo/roughness/normal maps;
        // this method creates no textures and no process geometry.
        var shader = new Shader { Code = """
            shader_type spatial;
            uniform vec4 exterior_color : source_color;
            uniform vec4 travel_color : source_color;
            uniform vec4 service_color : source_color;
            uniform vec4 travel_rectangle;
            uniform float service_start_z;
            uniform sampler2D ground_grain : filter_linear_mipmap, repeat_enable;
            uniform sampler2D ground_roughness : filter_linear_mipmap, repeat_enable;
            uniform sampler2D ground_normal : filter_linear_mipmap, repeat_enable;
            uniform sampler2D exterior_grain : filter_linear_mipmap, repeat_enable;
            uniform sampler2D exterior_roughness : filter_linear_mipmap, repeat_enable;
            uniform sampler2D exterior_normal : filter_linear_mipmap, repeat_enable;
            uniform sampler2D service_grain : filter_linear_mipmap, repeat_enable;
            uniform sampler2D service_roughness : filter_linear_mipmap, repeat_enable;
            uniform sampler2D service_normal : filter_linear_mipmap, repeat_enable;
            varying vec3 world_position;
            void vertex() { world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz; }
            void fragment() {
                vec2 p = world_position.xz;
                vec2 inside_low = smoothstep(travel_rectangle.xy - vec2(0.12), travel_rectangle.xy + vec2(0.12), p);
                vec2 inside_high = 1.0 - smoothstep(travel_rectangle.zw - vec2(0.12), travel_rectangle.zw + vec2(0.12), p);
                float travel = inside_low.x * inside_low.y * inside_high.x * inside_high.y;
                float service = smoothstep(service_start_z - 0.15, service_start_z + 0.15, p.y);
                vec3 surface = mix(exterior_color.rgb, service_color.rgb, service);
                surface = mix(surface, travel_color.rgb, travel);
                vec2 asphalt_uv = p * 0.45;
                vec2 gravel_uv = p * 0.65;
                vec2 concrete_uv = p * 0.18;
                vec3 grain = mix(texture(exterior_grain, gravel_uv).rgb, texture(service_grain, concrete_uv).rgb, service);
                grain = mix(grain, texture(ground_grain, asphalt_uv).rgb, travel);
                float roughness = mix(texture(exterior_roughness, gravel_uv).r, texture(service_roughness, concrete_uv).r, service);
                roughness = mix(roughness, texture(ground_roughness, asphalt_uv).r, travel);
                vec3 normal_map = mix(texture(exterior_normal, gravel_uv).rgb, texture(service_normal, concrete_uv).rgb, service);
                normal_map = mix(normal_map, texture(ground_normal, asphalt_uv).rgb, travel);
                ALBEDO = surface * grain;
                ROUGHNESS = clamp(roughness, 0.65, 1.0);
                NORMAL_MAP = normal_map;
                NORMAL_MAP_DEPTH = 0.22;
            }
            """ };
        var result = new ShaderMaterial { Shader = shader };
        result.SetShaderParameter("exterior_color", new Color(charging ? "#969B98" : "#9A978D").SrgbToLinear());
        result.SetShaderParameter("travel_color", new Color(charging ? "#515958" : "#424B4D").SrgbToLinear());
        result.SetShaderParameter("service_color", new Color(charging ? "#858E8F" : "#969E9E").SrgbToLinear());
        // Both existing candidates use authored WORLD X/Z; source vehicles,
        // bays and road never rotate with the camera. This is passive finish.
        result.SetShaderParameter("travel_rectangle", charging
            ? new Vector4(-6.4f, -3.4f, 6.4f, 2.2f) : new Vector4(-100f, -2.3f, 100f, 2.3f));
        result.SetShaderParameter("service_start_z", charging ? 2.6f : 3.2f);
        result.SetShaderParameter("ground_grain", detailed.AlbedoTexture!);
        result.SetShaderParameter("ground_roughness", detailed.RoughnessTexture!);
        result.SetShaderParameter("ground_normal", detailed.NormalTexture!);
        result.SetShaderParameter("exterior_grain", exterior.AlbedoTexture!);
        result.SetShaderParameter("exterior_roughness", exterior.RoughnessTexture!);
        result.SetShaderParameter("exterior_normal", exterior.NormalTexture!);
        result.SetShaderParameter("service_grain", service.AlbedoTexture!);
        result.SetShaderParameter("service_roughness", service.RoughnessTexture!);
        result.SetShaderParameter("service_normal", service.NormalTexture!);
        return result;
    }
}


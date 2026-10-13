using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

namespace RungProof.Next.App;

public partial class Main
{
    private sealed record BarrelRingGeometry(string Path, Vector3 Center, Vector3 Normal,
        float InnerRadius, float OuterRadius, float PlaneDeviation, int VertexCount);

    // Diagnostic evidence only: read the retained ring vertices without
    // changing geometry, points, plant state, materials, or camera framing.
    private void WriteBarrelInterfaceWitness()
    {
        if (_currentSceneId != "lab-4-11-barrel-fill-station" || _capturePath is null) return;
        var root = _sceneCompositionRoot ?? throw new InvalidOperationException("Barrel witness requires current composition.");
        MeshInstance3D MeshNode(string equipment, string name)
        {
            var matches = root.GetNode<Node3D>(equipment).FindChildren(name, "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToArray();
            if (matches.Length != 1 || !matches[0].IsVisibleInTree() || matches[0].Mesh is null)
                throw new InvalidOperationException("Barrel witness missing unique visible mesh: " + equipment + "/" + name);
            return matches[0];
        }
        Vector3[] WorldFaces(string equipment, string name)
        {
            var mesh = MeshNode(equipment, name);
            var vertices = mesh.Mesh!.GetFaces().Select(v => mesh.GlobalTransform * v).ToArray();
            if (vertices.Length < 3 || vertices.Length % 3 != 0
                || vertices.Any(v => !float.IsFinite(v.X) || !float.IsFinite(v.Y) || !float.IsFinite(v.Z)))
                throw new InvalidOperationException("Barrel witness invalid triangle faces: " + name);
            return vertices;
        }
        BarrelRingGeometry Ring(string equipment, string name)
        {
            var node = root.GetNode<Node3D>(equipment);
            var matches = node.FindChildren(name, "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToArray();
            if (matches.Length != 1 || !matches[0].IsVisibleInTree() || matches[0].Mesh is null)
                throw new InvalidOperationException("Barrel witness missing unique visible ring: " + equipment + "/" + name);
            var mesh = matches[0];
            var vertices = Enumerable.Range(0, mesh.Mesh!.GetSurfaceCount())
                .SelectMany(surface => mesh.Mesh.SurfaceGetArrays(surface)[(int)Mesh.ArrayType.Vertex].AsVector3Array())
                .Select(vertex => mesh.GlobalTransform * vertex).ToArray();
            if (vertices.Length < 3 || vertices.Any(v => !float.IsFinite(v.X) || !float.IsFinite(v.Y) || !float.IsFinite(v.Z)))
                throw new InvalidOperationException("Barrel witness invalid vertices: " + name);
            var center = vertices.Aggregate(Vector3.Zero, (sum, v) => sum + v) / vertices.Length;
            var normal = Vector3.Zero;
            for (var i = 0; i + 2 < vertices.Length && normal == Vector3.Zero; i += 3)
            {
                var cross = (vertices[i + 1] - vertices[i]).Cross(vertices[i + 2] - vertices[i]);
                if (cross.LengthSquared() > 1e-12f) normal = cross.Normalized();
            }
            if (normal == Vector3.Zero) throw new InvalidOperationException("Barrel witness degenerate ring: " + name);
            var deviations = vertices.Select(v => (v - center).Dot(normal)).ToArray();
            var radii = vertices.Select(v =>
            {
                var delta = v - center;
                return (delta - normal * delta.Dot(normal)).Length();
            }).ToArray();
            return new BarrelRingGeometry(root.GetPathTo(mesh).ToString(), center, normal,
                radii.Min(), radii.Max(), deviations.Max(value => MathF.Abs(value)), vertices.Length);
        }
        static object Evidence(BarrelRingGeometry ring) => new
        {
            path = ring.Path, worldCenter = new[] { ring.Center.X, ring.Center.Y, ring.Center.Z },
            worldNormal = new[] { ring.Normal.X, ring.Normal.Y, ring.Normal.Z },
            innerRadius = ring.InnerRadius, outerRadius = ring.OuterRadius,
            maxPlaneDeviation = ring.PlaneDeviation, vertexCount = ring.VertexCount,
        };
        var results = new List<object>();
        foreach (var (aId, aName, bId, bName) in new[]
        {
            ("tank_1", "SUPPLY_outlet_end_ring", "valve_2", "VALVE_bore_start_ring"),
            ("valve_2", "VALVE_bore_end_ring", "training_accessory_6", "METER_inlet_start_ring"),
            ("training_accessory_6", "METER_inlet_end_ring", "training_accessory_6", "METER_bore_start_ring"),
            ("training_accessory_6", "METER_bore_end_ring", "training_accessory_6", "METER_outlet_start_ring"),
        })
        {
            var a = Ring(aId, aName); var b = Ring(bId, bName);
            var difference = b.Center - a.Center;
            var planeGap = MathF.Abs(difference.Dot(a.Normal));
            var lateralGap = (difference - a.Normal * difference.Dot(a.Normal)).Length();
            var alignment = MathF.Abs(a.Normal.Dot(b.Normal));
            var radialOverlap = MathF.Min(a.OuterRadius, b.OuterRadius) - MathF.Max(a.InnerRadius, b.InnerRadius);
            var coplanarCoaxial = planeGap <= .001f && lateralGap <= .001f && alignment >= .9999f
                && a.PlaneDeviation <= .001f && b.PlaneDeviation <= .001f;
            // A gap is an observed result, never an exception or a fake PASS.
            results.Add(new { a = Evidence(a), b = Evidence(b), centerDistance = difference.Length(),
                planeGap, lateralGap, normalAlignment = alignment, radialOverlap,
                coplanarCoaxial, positiveAnnularOverlapWhenCoplanar = coplanarCoaxial && radialOverlap > .00001f });
        }
        var sockets = new List<object>();
        foreach (var (socketId, name, aId, aName, bId, bName) in new[]
        {
            ("valve_2", "VALVE_inlet_reducing_socket", "tank_1", "SUPPLY_outlet_end_ring", "valve_2", "VALVE_bore_start_ring"),
            ("valve_2", "VALVE_outlet_reducing_socket", "valve_2", "VALVE_bore_end_ring", "training_accessory_6", "METER_inlet_start_ring"),
            ("training_accessory_6", "METER_inlet_reducing_socket", "training_accessory_6", "METER_inlet_end_ring", "training_accessory_6", "METER_bore_start_ring"),
            ("training_accessory_6", "METER_outlet_reducing_socket", "training_accessory_6", "METER_bore_end_ring", "training_accessory_6", "METER_outlet_start_ring"),
        })
        {
            var start = Ring(socketId, name + "_start_ring");
            var end = Ring(socketId, name + "_end_ring");
            var length = start.Center.DistanceTo(end.Center);
            if (length <= .00001f) throw new InvalidOperationException("Barrel socket witness degenerate span: " + name);
            var axis = (end.Center - start.Center).Normalized();
            var faces = WorldFaces(socketId, name).Concat(WorldFaces(socketId, name + "_start_ring"))
                .Concat(WorldFaces(socketId, name + "_end_ring")).ToArray();
            var axial = faces.Select(v => (v - start.Center).Dot(axis)).ToArray();
            var radii = faces.Select(v =>
            {
                var delta = v - start.Center;
                return (delta - axis * delta.Dot(axis)).Length();
            }).ToArray();
            // Measure minimum triangle clearance from the whole infinite bore
            // axis, not just vertex radii: a disk/fan could have rim vertices
            // while its triangles plug the bore. Project each triangle to the
            // radial plane, check origin containment, then every edge distance.
            var minimumSurfaceRadius = float.PositiveInfinity;
            for (var triangle = 0; triangle < faces.Length; triangle += 3)
            {
                var projected = faces.Skip(triangle).Take(3).Select(v =>
                {
                    var delta = v - start.Center;
                    return delta - axis * delta.Dot(axis);
                }).ToArray();
                var area = (projected[1] - projected[0]).Cross(projected[2] - projected[0]).Dot(axis);
                var signs = Enumerable.Range(0, 3).Select(i => projected[i].Cross(projected[(i + 1) % 3]).Dot(axis)).ToArray();
                if (MathF.Abs(area) > 1e-10f && (signs.All(s => s >= 0) || signs.All(s => s <= 0))) minimumSurfaceRadius = 0;
                for (var edge = 0; edge < 3; edge++)
                {
                    var p = projected[edge]; var delta = projected[(edge + 1) % 3] - p;
                    var t = delta.LengthSquared() > 1e-12f ? Mathf.Clamp(-p.Dot(delta) / delta.LengthSquared(), 0, 1) : 0;
                    minimumSurfaceRadius = MathF.Min(minimumSurfaceRadius, (p + delta * t).Length());
                }
            }
            var contacts = new List<object>();
            var allowedMatingMeshes = new HashSet<string>(StringComparer.Ordinal)
            {
                socketId + "/" + name, socketId + "/" + name + "_start_ring", socketId + "/" + name + "_end_ring",
            };
            foreach (var (id, ringName) in new[] { (aId, aName), (bId, bName) })
            {
                var mate = Ring(id, ringName);
                var bodyName = ringName.EndsWith("_start_ring", StringComparison.Ordinal)
                    ? ringName[..^"_start_ring".Length] : ringName[..^"_end_ring".Length];
                allowedMatingMeshes.Add(id + "/" + bodyName);
                allowedMatingMeshes.Add(id + "/" + ringName);
                var mateFaces = WorldFaces(id, bodyName);
                var mateAxial = mateFaces.Select(v => (v - start.Center).Dot(axis)).ToArray();
                var axialOverlap = MathF.Min(axial.Max(), mateAxial.Max()) - MathF.Max(axial.Min(), mateAxial.Min());
                var radialOverlap = MathF.Min(radii.Max(), mate.OuterRadius) - MathF.Max(radii.Min(), mate.InnerRadius);
                var relative = mate.Center - start.Center;
                var jointAxial = relative.Dot(axis);
                var lateralGap = (relative - axis * jointAxial).Length();
                var alignment = MathF.Abs(mate.Normal.Dot(axis));
                contacts.Add(new { retainedRing = Evidence(mate), retainedBody = id + "/" + bodyName,
                    jointAxial, lateralGap, normalAlignment = alignment, axialOverlap, radialOverlap,
                    positiveAnnularBodyOverlap = jointAxial > .00001f && jointAxial < length - .00001f
                        && lateralGap <= .001f && alignment >= .9999f && radialOverlap > .00001f && axialOverlap > .00001f });
            }
            var socketBounds = ReviewBounds(MeshNode(socketId, name));
            var nonMatingIntersections = new List<object>();
            foreach (var neighbor in ReviewMeshes(root).Where(mesh => mesh.IsVisibleInTree()))
            {
                var path = root.GetPathTo(neighbor).ToString();
                if (allowedMatingMeshes.Contains(path)) continue;
                var bounds = ReviewBounds(neighbor);
                var overlap = new Vector3(MathF.Min(socketBounds.End.X, bounds.End.X) - MathF.Max(socketBounds.Position.X, bounds.Position.X),
                    MathF.Min(socketBounds.End.Y, bounds.End.Y) - MathF.Max(socketBounds.Position.Y, bounds.Position.Y),
                    MathF.Min(socketBounds.End.Z, bounds.End.Z) - MathF.Max(socketBounds.Position.Z, bounds.Position.Z));
                if (overlap.X > .00001f && overlap.Y > .00001f && overlap.Z > .00001f)
                    nonMatingIntersections.Add(new { path, positiveOverlap = new[] { overlap.X, overlap.Y, overlap.Z } });
            }
            sockets.Add(new { name, equipmentId = socketId, start = Evidence(start), end = Evidence(end),
                axialLength = length, actualBodyAxialMin = axial.Min(), actualBodyAxialMax = axial.Max(),
                actualBodyInnerVertexRadius = radii.Min(), actualBodyOuterVertexRadius = radii.Max(),
                minimumTriangleSurfaceDistanceToBoreAxis = minimumSurfaceRadius,
                throughOpenAtOriginalPipeRadius = minimumSurfaceRadius >= .038f - .00001f, contacts,
                worldBodyBounds = new { position = new[] { socketBounds.Position.X, socketBounds.Position.Y, socketBounds.Position.Z },
                    size = new[] { socketBounds.Size.X, socketBounds.Size.Y, socketBounds.Size.Z } },
                allowedMatingMeshes = allowedMatingMeshes.OrderBy(path => path, StringComparer.Ordinal).ToArray(),
                nonMatingIntersections, noPositiveNonMatingPlantOverlap = nonMatingIntersections.Count == 0 });
        }
        var stemMesh = MeshNode("valve_2", "VALVE_actuator_stem");
        var stemBounds = ReviewBounds(stemMesh);
        var boreStart = Ring("valve_2", "VALVE_bore_start_ring");
        var boreEnd = Ring("valve_2", "VALVE_bore_end_ring");
        Vector3[] RingBoundary(string name, BarrelRingGeometry ring, bool outer)
        {
            return WorldFaces("valve_2", name).Distinct().Where(v =>
            {
                var d = v - ring.Center;
                var radius = (d - ring.Normal * d.Dot(ring.Normal)).Length();
                return outer ? radius >= ring.OuterRadius - .0001f : radius <= ring.InnerRadius + .0001f;
            }).ToArray();
        }
        var outerPolygon = RingBoundary("VALVE_bore_start_ring", boreStart, true)
            .OrderBy(v => Math.Atan2(v.Y - boreStart.Center.Y, v.X - boreStart.Center.X)).ToArray();
        var innerBoundary = RingBoundary("VALVE_bore_start_ring", boreStart, false)
            .Concat(RingBoundary("VALVE_bore_end_ring", boreEnd, false)).ToArray();
        if (outerPolygon.Length < 3 || innerBoundary.Length < 3)
            throw new InvalidOperationException("Valve stem witness requires retained annular boundaries.");
        float? OuterTopAt(float x)
        {
            float? top = null;
            for (var i = 0; i < outerPolygon.Length; i++)
            {
                var a = outerPolygon[i]; var b = outerPolygon[(i + 1) % outerPolygon.Length];
                if (x < MathF.Min(a.X, b.X) || x > MathF.Max(a.X, b.X)) continue;
                var y = MathF.Abs(b.X - a.X) < 1e-8f ? MathF.Max(a.Y, b.Y)
                    : a.Y + (b.Y - a.Y) * (x - a.X) / (b.X - a.X);
                top = top is null ? y : MathF.Max(top.Value, y);
            }
            return top;
        }
        // For this convex polygon, the minimum upper-shell boundary across
        // the stem footprint occurs at an X endpoint. Use actual polygon
        // segments, not the cylindrical AABB top or ideal circle radius.
        var shellLeft = OuterTopAt(stemBounds.Position.X);
        var shellRight = OuterTopAt(stemBounds.End.X);
        var shellMinimum = shellLeft is not null && shellRight is not null ? MathF.Min(shellLeft.Value, shellRight.Value) : (float?)null;
        var lumenTop = innerBoundary.Max(v => v.Y);
        var boreBounds = ReviewBounds(MeshNode("valve_2", "VALVE_bore"));
        var horizontalBore = MathF.Abs(boreStart.Normal.Dot(Vector3.Forward)) >= .9999f;
        var actuator = MeshNode("valve_2", "VALVE_actuator");
        var actuatorBounds = ReviewBounds(actuator);
        var pointerMesh = MeshNode("valve_2", "VALVE_pointer");
        var pointerBounds = ReviewBounds(pointerMesh);
        var pointerLocal = pointerMesh.GetAabb();
        var pointerCenter = pointerMesh.GlobalTransform * pointerLocal.GetCenter();
        var stemCenter = stemBounds.GetCenter();
        var pointerInscribedRadius = MathF.Min(pointerLocal.Size.X * pointerMesh.GlobalBasis.X.Length(),
            pointerLocal.Size.Z * pointerMesh.GlobalBasis.Z.Length()) / 2;
        var stemRadius = MathF.Max(stemBounds.Size.X, stemBounds.Size.Z) / 2;
        var centerOffset = new Vector2(pointerCenter.X - stemCenter.X, pointerCenter.Z - stemCenter.Z).Length();
        var pointerVertical = MathF.Abs(pointerMesh.GlobalBasis.Y.Normalized().Dot(Vector3.Up)) >= .9999f;
        var pointerOrthogonal = MathF.Abs(pointerMesh.GlobalBasis.X.Normalized().Dot(pointerMesh.GlobalBasis.Z.Normalized())) <= .00001f;
        var pointerYOverlap = MathF.Min(stemBounds.End.Y, pointerBounds.End.Y) - MathF.Max(stemBounds.Position.Y, pointerBounds.Position.Y);
        var actuatorOverlap = new[]
        {
            MathF.Min(stemBounds.End.X, actuatorBounds.End.X) - MathF.Max(stemBounds.Position.X, actuatorBounds.Position.X),
            MathF.Min(stemBounds.End.Y, actuatorBounds.End.Y) - MathF.Max(stemBounds.Position.Y, actuatorBounds.Position.Y),
            MathF.Min(stemBounds.End.Z, actuatorBounds.End.Z) - MathF.Max(stemBounds.Position.Z, actuatorBounds.Position.Z),
        };
        var pointer = root.GetNode<Node3D>("valve_2/KIN_valve_pointer");
        object? fillValveOpen = null;
        if (_sceneRuntime is not null && _sceneRuntime.Points.TryGetValue("fill_valve_open", out var valveValue)) fillValveOpen = valveValue;
        var valveStem = new
        {
            actualPointerRotationY = pointer.Rotation.Y, fillValveOpen,
            stemBottomY = stemBounds.Position.Y, stemTopY = stemBounds.End.Y, stemRadius,
            retainedInnerBoreTopY = lumenTop, lumenClearance = stemBounds.Position.Y - lumenTop,
            shellTopAtFootprintLeft = shellLeft, shellTopAtFootprintRight = shellRight,
            minimumOuterShellTopAcrossFootprint = shellMinimum,
            minimumCurvedShellBearingOverlap = shellMinimum - stemBounds.Position.Y,
            fullFootprintInBoreAxialSpan = stemBounds.Position.Z >= boreBounds.Position.Z && stemBounds.End.Z <= boreBounds.End.Z,
            curvedShellBearingWithoutLumenIntrusion = horizontalBore && shellMinimum > stemBounds.Position.Y
                && stemBounds.Position.Y > lumenTop && stemBounds.Position.Z >= boreBounds.Position.Z && stemBounds.End.Z <= boreBounds.End.Z,
            actuatorPositiveOverlap = actuatorOverlap, actuatorBearing = actuatorOverlap.All(value => value > .00001f),
            pointerInscribedRadius, pointerAxisCenterOffset = centerOffset, pointerVerticalOverlap = pointerYOverlap,
            pointerBearingForAnyYaw = pointerMesh.Mesh is BoxMesh && pointerVertical && pointerOrthogonal
                && pointerInscribedRadius > centerOffset + stemRadius && pointerYOverlap > .00001f,
        };
        System.IO.File.WriteAllText(_capturePath + ".interfaces.json", JsonSerializer.Serialize(new
        {
            sceneId = _currentSceneId,
            scope = "Actual retained world ring vertices; annular interval witness, not pressure/fluid/continuous-clearance qualification.",
            planeAndCenterToleranceM = .001f, radialPositiveOverlapThresholdM = .00001f, interfaces = results,
            sockets,
            valveStem,
        }, new JsonSerializerOptions { WriteIndented = true }));
        GD.Print("BARREL_INTERFACE_WITNESS_SAVED " + _capturePath + ".interfaces.json");
    }
}

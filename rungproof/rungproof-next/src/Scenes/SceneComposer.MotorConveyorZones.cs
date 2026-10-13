using System;
using System.Linq;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static void ConfigureMotorConveyorZones(Node3D root, SceneDefinition scene)
    {
        if (scene.Id != "lab-10-06-ten-motor-array-startup") return;
        var installation = new Node3D { Name = "MotorConveyorInstallation" };
        root.AddChild(installation);
        installation.SetMeta("installationKind", "tenZoneConveyor");
        installation.SetMeta("illustrativeDriveReduction",1450.0*Math.PI*2/60/(MotorConveyorZonePlantModel.Speed/.09));
        var steel = StudioSceneMaterials.CreateDetailed("#627B78", "paint");
        var metal = StudioSceneMaterials.CreateDetailed("#ADB8B6", "metal");
        var rubber = StudioSceneMaterials.CreateDetailed("#343E40", "paint");
        // One original irregular wear witness in the belt's physical loop,
        // rather than a repeating stripe/grid. Every zone owns its UV offset.
        var beltImage=Image.CreateEmpty(128,128,false,Image.Format.Rgb8);
        var darkRubber=new Color("#343E40"); var wornRubber=new Color("#82908C");
        for(var py=0;py<128;py++) for(var px=0;px<128;px++)
        {
            var grain=((px*73+py*137+px*py*11)%31)/30f;
            var dx=(px-43f)/(9+grain*3); var dy=(py-69f)/(19+grain*5);
            var wear=MathF.Max(0,1-dx*dx-dy*dy);
            beltImage.SetPixel(px,py,darkRubber.Lerp(wornRubber,wear*.82f));
        }
        var beltTexture=ImageTexture.CreateFromImage(beltImage);
        var carton = StudioSceneMaterials.CreateDetailed("#B28A5A", "wood");
        for (var i = 0; i < 10; i++)
        {
            var x = -6.3f + i * 1.4f;
            var zone = new Node3D { Name = "Zone_" + i, Position = new Vector3(x, 0, 0) };
            zone.SetMeta("motorId", "motor_" + i);
            zone.SetMeta("zoneIndex", i);
            installation.AddChild(zone);
            var beltMaterial=(StandardMaterial3D)rubber.Duplicate();
            beltMaterial.Uv1Triplanar=false;
            beltMaterial.Uv1Scale=Vector3.One;
            beltMaterial.AlbedoColor=Colors.White;
            beltMaterial.AlbedoTexture=beltTexture;
            AddBox(zone, new Vector3(1.38f, .09f, 1), new Vector3(0, 1.245f, 0), rubber).Name = "ZoneBeltBody";
            // Explicit top-plane UVs span this physical belt section, avoiding
            // a primitive box's face-atlas ambiguity for the moving witness.
            zone.AddChild(new MeshInstance3D { Name="ZoneBeltSurface",Mesh=new PlaneMesh { Size=new Vector2(1.38f,1) },
                Position=new Vector3(0,1.2902f,0),MaterialOverride=beltMaterial });
            foreach (var side in new[] { -1f, 1f })
            {
                AddBox(zone, new Vector3(1.4f, .18f, .10f), new Vector3(0, 1.08f, side * .55f), steel).Name = "ZoneSideRail_"+side;
                foreach (var end in new[] { -1f, 1f })
                {
                    AddBox(zone, new Vector3(.18f, .035f, .22f), new Vector3(end * .56f, .0175f, side * .55f), steel).Name = "ZoneFloorFoot_"+side+"_"+end;
                    AddBox(zone, new Vector3(.07f, 1.045f, .07f), new Vector3(end * .56f, .5575f, side * .55f), steel).Name = "ZoneGroundLeg_"+side+"_"+end;
                }
            }
            var pulley = new MeshInstance3D { Name = "KIN_zone_drive", Mesh = new CylinderMesh { TopRadius = .09f, BottomRadius = .09f, Height = 1.06f },
                Position = new Vector3(.57f, 1.20f, 0), RotationDegrees = new Vector3(90, 0, 0), MaterialOverride = metal };
            zone.AddChild(pulley);
            // Asymmetric end key makes accepted independent rotation observable.
            AddBox(pulley, new Vector3(.04f, .025f, .08f), new Vector3(.065f, -.54f, 0), steel).Name = "DriveEndKey";
            var motor = root.GetNode<Node3D>("motor_" + i);
            // Lab10.6 is a compact geared teaching drive. Preserve the original
            // mesh/shaft hierarchy and scale uniformly, then measure its axis
            // before constructing a support; the older rigs remain unchanged.
            var authoredScale = motor.Scale;
            motor.RotationDegrees = new Vector3(0, 90, 0);
            motor.Scale = authoredScale * .55f;
            motor.Position = new Vector3(x + .57f, 0, 0);
            var shaftMesh = motor.FindChild("KIN_motor_shaft", true, false) as MeshInstance3D
                ?? throw new InvalidOperationException("Zone motor has no authored shaft.");
            var shaft = motor.Transform * ContextMeshBounds(motor, shaftMesh);
            // Its actual tip is 70mm beyond the belt drum end. Height comes
            // from the measured scaled shaft, never an unscaled foot datum.
            motor.Position += new Vector3(0, 1.20f-shaft.GetCenter().Y, .60f-shaft.Position.Z);
            shaft = motor.Transform * ContextMeshBounds(motor, shaftMesh);
            var feet = motor.Transform * ContextMemberBounds(motor, "MOTOR_foot_");
            if(!motor.Transform.IsFinite() || feet.Position.Y<=.12f)
                throw new InvalidOperationException("Scaled teaching drive lacks a finite supported foot plane.");
            if (MathF.Abs(shaft.GetCenter().Y - 1.20f) > .002f || MathF.Abs(shaft.GetCenter().X - (x + .57f)) > .002f)
                throw new InvalidOperationException("Authored motor shaft misses zone drive axis.");
            // Actual authored shaft exit, not a guessed motor-root connector.
            var tip = shaft.Position.Z;
            var coupling = new MeshInstance3D { Name="MeasuredDriveCoupling", Mesh=new CylinderMesh { TopRadius=.065f,BottomRadius=.065f,Height=MathF.Max(.04f,MathF.Abs(tip-.53f)+.02f) },
                Position=new Vector3(.57f,shaft.GetCenter().Y,(tip+.53f)/2),RotationDegrees=new Vector3(90,0,0),MaterialOverride=metal };
            zone.AddChild(coupling);
            // Closed reduction casing surrounds the measured axis between
            // motor and belt, without hiding the motor's shaft or stator.
            AddBox(zone,new Vector3(.22f,.23f,.20f),new Vector3(.57f,shaft.GetCenter().Y,(tip+.53f)/2),steel).Name="IllustrativeReductionHousing";
            zone.SetMeta("actualMotorShaftBounds",shaft);
            var pad = AddBox(zone, new Vector3(feet.Size.X + .16f, .06f, feet.Size.Z + .16f),
                new Vector3(feet.GetCenter().X - x, feet.Position.Y-.03f, feet.GetCenter().Z), metal);
            pad.Name = "MotorDriveMount";
            foreach (var end in new[] { -1f, 1f })
            {
                var z = feet.GetCenter().Z + end * (feet.Size.Z / 2 - .06f);
                AddBox(zone, new Vector3(.18f,.03f,.18f),new Vector3(.57f,.015f,z),steel).Name="MotorSupportFoot_"+end;
                var legHeight=feet.Position.Y-.06f-.03f;
                AddBox(zone,new Vector3(.07f,legHeight,.07f),new Vector3(.57f,.03f+legHeight/2,z),steel).Name="MotorSupportLeg_"+end;
            }
            motor.SetMeta("motorTestMounted", true);
            motor.SetMeta("teachingDriveScaleMultiplier", .55f);
            motor.SetMeta("motorConveyorZone", i);
            motor.SetMeta("motorTestMountedTransform", motor.Transform);
            motor.SetMeta("motorConveyorPlatePath", "MotorConveyorInstallation/Zone_"+i+"/MotorDriveMount");
            AddBox(zone,new Vector3(.055f,.27f,.055f),new Vector3(0,1.215f,-.62f),steel).Name="ZoneIndexRailPost";
            AddBox(zone,new Vector3(.55f,.30f,.025f),new Vector3(0,1.5f,-.62f),metal).Name="ZoneIndexPlate";
            zone.AddChild(new Label3D { Name="StaticZoneIndex",Text="Z"+i,FontSize=48,PixelSize=.004f,
                Position=new Vector3(0,1.5f,-.604f),Modulate=new Color("#182523"),OutlineSize=0 });
        }
        AddBox(installation,new Vector3(.60f,.34f,.55f),new Vector3(-6.7f,1.46f,0),carton).Name="TeachingCarton";
        VerifyMotorConveyorGeometry(root);
    }
    public static int VerifyMotorConveyorGeometry(Node3D root)
    {
        var installation = root.GetNodeOrNull<Node3D>("MotorConveyorInstallation");
        if (installation is null) return 0;
        for(var i=0;i<10;i++)
        {
            var motor=root.GetNode<Node3D>("motor_"+i);
            var pad=root.GetNode<MeshInstance3D>(motor.GetMeta("motorConveyorPlatePath").AsString());
            var p=ContextMeshBounds(root,pad);
            var zone=installation.GetNode<Node3D>("Zone_"+i);
            if (!motor.Transform.IsFinite() || !motor.Basis.Y.Normalized().IsEqualApprox(Vector3.Up)
                || motor.GetMeta("motorConveyorZone").AsInt32()!=i || zone.GetMeta("motorId").AsString()!=motor.Name.ToString())
                throw new InvalidOperationException("Motor zone identity/mount plane changed.");
            var shaftMesh=motor.FindChild("KIN_motor_shaft",true,false) as MeshInstance3D
                ??throw new InvalidOperationException("Mounted motor lost its original shaft.");
            var shaft=motor.Transform*ContextMeshBounds(motor,shaftMesh);
            var drum=ContextMeshBounds(root,zone.GetNode<MeshInstance3D>("KIN_zone_drive"));
            var coupling=ContextMeshBounds(root,zone.GetNode<MeshInstance3D>("MeasuredDriveCoupling"));
            if(MathF.Abs(shaft.GetCenter().Y-drum.GetCenter().Y)>.002f
                || MathF.Abs(shaft.GetCenter().X-drum.GetCenter().X)>.002f
                || coupling.Position.Z>drum.End.Z+.001f || coupling.End.Z<shaft.Position.Z-.001f)
                throw new InvalidOperationException("Scaled drive shaft/coupling no longer meets its drum axis.");
            var originalFeet=motor.FindChildren("MOTOR_foot_*","MeshInstance3D",true,false).OfType<MeshInstance3D>().ToArray();
            if(originalFeet.Length!=2)throw new InvalidOperationException("Teaching drive must retain both authored feet.");
            foreach(var foot in originalFeet)
            {
                var f=motor.Transform*ContextMeshBounds(motor,foot);
                if(MathF.Abs(f.Position.Y-p.End.Y)>.001f || f.Position.X<p.Position.X-.001f || f.End.X>p.End.X+.001f
                    || f.Position.Z<p.Position.Z-.001f || f.End.Z>p.End.Z+.001f)
                    throw new InvalidOperationException("Zone motor foot lacks pad bearing: "+i);
            }
            foreach(var leg in zone.FindChildren("MotorSupportLeg_*","MeshInstance3D",true,false).OfType<MeshInstance3D>())
            {
                var measured=ContextMeshBounds(root,leg);
                if(MathF.Abs(measured.Position.Y-.03f)>.001f || MathF.Abs(measured.End.Y-p.Position.Y)>.001f)
                    throw new InvalidOperationException("Motor zone pad lacks bearing leg.");
            }
            foreach(var foot in zone.FindChildren("*Foot_*","MeshInstance3D",true,false).OfType<MeshInstance3D>())
                if(MathF.Abs(ContextMeshBounds(root,foot).Position.Y)>.001f)
                    throw new InvalidOperationException("Zone support foot not grounded.");
        }
        var drivenMounts=installation.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>()
            .Where(m=>m.Name=="IllustrativeReductionHousing"||m.Name=="MotorDriveMount").Select(m=>ContextMeshBounds(root,m)).ToArray();
        for(var i=0;i<10;i++)
        {
            var sensor=root.GetNode<Node3D>("zone_sensor_"+i);
            foreach(var mesh in sensor.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>()
                .Where(m=>m.Mesh is not null&&!m.Name.ToString().StartsWith("KIN_beam",StringComparison.Ordinal)))
            {
                var bounds=sensor.Transform*ContextMeshBounds(sensor,mesh);
                if(drivenMounts.Any(m=>bounds.Intersects(m)))
                    throw new InvalidOperationException("Physical zone sensor intersects drive casing/pad: "+i+"/"+mesh.Name);
            }
        }
        return 10;
    }
}

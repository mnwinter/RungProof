using System;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static void ConfigureVisionSorterSplices(Node3D root)
    {
        Transform3D InScene(Node3D node)
        {
            var transform = node.Transform;
            for (var parent = node.GetParent() as Node3D; parent != null && parent != root; parent = parent.GetParent() as Node3D)
                transform = parent.Transform * transform;
            return transform;
        }
        foreach (var conveyor in root.GetChildren().OfType<Node3D>().Where(node =>
            node.Name == "conveyor_0" || node.Name.ToString().StartsWith("destination_lane_", StringComparison.Ordinal)))
        {
            MeshInstance3D Part(string name) => conveyor.FindChild(name, true, false) as MeshInstance3D
                ?? throw new InvalidOperationException($"Sorter conveyor requires {conveyor.Name}/{name}.");
            var belt = Part("KIN_belt_surface");
            var splice = Part("BELT_vulcanized_splice");
            // This authored witness strip must not protrude into a passing
            // carton. Retain it visibly flush with the measured belt plane.
            var rise = (InScene(splice) * splice.GetAabb()).End.Y - (InScene(belt) * belt.GetAabb()).End.Y;
            splice.Position -= InScene((Node3D)splice.GetParent()).Basis.Inverse() * (Vector3.Up * rise);
        }
    }
    // Static installation, not an automatic routing or protective device.
    // The curved nose mates to the parcel platter's measured circular edge.
    private static Node3D CreateVisionSorterBridge(SceneEquipment equipment)
    {
        var start = (float)Number(equipment.Config, "startX", -.0445833);
        var center = (float)Number(equipment.Config, "tableCenterX", 3.3);
        var radius = (float)Number(equipment.Config, "tableRadius", 1.28);
        var halfWidth = (float)Number(equipment.Config, "width", 1.4) / 2;
        var top = (float)Number(equipment.Config, "deckHeight", .9);
        if (halfWidth <= 0 || radius <= halfWidth || top <= .1f || start >= center-radius)
            throw new InvalidOperationException("Sorter bridge requires a positive supported span and valid circular nose.");
        var root = new Node3D();
        var steel = Material(new Color(.52f,.58f,.62f), .65f, .3f);
        steel.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        using var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        void Triangle(Vector3 a,Vector3 b,Vector3 c,Vector3 normal)
        {
            surface.SetNormal(normal);surface.AddVertex(a);surface.AddVertex(b);surface.AddVertex(c);
        }
        void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal)
        {
            Triangle(a,b,c,normal);Triangle(a,c,d,normal);
        }
        const int segments=64;
        const float thickness=.03f;
        var noseEnd=(float)Number(equipment.Config, "beltNoseEndX", start);
        float Thickness(float x)
        {
            if(noseEnd<=start)return thickness;
            var fraction=Math.Clamp((x-start)/(noseEnd-start),0,1);
            return thickness*fraction*fraction;
        }
        for(var i=0;i<segments;i++)
        {
            var z0=-halfWidth+2*halfWidth*i/segments;
            var z1=-halfWidth+2*halfWidth*(i+1)/segments;
            var end0=center-MathF.Sqrt(radius*radius-z0*z0);
            var end1=center-MathF.Sqrt(radius*radius-z1*z1);
            // Quadratic thin nose starts flush at the bearing edge and
            // thickens over the rounded belt end before the supported span.
            for(var strip=0;strip<=16;strip++)
            {
                var x0=strip==16 ? noseEnd : start+(noseEnd-start)*strip/16;
                var x1=strip==16 ? end0 : start+(noseEnd-start)*(strip+1)/16;
                var x2=strip==16 ? end1 : x1;
                var a=new Vector3(x0,top,z0);var b=new Vector3(x1,top,z0);
                var c=new Vector3(x2,top,z1);var d=new Vector3(x0,top,z1);
                var ad=a+Vector3.Down*Thickness(x0);var bd=b+Vector3.Down*Thickness(x1);
                var cd=c+Vector3.Down*Thickness(x2);var dd=d+Vector3.Down*Thickness(x0);
                Quad(a,d,c,b,Vector3.Up);Quad(ad,bd,cd,dd,Vector3.Down);
                if(strip==0) Quad(a,ad,dd,d,Vector3.Left);
                if(strip==16)
                {
                    var normal=new Vector3(center-(end0+end1)/2,0,-(z0+z1)/2).Normalized();
                    Quad(b,c,cd,bd,normal);
                }
                if(i==0) Quad(a,b,bd,ad,Vector3.Forward);
                if(i==segments-1) Quad(d,dd,cd,c,Vector3.Back);
            }
        }
        root.AddChild(new MeshInstance3D { Name="SORTER_handoff_deck", Mesh=surface.Commit(), MaterialOverride=steel });
        var supportXs=NumberArray(equipment.Config,"supportXs",new[]{.4,1.7});
        var supportHalfSpan=(float)Number(equipment.Config,"supportHalfSpan",.55);
        foreach(var xValue in supportXs)
        foreach(var z in new[]{-supportHalfSpan,supportHalfSpan})
        {
            var x=(float)xValue;
            AddBox(root,new Vector3(.16f,.08f,.16f),new Vector3(x,.04f,z),steel).Name=$"SORTER_bridge_foot_{x}_{z}";
            AddBox(root,new Vector3(.08f,top-thickness-.08f,.08f),
                new Vector3(x,(top-thickness+.08f)/2,z),steel).Name=$"SORTER_bridge_leg_{x}_{z}";
        }
        return root;
    }
}

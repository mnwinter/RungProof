using System;
using System.Collections.Generic;
using Godot;
namespace RungProof.Next.App;
public partial class DemoEnvironmentPresentation
{
    // Only the two review rooms call this opt-in factory. Cached original catalog
    // materials and maps are never edited. Texture frequencies are world metres:
    // one tile spans 1.7m plaster, 1.3m steel, 1.7m resin, 1.25m concrete.
    private static readonly Dictionary<string,StandardMaterial3D> ReferenceRoleMaterials=[];
    private static StandardMaterial3D ReferenceRoleSurface(string hex,string role)
    {
        var key=hex+":"+role;
        if(ReferenceRoleMaterials.TryGetValue(key,out var existing))return existing;
        const int size=256;
        var color=Image.CreateEmpty(size,size,false,Image.Format.Rgb8);
        var rough=Image.CreateEmpty(size,size,false,Image.Format.Rgb8);
        var normal=Image.CreateEmpty(size,size,false,Image.Format.Rgb8);
        var height=new float[size,size];
        var mean=role switch{"resin"=>.48f,"coated-steel"=>.46f,"paint"=>.66f,"plaster"=>.82f,_=>.83f};
        var uv=role switch{"plaster"=>.60f,"resin"=>.60f,"coated-steel"=>.80f,"paint"=>.70f,_=>.80f};
        float Hash(int x,int y)
        {
            unchecked{uint h=(uint)(x*374761393+y*668265263)^0x27d4eb2du;h=(h^(h>>13))*1274126177u;return ((h^(h>>16))&65535)/65535f;}
        }
        float Noise(int x,int y,int cells)
        {
            var px=x*cells/(float)size;var py=y*cells/(float)size;
            var ix=(int)px;var iy=(int)py;var fx=px-ix;var fy=py-iy;
            fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy);
            float H(int a,int b)=>Hash((a+cells)%cells,(b+cells)%cells);
            return Mathf.Lerp(Mathf.Lerp(H(ix,iy),H(ix+1,iy),fx),Mathf.Lerp(H(ix,iy+1),H(ix+1,iy+1),fx),fy)-.5f;
        }
        for(var y=0;y<size;y++)for(var x=0;x<size;x++)
        {
            var coarse=Noise(x,y,12);var middle=Noise(x,y,48);var fine=Hash(x,y)-.5f;
            // Distinct pigment and relief per finish: fine aggregate rather
            // than broad sandy clouds, and nearly uniform coated surfaces.
            var shade=role switch
            {
                "concrete"=>.985f+coarse*.018f+fine*.022f,
                "plaster"=>.985f+coarse*.012f+middle*.016f,
                "resin"=>.992f+fine*.008f,
                "coated-steel"=>.992f+fine*.006f,
                _=>.992f+fine*.005f,
            };
            color.SetPixel(x,y,new Color(shade,shade,shade));
            var variation=role switch
            {
                "concrete"=>middle*.10f+fine*.12f,
                "plaster"=>middle*.06f+fine*.04f,
                "resin"=>middle*.035f+fine*.04f,
                "coated-steel"=>middle*.025f+fine*.025f,
                _=>middle*.025f+fine*.03f,
            };
            var r=Mathf.Clamp(mean+variation,.25f,.97f);
            rough.SetPixel(x,y,new Color(r,r,r));
            height[x,y]=role switch
            {
                "concrete"=>middle*.10f+fine*.05f,
                "plaster"=>middle*.18f+fine*.04f,
                "resin"=>fine*.015f,
                "coated-steel"=>fine*.018f,
                _=>fine*.020f,
            };
        }
        for(var y=0;y<size;y++)for(var x=0;x<size;x++)
        {
            var dx=height[(x+1)%size,y]-height[(x+size-1)%size,y];
            var dy=height[x,(y+1)%size]-height[x,(y+size-1)%size];
            var n=new Vector3(-dx,-dy,1).Normalized();
            normal.SetPixel(x,y,new Color(n.X*.5f+.5f,n.Y*.5f+.5f,n.Z*.5f+.5f));
        }
        color.GenerateMipmaps();rough.GenerateMipmaps();normal.GenerateMipmaps();
        // StandardMaterial3D expects source sRGB color. A pinned Compatibility
        // unshaded probe reproduces the hex exactly; manual conversion darkens it.
        var material=new StandardMaterial3D{AlbedoColor=new Color(hex),AlbedoTexture=ImageTexture.CreateFromImage(color),
            Roughness=1,RoughnessTexture=ImageTexture.CreateFromImage(rough),NormalEnabled=true,NormalTexture=ImageTexture.CreateFromImage(normal),
            NormalScale=role switch{"plaster"=>.12f,"concrete"=>.12f,"coated-steel"=>.06f,"resin"=>.04f,_=>.05f},
            Metallic=role=="coated-steel"?.16f:0,Uv1Triplanar=true,Uv1Scale=Vector3.One*uv};
        ReferenceRoleMaterials.Add(key,material);return material;
    }
}

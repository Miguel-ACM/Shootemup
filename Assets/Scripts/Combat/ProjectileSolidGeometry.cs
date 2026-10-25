using System;
using System.Collections.Generic;
using UnityEngine;

// Faceted, volumetric ordnance. The hostile coloured equator is exactly the .44 collider radius;
// only the dark bevel and identification fins sit beyond it. No spinning size changes or glow fog.
public static class ProjectileSolidGeometry
{
    static readonly Color Ink=new Color(.035f,.045f,.075f);
    sealed class Builder
    {
        readonly List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>();
        readonly List<Color> colors=new List<Color>();readonly List<int> indices=new List<int>();
        void Triangle(Vector3 a,Vector3 b,Vector3 c,Color color)
        {
            color.a=1;
            var n=Vector3.Cross(b-a,c-a).normalized;
            int i=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);
            normals.Add(n);normals.Add(n);normals.Add(n);colors.Add(color);colors.Add(color);colors.Add(color);
            indices.Add(i);indices.Add(i+1);indices.Add(i+2);
        }
        // Concentric rings form a closed, bevelled lentil with a raised faceted front.
        public void Lens(Vector2 center,Vector2 size,Color color,int sides=16,float phase=0,float shaft=0)
        {
            float[] radii={0,.38f,.72f,.92f,1,1.115f,.90f,0};
            float[] depth={-.50f,-.46f,-.34f,-.18f,0,.035f,.20f,.35f};
            Color[] bands={Color.Lerp(color,new Color(1,.95f,.78f),.28f),color,color,Color.Lerp(color,Color.white,.22f),Ink,Ink,color*.55f};
            for(int ring=0;ring<radii.Length-1;ring++)for(int s=0;s<sides;s++)
            {
                float a=(s*360f/sides+phase)*Mathf.Deg2Rad,b=((s+1)*360f/sides+phase)*Mathf.Deg2Rad;
                Vector3 P(float angle,int r)=>new Vector3(center.x+Mathf.Cos(angle)*size.x*radii[r],center.y+(Mathf.Sin(angle)*size.y+Mathf.Sign(Mathf.Sin(angle))*shaft)*radii[r],depth[r]*Mathf.Min(size.x,size.y)*2);
                Triangle(P(a,ring),P(b,ring+1),P(a,ring+1),bands[ring]);
                Triangle(P(a,ring),P(b,ring),P(b,ring+1),bands[ring]);
            }
        }
        public void Fin(Vector2 a,Vector2 b,Vector2 c,Color color)
        {
            Vector3 aa=new Vector3(a.x,a.y,.04f),bb=new Vector3(b.x,b.y,.04f),cc=new Vector3(c.x,c.y,.04f);
            var peak=(aa+bb+cc)/3+Vector3.back*.16f;
            if(Vector3.Cross(bb-aa,cc-aa).z>0){var t=bb;bb=cc;cc=t;}
            Triangle(aa,bb,peak,color);Triangle(bb,cc,peak,Color.Lerp(color,Color.white,.25f));Triangle(cc,aa,peak,color*.65f);Triangle(cc,bb,aa,Ink);
        }
        public void Rail(Vector2 a,Vector2 b,float width,Color color)
        {
            var n=new Vector2(-(b-a).y,(b-a).x).normalized*width;
            Fin(a-n,a+n,b+n,color);Fin(a-n,b+n,b-n,color);
        }
        public void Ridge(Vector2[] path,float width,Color color)
        {
            var left=new Vector3[path.Length];var right=new Vector3[path.Length];var crest=new Vector3[path.Length];
            for(int i=0;i<path.Length;i++)
            {
                var tangent=(path[Mathf.Min(i+1,path.Length-1)]-path[Mathf.Max(0,i-1)]).normalized;
                var normal=new Vector2(-tangent.y,tangent.x)*width;
                left[i]=path[i]+normal;right[i]=path[i]-normal;crest[i]=(Vector3)path[i]+Vector3.back*.13f;
            }
            for(int i=0;i<path.Length-1;i++)
            {
                Triangle(left[i],left[i+1],crest[i],color);Triangle(crest[i],left[i+1],crest[i+1],color);
                Triangle(crest[i],crest[i+1],right[i],color*.7f);Triangle(right[i],crest[i+1],right[i+1],color*.7f);
                Triangle(right[i],right[i+1],left[i],Ink);Triangle(left[i],right[i+1],left[i+1],Ink);
            }
        }
        public Mesh Finish(string name)
        {
            var m=new Mesh{name=name};m.SetVertices(vertices);m.SetNormals(normals);m.SetColors(colors);m.SetTriangles(indices,0);m.RecalculateBounds();return m;
        }
    }
    public static Mesh Hostile(Type type)
    {
        var b=new Builder();var c=HostileProjectileArt.Accent(type);var dark=Color.Lerp(Ink,c,.48f);
        if(type==typeof(NeedleRound) || type==typeof(GuidedMissile))
        {
            b.Fin(new Vector2(-.26f,.28f),new Vector2(0,.83f),new Vector2(.26f,.28f),c);
            for(int s=-1;s<=1;s+=2)b.Fin(new Vector2(s*.22f,-.18f),new Vector2(s*.52f,-.59f),new Vector2(s*.13f,-.42f),dark);
        }
        if(type==typeof(HelixRound) || type==typeof(RicochetBolt))
            for(int s=-1;s<=1;s+=2)b.Fin(new Vector2(s*.28f,.30f),new Vector2(s*.65f,0),new Vector2(s*.28f,-.30f),c);
        if(type==typeof(SuspendedRound))
        {
            // Joined, swept arrow over the centred collision ball, with a visible gap.
            var path=new Vector2[11];for(int i=0;i<path.Length;i++)
            {
                float x=(i-5)*.125f;path[i]=new Vector2(x,1.06f-.55f*Mathf.Abs(x)-.60f*x*x);
            }
            b.Ridge(path,.075f,c);
        }
        if(type==typeof(ScatterCarrier))
            for(int s=-1;s<=1;s+=2)
            {
                b.Rail(new Vector2(s*.30f,0),new Vector2(s*.63f,0),.15f,dark);
                b.Lens(new Vector2(s*.63f,0),new Vector2(.065f,.12f),new Color(1,.48f,.16f),8);
            }
        if(type==typeof(ProximityMine) || type==typeof(ReturningDisc))
            for(int i=0;i<4;i++)
            {
                float a=(i*90+45)*Mathf.Deg2Rad;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var n=new Vector2(-d.y,d.x);
                b.Fin(d*.3f+n*.19f,d*.64f,d*.3f-n*.19f,dark);
            }
        // All hostile hit areas have the same honest circular boundary, including discs and mines.
        b.Lens(Vector2.zero,Vector2.one*.44f,c);
        if(type==typeof(PlasmaOrb))b.Lens(Vector2.zero,Vector2.one*.20f,new Color(.13f,.63f,.68f),8,22.5f);
        if(type==typeof(BurstShell))
            for(int i=0;i<4;i++)
            {
                float a=i*Mathf.PI*.5f;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                b.Rail(d*.28f,d*.43f,.025f,Ink);
            }
        return b.Finish(type.Name+" solid");
    }
    public static Mesh Friendly(PlayerDelivery mode,Vector2 collider)
    {
        var b=new Builder();Color c=mode==PlayerDelivery.Flame ? new Color(.95f,.46f,.16f) : new Color(.24f,.74f,.86f);
        var half=collider*.5f;
        if(mode==PlayerDelivery.Missile || mode==PlayerDelivery.Flame)
            for(int s=-1;s<=1;s+=2)b.Fin(new Vector2(s*half.x,-half.y*.25f),new Vector2(s*half.x*1.5f,-half.y),new Vector2(0,-half.y*.7f),c*.65f);
        // A capsule has full-width sides, not an ellipse that hides the contact shoulders.
        float radius=Mathf.Min(half.x,half.y),shaft=Mathf.Max(0,half.y-radius);
        b.Lens(Vector2.zero,Vector2.one*radius,c,16,11.25f,shaft);
        if(mode==PlayerDelivery.Disc)b.Lens(Vector2.zero,half*.4f,new Color(.04f,.26f,.36f),8);
        return b.Finish("Friendly "+mode+" solid");
    }
    public static Mesh Legacy(GameObject go,bool friendly)
    {
        var b=new Builder();var c=friendly ? new Color(.24f,.74f,.86f) : new Color(1,.44f,.16f);
        var edges=go.GetComponents<EdgeCollider2D>();
        if(edges.Length>0)
        {
            foreach(var edge in edges){var p=edge.points;for(int i=1;i<p.Length;i++)b.Rail(p[i-1]+edge.offset,p[i]+edge.offset,.035f,c);}
        }
        else if(go.TryGetComponent<BoxCollider2D>(out var box))
        {
            var h=box.size*.5f;var o=box.offset;
            b.Fin(o+new Vector2(-h.x,-h.y),o+new Vector2(-h.x,h.y),o+new Vector2(h.x,h.y),c);
            b.Fin(o+new Vector2(-h.x,-h.y),o+new Vector2(h.x,h.y),o+new Vector2(h.x,-h.y),c);
            b.Lens(o,new Vector2(h.x*.85f,h.y*.85f),c);
        }
        else if(go.TryGetComponent<CircleCollider2D>(out var circle))b.Lens(circle.offset,Vector2.one*circle.radius,c);
        else b.Lens(Vector2.zero,(Vector2)go.GetComponent<SpriteRenderer>().sprite.bounds.size*.5f,c);
        return b.Finish(go.name+" solid");
    }
}

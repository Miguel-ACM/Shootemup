using System.Collections.Generic;
using UnityEngine;

// Approved source PNGs remain neutral. Pixel coordinates are also the collision source.
public static class ProjectileSprites
{
    static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
    static readonly Dictionary<Sprite,PhysicsShapeGroup2D> paths=new Dictionary<Sprite,PhysicsShapeGroup2D>();
    static readonly Dictionary<Sprite,Rect[]> pixelBoxes=new Dictionary<Sprite,Rect[]>();
    public static Rect[] Boxes(Sprite sprite)=>pixelBoxes[sprite];
    public static Sprite Load(string name)
    {
        if(sprites.TryGetValue(name,out var sprite))return sprite;
        var texture=Resources.Load<Texture2D>("ProjectileSprites/"+name);
        if(texture==null)throw new System.InvalidOperationException("Missing 2D projectile texture: "+name);
        sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),Mathf.Max(texture.width,texture.height),0,SpriteMeshType.FullRect);
        sprite.name=name+" approved pixels";return sprites[name]=sprite;
    }
    public static void Fit(CustomCollider2D collider,Sprite sprite)
    {
        if(!paths.TryGetValue(sprite,out var outline))
        {
            var pixels=sprite.texture.GetPixels32();int width=sprite.texture.width,height=sprite.texture.height;
            var rectangles=new List<RectInt>();
            // Merge identical horizontal runs. Separate rectangles retain real holes and gaps;
            // a convex hull would fill the delayed arrow/disc and hide collision outside the ink.
            for(int y=0;y<height;y++)for(int x=0;x<width;)
            {
                if(pixels[y*width+x].a<128){x++;continue;}
                int start=x;while(x<width && pixels[y*width+x].a>=128)x++;
                int found=-1;
                for(int i=rectangles.Count-1;i>=0;i--)
                    if(rectangles[i].yMax==y && rectangles[i].x==start && rectangles[i].width==x-start){found=i;break;}
                if(found<0)rectangles.Add(new RectInt(start,y,x-start,1));
                else{var r=rectangles[found];r.height++;rectangles[found]=r;}
            }
            outline=new PhysicsShapeGroup2D();
            var boxes=new Rect[rectangles.Count];
            Vector2 P(float x,float y)=>(new Vector2(x,y)-sprite.pivot)/sprite.pixelsPerUnit;
            for(int i=0;i<rectangles.Count;i++)
            {
                var r=rectangles[i];
                boxes[i]=new Rect(P(r.x,r.y),new Vector2(r.width,r.height)/sprite.pixelsPerUnit);
                outline.AddBox(P(r.x+r.width*.5f,r.y+r.height*.5f),new Vector2(r.width,r.height)/sprite.pixelsPerUnit,0,0);
            }
            pixelBoxes[sprite]=boxes;
            paths[sprite]=outline;
        }
        // Direct box shapes avoid polygon tessellation bridging touching pixel runs.
        collider.SetCustomShapes(outline);
    }
}

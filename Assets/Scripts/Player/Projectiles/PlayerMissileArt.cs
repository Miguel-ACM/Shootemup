using UnityEngine;
public static partial class PlayerProjectileArt
{
    // Miguel's arrowhead dart (option D, 10 November 2026): four frames of a flickering,
    // swaying exhaust drawn by Tools/draw_player_missile.py. Every frame shares the same
    // size, so the round's scale and collider never change while it animates.
    public const float MissileFramesPerSecond = 14;
    const int MissileFrames = 4;
    static Sprite[] missileFrames;

    static Sprite Missile() => MissileFrame(0);

    public static Sprite MissileFrame(int frame)
    {
        if (missileFrames == null)
        {
            var texture = Resources.Load<Texture2D>("ProjectileSprites/player_missile");
            if (texture == null)
                throw new System.InvalidOperationException("Missing player missile strip");
            texture.filterMode = FilterMode.Point;
            int width = texture.width / MissileFrames;
            missileFrames = new Sprite[MissileFrames];
            for (int i = 0; i < MissileFrames; i++)
            {
                missileFrames[i] = Sprite.Create(texture, new Rect(i * width, 0, width, texture.height), new Vector2(.5f, .5f),
                    Mathf.Max(width, texture.height), 0, SpriteMeshType.FullRect);
                missileFrames[i].name = "Friendly arrowhead dart " + i;
            }
        }
        return missileFrames[(frame % MissileFrames + MissileFrames) % MissileFrames];
    }
}

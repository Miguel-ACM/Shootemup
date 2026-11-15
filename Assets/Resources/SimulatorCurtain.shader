Shader "Shootemup/Simulator Curtain"
{
    // The simulator's transition (Miguel, 15 November 2026): instead of clouds, the playfield is
    // jacked into a simulation. _Mode 0 covers: a scan front runs down the screen and every cell
    // of a grid flashes its wireframe and turns solid. While covered: a dark data void with a
    // scrolling grid, falling block glyphs and a turning hex reticle. _Mode 1 reveals: cells
    // dissolve outward from the pilot's area with outline flashes, and a faint wireframe lingers
    // over the new district before it fades. _Cover runs 0..1..0 over the trip.
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        _Cover ("Cover", Range(0,1)) = 0
        _Mode ("Mode (0 cover, 1 reveal)", Float) = 0
        _Clock ("Clock", Float) = 0
        _Aspect ("Width / height", Float) = .5625
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            float _Cover, _Mode, _Clock, _Aspect;
            float4 _ClipRect;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 world : TEXCOORD1; float4 color : COLOR; };
            v2f Vert(appdata v)
            {
                v2f o;
                o.world = v.vertex;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            float Hash(float2 p) { p = frac(p * float2(127.1, 311.7)); p += dot(p, p + 19.19); return frac(p.x * p.y); }

            // Distance to a hexagon outline of radius r (flat top), in the reticle's own space.
            float Hexagon(float2 p, float r)
            {
                p = abs(p);
                return max(dot(p, float2(.866, .5)), p.y) - r;
            }

            float4 Frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float p = _Mode < .5 ? _Cover : 1 - _Cover;
                // Interference: horizontal slices jitter while the sweep runs.
                float jitter = 4 * p * (1 - p) * (_Mode < .5 ? 1 : .7);
                uv.x += (Hash(float2(floor(uv.y * 64), floor(_Clock * 24))) - .5) * .022 * jitter * step(.86, Hash(float2(floor(uv.y * 64), 3)));

                // The cell grid: square cells across the playfield.
                float2 cells = float2(16, 16 / _Aspect);
                float2 cell = floor(uv * cells), f = frac(uv * cells);
                float h = Hash(cell);
                float2 centre = (cell + .5) / cells;
                float order = _Mode < .5
                    ? (1 - centre.y) * .84 + h * .16
                    : saturate(length((centre - float2(.5, .28)) * float2(_Aspect * 1.6, 1)) * 1.05) * .84 + h * .16;
                float local = saturate((p * 1.25 - order) * 7);
                float solid = _Mode < .5 ? smoothstep(.35, 1, local) : 1 - smoothstep(0, .65, local);
                float flash = saturate(1 - abs(local - .4) * 2.8) * step(.001, local) * step(local, .999);
                float border = min(min(f.x, 1 - f.x), min(f.y, 1 - f.y));
                float outline = smoothstep(.09, .02, border);

                // The data void: deep navy, a grid flowing down, falling block glyphs.
                float3 colour = float3(.012, .028, .05);
                float2 g = float2(uv.x * 9 * _Aspect / .5625, uv.y * 16 + _Clock * .9);
                float2 gf = abs(frac(g) - .5);
                float grid = smoothstep(.465, .5, max(gf.x, gf.y));
                colour += float3(.03, .3, .42) * grid * .55;
                float2 glyphs = float2(36, 36 * 1.25 / _Aspect);
                float2 gcell = floor(uv * glyphs), gfrac = frac(uv * glyphs);
                float lane = Hash(float2(gcell.x, 17.3));
                float head = frac(Hash(float2(gcell.x, 4.1)) - _Clock * (.18 + lane * .32));
                float behind = frac(uv.y - head);
                float trail = exp(-behind * 7) * step(.45, lane);
                float2 bit = floor(gfrac * float2(3, 5));
                float lit = step(.48, Hash(gcell * 1.7 + bit * 13.1 + floor(_Clock * 7 + lane * 9)));
                float inset = step(.12, gfrac.x) * step(gfrac.x, .88) * step(.1, gfrac.y) * step(gfrac.y, .9);
                float glyph = trail * lit * inset;
                colour += lerp(float3(.12, .85, .7), float3(.85, 1, .95), saturate(1 - behind * 30)) * glyph * .65;
                // A slowly turning hex reticle with broken segments at the centre.
                float2 r = (uv - .5) * float2(_Aspect, 1);
                float angle = _Clock * .7;
                float2 q = float2(r.x * cos(angle) - r.y * sin(angle), r.x * sin(angle) + r.y * cos(angle));
                float ring = smoothstep(.006, 0, abs(Hexagon(q, .16)));
                float segment = step(.35, frac(atan2(q.y, q.x) / 6.2832 * 6 + _Clock * .2));
                float inner = smoothstep(.004, 0, abs(Hexagon(float2(q.y, q.x), .1))) * .6;
                colour += float3(.2, .9, 1) * (ring * segment + inner) * .55;

                // Wireframe flashes on the flipping cells, with a magenta fringe offset beside the cyan.
                float3 glow = float3(.25, .95, 1) * outline * flash * 1.3;
                float fringe = smoothstep(.09, .02, min(min(frac(f.x + .06), 1 - frac(f.x + .06)), min(f.y, 1 - f.y)));
                glow += float3(1, .2, .75) * fringe * flash * .45;
                // The cover sweep's scan front, doubled in magenta.
                float front = 1 - p * 1.12;
                float scan = _Mode < .5 ? exp(-pow((uv.y - front) * 70, 2)) * step(.001, p) * step(p, .999) : 0;
                float ghost = _Mode < .5 ? exp(-pow((uv.y - front - .008) * 70, 2)) * step(.001, p) * step(p, .999) : 0;
                glow += float3(.5, 1, 1) * scan * 1.1 + float3(1, .25, .8) * ghost * .5;
                // After the reveal a faint wireframe lingers over the new district.
                float linger = _Mode > .5 ? saturate(_Cover * 3) * (1 - solid) : 0;
                glow += float3(.1, .7, .85) * grid * linger * .35;

                float alpha = solid;
                float3 rgb = colour * solid + glow;
                float4 result = float4(rgb, alpha);
                #ifdef UNITY_UI_CLIP_RECT
                result *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif
                return result;
            }
            ENDCG
        }
    }
}

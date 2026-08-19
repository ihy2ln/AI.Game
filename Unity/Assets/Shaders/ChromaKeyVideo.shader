// Unlit chroma-key shader for FMV battle clips (see BattleClipPlayer.cs). Discards pixels
// close to _KeyColor (ClipEntry.chromaKey, always solid #00FF00 per FOUNDATION.md) within
// _Tolerance (ClipEntry.chromaTolerance), leaving the rest opaque -- lets a VideoPlayer's
// render-texture output stand in for a unit's SpriteRenderer without baking any background
// into the clip itself, so one clip library serves any future camera/layout this project
// tries (see ClipSet's own class doc for why that separation matters).
//
// Cull Off: BattleClipPlayer flips a unit's clip quad via a negative localScale.x for
// facing (mirroring the sprite pipeline's SpriteRenderer.flipX), and a negative-scaled
// quad faces away from a single-sided shader.
Shader "Game/ChromaKeyVideo"
{
    Properties
    {
        _MainTex ("Video Texture", 2D) = "white" {}
        _KeyColor ("Chroma Key Color", Color) = (0, 1, 0, 1)
        _Tolerance ("Tolerance", Range(0, 1)) = 0.25
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _KeyColor;
            float _Tolerance;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                // Simple Euclidean distance in RGB -- the clips are generated against a
                // flat, solid key colour (no gradient/lighting on the backdrop), so this
                // doesn't need YCbCr-space keying the way a real green-screen shoot would.
                float dist = distance(col.rgb, _KeyColor.rgb);
                col.a = smoothstep(_Tolerance * 0.5, _Tolerance, dist);
                return col;
            }
            ENDCG
        }
    }
}

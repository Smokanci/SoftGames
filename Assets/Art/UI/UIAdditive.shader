// Additive so the glow only ever adds light to the panel under it. An alpha-blended glow on a
// dark panel has to darken the panel's own colour to brighten the edge, which reads as grey haze
// rather than heat. No mask support: nothing in this project puts a button inside a RectMask2D,
// and the clip-rect variants would triple the shader for an unused case.
Shader "SoftGames/UI Additive"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "RenderType"        = "Transparent"
            "IgnoreProjector"   = "True"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            sampler2D _MainTex;
            fixed4    _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                o.color  = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texel = tex2D(_MainTex, i.uv) * i.color;
                // Alpha is the intensity here, not a coverage mask — additive blending ignores
                // the alpha channel, so it has to be folded into the colour by hand.
                return fixed4(texel.rgb * texel.a, texel.a);
            }
            ENDCG
        }
    }
}

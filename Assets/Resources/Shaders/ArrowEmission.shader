Shader "Hue/ArrowEmission"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Glow ("Glow", Range(0, 3)) = 1.2
        _Fit ("Fit", Range(1, 2.2)) = 1.38
        _GlowColor ("Glow Color", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

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
            Name "Emission"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Glow;
            float _Fit;
            float4 _GlowColor;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            float4 SampleLod(float2 uv, float fit, float lod)
            {
                float2 suv = (uv - 0.5) * fit + 0.5;
                float2 inset = min(suv, 1.0 - suv);
                float inside = saturate(min(inset.x, inset.y) * 10.0);
                float4 sample = tex2Dlod(_MainTex, float4(clamp(suv, 0.001, 0.999), 0.0, lod));
                sample.a *= inside;
                return sample;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float4 body = SampleLod(IN.texcoord, _Fit, 0.0);
                float bodyA = body.a;
                float4 border = SampleLod(IN.texcoord, max(_Fit * 0.86, 1.05), 3.1);
                float ring = saturate(border.a - bodyA * 0.9);
                ring = smoothstep(0.04, 0.42, ring);
                float edge = saturate(border.a) * (1.0 - smoothstep(0.62, 1.0, bodyA));
                float band = saturate(ring + edge * 0.55);

                float3 glowCol = _GlowColor.rgb;
                float3 core = body.rgb * IN.color.rgb * bodyA;
                float3 fringe = glowCol * band * _Glow;

                float alpha = saturate(bodyA + band * _Glow * 0.85);
                alpha *= IN.color.a;
                float3 rgbOut = (core + fringe) * IN.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                float clipMask = UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                alpha *= clipMask;
                rgbOut *= clipMask;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha - 0.001);
                #endif

                return float4(rgbOut, alpha);
            }
            ENDCG
        }
    }
}

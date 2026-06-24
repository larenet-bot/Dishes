Shader "Hidden/IncrementalDishes/GameFilterUIElementLens"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        _Grayscale ("Grayscale", Range(0,1)) = 0
        _Saturation ("Saturation", Range(0,2)) = 1
        _Contrast ("Contrast", Range(0.25,3)) = 1
        _Brightness ("Brightness", Range(-0.5,0.5)) = 0
        _TintColor ("Tint Color", Color) = (1,1,1,1)
        _TintStrength ("Tint Strength", Range(0,1)) = 0
        _BlurStrength ("Blur Strength", Range(0,1)) = 0
        _GlowStrength ("Glow Strength", Range(0,1.5)) = 0
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _HazeStrength ("Haze Strength", Range(0,1)) = 0
        _HazeColor ("Haze Color", Color) = (1,1,1,1)
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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            float _Grayscale;
            float _Saturation;
            float _Contrast;
            float _Brightness;
            fixed4 _TintColor;
            float _TintStrength;
            float _BlurStrength;
            float _GlowStrength;
            fixed4 _GlowColor;
            float _HazeStrength;
            fixed4 _HazeColor;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 ApplyLens(fixed4 baseCol, float2 uv)
            {
                float2 px = _MainTex_TexelSize.xy * lerp(0.0, 13.0, _BlurStrength);
                fixed4 blurCol = baseCol * 0.28;
                blurCol += tex2D(_MainTex, uv + float2( px.x, 0)) * 0.11;
                blurCol += tex2D(_MainTex, uv + float2(-px.x, 0)) * 0.11;
                blurCol += tex2D(_MainTex, uv + float2(0,  px.y)) * 0.11;
                blurCol += tex2D(_MainTex, uv + float2(0, -px.y)) * 0.11;
                blurCol += tex2D(_MainTex, uv + float2( px.x,  px.y)) * 0.07;
                blurCol += tex2D(_MainTex, uv + float2(-px.x,  px.y)) * 0.07;
                blurCol += tex2D(_MainTex, uv + float2( px.x, -px.y)) * 0.07;
                blurCol += tex2D(_MainTex, uv + float2(-px.x, -px.y)) * 0.07;
                blurCol += tex2D(_MainTex, uv + float2( px.x * 2.0, 0)) * 0.035;
                blurCol += tex2D(_MainTex, uv + float2(-px.x * 2.0, 0)) * 0.035;
                blurCol += tex2D(_MainTex, uv + float2(0, px.y * 2.0)) * 0.035;
                blurCol += tex2D(_MainTex, uv + float2(0, -px.y * 2.0)) * 0.035;

                fixed4 col = lerp(baseCol, blurCol, _BlurStrength);
                float originalAlpha = col.a;

                float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = lerp(col.rgb, gray.xxx, _Grayscale);

                float satGray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = lerp(satGray.xxx, col.rgb, _Saturation);

                col.rgb = (col.rgb - 0.5) * _Contrast + 0.5;
                col.rgb += _Brightness;
                col.rgb = lerp(col.rgb, col.rgb * _TintColor.rgb, _TintStrength);

                float lum = dot(blurCol.rgb, float3(0.299, 0.587, 0.114));
                float glowMask = smoothstep(0.22, 0.92, lum);
                col.rgb += blurCol.rgb * glowMask * _GlowStrength * _GlowColor.rgb;
                col.rgb = lerp(col.rgb, _HazeColor.rgb, _HazeStrength * 0.42);
                col.rgb = saturate(col.rgb);
                col.a = originalAlpha;
                return col;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                color = ApplyLens(color, IN.texcoord);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}

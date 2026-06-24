Shader "Hidden/IncrementalDishes/GameFilterCameraEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                fixed4 baseCol = tex2D(_MainTex, uv);

                float2 px = _MainTex_TexelSize.xy * lerp(0.0, 10.0, _BlurStrength);
                fixed4 blurCol = baseCol * 0.24;
                blurCol += tex2D(_MainTex, uv + float2( px.x, 0)) * 0.10;
                blurCol += tex2D(_MainTex, uv + float2(-px.x, 0)) * 0.10;
                blurCol += tex2D(_MainTex, uv + float2(0,  px.y)) * 0.10;
                blurCol += tex2D(_MainTex, uv + float2(0, -px.y)) * 0.10;
                blurCol += tex2D(_MainTex, uv + float2( px.x,  px.y)) * 0.09;
                blurCol += tex2D(_MainTex, uv + float2(-px.x,  px.y)) * 0.09;
                blurCol += tex2D(_MainTex, uv + float2( px.x, -px.y)) * 0.09;
                blurCol += tex2D(_MainTex, uv + float2(-px.x, -px.y)) * 0.09;
                blurCol += tex2D(_MainTex, uv + float2( px.x * 2.0, 0)) * 0.035;
                blurCol += tex2D(_MainTex, uv + float2(-px.x * 2.0, 0)) * 0.035;
                blurCol += tex2D(_MainTex, uv + float2(0, px.y * 2.0)) * 0.035;
                blurCol += tex2D(_MainTex, uv + float2(0, -px.y * 2.0)) * 0.035;

                fixed4 col = lerp(baseCol, blurCol, _BlurStrength);

                float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = lerp(col.rgb, gray.xxx, _Grayscale);

                float satGray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = lerp(satGray.xxx, col.rgb, _Saturation);

                col.rgb = (col.rgb - 0.5) * _Contrast + 0.5;
                col.rgb += _Brightness;
                col.rgb = lerp(col.rgb, col.rgb * _TintColor.rgb, _TintStrength);

                float luminance = dot(blurCol.rgb, float3(0.299, 0.587, 0.114));
                float glowMask = smoothstep(0.28, 0.95, luminance);
                col.rgb += blurCol.rgb * glowMask * _GlowStrength * _GlowColor.rgb;

                float2 centerDelta = uv - 0.5;
                float softLens = 1.0 - saturate(length(centerDelta) * 1.15);
                float haze = _HazeStrength * (0.25 + softLens * 0.55);
                col.rgb = lerp(col.rgb, _HazeColor.rgb, haze);

                col.rgb = saturate(col.rgb);
                return col;
            }
            ENDCG
        }
    }
}

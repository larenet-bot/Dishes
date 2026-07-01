Shader "Hidden/IncrementalDishes/GameFilterProceduralLayer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Mode ("Mode", Float) = 0
        _ColorA ("Color A", Color) = (1,1,1,1)
        _ColorB ("Color B", Color) = (1,1,1,1)
        _Alpha ("Alpha", Range(0,1)) = 0
        _Scale ("Scale", Range(0.25,8)) = 1
        _Speed ("Speed", Range(0,4)) = 1
        _Intensity ("Intensity", Range(0,3)) = 1
        _Direction ("Direction", Vector) = (1,0,0,0)
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

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

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
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float _Mode;
            fixed4 _ColorA;
            fixed4 _ColorB;
            float _Alpha;
            float _Scale;
            float _Speed;
            float _Intensity;
            float4 _Direction;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float hash11(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash21(i);
                float b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1));
                float d = hash21(i + float2(1,1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float value = 0.0;
                float amp = 0.5;
                for (int i = 0; i < 5; i++)
                {
                    value += noise(p) * amp;
                    p *= 2.02;
                    amp *= 0.52;
                }
                return value;
            }

            float softBlob(float2 uv, float2 center, float radius, float warp, float seed)
            {
                float n = fbm(uv * (4.0 + warp * 4.5) + seed);
                float2 warped = uv + float2(n - 0.5, fbm(uv * 5.7 + seed * 1.91) - 0.5) * warp * 0.075;
                float d = distance(warped, center);
                return 1.0 - smoothstep(radius * 0.42, radius, d);
            }

            float cornerBlobField(float2 uv, out fixed3 outColor)
            {
                float t = _Time.y * _Speed;
                float a = 0.0;

                for (int i = 0; i < 16; i++)
                {
                    float cornerIndex = fmod(i, 4.0);
                    float2 corner = cornerIndex < 0.5 ? float2(0.04, 0.04) :
                                    cornerIndex < 1.5 ? float2(0.96, 0.04) :
                                    cornerIndex < 2.5 ? float2(0.04, 0.96) : float2(0.96, 0.96);

                    float2 jitter = float2(hash11(i * 19.3), hash11(i * 37.9)) - 0.5;
                    float2 center = corner + jitter * 0.34;
                    center += float2(sin(t * 0.17 + i) * 0.012, cos(t * 0.13 + i * 1.7) * 0.012);
                    float radius = lerp(0.13, 0.34, hash11(i * 11.2));
                    float b = softBlob(uv, center, radius, 1.6, i * 2.7);
                    b *= lerp(0.55, 1.0, fbm(uv * 12.0 + i));
                    a = max(a, b);
                }

                float cornerMask = max(max(1.0 - smoothstep(0.03, 0.46, distance(uv, float2(0,0))),
                                           1.0 - smoothstep(0.03, 0.46, distance(uv, float2(1,0)))),
                                       max(1.0 - smoothstep(0.03, 0.46, distance(uv, float2(0,1))),
                                           1.0 - smoothstep(0.03, 0.46, distance(uv, float2(1,1)))));
                a = saturate(a * 0.90 + cornerMask * 0.35);
                outColor = lerp(_ColorA.rgb, _ColorB.rgb, saturate(a));
                return saturate(pow(a, 1.05) * _Intensity);
            }

            float lavaLampField(float2 uv, out fixed3 outColor)
            {
                float t = _Time.y * max(0.05, _Speed);
                float field = 0.0;
                fixed3 colorSum = 0.0;
                float colorWeight = 0.0;

                for (int i = 0; i < 9; i++)
                {
                    float baseX = lerp(0.12, 0.88, hash11(i * 41.7));
                    float baseY = lerp(0.12, 0.88, hash11(i * 13.1));
                    float2 center = float2(
                        baseX + sin(t * lerp(0.10, 0.24, hash11(i * 4.1)) + i * 2.0) * lerp(0.05, 0.13, hash11(i * 7.3)),
                        baseY + sin(t * lerp(0.09, 0.20, hash11(i * 6.9)) + i * 1.3) * lerp(0.08, 0.22, hash11(i * 9.4))
                    );

                    float radius = lerp(0.085, 0.165, hash11(i * 21.4));
                    float angle = t * lerp(-0.42, 0.46, hash11(i * 5.6)) + i * 0.77;
                    float s = sin(angle);
                    float c = cos(angle);
                    float2 delta = uv - center;
                    float2 rotated = float2(delta.x * c - delta.y * s, delta.x * s + delta.y * c);

                    float ovalWave = sin(t * lerp(0.32, 0.62, hash11(i * 8.8)) + i * 2.4);
                    float aspect = lerp(0.94, 1.20, ovalWave * 0.5 + 0.5);
                    rotated.x /= aspect;
                    rotated.y *= aspect;

                    float d2 = dot(rotated, rotated) + 0.0018;
                    float contribution = (radius * radius) / d2;

                    // Shape shift the edge without turning the whole screen into gas.
                    float rimNoise = lerp(0.90, 1.14, fbm(uv * 5.0 + float2(i * 0.27, t * 0.04)));
                    contribution *= rimNoise;

                    field += contribution;
                    colorSum += lerp(_ColorA.rgb, _ColorB.rgb, hash11(i * 18.8)) * contribution;
                    colorWeight += contribution;
                }

                outColor = colorWeight > 0.001 ? colorSum / colorWeight : _ColorA.rgb;
                float alpha = smoothstep(1.05, 2.35, field);
                return saturate(alpha * _Intensity);
            }

            float fogCloud(float2 uv, float2 center, float radius, float seed)
            {
                float shape = softBlob(uv, center, radius, 2.4, seed);
                shape += softBlob(uv, center + float2(radius * 0.55, radius * 0.10), radius * 0.78, 1.9, seed + 3.1) * 0.65;
                shape += softBlob(uv, center + float2(-radius * 0.48, -radius * 0.08), radius * 0.72, 1.8, seed + 7.4) * 0.55;
                return saturate(shape);
            }

            float rollingFogField(float2 uv, out fixed3 outColor)
            {
                float t = _Time.y * max(0.05, _Speed);
                float cloudAlpha = 0.0;

                for (int i = 0; i < 11; i++)
                {
                    float seed = i * 23.71;
                    float life = frac(t * lerp(0.030, 0.060, hash11(seed + 1.7)) + hash11(seed));
                    float fadeIn = smoothstep(0.02, 0.26, life);
                    float fadeOut = 1.0 - smoothstep(0.64, 0.98, life);
                    float fade = fadeIn * fadeOut;

                    float startX = lerp(1.08, 1.42, hash11(seed + 6.1));
                    float endX = lerp(-0.42, -0.08, hash11(seed + 9.2));
                    float x = lerp(startX, endX, life);
                    float y = lerp(0.10, 0.90, hash11(seed + 3.4)) + sin(t * 0.25 + i) * 0.035;
                    float radius = lerp(0.16, 0.36, hash11(seed + 2.9));
                    cloudAlpha += fogCloud(uv, float2(x, y), radius, seed + t * 0.05) * fade * lerp(0.28, 0.76, hash11(seed + 11.8));
                }

                float alpha = saturate(cloudAlpha * _Intensity);
                outColor = lerp(_ColorB.rgb, _ColorA.rgb, saturate(alpha + 0.15));
                return alpha;
            }

            float dreamHaze(float2 uv, out fixed3 outColor)
            {
                float t = _Time.y * _Speed;
                float n = fbm(uv * 2.5 + float2(t * 0.045, t * 0.025));
                float soft = smoothstep(0.34, 0.94, n);
                float radialA = 1.0 - smoothstep(0.07, 0.72, distance(uv, float2(0.18, 0.20)));
                float radialB = 1.0 - smoothstep(0.08, 0.76, distance(uv, float2(0.85, 0.24)));
                float radialC = 1.0 - smoothstep(0.08, 0.88, distance(uv, float2(0.50, 0.75)));
                float alpha = saturate((soft * 0.28 + radialA * 0.23 + radialB * 0.17 + radialC * 0.15) * _Intensity);
                outColor = lerp(_ColorA.rgb, _ColorB.rgb, n);
                return alpha;
            }

            float2 neonZoneCenter(int zoneIndex, float segment)
            {
                float a = hash11(segment * 37.11 + zoneIndex * 17.31);
                float b = hash11(segment * 19.41 + zoneIndex * 29.17);

                if (zoneIndex == 0)
                {
                    return float2(lerp(0.10, 0.34, a), lerp(0.16, 0.84, b));
                }
                else if (zoneIndex == 1)
                {
                    return float2(lerp(0.66, 0.92, a), lerp(0.16, 0.84, b));
                }

                return float2(lerp(0.34, 0.66, a), lerp(0.12, 0.90, b));
            }

            float neonGlow(float2 uv, out fixed3 outColor)
            {
                float t = _Time.y * max(0.05, _Speed);
                float alpha = 0.0;
                fixed3 weighted = 0.0;
                float weight = 0.0;

                for (int i = 0; i < 3; i++)
                {
                    float phase = t * 0.16 + i * 0.39;
                    float segment = floor(phase);
                    float local = frac(phase);
                    float cross = smoothstep(0.62, 0.92, local);

                    float2 previousCenter = neonZoneCenter(i, segment);
                    float2 nextCenter = neonZoneCenter(i, segment + 1.0);
                    float radius = i == 2 ? 0.48 : 0.42;

                    float previousA = 1.0 - smoothstep(0.02, radius, distance(uv, previousCenter));
                    float nextA = 1.0 - smoothstep(0.02, radius, distance(uv, nextCenter));
                    previousA = previousA * previousA * (1.0 - cross);
                    nextA = nextA * nextA * cross;

                    fixed3 color = i == 1 ? _ColorB.rgb : (i == 2 ? lerp(_ColorA.rgb, _ColorB.rgb, 0.45) : _ColorA.rgb);
                    float spot = previousA + nextA;
                    alpha += spot;
                    weighted += color * spot;
                    weight += spot;
                }

                float flicker = lerp(0.88, 1.08, noise(float2(t * 1.7, 3.1)));
                alpha = saturate(alpha * flicker * _Intensity);
                outColor = weight > 0.001 ? weighted / weight : _ColorA.rgb;
                return alpha;
            }


            float classicNeonGlow(float2 uv, out fixed3 outColor)
            {
                float t = _Time.y * _Speed;
                float pulse = 0.85 + sin(t * 2.2) * 0.15;
                float2 p1 = float2(0.10 + sin(t * 0.31) * 0.035, 0.15 + cos(t * 0.23) * 0.025);
                float2 p2 = float2(0.92 + sin(t * 0.27) * 0.025, 0.12 + cos(t * 0.36) * 0.030);
                float2 p3 = float2(0.52 + sin(t * 0.18) * 0.045, 0.92);

                float a1 = 1.0 - smoothstep(0.02, 0.58, distance(uv, p1));
                float a2 = 1.0 - smoothstep(0.02, 0.58, distance(uv, p2));
                float a3 = 1.0 - smoothstep(0.02, 0.72, distance(uv, p3));
                a1 *= a1;
                a2 *= a2;
                a3 *= a3 * 0.65;

                float flicker = lerp(0.75, 1.25, noise(float2(t * 4.5, 3.1)));
                float alpha = saturate((a1 + a2 + a3) * pulse * flicker * _Intensity);
                outColor = normalize(_ColorA.rgb * a1 + _ColorB.rgb * a2 + lerp(_ColorA.rgb, _ColorB.rgb, 0.5) * a3 + 0.0001);
                return alpha;
            }

            float fixedTriColorNeonGlow(float2 uv, out fixed3 outColor)
            {
                float t = _Time.y * max(0.05, _Speed);

                // Fixed screen-space glow anchors:
                // _ColorA = blue bottom-left and top-right, _ColorB = pink bottom-right and top-left.
                float2 blueBottomLeftCenter = float2(0.00, 0.00);
                float2 blueTopRightCenter = float2(1.00, 1.00);
                float2 pinkBottomRightCenter = float2(1.00, 0.00);
                float2 pinkTopLeftCenter = float2(0.00, 1.00);

                // Pulse between a floor and a ceiling so no glow fully disappears.
                float bluePulse = lerp(0.62, 1.00, sin(t * 1.15 + 0.20) * 0.5 + 0.5);
                float pinkPulse = lerp(0.62, 1.00, sin(t * 1.10 + 2.35) * 0.5 + 0.5);

                float blueBottomLeft = 1.0 - smoothstep(0.02, 0.58, distance(uv, blueBottomLeftCenter));
                float blueTopRight = 1.0 - smoothstep(0.02, 0.58, distance(uv, blueTopRightCenter));
                float pinkBottomRight = 1.0 - smoothstep(0.02, 0.58, distance(uv, pinkBottomRightCenter));
                float pinkTopLeft = 1.0 - smoothstep(0.02, 0.58, distance(uv, pinkTopLeftCenter));

                blueBottomLeft *= blueBottomLeft * bluePulse;
                blueTopRight *= blueTopRight * bluePulse;
                pinkBottomRight *= pinkBottomRight * pinkPulse;
                pinkTopLeft *= pinkTopLeft * pinkPulse;

                float blue = blueBottomLeft + blueTopRight;
                float pink = pinkBottomRight + pinkTopLeft;
                float weight = blue + pink;

                if (weight > 0.001)
                {
                    outColor = (_ColorA.rgb * blue + _ColorB.rgb * pink) / weight;
                }
                else
                {
                    outColor = lerp(_ColorA.rgb, _ColorB.rgb, 0.5);
                }

                return saturate(weight * _Intensity);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                uv = (uv - 0.5) * _Scale + 0.5;

                fixed3 color = _ColorA.rgb;
                float alpha = 0.0;

                if (_Mode > 0.5 && _Mode < 1.5)
                {
                    alpha = cornerBlobField(uv, color);
                }
                else if (_Mode > 1.5 && _Mode < 2.5)
                {
                    alpha = lavaLampField(uv, color);
                }
                else if (_Mode > 2.5 && _Mode < 3.5)
                {
                    alpha = rollingFogField(uv, color);
                }
                else if (_Mode > 3.5 && _Mode < 4.5)
                {
                    alpha = dreamHaze(uv, color);
                }
                else if (_Mode > 4.5 && _Mode < 5.5)
                {
                    alpha = neonGlow(uv, color);
                }
                else if (_Mode > 5.5 && _Mode < 6.5)
                {
                    alpha = classicNeonGlow(uv, color);
                }
                else if (_Mode > 6.5 && _Mode < 7.5)
                {
                    alpha = fixedTriColorNeonGlow(uv, color);
                }

                alpha = saturate(alpha * _Alpha) * i.color.a;
                return fixed4(color, alpha);
            }
            ENDCG
        }
    }
}

Shader "BankerLoot/Washing Water"
{
    // Water inside the washing machine drum, drawn on a quad that faces the door.
    // A pool with a wavy foamy surface that rises with _Fill; the water inside swirls around the drum at _Speed.
    Properties
    {
        _DeepColor ("Deep Color", Color) = (0.06, 0.34, 0.68, 1)
        _ShallowColor ("Shallow Color", Color) = (0.42, 0.78, 1, 1)
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _Alpha ("Alpha", Range(0, 1)) = 0.8
        _Fill ("Fill", Range(0, 1)) = 1
        _Level ("Water Level At Full", Range(-1, 1)) = 0.2
        _Speed ("Swirl Speed (rad/s, positive = counter clockwise seen from the door)", Float) = 3.8
        _WaveHeight ("Wave Height", Float) = 0.05
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Unlit"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _ShallowColor;
                half4 _FoamColor;
                half _Alpha;
                half _Fill;
                half _Level;
                half _Speed;
                half _WaveHeight;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float amp = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    v += valueNoise(p) * amp;
                    p = p * 2.03 + 17.7;
                    amp *= 0.5;
                }
                return v;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 p = (IN.uv - 0.5) * 2.0;
                float t = _Time.y;

                // round drum
                float disc = 1.0 - smoothstep(0.86, 1.0, length(p));

                // water line: rises with _Fill, moving waves on top
                float level = lerp(-1.05, _Level, saturate(_Fill));
                float wave = sin(p.x * 9.0 + t * 2.6) * _WaveHeight
                           + sin(p.x * 17.0 - t * 3.7) * _WaveHeight * 0.5
                           + (valueNoise(float2(p.x * 6.0 + t * 1.2, t * 0.5)) - 0.5) * _WaveHeight;
                float depth = level + wave - p.y;
                float body = smoothstep(0.0, 0.03, depth);

                // the water turns with the notes (counter clockwise seen from the door)
                float a = _Speed * t;
                float c = cos(a);
                float s = sin(a);
                float2 q = float2(c * p.x + s * p.y, -s * p.x + c * p.y);
                float n = fbm(q * 3.2 + float2(0.0, t * 0.15));
                float n2 = fbm(q * 7.0 - t * 0.3);

                float3 col = lerp(_DeepColor.rgb, _ShallowColor.rgb, n);
                // white streaks in the current and foam along the water line
                float lineFoam = 1.0 - smoothstep(0.0, 0.07, depth);
                float foam = saturate(lineFoam + smoothstep(0.6, 0.8, n2) * 0.55);
                col = lerp(col, _FoamColor.rgb, foam);

                float alpha = body * disc * _Alpha * (0.78 + 0.22 * n);
                alpha = max(alpha, foam * body * disc * 0.9);
                alpha *= saturate(_Fill * 4.0);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}

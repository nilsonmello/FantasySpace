Shader "Custom/OrganicDarknessMask"
{
    Properties
    {
        _WallColor ("Wall Color", Color) = (0.03, 0.03, 0.03, 1)
        _WallNoiseColor ("Wall Noise Variation Color", Color) = (0.07, 0.07, 0.07, 1)
        _WallNoiseAmount ("Wall Noise Amount", Range(0, 1)) = 0.35

        _MaskTex ("Darkness Mask (blurred)", 2D) = "black" {}
        _WorldSize ("World Size (setado pelo DungeonDarknessMask.cs)", Float) = 100

        _WarpFrequency ("Warp Frequency", Float) = 0.08
        _WarpStrength ("Warp Strength (em unidades de mundo)", Float) = 1.2

        _SpikeFrequency ("Spike Frequency", Float) = 0.35
        _SpikeReach ("Spike Reach (unidades de mundo pra dentro da sala)", Float) = 2.5
        _SpikeThresholdNear ("Spike Threshold Near Wall (mais baixo = mais espinhos)", Range(0, 1)) = 0.35

        _WaveSpeed ("Wave Speed (velocidade da ondulacao)", Float) = 0.06
        _PulseSpeed ("Pulse Speed", Float) = 0.8
        _PulseAmount ("Pulse Amount (0 = desliga a pulsacao)", Range(0, 1)) = 0.15

        _PixelsPerUnit ("Pixel Snap (bata com o PPU do tileset)", Float) = 16
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _WallColor;
                float4 _WallNoiseColor;
                float _WallNoiseAmount;
                float _WorldSize;

                float _WarpFrequency;
                float _WarpStrength;

                float _SpikeFrequency;
                float _SpikeReach;
                float _SpikeThresholdNear;

                float _WaveSpeed;
                float _PulseSpeed;
                float _PulseAmount;

                float _PixelsPerUnit;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float2 hash22(float2 p)
            {
                return float2(hash21(p), hash21(p + 17.13));
            }

            float noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));

                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float value = 0;
                float amp = 0.5;
                float freq = 1.0;
                [unroll]
                for (int o = 0; o < 3; o++)
                {
                    value += amp * (noise2D(p * freq) * 2.0 - 1.0);
                    freq *= 2.13;
                    amp *= 0.5;
                }
                return value;
            }

            float ridgedFbm(float2 p)
            {
                float value = 0;
                float amp = 0.5;
                float freq = 1.0;
                [unroll]
                for (int o = 0; o < 3; o++)
                {
                    float n = 1.0 - abs(noise2D(p * freq) * 2.0 - 1.0);
                    n *= n;
                    value += amp * n;
                    freq *= 2.13;
                    amp *= 0.5;
                }
                return value; 
            }

            float4 frag(Varyings i) : SV_Target
            {
                float2 worldPos = i.uv * _WorldSize;
                float2 p = floor(worldPos * _PixelsPerUnit) / _PixelsPerUnit; 

                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;

                float2 timeOffset = _Time.y * _WaveSpeed;
                float2 warp = float2(
                    fbm(p * _WarpFrequency + timeOffset + 11.3),
                    fbm(p * _WarpFrequency - timeOffset + 47.7)
                ) * _WarpStrength * pulse;

                float2 warpedUV = (p + warp) / _WorldSize;
                float mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, warpedUV).r; 
                float coreWall = step(0.5, mask);

                float distIntoRoom = max(0.5 - mask, 0.0);

                float spikeFade = saturate(distIntoRoom / max(_SpikeReach / _WorldSize, 1e-5));
                float spikes = ridgedFbm(p * _SpikeFrequency + timeOffset * 1.4);
                float spikeThreshold = lerp(_SpikeThresholdNear, 1.0, spikeFade) / pulse;
                float spikeMask = step(spikeThreshold, spikes) * step(mask, 0.5);

                float wallMask = max(coreWall, spikeMask);
                if (wallMask < 0.5) discard;

                float shade = noise2D(p * 0.6 + 3.7);
                float4 col = lerp(_WallColor, _WallNoiseColor, shade * _WallNoiseAmount);

                return col;
            }
            ENDHLSL
        }
    }
}

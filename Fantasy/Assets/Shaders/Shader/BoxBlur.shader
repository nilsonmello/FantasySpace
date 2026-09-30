Shader "Hidden/Custom/BoxBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        float4 _MainTex_TexelSize;
        float _BlurSize;

        struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
        struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

        Varyings vert(Attributes v)
        {
            Varyings o;
            o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
            o.uv = v.uv;
            return o;
        }
        ENDHLSL

        Pass 
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 frag(Varyings i) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy;
                float4 sum = 0;
                float weightSum = 0;
                for (int x = -4; x <= 4; x++)
                {
                    float w = 1.0 - abs(x) / 5.0;
                    sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(texel.x * x * _BlurSize, 0)) * w;
                    weightSum += w;
                }
                return sum / weightSum;
            }
            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 frag(Varyings i) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy;
                float4 sum = 0;
                float weightSum = 0;
                for (int y = -4; y <= 4; y++)
                {
                    float w = 1.0 - abs(y) / 5.0;
                    sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0, texel.y * y * _BlurSize)) * w;
                    weightSum += w;
                }
                return sum / weightSum;
            }
            ENDHLSL
        }
    }
}

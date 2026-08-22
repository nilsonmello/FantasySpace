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

        // NOVO: controla, em unidades de MUNDO (nao em valor de mask 0-1),
        // o quanto a fronteira recua (revela mais tile) ou avanca (cobre
        // mais tile). Positivo = avanca sobre o tile. Negativo = recua,
        // deixa mais tile visivel por baixo do shader.
        _EdgeOffsetWorldUnits ("Edge Offset (unidades de mundo)", Range(-3, 3)) = 0

        // NOVO: fator de calibracao entre unidades de mundo e o "espaco de
        // mask" (0-1) que o blur produz. So precisa mexer aqui UMA VEZ ao
        // configurar (ou sempre que trocar texelsPerCell/blurIterations no
        // DungeonDarknessMask.cs) — depois disso, _EdgeOffsetWorldUnits
        // passa a se comportar de forma consistente.
        // Como calibrar: bote _EdgeOffsetWorldUnits = 1, olhe quanto a
        // fronteira andou usando o grid do tileset como regua, e ajusta
        // esse fator ate 1 unidade de mundo corresponder de fato a 1
        // unidade visual. Blur mais largo (mais texelsPerCell/blurIterations)
        // = precisa de um fator MENOR aqui (o gradiente ja cobre mais
        // espaco por unidade de mask).
        _EdgeBiasPerWorldUnit ("Edge Bias Calibration", Float) = 0.15

        // NOVO: controla o quanto a Global Light 2D consegue "clarear" a parede.
        // 0 = luz não afeta nada (comportamento antigo). 1 = luz afeta em cheio.
        _LightInfluence ("Light Influence", Range(0, 1)) = 1

        // NOVO: toggle. Desligado (0, padrao) = comportamento de sempre,
        // o shader so aparece perto do tileset real (_MaskTex). Ligado (1)
        // = o shader TAMBEM preenche qualquer pixel que esteja sem luz
        // nenhuma (fora do alcance da Global Light), mesmo que esteja bem
        // longe de qualquer parede — cobre toda area escura do cenario,
        // nao so o entorno do tileset.
        [Toggle] _FillAllDarkArea ("Aplicar em toda area escura (nao so no tileset)", Float) = 0

        // NOVO: so importa quando _FillAllDarkArea esta ligado. Luminancia
        // da luz abaixo desse valor conta como "escuro o bastante" pra
        // preencher. Mais alto = preenche uma area maior (conta como
        // escuro mais cedo). Mais baixo = so preenche onde esta
        // completamente sem luz.
        _DarknessCutoff ("Darkness Cutoff (usado com Fill All Dark Area)", Range(0, 1)) = 0.12
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

            // NOVO: screenPos pra poder amostrar a textura de luz do 2D Renderer
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; float4 screenPos : TEXCOORD1; };

            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            // NOVO: textura global de luz (Blend Style 0) exposta pelo URP 2D Renderer.
            // Se sua Global Light estiver configurada num Blend Style diferente de 0,
            // troca pra _ShapeLightTexture1 / _ShapeLightTexture2 / _ShapeLightTexture3.
            TEXTURE2D(_ShapeLightTexture0);
            SAMPLER(sampler_ShapeLightTexture0);

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

                float _EdgeOffsetWorldUnits; // NOVO
                float _EdgeBiasPerWorldUnit; // NOVO

                float _LightInfluence; // NOVO

                float _FillAllDarkArea; // NOVO
                float _DarknessCutoff; // NOVO
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.screenPos = ComputeScreenPos(o.positionHCS); // NOVO
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

                // CORRIGIDO: pulse agora e unidirecional. Antes, o seno
                // oscilava pra cima E pra baixo do baseline (1.0), e do
                // lado de baixo (_PulseAmount alto o suficiente) pulse
                // podia chegar perto de 0 — e como o threshold dos
                // espinhos divide por pulse, isso estourava o threshold
                // pra cima e apagava os espinhos, voltando ao formato cru
                // da parede. Agora pulse vai de 1.0 (baseline, "ja
                // estabelecido") ate 1.0 + _PulseAmount (pico) e volta pro
                // baseline — nunca encolhe alem do que ja esta configurado.
                float pulse = 1.0 + (sin(_Time.y * _PulseSpeed) * 0.5 + 0.5) * _PulseAmount;

                float2 timeOffset = _Time.y * _WaveSpeed;
                float2 warp = float2(
                    fbm(p * _WarpFrequency + timeOffset + 11.3),
                    fbm(p * _WarpFrequency - timeOffset + 47.7)
                ) * _WarpStrength * pulse;

                float2 warpedUV = (p + warp) / _WorldSize;
                float rawMask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, warpedUV).r;

                // NOVO: converte a distancia desejada (em unidades de mundo)
                // pro espaco de mask (0-1) usando o fator de calibracao, e
                // so entao desloca o mask antes de qualquer threshold. E o
                // que faz a fronteira (parede + espinhos) recuar/avancar
                // como um bloco so, com um numero que significa a mesma
                // coisa mesmo se voce mudar o blur depois.
                float bias = _EdgeOffsetWorldUnits * _EdgeBiasPerWorldUnit;
                float mask = saturate(rawMask + bias);

                float coreWall = step(0.5, mask);

                float distIntoRoom = max(0.5 - mask, 0.0);

                // CORRIGIDO (de novo): antes o pulse ainda entrava dividindo
                // o threshold perto da parede. Isso funciona SO SE a "zona
                // perto da parede" (onde spikeFade < 1) for realmente
                // pequena — mas se _SpikeReach/_WorldSize nao for bem menor
                // que 1, essa zona cobre a tela quase inteira, e qualquer
                // divisao por pulse ali vaza ruido em tela cheia (o que
                // voce viu no print).
                //
                // Agora o pulse so mexe no ALCANCE (_SpikeReach * pulse),
                // nunca no threshold em si. O threshold fica sempre
                // exatamente lerp(_SpikeThresholdNear, 1.0, spikeFade), que
                // matematicamente nunca passa de 1.0 — entao o campo
                // distante (spikeFade = 1) fica travado em 1.0 SEMPRE,
                // impossivel de furar, nao importa o valor de pulse.
                // Visualmente o efeito continua sendo um "respirar": a
                // zona onde os espinhos existem estica um pouco mais longe
                // no pico e volta a encolher, sem nunca abrir brecha longe
                // da parede.
                float effectiveReach = max(_SpikeReach * pulse, 1e-5);
                float spikeFade = saturate(distIntoRoom / (effectiveReach / _WorldSize));
                float spikes = ridgedFbm(p * _SpikeFrequency + timeOffset * 1.4);
                float spikeThreshold = lerp(_SpikeThresholdNear, 1.0, spikeFade);
                float spikeMask = step(spikeThreshold, spikes) * step(mask, 0.5);

                float wallMask = max(coreWall, spikeMask);

                // NOVO: amostra a luz global (Blend Style 0) na posição de
                // tela deste pixel. Precisa ser feito ANTES do discard
                // porque agora ela tambem pode decidir se o pixel fica
                // visivel (nao so a cor final dele).
                float2 lightUV = i.screenPos.xy / i.screenPos.w;
                float3 shapeLight = SAMPLE_TEXTURE2D(_ShapeLightTexture0, sampler_ShapeLightTexture0, lightUV).rgb;

                // NOVO: com _FillAllDarkArea ligado, qualquer pixel que
                // esteja sem luz (luminancia abaixo de _DarknessCutoff)
                // tambem conta como "deve mostrar o shader", mesmo estando
                // longe de qualquer parede do tileset. _FillAllDarkArea e
                // 0 ou 1 (checkbox), entao o lerp abaixo funciona como um
                // liga/desliga sem precisar de branch.
                float luminance = dot(shapeLight, float3(0.299, 0.587, 0.114));
                float darkAreaMask = step(luminance, _DarknessCutoff);
                float combinedMask = max(wallMask, darkAreaMask * _FillAllDarkArea);

                if (combinedMask < 0.5) discard;

                float shade = noise2D(p * 0.6 + 3.7);
                float4 col = lerp(_WallColor, _WallNoiseColor, shade * _WallNoiseAmount);

                // lerp(1, shapeLight, _LightInfluence) deixa o efeito ajustável no Inspector:
                // _LightInfluence = 0 -> shader ignora luz (comportamento antigo)
                // _LightInfluence = 1 -> luz afeta em cheio
                float3 lightMul = lerp(float3(1, 1, 1), shapeLight, _LightInfluence);
                col.rgb *= lightMul;

                return col;
            }
            ENDHLSL
        }
    }
}

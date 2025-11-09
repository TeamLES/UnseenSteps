Shader "UnseenSteps/FrostedGlassSprite2D_RT"
{
    Properties
    {
        [PerRendererData]_MainTex("Sprite (mask)", 2D) = "white" {}
        _Color ("Mask Tint", Color) = (1,1,1,1)

        // „Mliečnosť“ – ako veľmi to vybieli (bazálna hmla)
        _Fog ("Base Fog", Range(0,1)) = 0.65
        _FogColor ("Fog Color", Color) = (1,1,1,1)

        // Frost vzor (grayscale, tile-ovateľný)
        _FrostTex ("Frost Pattern", 2D) = "gray" {}
        _FrostTiling ("Frost Tiling", Float) = 2.0
        _FrostStrength ("Frost Brighten", Range(0,1)) = 0.35

        // Jemné skreslenie (statická refrakcia v pixeloch)
        _DistortStrength ("Distortion (px)", Range(0,3)) = 0.8

        // Zafarbenie a desaturácia rozmazaného podkladu
        _Desaturate ("Desaturate", Range(0,1)) = 0.6
        _Tint ("Glass Tint", Color) = (1,1,1,1)
        _TintStrength ("Tint Strength", Range(0,1)) = 0.35

        _Opacity ("Opacity", Range(0,1)) = 0.92
    }

    SubShader
    {
        Tags{
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "FROSTED_2D_RT"
            Tags{ "LightMode"="Universal2D" } // 2D Renderer kompatibilné

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS: POSITION; float2 uv: TEXCOORD0; };
            struct Varyings { float4 positionHCS: SV_POSITION; float2 uv: TEXCOORD0; float4 screenPos: TEXCOORD1; };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            // rozmazaný obraz z BlurCapture.cs
            TEXTURE2D_X(_Unseen_BlurTex); SAMPLER(sampler_Unseen_BlurTex);

            // frost vzor (lokálny, viazaný na sprite UV)
            TEXTURE2D(_FrostTex); SAMPLER(sampler_FrostTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;

                float _Fog;
                float4 _FogColor;

                float _FrostTiling;
                float _FrostStrength;

                float _DistortStrength;

                float _Desaturate;
                float4 _Tint;
                float _TintStrength;
                float _Opacity;
            CBUFFER_END

            Varyings Vert(Attributes v){
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenPos = ComputeScreenPos(o.positionHCS);
                return o;
            }

            float FrostSample(float2 uv)
            {
                // tile-ovanie lokálne na sprit
                float2 tuv = uv * _FrostTiling;
                return SAMPLE_TEXTURE2D(_FrostTex, sampler_FrostTex, tuv).r;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                // maska tvaru skla
                half4 mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Color;
                if (mask.a <= 0.001h) discard;

                // screen-space uv pre rozmazaný podklad
                float2 uv = i.screenPos.xy / i.screenPos.w;
                float2 texel = 1.0 / _ScreenParams.xy;

                // Frost vzor
                float f0 = FrostSample(i.uv);

                // odhad gradientu z frost mapy pre statické „refrakčné“ vychýlenie
                float fx = FrostSample(i.uv + float2(1.0/_ScreenParams.x, 0)) - FrostSample(i.uv - float2(1.0/_ScreenParams.x, 0));
                float fy = FrostSample(i.uv + float2(0, 1.0/_ScreenParams.y)) - FrostSample(i.uv - float2(0, 1.0/_ScreenParams.y));
                float2 distort = float2(fx, fy) * _DistortStrength; // v px
                float2 uvDistorted = uv + distort * texel; // prepočet z px na uv

                // vzorka rozmazaného obrazu
                float3 col = SAMPLE_TEXTURE2D_X(_Unseen_BlurTex, sampler_Unseen_BlurTex, uvDistorted).rgb;

                // mrazený look – silná desaturácia + mierny tint
                float g = dot(col, float3(0.299, 0.587, 0.114));
                col = lerp(col, float3(g,g,g), saturate(_Desaturate));
                col = lerp(col, col * _Tint.rgb, saturate(_TintStrength));

                // „mliečny“ závoj: miešaj k FogColor (biela) podľa _Fog a frost vzoru
                float fogAmount = saturate(_Fog + f0 * _FrostStrength);
                col = lerp(col, _FogColor.rgb, fogAmount);

                return half4(col, mask.a * _Opacity);
            }
            ENDHLSL
        }
    }
}

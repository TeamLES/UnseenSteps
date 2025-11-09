Shader "Hidden/Unseen/KawaseBlur"
{
    Properties { _MainTex ("Source", 2D) = "white" {}  _Offset ("Offset", Range(0,5)) = 1 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Name "KAWASE"
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct VIn { float4 pos: POSITION; float2 uv: TEXCOORD0; };
            struct VOut { float4 pos: SV_POSITION; float2 uv: TEXCOORD0; };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_TexelSize; // x=1/w, y=1/h
                float _Offset;
            CBUFFER_END

            VOut Vert(VIn v){ VOut o; o.pos = TransformObjectToHClip(v.pos.xyz); o.uv = v.uv; return o; }

            half4 Frag(VOut i) : SV_Target
            {
                float2 t = _MainTex_TexelSize.xy * _Offset;
                float3 c = 0;
                c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2( t.x,  t.y)).rgb;
                c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-t.x,  t.y)).rgb;
                c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2( t.x, -t.y)).rgb;
                c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-t.x, -t.y)).rgb;
                c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2( 0,  2*t.y)).rgb;
                c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2( 0, -2*t.y)).rgb;
                c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2( 2*t.x, 0)).rgb;
                c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-2*t.x, 0)).rgb;
                return half4(c / 8.0, 1);
            }
            ENDHLSL
        }
    }
}

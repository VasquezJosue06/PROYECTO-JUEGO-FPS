Shader "TF2/2Fort Water"
{
    Properties
    {
        _WaterColor("Water tint", Color) = (0.20,0.17,0.05,0.65)
        _NormalMap("Source normal texture", 2D) = "gray" {}
        _WaveStrength("Wave strength", Range(0,0.2)) = 0.035
        _ScrollSpeed("Scroll speed", Float) = 0.025
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _WaterColor;
            float4 _NormalMap_ST;
            float _WaveStrength, _ScrollSpeed;
            CBUFFER_END
            struct Attributes { float4 positionOS: POSITION; float3 normalOS: NORMAL; float2 uv: TEXCOORD0; };
            struct Varyings { float4 positionCS: SV_POSITION; float3 positionWS: TEXCOORD0; float3 normalWS: TEXCOORD1; float2 uv: TEXCOORD2; float fog: TEXCOORD3; };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _NormalMap);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 Frag(Varyings i): SV_Target
            {
                float2 uv = i.uv + _Time.y * _ScrollSpeed * float2(1,0.7);
                // Source PNG is read as RGB; normal-map importer conversion is not assumed.
                float2 wave = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv).rg * 2 - 1;
                float3 n = normalize(i.normalWS + float3(wave.x,0,wave.y) * _WaveStrength);
                float3 view = normalize(GetWorldSpaceViewDir(i.positionWS));
                float fresnel = pow(1 - saturate(abs(dot(n, view))), 4);
                half3 color = lerp(_WaterColor.rgb, half3(0.48,0.55,0.60), fresnel * 0.7);
                return half4(MixFog(color,i.fog), saturate(_WaterColor.a + fresnel * 0.25));
            }
            ENDHLSL
        }
    }
}

Shader "FPS/Weapon Tracer"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                // Bordes suaves, cola tenue y centro brillante sin textura adicional.
                half center = saturate(1.0 - abs(input.uv.y * 2.0 - 1.0));
                half tail = smoothstep(0.0, 0.4, input.uv.x);
                half tip = 1.0 - smoothstep(0.9, 1.0, input.uv.x);
                half core = pow(center, 6.0);
                half3 glow = input.color.rgb * 1.4 + half3(1.0, 0.85, 0.55) * core;
                return half4(glow, input.color.a * center * tail * tip);
            }
            ENDHLSL
        }
    }
}

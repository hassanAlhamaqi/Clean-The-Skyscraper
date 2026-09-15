Shader "Sandouq/DirtyGlass"
{
    Properties
    {
        _DirtMask ("Dirt Mask", 2D) = "white" {}
        _DirtColor ("Dirt Color", Color) = (0.32, 0.22, 0.1, 0.96)
        _GlassColor ("Clean Glass Tint", Color) = (0.25, 0.7, 0.85, 0.12)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "DirtyGlass"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_DirtMask); SAMPLER(sampler_DirtMask);
            CBUFFER_START(UnityPerMaterial)
            float4 _DirtColor, _GlassColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float dirt = SAMPLE_TEXTURE2D(_DirtMask, sampler_DirtMask, input.uv).r;
                return lerp(_GlassColor, _DirtColor, dirt);
            }
            ENDHLSL
        }
    }
}

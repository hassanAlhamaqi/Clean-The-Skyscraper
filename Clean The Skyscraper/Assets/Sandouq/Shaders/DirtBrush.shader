Shader "Hidden/Sandouq/DirtBrush"
{
    Properties { _MainTex ("Mask", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex, _Pattern;
        float4 _Stroke, _WorldSize, _PatternTransform;
        float _Radius, _Strength, _Falloff, _UsePattern, _DirtAmount;
        float4 Initialize(v2f_img input) : SV_Target
        {
            float2 uv = input.uv * _PatternTransform.xy + _PatternTransform.zw;
            float pattern = 0.65 + 0.18 * sin(uv.x * 61 + sin(uv.y * 27)) + 0.17 * cos(uv.y * 47 + uv.x * 19);
            float mass = saturate(lerp(pattern, tex2D(_Pattern, uv).r, _UsePattern) * _DirtAmount);
            return float4(mass, mass, mass, 1);
        }
        float4 Erase(v2f_img input) : SV_Target
        {
            float2 position = input.uv * _WorldSize.xy;
            float2 start = _Stroke.xy * _WorldSize.xy;
            float2 finish = _Stroke.zw * _WorldSize.xy;
            float2 segment = finish - start;
            float along = saturate(dot(position - start, segment) / max(dot(segment, segment), 0.000001));
            float distanceToStroke = length(position - start - segment * along);
            float coverage = 1 - smoothstep(_Radius * (1 - _Falloff), _Radius, distanceToStroke);
            float mass = max(0, tex2D(_MainTex, input.uv).r - coverage * _Strength);
            return float4(mass, mass, mass, 1);
        }
        ENDCG
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Initialize
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Erase
            ENDCG
        }
    }
}

Shader "Custom/URP_Lambert_VertexColors"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,1)
        _BaseMap ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4 _BaseColor;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS   = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // 基础颜色 = 纹理 × Tint × 顶点色
                float3 baseColor = (SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor * i.color).rgb;

                // --- 简单漫反射光照 ---
                Light mainLight = GetMainLight();
                float3 N = normalize(i.normalWS);
                float3 L = normalize(mainLight.direction);
                float NdotL = saturate(dot(N, L));

                float3 diffuse = baseColor * mainLight.color * NdotL;

                return half4(diffuse, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}

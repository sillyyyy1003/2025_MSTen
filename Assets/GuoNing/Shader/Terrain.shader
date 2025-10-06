Shader "Custom/URP_Terrain"
{
    Properties
    {
        _BaseColor ("Tint Color", Color) = (1,1,1,1)
        _MainTex ("Terrain Texture Array", 2DArray) = "" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _TileScale ("Tile Scale", Float) = 0.02
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5   // 需要支持 Texture2DArray

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
            };

            // --- 声明 Texture2DArray ---
            TEXTURE2D_ARRAY(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _BaseColor;
            float _Glossiness;
            float _Metallic;
            float _TileScale;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionWS  = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS    = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // 世界坐标 XZ → 纹理坐标
                float2 uv = i.positionWS.xz * _TileScale;

                // 采样第 0 张纹理（以后可扩展）
               float3 texColor = SAMPLE_TEXTURE2D_ARRAY(_MainTex, sampler_MainTex, uv, 0).rgb;

                float3 baseColor = texColor * _BaseColor.rgb;

                // --- 简单漫反射 ---
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

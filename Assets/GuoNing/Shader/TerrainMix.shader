Shader "Custom/URP_TerrainBlended"
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
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5   // Texture2DArray 需要 3.5+

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;       // 用作 splat 权重
                float3 terrain    : TEXCOORD2;   // 存放三个 index
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float4 color       : COLOR;
                float3 terrain     : TEXCOORD2;
            };

            // --- 声明 Texture2DArray ---
            TEXTURE2D_ARRAY(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _BaseColor;
            float _Glossiness;
            float _Metallic;
            float _TileScale;

            // 顶点函数
            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionWS  = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS    = TransformObjectToWorldNormal(v.normalOS);

                o.color = v.color;       // 顶点色作为混合权重
                o.terrain = v.terrain;   // 保存 index
                return o;
            }

            // --- 工具函数：采样某个 index ---
            float4 GetTerrainColor(Varyings i, int index)
            {
                float2 uv = i.positionWS.xz * _TileScale;

                // index 对应 slice
                int slice = (int)round(i.terrain[index]);

                float4 tex = SAMPLE_TEXTURE2D_ARRAY(_MainTex, sampler_MainTex, uv, slice);

                // 对应的混合权重 = 顶点色.r/g/b
                float weight = i.color[index];

                return tex * weight;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // 混合三种地形纹理
                float4 c = 
                    GetTerrainColor(i, 0) +
                    GetTerrainColor(i, 1) +
                    GetTerrainColor(i, 2);

                float3 baseColor = c.rgb * _BaseColor.rgb;

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

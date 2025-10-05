Shader "Custom/URP_TerrainBlended"
{
    Properties
    {
        _BaseColor ("Tint Color", Color) = (1,1,1,1)
        _MainTex ("Terrain Texture Array", 2DArray) = "" {}
        _GridTex ("Grid Texture", 2D) = "white" {}        // ★ 新增：网格贴图
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
            #pragma multi_compile _ GRID_ON   // ★ 新增：支持开关网格

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

            // --- 新增：Grid 贴图 ---
            TEXTURE2D(_GridTex);
            SAMPLER(sampler_GridTex);

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
                int slice = (int)round(i.terrain[index]);
                float4 tex = SAMPLE_TEXTURE2D_ARRAY(_MainTex, sampler_MainTex, uv, slice);
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

                // --- 计算光照 ---
                Light mainLight = GetMainLight();
                float3 N = normalize(i.normalWS);
                float3 L = normalize(mainLight.direction);
                float NdotL = saturate(dot(N, L));
                float3 diffuse = baseColor * mainLight.color * NdotL;

                // --- 可选：网格贴图 ---
                float3 finalColor = diffuse;

                #if defined(GRID_ON)
                    float2 gridUV = i.positionWS.xz;
                    gridUV.x *= 1.0 / (4.0 * 8.66025404);   // 约等于 1 / (4√3*5)
                    gridUV.y *= 1.0 / (2.0 * 15.0);
                    float4 grid = SAMPLE_TEXTURE2D(_GridTex, sampler_GridTex, gridUV);
                    finalColor *= grid.rgb;  // 将灰色格线“烧”进地形
                #endif

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}

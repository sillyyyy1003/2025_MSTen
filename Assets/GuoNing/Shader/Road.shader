Shader "Custom/URP_RoadLambert"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Texture", 2D) = "white" {}
        _Metallic("Metallic", Range(0,1)) = 0.5
        _Smoothness("Smoothness", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "RenderType"="Opaque" "Queue"="Geometry+1" }
        LOD 200
        Offset -1, -1   // 深度偏移，避免跟地形z冲突

        Pass
        {
            Name "LambertLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 positionWS   : TEXCOORD2;   // worldPos
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Metallic;
                float _Smoothness;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 使用世界坐标XZ采样噪声
                float2 noiseUV = IN.positionWS.xz * 0.025;
                float4 noise = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, noiseUV);

                // 颜色脏化（用 noise.y 通道）
                half4 baseColor = _Color * (noise.y * 0.75 + 0.25);//0.75
            
                // 过渡扰动（用 noise.x 通道）
                float blend = IN.uv.x;
                blend *= (noise.x + 0.5);   // 防止大面积被抹掉
                blend = smoothstep(0.4, 0.7, blend);

                baseColor.a = blend;
                return baseColor;
            }
            ENDHLSL
        }
    }
}

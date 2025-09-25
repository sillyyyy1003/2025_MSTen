Shader "Custom/URP_River_Transparent"
{
    Properties
    {
        _BaseColor ("Color", Color) = (0.2,0.4,1,0.5)   // 蓝色半透明水
        _NoiseTex ("Noise Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha   // 启用透明混合
            ZWrite Off                        // 不写深度（否则被挡）
            Cull Back                         // 可选，正常背面裁剪

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

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
                float3 normalWS    : NORMAL;
                float3 positionWS  : TEXCOORD1;
            };

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            float4 _BaseColor;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS   = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // === 第一层 UV 动画 ===
                float2 uv1 = i.uv;
                uv1.x = uv1.x * 0.0625 + _Time.y * 0.005;
                uv1.y -= _Time.y * 0.25;
                float4 noise1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv1);

                // === 第二层 UV 动画 ===
                float2 uv2 = i.uv;
                uv2.x = uv2.x * 0.0625 - _Time.y * 0.0052;
                uv2.y -= _Time.y * 0.23;
                float4 noise2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv2);

                // === 噪声叠加在颜色上并 clamp ===
                float noiseMix = noise1.r * noise2.a;
                float3 baseColor = saturate(_BaseColor.rgb + noiseMix);
                float alpha = saturate(_BaseColor.a + noiseMix * 0.5); // 透明度随噪声变化

                // === Lambert 漫反射光照 ===
                Light mainLight = GetMainLight();
                float3 N = normalize(i.normalWS);
                float3 L = normalize(mainLight.direction);
                float NdotL = saturate(dot(N, L));

                float3 diffuse = baseColor * mainLight.color * NdotL;

                return half4(diffuse, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}

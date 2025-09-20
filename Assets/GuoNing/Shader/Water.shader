Shader "Custom/URPWaterWaves"
{
    Properties
    {
        _BaseColor ("Color", Color) = (0.1,0.4,0.8,1)
        _MainTex ("Noise Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        Pass
        {
            Name "UnlitWater"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // --- Noise sample 1 (animate V coord) ---
                float2 uv1 = IN.worldPos.xz;
                uv1.y += _Time.y;
                float4 noise1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv1 * 0.025);

                // --- Noise sample 2 (animate U coord) ---
                float2 uv2 = IN.worldPos.xz;
                uv2.x += _Time.y;
                float4 noise2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv2 * 0.025);

                // --- Blend wave: diagonal sine + noise jitter ---
                float blendWave = sin(
                    (IN.worldPos.x + IN.worldPos.z) * 0.1 +
                    (noise1.y + noise2.z) +
                    _Time.y
                );
                blendWave *= blendWave; // remap -1..1 to 0..1

                // --- Interpolate different channels with blendWave ---
                float waves =
                    lerp(noise1.z, noise1.w, blendWave) +
                    lerp(noise2.x, noise2.y, blendWave);

                // Compress to 0–1 range with smoothstep
                waves = smoothstep(0.75, 2.0, waves);

                // Expand waves to float4 so it matches _BaseColor
                float4 c = saturate(_BaseColor + float4(waves, waves, waves, 0));

                return c;
            }
            ENDHLSL
        }
    }
    FallBack Off
}

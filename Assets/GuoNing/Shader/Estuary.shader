Shader "Custom/URP_Estuary"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.0,0.3,0.6,0.7)
        _MainTex ("Noise Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Water.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float2 uv2        : TEXCOORD1;   // manually fetch UV2
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float2 riverUV     : TEXCOORD1;  // renamed, avoids uv2_MainTex bug
                float3 worldPos    : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.riverUV = IN.uv2;   // assign UV2 manually
                OUT.worldPos = TransformObjectToWorld(IN.positionOS).xyz;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float time = _Time.y;

                float shore = IN.uv.y;
                float foam = Foam(shore, IN.worldPos.xz, time, _MainTex, sampler_MainTex);
                float waves = Waves(IN.worldPos.xz, time, _MainTex, sampler_MainTex);
                float shoreWater = max(foam, waves);

                // use manually passed UV2
                float river = River(IN.riverUV, time, _MainTex, sampler_MainTex);

                // blend shore and river by shore mask
            float water = lerp(shoreWater, river, IN.uv.x);

                half4 c = saturate(_BaseColor + water);
                return c;
            }
            ENDHLSL
        }
    }
    FallBack Off
}

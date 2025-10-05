Shader "Custom/URP_WaterShore"
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
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 worldPos    : TEXCOORD1;
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
                OUT.worldPos = TransformObjectToWorld(IN.positionOS).xyz;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
               float time = _Time.y;

                float shore = IN.uv.y;
                float foam = Foam(shore, IN.worldPos.xz, time, _MainTex, sampler_MainTex);
                float waves = Waves(IN.worldPos.xz, time, _MainTex, sampler_MainTex);
                waves *= 1 - shore; // fade waves near shore

                float river = River(IN.uv, time, _MainTex, sampler_MainTex);

                half4 c = saturate(_BaseColor + max(foam, waves) + river);
                return c;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
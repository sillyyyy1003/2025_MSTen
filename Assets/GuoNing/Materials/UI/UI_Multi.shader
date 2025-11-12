Shader "UI/MultiplyMask_URP"
{
    Properties
    {
        _MainTex ("Main Texture (RenderTexture)", 2D) = "white" {}
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _UseMask ("Use Mask", Float) = 0
        _BlurSize("Blur Size", Range(0, 5)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);
      

            float4 _MainTex_TexelSize;   // 自动由 Unity 传入: (1/width, 1/height, width, height)
            float _UseMask;
            float _BlurSize;
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // half4 mainCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                // half4 maskCol = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, IN.uv);
                // half4 result=mainCol;

                // if (_UseMask > 0.5)
                // {
                //     if (maskCol.a > 0)
                //         result = mainCol * maskCol;
                // }

                // return result;
                 // === 高斯权重（3x3核，可扩展） ===
                float kernel[9] = {
                    0.0625, 0.125, 0.0625,
                    0.125,  0.25,  0.125,
                    0.0625, 0.125, 0.0625
                };

                // === 取样偏移 ===
                int index = 0;
                half4 blurColor = 0;
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 offset = float2(x, y) * _MainTex_TexelSize.xy * _BlurSize;
                        blurColor += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + offset) * kernel[index];
                        index++;
                    }
                }

                half4 mainCol = blurColor;
                half4 maskCol = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, IN.uv);
                half4 result = mainCol;

                if (_UseMask > 0.5)
                {
                    if (maskCol.a > 0)
                        result = mainCol * maskCol;
                }

                return result;
         
            }
            ENDHLSL
        }
    }
}

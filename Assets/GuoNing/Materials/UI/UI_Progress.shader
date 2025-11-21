Shader "UI/ColorFlowWithProgress"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)

        _FlowIntensity ("Flow Intensity", Float) = 1.5
        _FlowWidth ("Flow Width", Float) = 0.25
        _FlowSpeed ("Flow Speed", Float) = 1.0

        _FillAmount ("Fill Amount (Radial360)", Range(0,1)) = 1
    }

    SubShader
    {
        Tags{
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ================================
            // Structs
            // ================================
            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            // ================================
            // Properties
            // ================================
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTex_ST;
            float4 _Color;

            float _FillAmount;
            float _FlowIntensity;
            float _FlowWidth;
            float _FlowSpeed;

            // ================================
            // Vertex Shader
            // ================================
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            // ================================
            // Fragment Shader
            // ================================
            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;

                // 读取贴图
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * i.color;

                if (col.a <= 0) return col;

                // ==========================
                // 计算 Radial360 裁剪
                // ==========================
                float2 center = float2(0.5, 0.5);
                float2 d = uv - center;

                float angle = atan2(d.y, d.x); // -PI ~ PI
                angle = (angle + 3.1415926) / (2 * 3.1415926); // 0~1

                // 如果角度超过 fillAmount → 抛弃像素
                if (angle > _FillAmount)
                    col.a = 0;

                if (col.a <= 0) return col;


                // ==========================
                // 流光效果 (沿圆周方向)
                // ==========================
                float flow = sin((angle + _Time.y * _FlowSpeed) * 6.28318);
                flow = (flow * 0.5 + 0.5);
                flow = smoothstep(1 - _FlowWidth, 1, flow);

                col.rgb += flow * _FlowIntensity;

                return col;
            }

            ENDHLSL
        }
    }
}

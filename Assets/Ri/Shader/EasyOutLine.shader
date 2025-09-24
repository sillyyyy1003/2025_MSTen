Shader "Custom/EasyOutLine" {
     Properties {
        _OutlineColor ("Outline Color", Color) = (1,0.5,0,1)
        _OutlineWidth ("Outline Width", Range(0.01, 0.1)) = 0.03
    }
    
    SubShader {
        Tags { 
            "RenderType"="Opaque" 
            "Queue"="Geometry+10"  // ȷ��������֮����Ⱦ
            "IgnoreProjector"="True"
        }
        LOD 100
        
        Pass {
            Name "OUTLINE"
            Cull Front  // ֻ��Ⱦ���棨��ߣ�
            ZWrite On
            ZTest LEqual
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            
            struct v2f {
                float4 vertex : SV_POSITION;
            };
            
            float _OutlineWidth;
            fixed4 _OutlineColor;
            
            v2f vert (appdata v) {
                v2f o;
                
                // ����1��������ռ���չ
                float4 pos = v.vertex;
                pos.xyz += normalize(v.normal) * _OutlineWidth;
                o.vertex = UnityObjectToClipPos(pos);
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                return _OutlineColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
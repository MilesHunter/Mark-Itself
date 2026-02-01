// 简单的Shader示例（或者使用Shader Graph）
Shader "Custom/WaveEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WaveFrequency ("Wave Frequency", Float) = 10.0
        _Distortion ("Distortion", Float) = 0.1
        _FadeDistance ("Fade Distance", Float) = 0.5
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            float4 _Color;
            float _WaveSpeed;
            float _WaveFrequency;
            float _Distortion;
            float _FadeDistance;
            
            v2f vert (appdata v)
            {
                v2f o;
                
                // 添加波纹扭曲
                float wave = sin(_Time.y * _WaveSpeed + v.vertex.x * _WaveFrequency) * _Distortion;
                v.vertex.y += wave;
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // 计算径向渐变
                float2 center = float2(0.5, 0.5);
                float dist = distance(i.uv, center);
                
                // 边缘淡出
                float fade = 1 - smoothstep(0.5 - _FadeDistance, 0.5, dist);
                
                // 添加同心波纹
                float rings = sin(dist * 20 - _Time.y * 3) * 0.5 + 0.5;
                
                fixed4 col = _Color;
                col.a *= fade * rings;
                
                return col;
            }
            ENDCG
        }
    }
}
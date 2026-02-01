Shader "UniversalRenderPipeline/Custom/CRTPostProcess"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }

    SubShader
    {
        // 关键：必须声明此 Tag 才能在 URP 下工作
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType"="Opaque" }
        
        Pass
        {
            Name "CRT_Pass"
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // 引用 URP 核心库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // 在 URP 中，纹理需要这样声明以保证兼容性
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // CBUFFER 包含所有可调节参数，这是 URP 合批（SRP Batcher）的要求
            CBUFFER_START(UnityPerMaterial)
                float _Distortion;
                float _ScanlineCount;
                float _ScanlineSpeed;
                float _ScanlineIntensity;
                float _FlashIntensity; // 闪烁强度
                float _Jitter;         // 抽搐偏移量
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float2 BarrelDistortion(float2 uv)
            {
                float2 centered = uv - 0.5;
                float dist = dot(centered, centered);
                return uv + centered * dist * _Distortion;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // 增加随机抽搐逻辑
                float2 uv = input.uv;
                uv.x += (sin(_Time.y * 100) * _Jitter); // 极小范围的水平快速抖动
    
                uv = BarrelDistortion(uv); // 应用原有的桶形畸变

                if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
                    return half4(0, 0, 0, 1);

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // 动态扫描线逻辑保持不变...
                float scanline = sin(uv.y * _ScanlineCount + _Time.y * _ScanlineSpeed);
                scanline = lerp(1.0, (scanline + 1.0) * 0.5, _ScanlineIntensity);
                col.rgb *= scanline;

                // 增加闪烁逻辑：直接叠加白色增益
                col.rgb += _FlashIntensity; 

                return col;
            }
            ENDHLSL
        }
    }
}
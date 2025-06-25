Shader "Universal Render Pipeline/Custom/SpriteDotsInstancingAlpha"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        _BaseColor ("Base Colour", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "PreviewType" = "Plane" }

        Pass
        {
            Name "Forward"
            Tags
            {
                "LightMode"="UniversalForward"
            }

            Fog { Mode Off }
            ZWrite Off
            Cull Off
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            CBUFFER_END

            #ifndef UNITY_DOTS_INSTANCING_ENABLED
                #define _BaseColor _BaseColor
            #endif

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
                #define _BaseColor UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor)
            #endif

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings UnlitPassVertex(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;

                output.uv = input.uv;

                output.color = input.color;
                return output;
            }

            half4 UnlitPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 baseMap = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                baseMap.rgb *= baseMap.a;

                return baseMap * _BaseColor * input.color;
            }
            ENDHLSL
        }
    }
}

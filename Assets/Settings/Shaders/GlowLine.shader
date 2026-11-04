// 라인 렌더러용 발광 셰이더. 조명을 받지 않고 가산 블렌딩으로 더해지며, _Intensity로 밝기를 1 넘게 올려 Bloom에 걸리게 한다.
// 선 두께 방향 가운데는 흰 심지(_CoreIntensity)를 더해 빛줄기처럼 보이게 하고, 무늬를 흘리고(_ScrollSpeed) 깜빡인다(_FlickerAmount).
Shader "Avaritia/GlowLine"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HDR] _Color ("Glow Color", Color) = (0.4, 0.9, 1, 1)
        _Intensity ("Intensity", Float) = 3
        _CoreIntensity ("Core Intensity", Float) = 1.5
        _CoreWidth ("Core Width (0 ~ 1)", Range(0.01, 1)) = 0.3
        _ScrollSpeed ("Scroll Speed", Float) = 2
        _FlickerSpeed ("Flicker Speed", Float) = 25
        _FlickerAmount ("Flicker Amount", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            half4 _Color;
            float _Intensity;
            float _CoreIntensity;
            float _CoreWidth;
            float _ScrollSpeed;
            float _FlickerSpeed;
            float _FlickerAmount;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = TRANSFORM_TEX(input.uv, _MainTex);
                // 텍스처가 Clamp여도 흐르게 frac으로 감싼다. 이음매에서 밉이 튀지 않게 0레벨로 샘플링한다.
                uv.x = frac(uv.x - _Time.y * _ScrollSpeed);
                half4 tex = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0);

                // 두께 방향 가운데(uv.y = 0.5)일수록 1. 심지는 그 안쪽 _CoreWidth만큼만 밝게 더한다.
                float center = 1.0 - abs(input.uv.y - 0.5) * 2.0;
                float core = smoothstep(1.0 - _CoreWidth, 1.0, center);

                // 여러 주기를 섞어 규칙적이지 않게 깜빡인다.
                float t = _Time.y * _FlickerSpeed;
                float flicker = 1.0 - _FlickerAmount * (0.5 + 0.5 * sin(t) * sin(t * 1.7 + 1.3));

                half3 glow = tex.rgb * _Color.rgb * input.color.rgb * _Intensity;
                glow += core * _CoreIntensity * tex.a;
                half alpha = saturate(tex.a * _Color.a * input.color.a);
                return half4(glow * flicker, alpha);
            }
            ENDHLSL
        }
    }
}

// 고리 모양 스프라이트를 원호(_CenterAngle ± _ArcAngle/2)만 남기고 _Segments칸으로 나눈다.
// 칸 사이(_GapDegrees)는 투명하게 뚫고, 앞에서부터 _FilledCount칸만 보인다. _SpecialSegments 칸은 그 칸의 색·무늬로 칠한다. 탄약 게이지용 UI 셰이더.
Shader "Avaritia/SegmentedArcGauge"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Segments ("Segments", Float) = 10
        _FilledCount ("Filled Count", Float) = 10
        _GapDegrees ("Gap (degrees)", Float) = 1
        _CenterAngle ("Center Angle", Float) = 180
        _ArcAngle ("Arc Angle", Float) = 120
        [Toggle] _Clockwise ("Clockwise", Float) = 1
        // 고리 안쪽·바깥쪽 반지름(UV 기준, 중심에서 0 ~ 0.5). 특별 칸 무늬의 세로 방향에 쓴다.
        _RingInner ("Ring Inner", Float) = 0.34
        _RingOuter ("Ring Outer", Float) = 0.47
        // 다르게 칠할 칸 인덱스(최대 4개, -1이면 없음)와 각 칸의 색·무늬.
        _SpecialSegments ("Special Segments", Vector) = (-1, -1, -1, -1)
        _SpecialColor0 ("Special Color 0", Color) = (1, 1, 1, 1)
        _SpecialColor1 ("Special Color 1", Color) = (1, 1, 1, 1)
        _SpecialColor2 ("Special Color 2", Color) = (1, 1, 1, 1)
        _SpecialColor3 ("Special Color 3", Color) = (1, 1, 1, 1)
        _SpecialTex0 ("Special Pattern 0", 2D) = "white" {}
        _SpecialTex1 ("Special Pattern 1", 2D) = "white" {}
        _SpecialTex2 ("Special Pattern 2", 2D) = "white" {}
        _SpecialTex3 ("Special Pattern 3", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

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
            half4 _Color;
            float _Segments;
            float _FilledCount;
            float _GapDegrees;
            float _CenterAngle;
            float _ArcAngle;
            float _Clockwise;
            float _RingInner;
            float _RingOuter;
            float4 _SpecialSegments;
            half4 _SpecialColor0, _SpecialColor1, _SpecialColor2, _SpecialColor3;
            TEXTURE2D(_SpecialTex0); SAMPLER(sampler_SpecialTex0);
            TEXTURE2D(_SpecialTex1); SAMPLER(sampler_SpecialTex1);
            TEXTURE2D(_SpecialTex2); SAMPLER(sampler_SpecialTex2);
            TEXTURE2D(_SpecialTex3); SAMPLER(sampler_SpecialTex3);

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color * _Color;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 col = tex * input.color;

                float2 p = input.uv - 0.5;
                float rel = degrees(atan2(p.y, p.x)) - _CenterAngle;
                rel -= 360.0 * round(rel / 360.0);

                // 원호 시작(0) ~ 끝(1). 시계 방향이면 큰 각도 쪽에서 시작한다.
                float t = (rel + _ArcAngle * 0.5) / _ArcAngle;
                if (_Clockwise > 0.5)
                    t = 1.0 - t;

                float segments = max(_Segments, 1.0);
                float seg = t * segments;
                float index = floor(seg);
                float frac = seg - index;
                float halfGap = min(_GapDegrees / (_ArcAngle / segments), 0.9) * 0.5;
                float aa = fwidth(seg);

                // 틈이 0이면 칸 경계에 이음매가 생기지 않게 칸 구분을 아예 건너뛴다.
                float mask = _GapDegrees > 0.0
                    ? smoothstep(halfGap - aa, halfGap + aa, frac) * smoothstep(1.0 - halfGap + aa, 1.0 - halfGap - aa, frac)
                    : 1.0;
                mask *= step(0.0, t) * step(t, 1.0);
                mask *= step(index + 0.5, _FilledCount);

                // 특별 칸: 칸 안 좌표(가로 = 원호 방향, 세로 = 고리 두께 방향)로 무늬를 입히고 그 칸의 색을 쓴다.
                float2 slotUV = float2(
                    saturate((frac - halfGap) / max(1.0 - 2.0 * halfGap, 1e-4)),
                    saturate((length(p) - _RingInner) / max(_RingOuter - _RingInner, 1e-4)));
                if (abs(_SpecialSegments.x - index) < 0.5)
                    col = tex * _SpecialColor0 * SAMPLE_TEXTURE2D(_SpecialTex0, sampler_SpecialTex0, slotUV);
                else if (abs(_SpecialSegments.y - index) < 0.5)
                    col = tex * _SpecialColor1 * SAMPLE_TEXTURE2D(_SpecialTex1, sampler_SpecialTex1, slotUV);
                else if (abs(_SpecialSegments.z - index) < 0.5)
                    col = tex * _SpecialColor2 * SAMPLE_TEXTURE2D(_SpecialTex2, sampler_SpecialTex2, slotUV);
                else if (abs(_SpecialSegments.w - index) < 0.5)
                    col = tex * _SpecialColor3 * SAMPLE_TEXTURE2D(_SpecialTex3, sampler_SpecialTex3, slotUV);

                col.a *= mask;
                return col;
            }
            ENDHLSL
        }
    }
}

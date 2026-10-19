Shader "Avaritia/SteampunkAtmosphere"
{
    Properties
    {
        [Header(Light Shafts)]
        _LightColor         ("Light Color", Color)                          = (1.0, 0.78, 0.45, 1)
        _LightIntensity     ("Light Intensity", Range(0, 3))                = 0.9
        _LightPos           ("Light Origin (Viewport UV)", Vector)          = (0.25, 1.3, 0, 0)
        _RayDirection       ("Ray Direction (deg, 0 = Down)", Range(-90, 90)) = 20
        _RaySpread          ("Ray Spread (deg)", Range(1, 120))             = 45
        _RayDensity         ("Ray Density", Range(1, 80))                   = 24
        _RaySharpness       ("Ray Sharpness", Range(0.5, 8))                = 2.5
        _RaySpeed           ("Ray Sway Speed", Range(0, 2))                 = 0.15
        _RayFalloff         ("Distance Falloff", Range(0, 4))               = 0.8
        _Flicker            ("Flicker", Range(0, 1))                        = 0.08

        [Header(Occlusion)]
        _OcclusionStrength  ("Occlusion Strength", Range(0, 1))             = 0.85
        _ShadowBrightness   ("Shadow Scatter Brightness", Range(0, 1))      = 0
        _OccluderSurfaceLight ("Occluder Surface Light", Range(0, 4))       = 2

        [Header(Dust)]
        _DustDensity        ("Dust Density", Range(0, 1))                   = 0.25
        _DustCellSize       ("Dust Cell Size (px)", Range(4, 64))           = 18
        _DustSize           ("Dust Size", Range(0.5, 4))                    = 1
        _DustSpeed          ("Dust Speed", Range(0, 60))                    = 8
        _DustBrightness     ("Dust Brightness", Range(0, 3))                = 1.2

        [Header(Atmosphere)]
        _HazeColor          ("Haze Color", Color)                           = (0.42, 0.30, 0.18, 1)
        _HazeTop            ("Haze Top", Range(0, 1))                       = 0.25
        _HazeBottom         ("Haze Bottom", Range(0, 1))                    = 0.08

        [Header(Color Grade)]
        _ShadowTint         ("Shadow Tint", Color)                          = (0.10, 0.17, 0.16, 1)
        _HighlightTint      ("Highlight Tint", Color)                       = (1.0, 0.85, 0.60, 1)
        _SplitToneAmount    ("Split Tone Amount", Range(0, 1))              = 0.5
        _Sepia              ("Sepia", Range(0, 1))                          = 0.25
        _Contrast           ("Contrast", Range(0.5, 1.5))                   = 1.08
        _Saturation         ("Saturation", Range(0, 2))                     = 0.9

        [Header(Finish)]
        _VignetteColor      ("Vignette Color", Color)                       = (0.08, 0.05, 0.03, 1)
        _VignetteIntensity  ("Vignette Intensity", Range(0, 1))             = 0.45
        _VignetteSmoothness ("Vignette Smoothness", Range(0.01, 1))         = 0.55
        _Grain              ("Film Grain", Range(0, 0.2))                   = 0.04

        [Header(Pixel Art)]
        _PixelSize          ("Light Pixel Size (0 = Smooth)", Range(0, 8))  = 2
        _LightSteps         ("Light Posterize Steps (0 = Off)", Range(0, 16)) = 6
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "SteampunkAtmosphere"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #define OCCLUSION_SAMPLES 64
            static const half3 LUMA = half3(0.2126, 0.7152, 0.0722);

            float4 _LightColor;
            float  _LightIntensity;
            float4 _LightPos;
            float  _RayDirection, _RaySpread, _RayDensity, _RaySharpness, _RaySpeed, _RayFalloff, _Flicker;
            float  _OcclusionStrength, _ShadowBrightness, _OccluderSurfaceLight;
            float  _DustDensity, _DustCellSize, _DustSize, _DustSpeed, _DustBrightness;
            float4 _HazeColor;
            float  _HazeTop, _HazeBottom;
            float4 _ShadowTint, _HighlightTint;
            float  _SplitToneAmount, _Sepia, _Contrast, _Saturation;
            float4 _VignetteColor;
            float  _VignetteIntensity, _VignetteSmoothness, _Grain;
            float  _PixelSize, _LightSteps;

            // LightShaftOccluderFeature가 그린 가림막 마스크 (order in layer가 기준 이하인 스프라이트는 빠져 있음)
            TEXTURE2D(_LightShaftOccluderMask);

            // ---------- Noise ----------
            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float2 Hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            float ValueNoise1D(float x)
            {
                float i = floor(x);
                float f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(Hash11(i), Hash11(i + 1.0), f);
            }

            // 밝기는 유지하고 색만 입히도록 틴트를 휘도로 정규화
            half3 NormalizeTint(half3 tint)
            {
                return tint / max(dot(tint, LUMA), 1e-3);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv     = input.texcoord;
                float  t      = _Time.y;
                float  aspect = _ScreenParams.x / _ScreenParams.y;
                float  pixel  = max(_PixelSize, 1.0);

                half3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;

                // 빛 계산용 UV는 픽셀 그리드에 스냅 (도트 느낌 유지)
                float2 uvL = uv;
                if (_PixelSize > 0.0)
                {
                    float2 blocks = _ScreenParams.xy / _PixelSize;
                    uvL = (floor(uvL * blocks) + 0.5) / blocks;
                }

                // ===== 1. 컬러 그레이딩: 청록 그림자 / 황동 하이라이트 =====
                half luma = dot(col, LUMA);
                half3 split = lerp(NormalizeTint(_ShadowTint.rgb),
                                   NormalizeTint(_HighlightTint.rgb),
                                   smoothstep(0.05, 0.6, luma));
                col = lerp(col, col * split, _SplitToneAmount);

                half3 sepia = luma * half3(1.07, 0.74, 0.43);
                col = lerp(col, sepia, _Sepia);

                half luma2 = dot(col, LUMA);
                col = lerp(luma2.xxx, col, _Saturation);
                col = max((col - 0.5) * _Contrast + 0.5, 0.0);

                // ===== 2. 증기/매연 안개 (높이 그라데이션) =====
                col = lerp(col, _HazeColor.rgb, lerp(_HazeBottom, _HazeTop, uv.y));

                // ===== 3. 빛줄기 =====
                float2 d     = (uvL - _LightPos.xy) * float2(aspect, 1.0);
                float  dist  = length(d);
                float  angle = degrees(atan2(d.x, -d.y)); // 0 = 바로 아래, + = 오른쪽으로 기울어짐

                float halfSpread = _RaySpread * 0.5;
                float cone = 1.0 - smoothstep(halfSpread * 0.6, halfSpread, abs(angle - _RayDirection));

                float a  = radians(angle) * _RayDensity;
                float n1 = ValueNoise1D(a + t * _RaySpeed);
                float n2 = ValueNoise1D(a * 2.37 - t * _RaySpeed * 0.63 + 17.0);
                float rays = pow(saturate(n1 * 0.65 + n2 * 0.35), _RaySharpness);

                float envelope = cone * exp(-dist * _RayFalloff) * (1.0 - _Flicker * ValueNoise1D(t * 7.0));
                rays *= envelope;

                // 마스크에 그려진 렌더러(order in layer가 기준보다 큼)는 빛줄기보다 앞에 있음
                half selfMask = SAMPLE_TEXTURE2D_LOD(_LightShaftOccluderMask, sampler_PointClamp, uv, 0).a;

                // 빛 근원 쪽으로 레이를 쏴서 빛보다 앞에 있는 렌더러(마스크)가 있으면 가림
                if (_OcclusionStrength > 0.0)
                {
                    // 픽셀마다 샘플 시작점을 흔들어 실루엣이 계단식으로 복제되는 밴딩 제거
                    float2 jp     = floor(uvL * _ScreenParams.xy);
                    float  jitter = frac(52.9829189 * frac(dot(jp, float2(0.06711056, 0.00583715))));

                    // 광원까지 가는 길에 가림막이 하나라도 있으면 빛이 막힘 (평균을 내면 얇은 물체 뒤로 빛이 새어 나옴)
                    // 자기 실루엣을 벗어나기 전까지의 샘플은 무시해서 가림막 자신은 빛을 받게 함
                    float occ    = 0.0;
                    float inside = selfMask;
                    [loop]
                    for (int i = 0; i < OCCLUSION_SAMPLES; i++)
                    {
                        float  s  = (i + jitter) / OCCLUSION_SAMPLES;
                        float2 sp = lerp(uvL, _LightPos.xy, s);
                        if (all(sp >= 0.0) && all(sp <= 1.0))
                        {
                            half m = SAMPLE_TEXTURE2D_LOD(_LightShaftOccluderMask, sampler_PointClamp, sp, 0).a;
                            inside *= m;
                            occ = max(occ, m * (1.0 - inside));
                        }
                    }
                    // 막힌 곳은 줄무늬 없이 안개에 퍼진 빛만 Shadow Scatter Brightness만큼 남김
                    rays = lerp(rays, envelope * _ShadowBrightness, _OcclusionStrength * occ);
                }
                // 빛 세기를 단계로 끊어서 도트 아트 톤에 맞춤
                if (_LightSteps >= 1.0)
                    rays = floor(rays * _LightSteps + 0.5) / _LightSteps;

                // 공기 중 산란은 빛줄기 앞에 있는 가림막 위엔 그리지 않고, 표면이 빛 받는 효과는 모두에게 적용
                half3 light = _LightColor.rgb * rays * _LightIntensity;
                col += light * 0.55 * (1.0 - selfMask) + col * light * lerp(0.9, _OccluderSurfaceLight, selfMask);

                // ===== 4. 빛줄기 안에서만 보이는 먼지 =====
                float2 p = uv * _ScreenParams.xy;
                p += float2(t * _DustSpeed * 0.35 + sin(t * 0.5) * 4.0, -t * _DustSpeed * 0.5);
                if (_PixelSize > 0.0)
                    p = floor(p / _PixelSize) * _PixelSize;

                float2 cell   = floor(p / _DustCellSize);
                float2 local  = frac(p / _DustCellSize) * _DustCellSize;
                float2 rnd    = Hash22(cell);
                float  spawn  = step(1.0 - _DustDensity, Hash11(cell.x * 57.0 + cell.y * 113.0));
                float2 center = (0.2 + rnd * 0.6) * _DustCellSize;
                float2 off    = abs(local - center);
                float  mote   = step(max(off.x, off.y), _DustSize * pixel); // 사각 도트
                float  twinkle = 0.5 + 0.5 * sin(t * (1.5 + rnd.y * 2.0) + rnd.x * 6.2831);

                col += _LightColor.rgb * mote * spawn * twinkle * saturate(rays * 3.0) * _DustBrightness * (1.0 - selfMask);

                // ===== 5. 비네트 (그을음 색) =====
                float2 vd   = (uv - 0.5) * float2(aspect, 1.0);
                float  vdist = length(vd) / length(float2(aspect, 1.0) * 0.5);
                float  vig  = smoothstep(1.0 - _VignetteSmoothness, 1.0, vdist) * _VignetteIntensity;
                col = lerp(col, _VignetteColor.rgb, vig);

                // ===== 6. 필름 그레인 =====
                float g = Hash11(dot(floor(uv * _ScreenParams.xy / pixel), float2(12.9898, 78.233)) + frac(t) * 91.7) - 0.5;
                col += g * _Grain;

                return half4(max(col, 0.0), 1.0);
            }
            ENDHLSL
        }
    }
}

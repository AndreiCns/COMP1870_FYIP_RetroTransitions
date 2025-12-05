Shader "Hidden/NTSCPass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TextureSize("TextureSize", Vector) = (853,480,0,0)
        _InputSize("InputSize", Vector) = (853,480,0,0)
        _OutputSize("OutputSize", Vector) = (1136,640,0,0)
        _CrossTalkStrength("CrossTalkStrength", Float) = 2
        _Brightness("Brightness", Float) = 0.95
        _BlackLevel("BlackLevel", Float) = 1.0526
        _ArtifactStrength("ArtifactStrength", Float) = 1
        _FringeStrength("FringeStrength", Float) = 0.75
        _ChromaModFrequencyScale("ChromaModulateFrequencyScale", Float) = 1
        _ChromaPhaseShiftScale("ChromaPhaseShiftScale", Float) = 1
        _ScanlineStrength("ScanlineStrength", Float) = 0.5
        _BeamSpread("BeamSpread", Float) = 0.5
        _BeamStrength("BeamStrength", Float) = 1.15
        _OverscanScale("OverscanScale", Float) = 0.985
        _MaskRadius("MaskRadius", Float) = 16
        [KeywordEnum(NONE,VERTICAL,SLANT,SLANT_NOIZE)] CrossTalk("Cross-Talk Mode", Float) = 1
        [KeywordEnum(TAP4,TAP8,TAP24)] TapSize("Composite Blur Tap Size", Float) = 0
        [Toggle(USE_CURVATURE)] _UseCurvature("Use Curvature", Float) = 0
        [Toggle(USE_CORNER_MASK)] _UseCornerMask("Use Corner Mask", Float) = 1
        [KeywordEnum(NONE,CW,CCW)] Turn("Turn Mode", Float) = 0
    }
    
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass //#0: RGB to YIQ pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile CROSSTALK_NONE CROSSTALK_VERTICAL CROSSTALK_SLANT CROSSTALK_SLANT_NOISE
            #pragma multi_compile _ USE_VERTICAL_CROSSTALK
            #pragma multi_compile _ _UNITY_RENDER_PIPELINE_HDRP

            #include "MultiPipeline.hlsl"

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 tex_coord : TEXCOORD0;
                float2 pixel : TEXCOORD1;
            };

            DEF_SAMPLER2D(_MainTex);
            float2 _TextureSize;
            float2 _InputSize;
            float2 _OutputSize;
            float _CrossTalkStrength;
            float _Brightness;
            float _ArtifactStrength;
            float _FringeStrength;
            float _ChromaModFrequencyScale;
            float _ChromaPhaseShiftScale;
            float _FrameCountNum;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = DEF_VERTEX_POS(v);
                o.tex_coord = DEF_UV(v);
                o.pixel = o.tex_coord * _TextureSize * (_OutputSize / _InputSize);
                return o;
            }

            #define PI (3.14159265)

            #if CROSSTALK_VERTICAL
            #define HSCAN_RATE  (1.0)
            #define HSCAN_SHIFT (2.0)
            #elif CROSSTALK_SLANT || CROSSTALK_SLANT_NOISE
            #define HSCAN_RATE  (0.75)
            #define HSCAN_SHIFT (86.0)
            #else
            #define HSCAN_RATE (0)
            #define HSCAN_SHIFT (0)
            #endif

            static const float ChromaModFreq = (HSCAN_RATE * PI / 3.0) * _ChromaModFrequencyScale;
            static const float ChromaPhaseShift = (HSCAN_SHIFT * PI) * _ChromaPhaseShiftScale;

            static const float crossTalkStrength = max(0.1, _CrossTalkStrength);
            static const float3x3 mix_mat = float3x3(
                _Brightness, _ArtifactStrength, _ArtifactStrength,
                _FringeStrength, crossTalkStrength, 0.0,
                _FringeStrength, 0.0, crossTalkStrength 
            );

            static const float3x3 yiq_mat = float3x3(
                0.299, 0.587, 0.114,
                0.596, -0.274, -0.322,
                0.211, -0.523, 0.312
            );

            inline float3 RGBToYIQ(float3 col)
            {
                return mul(yiq_mat, col);
            }

            DEF_OUT_FRAGMENT frag (v2f i) : SV_Target
            {
                const float3 col = DEF_SAMPLE_TEXTURE2D(_MainTex, i.tex_coord).rgb;
                float3 yiq = RGBToYIQ(col);

                #if CROSSTALK_VERTICAL
                const float chroma_phase = ChromaPhaseShift;
                #elif CROSSTALK_SLANT
                const float chroma_phase = ChromaPhaseShift * (fmod(i.pixel.y, 3.0) + 1) / 3.0;
                #elif CROSSTALK_SLANT_NOISE
                const uint fc = (_FrameCountNum / 2);
                const float chroma_phase = ChromaPhaseShift * (fmod(i.pixel.y, 3.0) + 1 + (fc % 2)) / 3.0;
                #endif
                #if CROSSTALK_VERTICAL || CROSSTALK_SLANT || CROSSTALK_SLANT_NOISE
                const float mod_phase = chroma_phase + i.pixel.x * ChromaModFreq;

                float i_mod, q_mod;
                sincos(mod_phase, i_mod, q_mod);

                // Cross-Talk
                yiq.yz *= float2(i_mod, q_mod) / crossTalkStrength;
                yiq = mul(mix_mat, yiq); // Cross-Talk
                yiq.yz *= float2(i_mod, q_mod) * crossTalkStrength;
                #endif
                
                return float4(yiq * 0.5 + 0.5, 1.0);
            }
            ENDHLSL
        }

        Pass //#1: YIQ to RGB pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile TAPSIZE_TAP4 TAPSIZE_TAP8 TAPSIZE_TAP24
            #pragma multi_compile _ _UNITY_RENDER_PIPELINE_HDRP

            #include "MultiPipeline.hlsl"

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 tex_coord : TEXCOORD0;
            };

            DEF_SAMPLER2D(_MainTex);
            float2 _TextureSize;
            float2 _InputSize;
            float2 _OutputSize;
            float _BlackLevel;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = DEF_VERTEX_POS(v);
                o.tex_coord = DEF_UV(v) - float2(0.5 / _TextureSize.x, 0.0);
                return o;
            }

            #if TAPSIZE_TAP24
                #define TAPS 24
                static const float luma_filter[TAPS + 1] = {
                -0.00001202,
                -0.00002215,
                -0.00001316,
                -0.00001202,
                -0.00004998,
                -0.00011394,
                -0.00012215,
                -0.00000561,
                0.00017052,
                0.00023720,
                0.00016964,
                0.00028569,
                0.00098457,
                0.00201868,
                0.00200228,
                -0.00090988,
                -0.00704908,
                -0.01322286,
                -0.01260693,
                0.00246086,
                0.03586823,
                0.08401645,
                0.13556350,
                0.17526127,
                0.19017655};

                static float chroma_filter[TAPS + 1] = {
                -0.00011885,
                -0.00027131,
                -0.00050264,
                -0.00093083,
                -0.00145101,
                -0.00206474,
                -0.00270043,
                -0.00324128,
                -0.00352495,
                -0.00335028,
                -0.00249173,
                -0.00072115,
                0.00216466,
                0.00631364,
                0.01178910,
                0.01854566,
                0.02641440,
                0.03510071,
                0.04419657,
                0.05320720,
                0.06159028,
                0.06880360,
                0.07435619,
                0.07785656,
                0.07905240};
            #elif TAPSIZE_TAP8
                #define TAPS 8
                static const float luma_filter[9] = {
                    0.0019, 0.0052, 0.0035, -0.0163, -0.0407,
                    -0.0118, 0.1111, 0.2729, 0.3489
                };

                static const float chroma_filter[9] = {
                    0.0025, 0.0057, 0.0147, 0.0315, 0.0555,
                    0.0834, 0.1099, 0.1289, 0.1358
                };
            #else //TAPSIZE_TAP4
                #define TAPS 4
                static const float luma_filter[5] = {
                    0.0071, -0.0128, -0.0525,
                    0.384, 0.3489
                };

                static const float chroma_filter[5] = {
                    0.0082, 0.0462, 0.1389,
                    0.2388, 0.1358
                };
            #endif

            static const float3x3 YIQToRGB_mat = float3x3(
                1.0, 0.956, 0.621,
                1.0, -0.272, -0.647,
                1.0, -1.106, 1.703);

            inline float3 YIQToRGB(float3 yiq)
            {
                return mul(YIQToRGB_mat, yiq);
            }

            inline float3 fetch_offset(float2 uv, float offset, float x)
            {
                return DEF_SAMPLE_TEXTURE2D(_MainTex, uv + float2(offset * x, 0.0)).xyz;
            }

            DEF_OUT_FRAGMENT frag (v2f i) : SV_Target
            {
                const float one_div_x = 1.0 / _TextureSize.x;
                float3 signal = float3(0.0, 0.0, 0.0);
                for (int j = 0; j < TAPS; j++)
                {
                    const float3 sums = fetch_offset(i.tex_coord, float(j) - float(TAPS), one_div_x) +
                        fetch_offset(i.tex_coord, float(TAPS) - float(j), one_div_x);
                    signal += sums * float3(luma_filter[j]*0.95*_BlackLevel, chroma_filter[j], chroma_filter[j]);
                }
                signal += DEF_SAMPLE_TEXTURE2D(_MainTex, i.tex_coord).xyz *
                    float3(luma_filter[TAPS], chroma_filter[TAPS], chroma_filter[TAPS]);
                return float4(YIQToRGB(signal * 2.0 - 1.0), 1.0);
            }
            ENDHLSL
        }

        Pass //#2: Gauss pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ USE_CURVATURE
            #pragma multi_compile _ USE_CORNER_MASK
            #pragma multi_compile TURN_NONE TURN_CW TURN_CCW
            #pragma multi_compile _ _UNITY_RENDER_PIPELINE_HDRP

            #include "MultiPipeline.hlsl"
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 tex_coord : TEXCOORD0;
                float2 pixel : TEXCOORD1;
            };

            DEF_SAMPLER2D(_MainTex);
            float2 _MainTexScale;
            float2 _TextureSize;
            float2 _InputSize;
            float2 _OutputSize;
            float _ScanlineStrength;
            float _OverscanScale;
            float _MaskRadius;
            float _BeamStrength;
            float _BeamSpread;

            #if USE_CURVATURE
                #define CRT_warpX 0.031 
                #define CRT_warpY 0.041 
                #define CRT_cornersize 0.02 
                #define CRT_cornersmooth 1000.0
            
                float Corner(float2 coord)
                {
                    const float2 corner_aspect = float2(1.0,  0.75);
                    coord = (coord - 0.5) / _OverscanScale + 0.5;
                    coord = min(coord, 1.0 - coord) * corner_aspect;
                    const float2 cdist = float2(CRT_cornersize, CRT_cornersize);
                    coord = (cdist - min(coord, cdist));
                    float dist = sqrt(dot(coord, coord));
                    return clamp((cdist.x - dist) * CRT_cornersmooth, 0.0, 1.0);
                }

                float2 Warp(float2 texCoord){
                    const float2 CRT_Distortion = float2(CRT_warpX, CRT_warpY) * 15.0;
                    float2 curvedCoords = texCoord * 2.0 - 1.0;
                    const float curvedCoordsDistance = sqrt(curvedCoords.x * curvedCoords.x + curvedCoords.y * curvedCoords.y);
                    const float dist = 1.0 / (1.0 + CRT_Distortion * 0.2);
                    curvedCoords = curvedCoords / curvedCoordsDistance;
                    curvedCoords = curvedCoords * (1.0-pow((1.0 - (curvedCoordsDistance / 1.414214)), dist));
                    curvedCoords = curvedCoords / (1.0-pow(0.295, dist));
                    curvedCoords = curvedCoords * 0.5 + 0.5;
                    return curvedCoords;
                }
            #endif

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = DEF_VERTEX_POS(v);
                
                float2 uv = DEF_UV_ANTI_SCALED(v);
                #if TURN_CW
                    uv = float2(1 - uv.y, uv.x);
                #elif TURN_CCW
                    uv = float2(uv.y, 1 - uv.x);
                #endif
                
                o.tex_coord = (uv - 0.5) * _OverscanScale + 0.5;
                o.pixel = uv * _TextureSize;
                return o;
            }

            #define NTSC_CRT_GAMMA (2.5)
            #define NTSC_DISPLAY_GAMMA (2.1)
            #define PI (3.14159265)
            const static float HeightScale = (_OutputSize.y / _TextureSize.y) * PI;

            inline float4 NTSCGauss(float2 uv, float2 pixel)
            {
                const float3 frame = pow(DEF_SAMPLE_TEXTURE2D(_MainTex, uv).rgb, float3(NTSC_CRT_GAMMA, NTSC_CRT_GAMMA, NTSC_CRT_GAMMA));
                const float lum = 1 - saturate(dot(frame, float3(0.299f, 0.587f, 0.114f)));
                const float scanlineLum = sin(pixel.y * HeightScale) + 1;
                const float scanlineStr = _ScanlineStrength * lerp(1-_BeamSpread, 1, lum * lum * scanlineLum);
                const float3 scanline = frame * lerp(1, scanlineLum, scanlineStr);

                const float3 gamma_mod = float3(1.0 / NTSC_DISPLAY_GAMMA, 1.0 / NTSC_DISPLAY_GAMMA, 1.0 / NTSC_DISPLAY_GAMMA);

                const float p = _BeamStrength;
                return float4(pow(float3(p, p, p) * scanline, gamma_mod), 1.0);
            }

            #if USE_CORNER_MASK
                inline float rectangle(float2 samplePosition, float2 halfSize){
                    const float2 componentWiseEdgeDistance = abs(samplePosition) - halfSize;
                    const float outsideDistance = length(max(componentWiseEdgeDistance, 0));
                    const float insideDistance = min(max(componentWiseEdgeDistance.x, componentWiseEdgeDistance.y), 0);
                    return outsideDistance + insideDistance;
                }
            #endif

            DEF_OUT_FRAGMENT frag (v2f i) : SV_Target
            {
                #if USE_CURVATURE
                    const float2 uv = Warp(i.tex_coord.xy);
                #else
                    const float2 uv = i.tex_coord.xy;
                #endif
                
                #if USE_CORNER_MASK
                    #if USE_CURVATURE
                        const float mask = Corner(uv);
                    #else
                        //frame mask
                        const float2 size = _TextureSize * 0.5 - _MaskRadius;
                        const float mask = step(-_MaskRadius, 1.0 - rectangle(i.pixel - _TextureSize * 0.5, size));
                    #endif
                #else
                    const float mask = 1;
                #endif

                return NTSCGauss(uv, i.pixel) * mask;
            }
            ENDHLSL
        }
        
        Pass //#3: Grab Framebuffer
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ USE_FLIPY
            #pragma multi_compile TURN_NONE TURN_CW TURN_CCW
            #pragma multi_compile _ _UNITY_RENDER_PIPELINE_HDRP

            #include "MultiPipeline.hlsl"

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 tex_coord : TEXCOORD0;
            };

            DEF_SAMPLER2D(_MainTex);

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = DEF_VERTEX_POS(v);
                float2 uv = DEF_UV_SCALED(v).xy;
                #if USE_FLIPY
                    #if UNITY_UV_STARTS_AT_TOP
                        #if TURN_CW || TURN_CCW
                            uv.x = 1 - uv.x;
                        #else
                            uv.y = 1 - uv.y;
                        #endif
                    #endif
                #endif
                o.tex_coord = uv;
                #if TURN_CW
                    o.tex_coord = float2(uv.y, 1 - uv.x);
                #elif TURN_CCW
                    o.tex_coord = float2(1 - uv.y, uv.x);
                #endif
                return o;
            }

            DEF_OUT_FRAGMENT frag (v2f i) : SV_Target
            {
                return DEF_SAMPLE_TEXTURE2D(_MainTex, i.tex_coord);
            }
            ENDHLSL
        }
    }
}

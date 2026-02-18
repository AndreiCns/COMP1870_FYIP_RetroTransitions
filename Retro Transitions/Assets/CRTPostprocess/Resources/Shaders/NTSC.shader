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
        [KeywordEnum(NARROW,MEDIUM,WIDE)] TapSize("Composite Blur Tap Size", Float) = 0
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

            #pragma multi_compile_local CROSSTALK_NONE CROSSTALK_VERTICAL CROSSTALK_SLANT CROSSTALK_SLANT_NOISE
            #pragma multi_compile_local _ USE_VERTICAL_CROSSTALK
            #pragma multi_compile_local _ _UNITY_RENDER_PIPELINE_HDRP

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = DEF_VERTEX_POS(v);
                o.tex_coord = DEF_UV(v);
                o.pixel = o.tex_coord * _TextureSize * (_OutputSize / _InputSize);
                return o;
            }

            #define PI (3.14159265)

            #if CROSSTALK_VERTICAL
            static const float CHROMA_MOD_FREQ = (PI / 3.0) * _ChromaModFrequencyScale;
            static const float CHROMA_PHASE_SHIFT = (2.0 * PI) * _ChromaPhaseShiftScale;
            #elif CROSSTALK_SLANT || CROSSTALK_SLANT_NOISE
            static const float CHROMA_MOD_FREQ = (0.75 * PI / 3.0) * _ChromaModFrequencyScale;
            static const float CHROMA_PHASE_SHIFT = (86.0 * PI) * _ChromaPhaseShiftScale;
            #else
            static const float CHROMA_MOD_FREQ = 0;
            static const float CHROMA_PHASE_SHIFT = 0;
            #endif
            static const float CROSSTALK_STRENGTH = max(0.1, _CrossTalkStrength);
            static const float FRINGE_STRENGTH = _FringeStrength * 0.7;
            static const float3x3 YIQ_MAT = float3x3(
                0.299, 0.587, 0.114,
                0.596, -0.274, -0.322,
                0.211, -0.523, 0.312
            );
            inline float3 RGBToYIQ(float3 col)
            {
                return mul(YIQ_MAT, col);
            }

            DEF_OUT_FRAGMENT frag(v2f i) : SV_Target
            {
                const float3 col = DEF_SAMPLE_TEXTURE2D(_MainTex, i.tex_coord).rgb;
                float3 yiq = RGBToYIQ(col);
                
                #if CROSSTALK_VERTICAL
                const float chroma_phase = CHROMA_PHASE_SHIFT;
                #elif CROSSTALK_SLANT
                const float chroma_phase = CHROMA_PHASE_SHIFT * (fmod(i.pixel.y, 3.0) + 1) / 3.0;
                #elif CROSSTALK_SLANT_NOISE
                const uint fc = (_FrameCountNum / 2);
                const float chroma_phase = CHROMA_PHASE_SHIFT * (fmod(i.pixel.y, 3.0) + 1 + (fc % 2)) / 3.0;
                #endif
                #if CROSSTALK_VERTICAL || CROSSTALK_SLANT || CROSSTALK_SLANT_NOISE
                const float mod_phase = chroma_phase + i.pixel.x * CHROMA_MOD_FREQ;

                float i_mod, q_mod;
                sincos(mod_phase, i_mod, q_mod);

                // Cross-Talk
                yiq.yz *= float2(i_mod, q_mod) / CROSSTALK_STRENGTH;
                yiq = float3(
                    yiq.x * _Brightness + yiq.y * _ArtifactStrength + yiq.z * _ArtifactStrength,
                    yiq.x * FRINGE_STRENGTH + yiq.y * CROSSTALK_STRENGTH,
                    yiq.x * FRINGE_STRENGTH + yiq.z * CROSSTALK_STRENGTH
                );
                yiq.yz *= float2(i_mod, q_mod) * CROSSTALK_STRENGTH;
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

            #pragma multi_compile_local TAPSIZE_NARROW TAPSIZE_MEDIUM TAPSIZE_WIDE
            #pragma multi_compile_local _ _UNITY_RENDER_PIPELINE_HDRP

            #include "MultiPipeline.hlsl"
            #include "HLSLSupport.cginc"

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

            #define PI (3.14159265)

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = DEF_VERTEX_POS(v);
                o.tex_coord = DEF_UV(v) - float2(0.5 / _TextureSize.x, 0.0);
                return o;
            }
            
            #if TAPSIZE_WIDE
                // LUMA Filter: Taps=11, Cutoff=0.12
                static const float LUMA_CENTER_W = 0.22613725;
                static const int LUMA_PAIRS = 3;
                static const float2 LUMA_DATA[] = {
                    float2(1.40446515, 0.33685664),
                    float2(3.09226098, 0.06834473),
                    float2(5.00000000, -0.01827000),
                };
                // CHROMA Filter: Taps=17, Cutoff=0.01
                static const float CHROMA_CENTER_W = 0.11365996;
                static const int CHROMA_PAIRS = 4;
                static const float2 CHROMA_DATA[] = {
                    float2(1.47332442, 0.20834934),
                    float2(3.43377501, 0.14509308),
                    float2(5.38053278, 0.07057921),
                    float2(7.27566777, 0.01914838),
                };
            #elif TAPSIZE_MEDIUM
                // LUMA Filter: Taps=7, Cutoff=0.2
                static const float LUMA_CENTER_W = 0.38141133;
                static const int LUMA_PAIRS = 2;
                static const float2 LUMA_DATA[] = {
                    float2(1.20499519, 0.34277527),
                    float2(3.00000000, -0.03348094),
                };
                // CHROMA Filter: Taps=13, Cutoff=0.01
                static const float CHROMA_CENTER_W = 0.14815713;
                static const int CHROMA_PAIRS = 3;
                static const float2 CHROMA_DATA[] = {
                    float2(1.45430160, 0.25574073),
                    float2(3.38106716, 0.13630954),
                    float2(5.24743691, 0.03387117),
                };
            #else
                // LUMA Filter: Taps=5, Cutoff=0.36
                static const float LUMA_CENTER_W = 0.74498622;
                static const int LUMA_PAIRS = 1;
                static const float2 LUMA_DATA[] = {
                    float2(1.00000000, 0.12750689),
                };
                // CHROMA Filter: Taps=9, Cutoff=0.01
                static const float CHROMA_CENTER_W = 0.21339512;
                static const int CHROMA_PAIRS = 2;
                static const float2 CHROMA_DATA[] = {
                    float2(1.40312392, 0.31558230),
                    float2(3.21252814, 0.07772014),
                };
            #endif

            static const float3x3 RGB_MAT = float3x3(
                1.0, 0.956, 0.621,
                1.0, -0.272, -0.647,
                1.0, -1.106, 1.703);
            inline float3 YIQToRGB(float3 yiq)
            {
                return mul(RGB_MAT, yiq);
            }
            
            static const float BLACK_LEVEL = 0.95 * _BlackLevel;

            DEF_OUT_FRAGMENT frag(v2f i) : SV_Target
            {
                const float pixelU = 1.0 / _TextureSize.x;
                i.tex_coord.x += pixelU * 0.5;
                float3 center = DEF_SAMPLE_TEXTURE2D(_MainTex, i.tex_coord).xyz;
                float ySums = center.x * LUMA_CENTER_W;
                float2 iqSums = center.yz * CHROMA_CENTER_W;
                int j;
                UNITY_UNROLL
                for (j = 0; j < LUMA_PAIRS; j++)
                {
                    const float2 offset = float2(LUMA_DATA[j].x * pixelU, pixelU * 0.0);
                    const float weight = LUMA_DATA[j].y;
                    const float valL = DEF_SAMPLE_TEXTURE2D(_MainTex, i.tex_coord - offset).x;
                    const float valR = DEF_SAMPLE_TEXTURE2D(_MainTex, i.tex_coord + offset).x;
                    ySums += (valL + valR) * weight * BLACK_LEVEL;
                }
                UNITY_UNROLL
                for (j = 0; j < CHROMA_PAIRS; j++)
                {
                    const float2 offset = float2(CHROMA_DATA[j].x * pixelU, pixelU * 0.0);
                    const float weight = CHROMA_DATA[j].y;
                    const float2 valL = DEF_SAMPLE_TEXTURE2D(_MainTex, i.tex_coord - offset).yz;
                    const float2 valR = DEF_SAMPLE_TEXTURE2D(_MainTex, i.tex_coord + offset).yz;
                    iqSums += (valL + valR) * weight;
                }
                return float4(YIQToRGB(float3(ySums, iqSums) * 2.0 - 1.0), 1.0);
            }
            ENDHLSL
        }

        Pass //#2: Gauss pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ USE_CURVATURE
            #pragma multi_compile_local _ USE_CORNER_MASK
            #pragma multi_compile_local TURN_NONE TURN_CW TURN_CCW
            #pragma multi_compile_local _ _UNITY_RENDER_PIPELINE_HDRP

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

            v2f vert(appdata v)
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
            const static float HEIGHT_SCALE = (_OutputSize.y / _TextureSize.y) * PI;

            inline float4 Scanline(float2 uv, float2 pixel)
            {
                const float3 frame = pow(DEF_SAMPLE_TEXTURE2D(_MainTex, uv).rgb, NTSC_CRT_GAMMA.xxx);
                const float lum = 1 - saturate(dot(frame, float3(0.299f, 0.587f, 0.114f)));
                const float scanlineLum = sin(pixel.y * HEIGHT_SCALE) + 1;
                const float scanlineStr = _ScanlineStrength * lerp(1-_BeamSpread, 1, lum * lum * scanlineLum);
                const float3 scanline = frame * lerp(1, scanlineLum, scanlineStr);
                return float4(pow(_BeamStrength * scanline, 1.0 / NTSC_DISPLAY_GAMMA), 1.0);
            }

            #if USE_CORNER_MASK
                inline float Rectangle(float2 samplePosition, float2 halfSize){
                    const float2 componentWiseEdgeDistance = abs(samplePosition) - halfSize;
                    const float outsideDistance = length(max(componentWiseEdgeDistance, 0));
                    const float insideDistance = min(max(componentWiseEdgeDistance.x, componentWiseEdgeDistance.y), 0);
                    return outsideDistance + insideDistance;
                }
            #endif

            DEF_OUT_FRAGMENT frag(v2f i) : SV_Target
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
                        const float mask = step(-_MaskRadius, 1.0 - Rectangle(i.pixel - _TextureSize * 0.5, size));
                    #endif
                #else
                    const float mask = 1;
                #endif

                return Scanline(uv, i.pixel) * mask;
            }
            ENDHLSL
        }
        
        Pass //#3: Grab Framebuffer
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            //#pragma multi_compile_local _ USE_FLIPY
            #pragma multi_compile_local TURN_NONE TURN_CW TURN_CCW
            #pragma multi_compile_local _ _UNITY_RENDER_PIPELINE_HDRP

            #include "MultiPipeline.hlsl"

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 tex_coord : TEXCOORD0;
            };

            DEF_SAMPLER2D(_MainTex);

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = DEF_VERTEX_POS(v);
                float2 uv = DEF_UV_SCALED(v).xy;
                /*#if USE_FLIPY
                    #if UNITY_UV_STARTS_AT_TOP
                        #if TURN_CW || TURN_CCW
                            uv.x = 1 - uv.x;
                        #else
                            uv.y = 1 - uv.y;
                        #endif
                    #endif
                #endif*/
                o.tex_coord = uv;
                #if TURN_CW
                    o.tex_coord = float2(uv.y, 1 - uv.x);
                #elif TURN_CCW
                    o.tex_coord = float2(1 - uv.y, uv.x);
                #endif
                return o;
            }

            DEF_OUT_FRAGMENT frag(v2f i) : SV_Target
            {
                return DEF_SAMPLE_TEXTURE2D(_MainTex, i.tex_coord);
            }
            ENDHLSL
        }
    }
}

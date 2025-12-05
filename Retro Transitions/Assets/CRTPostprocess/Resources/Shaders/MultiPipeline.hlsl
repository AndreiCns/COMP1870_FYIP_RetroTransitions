#ifndef _MULTI_PIPELINE
#define _MULTI_PIPELINE

#include "HDRPChecker.hlsl"
#if _UNITY_RENDER_PIPELINE_HDRP && _HDRP_INSTALLED
    //HDRP
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"

    struct appdata
    {
        uint vertexID : SV_VertexID;
    };
    float2 _UVScale = float2(1,1);
    #define DEF_SAMPLER2D(tex) TEXTURE2D_X(tex)
    #define DEF_VERTEX_POS(v) GetFullScreenTriangleVertexPosition(v.vertexID)
    #define DEF_UV(v) GetFullScreenTriangleTexCoord(v.vertexID)
    #define DEF_UV_SCALED(v) DEF_UV(v)/_UVScale
    #define DEF_UV_ANTI_SCALED(v) DEF_UV(v)*_UVScale
    #define DEF_SAMPLE_TEXTURE2D(tex,uv) SAMPLE_TEXTURE2D_X(tex,s_linear_clamp_sampler,uv)
    #define DEF_OUT_FRAGMENT float4
#else
    //Legacy Pipeline or _UNITY_RENDER_PIPELINE_URP
    #include "UnityCG.cginc"
    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };
    float2 _UVScale = float2(1,1);
    #define DEF_SAMPLER2D(tex) sampler2D tex
    #define DEF_VERTEX_POS(v) UnityObjectToClipPos(v.vertex)
    #define DEF_UV(v) (v.uv)
    #define DEF_UV_SCALED(v) DEF_UV(v)
    #define DEF_UV_ANTI_SCALED(v) DEF_UV(v)
    #define DEF_SAMPLE_TEXTURE2D(tex,uv) tex2D(tex,uv)
    #define DEF_OUT_FRAGMENT fixed4
#endif

#endif

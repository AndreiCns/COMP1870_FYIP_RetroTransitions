using UnityEngine;
#if UNITY_PIPELINE_HDRP
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;
using UnityEngine.Experimental.Rendering;

namespace CRTPostprocess
{
    [Serializable, VolumeComponentMenu("Post-processing/Custom/NTSCPostprocessHDRP")]
    public sealed class NTSCPostprocessHDRP : CustomPostProcessVolumeComponent, IPostProcessComponent
    {
        public enum CrossTalkMode
        {
            None,
            Vertical,
            Slant,
            SlantNoise
        }

        public enum GaussianBlurWidth
        {
            TAP4,
            TAP8,
            TAP24
        }

        public enum DisplayOrientation
        {
            None,
            CW,
            CCW
        }
        
        public BoolParameter enable = new BoolParameter(true);
        public IntParameter bufferHeight = new IntParameter(480);
        public IntParameter outputHeight = new IntParameter(240);
        #if UNITY_2023_2_OR_NEWER
        public EnumParameter<CrossTalkMode> crossTalkMode = new EnumParameter<CrossTalkMode>(CrossTalkMode.Vertical);
        #else
        [Tooltip("0=None, 1=Vertical, 2=Slant, 3=SlantNoise")]
        public ClampedIntParameter crossTalkMode = new ClampedIntParameter((int)CrossTalkMode.Vertical, 0, 3);
        #endif
        public FloatParameter crossTalkStrength = new FloatParameter(2f);
        public FloatParameter brightness = new FloatParameter(0.95f);
        public FloatParameter blackLevel = new FloatParameter(1.0526f);
        public FloatParameter artifactStrength = new FloatParameter(1f);
        public FloatParameter fringeStrength = new FloatParameter(0.75f);
        public FloatParameter chromaModFrequencyScale = new FloatParameter(1f);
        public FloatParameter chromaPhaseShiftScale = new FloatParameter(1f);
        #if UNITY_2023_2_OR_NEWER
        public EnumParameter<GaussianBlurWidth> gaussianBlurWidth = new EnumParameter<GaussianBlurWidth>(GaussianBlurWidth.TAP8);
        #else
        [Tooltip("0=TAP4, 1=TAP8, 2=TAP24")]
        public ClampedIntParameter gaussianBlurWidth = new ClampedIntParameter((int)GaussianBlurWidth.TAP8, 0, 2);
        #endif
        public BoolParameter curvature = new BoolParameter(true);
        public BoolParameter cornerMask = new BoolParameter(true);
        [Tooltip("Non curvature mode uses this parameter.")] public IntParameter cornerRadius = new IntParameter(16);
        public FloatParameter scanlineStrength = new FloatParameter(1f);
        public FloatParameter beamSpread = new FloatParameter(0.5f);
        public FloatParameter beamStrength = new FloatParameter(1f);
        public FloatParameter overscanScale = new FloatParameter(0.985f);
        #if UNITY_2023_2_OR_NEWER
        public EnumParameter<DisplayOrientation> displayOrientation = new EnumParameter<DisplayOrientation>(DisplayOrientation.None);
        #else
        [Tooltip("0=None, 1=CW, 2=CCW")]
        public ClampedIntParameter displayOrientation = new ClampedIntParameter((int)DisplayOrientation.None, 0, 2);
        #endif

        private Material _material;

        private const CustomPostProcessInjectionPoint _injectionEvent = CustomPostProcessInjectionPoint.AfterPostProcess;
        private const string _shaderName = "Hidden/NTSCPass";

        private static readonly int _propTextureSize = Shader.PropertyToID("_TextureSize");
        private static readonly int _propInputSize = Shader.PropertyToID("_InputSize");
        private static readonly int _propOutputSize = Shader.PropertyToID("_OutputSize");
        private static readonly int _propMaskRadius = Shader.PropertyToID("_MaskRadius");
        private static readonly int _propCrossTalkStrength = Shader.PropertyToID("_CrossTalkStrength");
        private static readonly int _propScanlineStrength = Shader.PropertyToID("_ScanlineStrength");
        private static readonly int _propBeamSpread = Shader.PropertyToID("_BeamSpread");
        private static readonly int _propBeamStrength = Shader.PropertyToID("_BeamStrength");
        private static readonly int _propOverscanScale = Shader.PropertyToID("_OverscanScale");
        private static readonly int _propBrightness = Shader.PropertyToID("_Brightness");
        private static readonly int _propBlackLevel = Shader.PropertyToID("_BlackLevel");
        private static readonly int _propArtifactStrength = Shader.PropertyToID("_ArtifactStrength");
        private static readonly int _propFringeStrength = Shader.PropertyToID("_FringeStrength");
        private static readonly int _propChromaModFrequencyScale = Shader.PropertyToID("_ChromaModFrequencyScale");
        private static readonly int _propChromaPhaseShiftScale = Shader.PropertyToID("_ChromaPhaseShiftScale");
        private static readonly int _propFrameCount = Shader.PropertyToID("_FrameCountNum");
        private static readonly int _propUVScale = Shader.PropertyToID("_UVScale");
        
        private static readonly int _renderTex1Id = Shader.PropertyToID("_NTSCTex1");
        private static readonly int _renderTex2Id = Shader.PropertyToID("_NTSCTex2");

        private static LocalKeyword _turnNone;
        private static LocalKeyword _turnCW;
        private static LocalKeyword _turnCCW;
        private static LocalKeyword _keyTap4;
        private static LocalKeyword _keyTap8;
        private static LocalKeyword _keyTap24;
        private static LocalKeyword _keyCrossVertical;
        private static LocalKeyword _keyCrossSlant;
        private static LocalKeyword _keyCrossSlantNoise;
        private static LocalKeyword _keyCurvature;
        private static LocalKeyword _keyCornerMask;
        
        public bool IsActive() => _material != null && enable.value;
        public override CustomPostProcessInjectionPoint injectionPoint => _injectionEvent;

        private int GetTargetWidth(float width, float height, float newHeight)
        {
            float aspect = width / height;
            return Mathf.FloorToInt(aspect * newHeight);
        }
        
        private void InitKeywords(Shader shader)
        {
            _turnNone = new LocalKeyword(shader, "TURN_NONE");
            _turnCW = new LocalKeyword(shader, "TURN_CW");
            _turnCCW = new LocalKeyword(shader, "TURN_CCW");
            _keyTap4 = new LocalKeyword(shader, "TAPSIZE_TAP4");
            _keyTap8 = new LocalKeyword(shader, "TAPSIZE_TAP8");
            _keyTap24 = new LocalKeyword(shader, "TAPSIZE_TAP24");
            _keyCrossVertical = new LocalKeyword(shader, "CROSSTALK_VERTICAL");
            _keyCrossSlant = new LocalKeyword(shader, "CROSSTALK_SLANT");
            _keyCrossSlantNoise = new LocalKeyword(shader, "CROSSTALK_SLANT_NOISE");
            _keyCurvature = new LocalKeyword(shader, "USE_CURVATURE");
            _keyCornerMask = new LocalKeyword(shader, "USE_CORNER_MASK");
        }

        public override void Setup()
        {
            if (Shader.Find(_shaderName) != null)
            {
                _material = new Material(Shader.Find(_shaderName));
                _material.hideFlags = HideFlags.HideAndDontSave;
                LocalKeyword keyword = new LocalKeyword(_material.shader, "_UNITY_RENDER_PIPELINE_HDRP");
                _material.SetKeyword(keyword, true);
                InitKeywords(_material.shader);
            }
            else
            {
                Debug.LogError($"Unable to find shader '{_shaderName}'.");
            }
        }

        public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
        {
            if (_material == null)
                return;

            var screenWidth = camera.actualWidth;
            var screenHeight = camera.actualHeight;
            var destSize = destination.GetScaledSize();
            UpdateParameters(screenWidth, screenHeight, destSize.x, destSize.y);

            // 一時的なレンダーテクスチャを作成
            int h = bufferHeight.value;
            int w = GetTargetWidth(screenWidth, screenHeight, bufferHeight.value);
            #if UNITY_2023_2_OR_NEWER
            if (displayOrientation.value != DisplayOrientation.None)
            #else
            if (displayOrientation.value != (int)DisplayOrientation.None)
            #endif
            {
                (h, w) = (w, h);
            }

            cmd.GetTemporaryRTArray(_renderTex1Id, w, h, 1, 0, FilterMode.Bilinear, GraphicsFormat.R16G16B16A16_SFloat);
            cmd.GetTemporaryRTArray(_renderTex2Id, w, h, 1, 0, FilterMode.Bilinear, GraphicsFormat.R16G16B16A16_SFloat);

            #if UNITY_2023_2_OR_NEWER
            if (crossTalkMode == CrossTalkMode.SlantNoise)
            #else
            if (crossTalkMode.value == (int)CrossTalkMode.SlantNoise)
            #endif
            {
                cmd.SetGlobalInt(_propFrameCount, Time.frameCount);
            }

            // NTSC
            cmd.Blit(source, _renderTex1Id, _material, 3); // Copy to mini buffer
            cmd.Blit(_renderTex1Id, _renderTex2Id, _material, 0); // RGB to YIQ
            cmd.Blit(_renderTex2Id, _renderTex1Id, _material, 1); // YIQ to RGB and crosstalk
            cmd.ReleaseTemporaryRT(_renderTex2Id);
            cmd.Blit(_renderTex1Id, destination, _material, 2); // Gauss
            cmd.ReleaseTemporaryRT(_renderTex1Id);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(_material);
        }
        
        public void UpdateParameters(int screenWidth, int screenHeight, int texWidth, int texHeight)
        {
            if (_material == null) return;
            
            // UV Scaling
            _material.SetVector(_propUVScale,  new Vector2(texWidth, texHeight) / new Vector2(screenWidth, screenHeight));
            
            // Display Orientation
            _material.SetKeyword(_turnNone, false);
            _material.SetKeyword(_turnCW, false);
            _material.SetKeyword(_turnCCW, false);
            switch (displayOrientation.value)
            {
                #if UNITY_2023_2_OR_NEWER
                case DisplayOrientation.None:
                #else
                case (int)DisplayOrientation.None:
                #endif
                    _material.SetKeyword(_turnNone, true);
                    break;
                #if UNITY_2023_2_OR_NEWER
                case DisplayOrientation.CW:
                #else
                case (int)DisplayOrientation.CW:
                #endif
                    _material.SetKeyword(_turnCW, true);
                    break;
                #if UNITY_2023_2_OR_NEWER
                case DisplayOrientation.CCW:
                #else
                case (int)DisplayOrientation.CCW:
                #endif
                    _material.SetKeyword(_turnCCW, true);
                    break;
            }
            
            // Texture Sizes
            #if UNITY_2023_2_OR_NEWER
            if (displayOrientation != DisplayOrientation.None)
            #else
            if (displayOrientation.value != (int)DisplayOrientation.None)
            #endif
            {
                (screenHeight, screenWidth) =  (screenWidth, screenHeight);
            }
            int w = GetTargetWidth(screenWidth, screenHeight, bufferHeight.value);
            _material.SetVector(_propTextureSize, new Vector4(w, bufferHeight.value, 0, 0));
            _material.SetVector(_propInputSize, new Vector4(w, bufferHeight.value, 0, 0));
            _material.SetVector(_propOutputSize, new Vector4(1137, outputHeight.value, 0, 0));

            // Gaussian Blur Width
            _material.SetKeyword(_keyTap4, false);
            _material.SetKeyword(_keyTap8, false);
            _material.SetKeyword(_keyTap24, false);
            switch (gaussianBlurWidth.value)
            {
                #if UNITY_2023_2_OR_NEWER
                case GaussianBlurWidth.TAP4:
                #else
                case (int)GaussianBlurWidth.TAP4:
                #endif
                    _material.SetKeyword(_keyTap4, true);
                    break;
                #if UNITY_2023_2_OR_NEWER
                case GaussianBlurWidth.TAP8:
                #else
                case (int)GaussianBlurWidth.TAP8:
                #endif
                    _material.SetKeyword(_keyTap8, true);
                    break;
                #if UNITY_2023_2_OR_NEWER
                case GaussianBlurWidth.TAP24:
                #else
                case (int)GaussianBlurWidth.TAP24:
                #endif
                    _material.SetKeyword(_keyTap24, true);
                    break;
            }
            
            // Cross-Talk Mode
            _material.SetKeyword(_keyCrossVertical, false);
            _material.SetKeyword(_keyCrossSlant, false);
            _material.SetKeyword(_keyCrossSlantNoise, false);
            switch (crossTalkMode.value)
            {
                #if UNITY_2023_2_OR_NEWER
                case CrossTalkMode.Vertical:
                #else
                case (int)CrossTalkMode.Vertical:
                #endif
                    _material.SetKeyword(_keyCrossVertical, true);
                    break;
                #if UNITY_2023_2_OR_NEWER
                case CrossTalkMode.Slant:
                #else
                case (int)CrossTalkMode.Slant:
                #endif
                    _material.SetKeyword(_keyCrossSlant, true);
                    break;
                #if UNITY_2023_2_OR_NEWER
                case CrossTalkMode.SlantNoise:
                #else
                case (int)CrossTalkMode.SlantNoise:
                #endif
                    _material.SetKeyword(_keyCrossSlantNoise, true);
                    break;
            }
            _material.SetFloat(_propCrossTalkStrength, crossTalkStrength.value);
            _material.SetFloat(_propBrightness, brightness.value);
            _material.SetFloat(_propBlackLevel, blackLevel.value);
            _material.SetFloat(_propArtifactStrength, artifactStrength.value);
            _material.SetFloat(_propFringeStrength, fringeStrength.value);
            _material.SetFloat(_propChromaModFrequencyScale, chromaModFrequencyScale.value);
            _material.SetFloat(_propChromaPhaseShiftScale, chromaPhaseShiftScale.value);

            // Curvature
            _material.SetKeyword(_keyCurvature, curvature.value);
            
            // Non Curvature Corner Mask
            _material.SetKeyword(_keyCornerMask, cornerMask.value);
            _material.SetFloat(_propMaskRadius, cornerRadius.value);
            
            // Scanline Strength
            _material.SetFloat(_propScanlineStrength, scanlineStrength.value);
            _material.SetFloat(_propBeamSpread, beamSpread.value);
            _material.SetFloat(_propBeamStrength, beamStrength.value);
            
            // Overscan Scale
            _material.SetFloat(_propOverscanScale, overscanScale.value);
        }
    }
}
#endif

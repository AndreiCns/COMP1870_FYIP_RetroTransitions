using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace CRTPostprocess
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class NTSCPostprocess : MonoBehaviour
    {
        public enum CrossTalkMode
        {
            None,
            Vertical,
            Slant,
            SlantNoise
        }

        public enum BlurWidth
        {
            Narrow,
            Medium,
            Wide
        }

        public enum DisplayOrientation
        {
            None,
            CW,
            CCW
        }
        
        public int bufferHeight = 480;
        public int outputHeight = 240;
        public CrossTalkMode crossTalkMode = CrossTalkMode.Vertical;
        public float crossTalkStrength = 2f;
        public float brightness = 0.95f;
        public float blackLevel = 1.0526f;
        public float artifactStrength = 1f;
        public float fringeStrength = 0.75f;
        public float chromaModFrequencyScale = 1f;
        public float chromaPhaseShiftScale = 1f;
        [FormerlySerializedAs("gaussianBlurWidth")] public BlurWidth blurWidth = BlurWidth.Medium;
        public bool curvature = true;
        public bool cornerMask = true;
        [Tooltip("Non curvature mode uses this parameter.")] public int cornerRadius = 16;
        public float scanlineStrength = 1f;
        public float beamSpread = 0.5f;
        public float beamStrength = 1f;
        public float overscanScale = 0.985f;
        public DisplayOrientation displayOrientation = DisplayOrientation.None;

        private const CameraEvent _cameraEvent = CameraEvent.BeforeImageEffects; //AfterEverythingだとAndroidでは正しくフレームバッファを得られない
        private const string _shaderName = "Hidden/NTSCPass";
        private Material _material;
        private Camera _camera;
        private CommandBuffer _cmd;
        
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

        private static readonly int _renderTex1Id = Shader.PropertyToID("_NTSCTex1");
        private static readonly int _renderTex2Id = Shader.PropertyToID("_NTSCTex2");

        private static LocalKeyword _turnNone;
        private static LocalKeyword _turnCW;
        private static LocalKeyword _turnCCW;
        private static LocalKeyword _keyNarrow;
        private static LocalKeyword _keyMedium;
        private static LocalKeyword _keyWide;
        private static LocalKeyword _keyCrossVertical;
        private static LocalKeyword _keyCrossSlant;
        private static LocalKeyword _keyCrossSlantNoise;
        private static LocalKeyword _keyCurvature;
        private static LocalKeyword _keyCornerMask;
        
        private void OnValidate() => Setup();
        private void OnEnable() => Setup();
        private void OnDisable() => Teardown();
        
        private void InitKeywords(Shader shader)
        {
            _turnNone = new LocalKeyword(shader, "TURN_NONE");
            _turnCW = new LocalKeyword(shader, "TURN_CW");
            _turnCCW = new LocalKeyword(shader, "TURN_CCW");
            _keyNarrow = new LocalKeyword(shader, "TAPSIZE_NARROW");
            _keyMedium = new LocalKeyword(shader, "TAPSIZE_MEDIUM");
            _keyWide = new LocalKeyword(shader, "TAPSIZE_WIDE");
            _keyCrossVertical = new LocalKeyword(shader, "CROSSTALK_VERTICAL");
            _keyCrossSlant = new LocalKeyword(shader, "CROSSTALK_SLANT");
            _keyCrossSlantNoise = new LocalKeyword(shader, "CROSSTALK_SLANT_NOISE");
            _keyCurvature = new LocalKeyword(shader, "USE_CURVATURE");
            _keyCornerMask = new LocalKeyword(shader, "USE_CORNER_MASK");
        }
        
        private int GetTargetWidth(float width, float height, float newHeight)
        {
            float aspect = width / height;
            return Mathf.FloorToInt(aspect * newHeight);
        }

        private void GetScreenSize(out int width, out int height)
        {
            #if UNITY_EDITOR
            string[] res = UnityEditor.UnityStats.screenRes.Split('x');
            width = int.Parse(res[0]);
            height = int.Parse(res[1]);
            #else
            width = Screen.width;
            height = Screen.height;
            #endif
        }

        private bool IsLegacyPipeline()
        {
            if (GraphicsSettings.defaultRenderPipeline != null) return false;
            if (QualitySettings.renderPipeline != null) return false;
            return true;
        }
        
        private void Setup()
        {
            Teardown();
            
            _camera = GetComponent<Camera>();
            var shader = Shader.Find(_shaderName);
            _material = new Material(shader);
            _material.hideFlags = HideFlags.HideAndDontSave;
            InitKeywords(_material.shader);
            
            UpdateParameters();
            
            _cmd = new CommandBuffer();
            _cmd.name = "NTSCPass";
            GetScreenSize(out var screenWidth, out var screenHeight);
            int h = bufferHeight;
            int w = GetTargetWidth(screenWidth, screenHeight, bufferHeight);
            if (displayOrientation != DisplayOrientation.None)
            {
                (h, w) = (w, h);
            }
            
            _cmd.GetTemporaryRT(_renderTex1Id, w, h, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            _cmd.GetTemporaryRT(_renderTex2Id, w, h, 0, FilterMode.Bilinear, RenderTextureFormat.Default);

            // NTSC
            _cmd.Blit(BuiltinRenderTextureType.CameraTarget, _renderTex1Id, _material, 3); // Copy to mini buffer
            _cmd.Blit(_renderTex1Id, _renderTex2Id, _material, 0); // RGB to YIQ
            _cmd.Blit(_renderTex2Id, _renderTex1Id, _material, 1); // YIQ to RGB and crosstalk
            _cmd.ReleaseTemporaryRT(_renderTex2Id);
            _cmd.Blit(_renderTex1Id, BuiltinRenderTextureType.CameraTarget, _material, 2); // Gauss
            _cmd.ReleaseTemporaryRT(_renderTex1Id);

            if (IsLegacyPipeline())
            {
                //_camera.forceIntoRenderTexture = true;
                _camera.AddCommandBuffer(_cameraEvent, _cmd);
            }
        }

        public void Update()
        {
            if (crossTalkMode == CrossTalkMode.SlantNoise)
            {
                Shader.SetGlobalInt(_propFrameCount, Time.frameCount);
            }
            
            #if UNITY_EDITOR
            UpdateParameters();
            #endif
        }

        public void UpdateParameters()
        {
            if (_material == null) return;
            
            // Display Orientation
            _material.SetKeyword(_turnNone, false);
            _material.SetKeyword(_turnCW, false);
            _material.SetKeyword(_turnCCW, false);
            switch (displayOrientation)
            {
                case DisplayOrientation.None:
                    _material.SetKeyword(_turnNone, true);
                    break;
                case DisplayOrientation.CW:
                    _material.SetKeyword(_turnCW, true);
                    break;
                case DisplayOrientation.CCW:
                    _material.SetKeyword(_turnCCW, true);
                    break;
            }
            
            // Texture Sizes
            GetScreenSize(out var screenWidth, out var screenHeight);
            if (displayOrientation != DisplayOrientation.None)
            {
                (screenHeight, screenWidth) =  (screenWidth, screenHeight);
            }
            int w = GetTargetWidth(screenWidth, screenHeight, bufferHeight);
            _material.SetVector(_propTextureSize, new Vector4(w, bufferHeight, 0, 0));
            _material.SetVector(_propInputSize, new Vector4(w, bufferHeight, 0, 0));
            _material.SetVector(_propOutputSize, new Vector4(1137, outputHeight, 0, 0));

            // Blur Width
            _material.SetKeyword(_keyNarrow, false);
            _material.SetKeyword(_keyMedium, false);
            _material.SetKeyword(_keyWide, false);
            switch (blurWidth)
            {
                case BlurWidth.Narrow:
                    _material.SetKeyword(_keyNarrow, true);
                    break;
                case BlurWidth.Medium:
                    _material.SetKeyword(_keyMedium, true);
                    break;
                case BlurWidth.Wide:
                    _material.SetKeyword(_keyWide, true);
                    break;
            }
            
            // Cross-Talk Mode
            _material.SetKeyword(_keyCrossVertical, false);
            _material.SetKeyword(_keyCrossSlant, false);
            _material.SetKeyword(_keyCrossSlantNoise, false);
            switch (crossTalkMode)
            {
                case CrossTalkMode.Vertical:
                    _material.SetKeyword(_keyCrossVertical, true);
                    break;
                case CrossTalkMode.Slant:
                    _material.SetKeyword(_keyCrossSlant, true);
                    break;
                case CrossTalkMode.SlantNoise:
                    _material.SetKeyword(_keyCrossSlantNoise, true);
                    break;
            }
            _material.SetFloat(_propCrossTalkStrength, crossTalkStrength);
            _material.SetFloat(_propBrightness, brightness);
            _material.SetFloat(_propBlackLevel, blackLevel);
            _material.SetFloat(_propArtifactStrength, artifactStrength);
            _material.SetFloat(_propFringeStrength, fringeStrength);
            _material.SetFloat(_propChromaModFrequencyScale, chromaModFrequencyScale);
            _material.SetFloat(_propChromaPhaseShiftScale, chromaPhaseShiftScale);

            // Curvature
            _material.SetKeyword(_keyCurvature, curvature);
            
            // Non Curvature Corner Mask
            _material.SetKeyword(_keyCornerMask, cornerMask);
            _material.SetFloat(_propMaskRadius, cornerRadius);
            
            // Scanline Strength
            _material.SetFloat(_propScanlineStrength, scanlineStrength);
            _material.SetFloat(_propBeamSpread, beamSpread);
            _material.SetFloat(_propBeamStrength, beamStrength);
            
            // Overscan Scale
            _material.SetFloat(_propOverscanScale, overscanScale);
        }

        private void Teardown()
        {
            if (_cmd != null)
            {
                if (IsLegacyPipeline()) _camera.RemoveCommandBuffer(_cameraEvent, _cmd);
                _cmd = null;
            }
            if (_material != null)
            {
                DestroyImmediate(_material);
                _material = null;
            }
        }
    }
}

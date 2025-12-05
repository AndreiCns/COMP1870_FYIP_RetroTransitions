using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_PIPELINE_URP
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace CRTPostprocess
{
    public class NTSCPass : ScriptableRenderPass
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

        public struct Parameter
        {
            public int bufferHeight;
            public int outputHeight;
            public CrossTalkMode crossTalkMode;
            public float crossTalkStrength;
            public float brightness;
            public float blackLevel;
            public float artifactStrength;
            public float fringeStrength;
            public float chromaModFrequencyScale;
            public float chromaPhaseShiftScale;
            public GaussianBlurWidth gaussianBlurWidth;
            public bool useCurvature;
            public bool useCornerMask;
            public float cornerRadius;
            public float scanlineStrength;
            public float beamSpread;
            public float beamStrength;
            public float overscanScale;
            public DisplayOrientation displayOrientation;
        }

        private const string Tag = nameof(NTSCPass);
        private Material _material;
        private Parameter _param;
        #if !UNITY_6000_0_OR_NEWER
        private PassData _passData;
        #endif

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
        private static readonly int _propFrameCount = Shader.PropertyToID("_FrameCount");

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

        public NTSCPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        public void InitKeywords(Shader shader)
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
        
        private class PassData
        {
            #if UNITY_6000_0_OR_NEWER
            internal TextureHandle activeColorBuffer;
            #else
            internal RenderTargetIdentifier activeColorBuffer;
            #endif
            internal int targetHeight;
            internal int targetWidth;
            internal Material material;
            internal DisplayOrientation displayOrientation;
        }
        
        public void SetupParameters(Material mat, Parameter param)
        {
            _material = mat;
            _param = param;
            
            #if !UNITY_6000_0_OR_NEWER
            if (_passData == null) _passData = new PassData();
            GetScreenSize(out var width, out var height);
            UpdateMaterial(_passData, width, height);
            #endif
        }

        private int GetTargetWidth(float width, float height, float newHeight)
        {
            float aspect = width / height;
            return Mathf.FloorToInt(aspect * newHeight);
        }

        #if UNITY_6000_0_OR_NEWER
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            const string passName = Tag;
            using (var builder = renderGraph.AddUnsafePass<PassData>(passName, out var passData))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                passData.activeColorBuffer = resourceData.activeColorTexture;
                var desc = resourceData.activeColorTexture.GetDescriptor(renderGraph);

                UpdateMaterial(passData, desc.width, desc.height);

                builder.UseTexture(passData.activeColorBuffer, AccessFlags.ReadWrite);
                builder.SetRenderFunc((PassData data, UnsafeGraphContext context) => ExecutePass(data, context));
            }
        }
        #else
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
        public void SetRenderingData(RenderTargetIdentifier renderTarget)
        {
            if (_passData == null) _passData = new PassData();
            _passData.activeColorBuffer = renderTarget;
            GetScreenSize(out var width, out var height);
            UpdateMaterial(_passData, width, height);
        }
        #endif

        private void UpdateMaterial(PassData passData, int pixelWidth, int pixelHeight)
        {
            // Texture Sizes
            passData.displayOrientation = _param.displayOrientation;
            if (passData.displayOrientation != DisplayOrientation.None)
            {
                (pixelWidth, pixelHeight) = (pixelHeight, pixelWidth);
            }
            passData.targetHeight = _param.bufferHeight;
            passData.targetWidth = GetTargetWidth(pixelWidth, pixelHeight, _param.bufferHeight);
            passData.material = _material;
            _material.SetVector(_propTextureSize, new Vector4(passData.targetWidth, _param.bufferHeight, 0, 0));
            _material.SetVector(_propInputSize, new Vector4(passData.targetWidth, _param.bufferHeight, 0, 0));
            _material.SetVector(_propOutputSize, new Vector4(1137, _param.outputHeight, 0, 0));

            // Display Orientation
            _material.SetKeyword(_turnNone, false);
            _material.SetKeyword(_turnCW, false);
            _material.SetKeyword(_turnCCW, false);
            switch (_param.displayOrientation)
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

            // Gaussian Blur Width
            _material.SetKeyword(_keyTap4, false);
            _material.SetKeyword(_keyTap8, false);
            _material.SetKeyword(_keyTap24, false);
            switch (_param.gaussianBlurWidth)
            {
                case GaussianBlurWidth.TAP4:
                    _material.SetKeyword(_keyTap4, true);
                    break;
                case GaussianBlurWidth.TAP8:
                    _material.SetKeyword(_keyTap8, true);
                    break;
                case GaussianBlurWidth.TAP24:
                    _material.SetKeyword(_keyTap24, true);
                    break;
            }
            
            // Cross-Talk Mode
            _material.SetKeyword(_keyCrossVertical, false);
            _material.SetKeyword(_keyCrossSlant, false);
            _material.SetKeyword(_keyCrossSlantNoise, false);
            switch (_param.crossTalkMode)
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
            _material.SetFloat(_propCrossTalkStrength, _param.crossTalkStrength);
            _material.SetFloat(_propBrightness, _param.brightness);
            _material.SetFloat(_propBlackLevel, _param.blackLevel);
            _material.SetFloat(_propArtifactStrength, _param.artifactStrength);
            _material.SetFloat(_propFringeStrength, _param.fringeStrength);
            _material.SetFloat(_propChromaModFrequencyScale, _param.chromaModFrequencyScale);
            _material.SetFloat(_propChromaPhaseShiftScale, _param.chromaPhaseShiftScale);
            
            // Curvature
            _material.SetKeyword(_keyCurvature, _param.useCurvature);
            
            // Non Curvature Corner Mask
            _material.SetKeyword(_keyCornerMask, _param.useCornerMask);
            _material.SetFloat(_propMaskRadius, _param.cornerRadius);
            
            // Scanline Strength
            _material.SetFloat(_propScanlineStrength, _param.scanlineStrength);
            _material.SetFloat(_propBeamSpread, _param.beamSpread);
            _material.SetFloat(_propBeamStrength, _param.beamStrength);
            
            // Overscan Scale
            _material.SetFloat(_propOverscanScale, _param.overscanScale);
        }
        
        #if UNITY_6000_0_OR_NEWER
        static void ExecutePass(PassData data, UnsafeGraphContext context)
        {
            var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

            var w = data.targetWidth;
            var h = data.targetHeight;

            cmd.SetGlobalInt(_propFrameCount, Time.frameCount);

            cmd.GetTemporaryRT(_renderTex1Id, w, h, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            cmd.GetTemporaryRT(_renderTex2Id, w, h, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            
            // NTSC
            cmd.Blit(data.activeColorBuffer, _renderTex1Id, data.material, 3); // Copy to mini buffer
            cmd.Blit(_renderTex1Id, _renderTex2Id, data.material, 0); // RGB to YIQ
            cmd.Blit(_renderTex2Id, _renderTex1Id, data.material, 1); // YIQ to RGB and crosstalk
            cmd.ReleaseTemporaryRT(_renderTex2Id);
            cmd.Blit(_renderTex1Id, data.activeColorBuffer, data.material, 2); // Gauss
            cmd.ReleaseTemporaryRT(_renderTex1Id);
        }
        #else
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get(Tag);

            var renderTex1Id = Shader.PropertyToID("_NTSCTex1");
            var renderTex2Id = Shader.PropertyToID("_NTSCTex2");
            var w = _passData.targetWidth;
            var h = _passData.targetHeight;

            cmd.SetGlobalInt(_propFrameCount, Time.frameCount);

            cmd.GetTemporaryRT(renderTex1Id, w, h, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            cmd.GetTemporaryRT(renderTex2Id, w, h, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            
            // NTSC
            cmd.Blit(_passData.activeColorBuffer, renderTex1Id, _passData.material, 3); // Copy to mini buffer
            cmd.Blit(renderTex1Id, renderTex2Id, _passData.material, 0); // RGB to YIQ
            cmd.Blit(renderTex2Id, renderTex1Id, _passData.material, 1); // YIQ to RGB and crosstalk
            cmd.ReleaseTemporaryRT(renderTex2Id);
            cmd.Blit(renderTex1Id, _passData.activeColorBuffer, _passData.material, 2); // Gauss
            cmd.ReleaseTemporaryRT(renderTex1Id);
            
            context.ExecuteCommandBuffer(cmd);
            context.Submit();
            CommandBufferPool.Release(cmd);
        }
        #endif
    }
}
#endif

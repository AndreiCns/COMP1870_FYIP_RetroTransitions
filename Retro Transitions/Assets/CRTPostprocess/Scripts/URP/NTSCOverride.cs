using UnityEngine;
#if UNITY_PIPELINE_URP
using UnityEngine.Rendering;

namespace CRTPostprocess
{
    [System.Serializable]
    [VolumeComponentMenu("Post-processing/NTSC Postprocess")]
    public class NTSCOverride : VolumeComponent
    {
        public BoolParameter enable = new BoolParameter(true);
        public IntParameter bufferHeight = new IntParameter(480);
        public IntParameter outputHeight = new IntParameter(240);
        public EnumParameter<NTSCPass.CrossTalkMode> crossTalkMode = new EnumParameter<NTSCPass.CrossTalkMode>(NTSCPass.CrossTalkMode.Vertical);
        public FloatParameter crossTalkStrength = new FloatParameter(2f);
        public FloatParameter brightness = new FloatParameter(0.95f);
        public FloatParameter blackLevel = new FloatParameter(1.0526f);
        public FloatParameter artifactStrength = new FloatParameter(1f);
        public FloatParameter fringeStrength = new FloatParameter(0.75f);
        public FloatParameter chromaModFrequencyScale = new FloatParameter(1f);
        public FloatParameter chromaPhaseShiftScale = new FloatParameter(1f);
        public EnumParameter<NTSCPass.GaussianBlurWidth> gaussianBlurWidth = new EnumParameter<NTSCPass.GaussianBlurWidth>(NTSCPass.GaussianBlurWidth.TAP8);
        public BoolParameter curvature = new BoolParameter(true);
        public BoolParameter cornerMask = new BoolParameter(true);
        [Tooltip("Non curvature mode uses this parameter.")] public IntParameter cornerRadius = new IntParameter(16);
        public FloatParameter scanlineStrength = new FloatParameter(1f);
        public FloatParameter beamSpread = new FloatParameter(0.5f);
        public FloatParameter beamStrength = new FloatParameter(1f);
        public FloatParameter overscanScale = new FloatParameter(0.985f);
        public EnumParameter<NTSCPass.DisplayOrientation> displayOrientation = new EnumParameter<NTSCPass.DisplayOrientation>(NTSCPass.DisplayOrientation.None);
        
        public bool IsActive => enable.value;
    }
}

#endif

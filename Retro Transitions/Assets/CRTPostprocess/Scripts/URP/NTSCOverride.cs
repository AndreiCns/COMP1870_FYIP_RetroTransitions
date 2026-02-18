using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_PIPELINE_URP
using UnityEngine.Rendering;

namespace CRTPostprocess
{
    [System.Serializable]
    [VolumeComponentMenu("Post-processing/NTSC Postprocess")]
    public class NTSCOverride : VolumeComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public IntParameter bufferHeight = new IntParameter(480);
        public IntParameter outputHeight = new IntParameter(240);
        #if UNITY_2023_2_OR_NEWER
        public EnumParameter<NTSCPass.CrossTalkMode> crossTalkMode = new EnumParameter<NTSCPass.CrossTalkMode>(NTSCPass.CrossTalkMode.Vertical);
        #else
        [Tooltip("0=None, 1=Vertical, 2=Slant, 3=SlantNoise")]
        public ClampedIntParameter crossTalkMode = new ClampedIntParameter((int)NTSCPass.CrossTalkMode.Vertical, 0, 3);
        #endif
        public FloatParameter crossTalkStrength = new FloatParameter(2f);
        public FloatParameter brightness = new FloatParameter(0.95f);
        public FloatParameter blackLevel = new FloatParameter(1.0526f);
        public FloatParameter artifactStrength = new FloatParameter(1f);
        public FloatParameter fringeStrength = new FloatParameter(0.75f);
        public FloatParameter chromaModFrequencyScale = new FloatParameter(1f);
        public FloatParameter chromaPhaseShiftScale = new FloatParameter(1f);
        #if UNITY_2023_2_OR_NEWER
        [FormerlySerializedAs("gaussianBlurWidth")] public EnumParameter<NTSCPass.BlurWidth> blurWidth = new EnumParameter<NTSCPass.BlurWidth>(NTSCPass.BlurWidth.Medium);
        #else
        [Tooltip("0=Narrow, 1=Medium, 2=Wide")]
        [FormerlySerializedAs("gaussianBlurWidth")] public ClampedIntParameter blurWidth = new ClampedIntParameter((int)NTSCPass.BlurWidth.Medium, 0, 2);
        #endif
        public BoolParameter curvature = new BoolParameter(true);
        public BoolParameter cornerMask = new BoolParameter(true);
        [Tooltip("Non curvature mode uses this parameter.")] public IntParameter cornerRadius = new IntParameter(16);
        public FloatParameter scanlineStrength = new FloatParameter(1f);
        public FloatParameter beamSpread = new FloatParameter(0.5f);
        public FloatParameter beamStrength = new FloatParameter(1f);
        public FloatParameter overscanScale = new FloatParameter(0.985f);
        #if UNITY_2023_2_OR_NEWER
        public EnumParameter<NTSCPass.DisplayOrientation> displayOrientation = new EnumParameter<NTSCPass.DisplayOrientation>(NTSCPass.DisplayOrientation.None);
        #else
        [Tooltip("0=None, 1=CW, 2=CCW")]
        public ClampedIntParameter displayOrientation = new ClampedIntParameter((int)NTSCPass.DisplayOrientation.None, 0, 2);
        #endif
        
        public bool IsActive => enable.value;
    }
}

#endif

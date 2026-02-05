using UnityEngine;
#if UNITY_PIPELINE_HDRP
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
#endif

namespace CRTPostprocess.Sample
{
    public class SampleForHDRP : MonoBehaviour
    {
        #if UNITY_PIPELINE_HDRP
        
        public GameObject[] sampleData;
        public Volume volume;
        public UnityEngine.UI.Text textForInfo;
        public bool quickImageChange = false;
        public bool animParam = true;

        private const float _secToNextDefault = 1f;
        private int _index = 0;
        private float _secToNext = _secToNextDefault;

        private int _currentCrossTalkMode = 0;
        private int _currentGaussianBlurWidthMode = 0;
        private bool _currentCurvature = true;
        private NTSCPostprocessHDRP _profile = null;

        private bool _featureActive;
        private NTSCPostprocessHDRP.CrossTalkMode _crossTalkModeDefault;
        private NTSCPostprocessHDRP.GaussianBlurWidth _gaussianBlurWidthDefault;
        private bool _curvatureDefault;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Application.targetFrameRate = 60;
            
            volume.profile.TryGet<NTSCPostprocessHDRP>(out _profile);
            if (_profile != null)
            {
                #if UNITY_2023_2_OR_NEWER
                _crossTalkModeDefault = _profile.crossTalkMode.value;
                _gaussianBlurWidthDefault = _profile.gaussianBlurWidth.value;
                #else
                _crossTalkModeDefault = (NTSCPostprocessHDRP.CrossTalkMode)_profile.crossTalkMode.value;
                _gaussianBlurWidthDefault = (NTSCPostprocessHDRP.GaussianBlurWidth)_profile.gaussianBlurWidth.value;
                #endif
                _curvatureDefault = _profile.curvature.value;
                _featureActive = _profile.enable.value;
                
                _profile.crossTalkMode.overrideState = true;
                _profile.gaussianBlurWidth.overrideState = true;
                _profile.curvature.overrideState = true;
                _profile.enable.overrideState = true;
            }

            UpdateTextForInfo();
            
            
        }

        private void OnApplicationQuit()
        {
            if (_profile != null)
            {
                #if UNITY_2023_2_OR_NEWER
                _profile.crossTalkMode.value = _crossTalkModeDefault;
                _profile.gaussianBlurWidth.value = _gaussianBlurWidthDefault;
                #else
                _profile.crossTalkMode.value = (int)_crossTalkModeDefault;
                _profile.gaussianBlurWidth.value = (int)_gaussianBlurWidthDefault;
                #endif
                _profile.curvature.value = _curvatureDefault;
                _profile.enable.value = _featureActive;

                _profile.crossTalkMode.overrideState = false;
                _profile.gaussianBlurWidth.overrideState = false;
                _profile.curvature.overrideState = false;
                _profile.enable.overrideState = false;
            }
        }

        private void UpdateTextForInfo()
        {
            if (_profile != null && !_profile.enable.value)
            {
                textForInfo.text = "No Effects";
            }
            else
            {
                // Update Text for Info
                string t = "Cross-Talk: " + (NTSCPostprocessHDRP.CrossTalkMode)(_currentCrossTalkMode + 1);
                t += ", Curvature: " + (_currentCurvature ? "ON" : "OFF");
                t += ", GaussWidth: " + (NTSCPostprocessHDRP.GaussianBlurWidth)_currentGaussianBlurWidthMode;
                textForInfo.text = t;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!animParam) return;
            
            _secToNext -= Time.deltaTime;
            if (_secToNext < 0)
            {
                _secToNext = _secToNextDefault;

                _currentGaussianBlurWidthMode = (_currentGaussianBlurWidthMode + 1) % 3;
                if (_currentGaussianBlurWidthMode == 0)
                {
                    if (quickImageChange) ChangeImage();

                    _currentCurvature = !_currentCurvature;
                    if (_currentCurvature)
                    {
                        _currentCrossTalkMode = (_currentCrossTalkMode + 1) % 4;
                    }
                }

                if (_currentCrossTalkMode == 3)
                {
                    if (!quickImageChange) ChangeImage();
                    
                    if (_profile != null) _profile.enable.value = false;
                    _currentCrossTalkMode = 0;
                }
                else
                {
                    if (_profile != null) _profile.enable.value = true;
                    SetNTSC(_currentCurvature, (NTSCPostprocessHDRP.CrossTalkMode)(_currentCrossTalkMode + 1),
                        (NTSCPostprocessHDRP.GaussianBlurWidth)_currentGaussianBlurWidthMode);
                }

                UpdateTextForInfo();
            }
        }

        void ChangeImage()
        {
            _index = (_index + 1) % sampleData.Length;
            foreach (var sample in sampleData)
            {
                sample.SetActive(false);
            }
            sampleData[_index].SetActive(true);
        }

        void SetNTSC(bool _curvature, NTSCPostprocessHDRP.CrossTalkMode _crossTalkMode, NTSCPostprocessHDRP.GaussianBlurWidth _gaussianBlurWidth)
        {
            if (_profile == null) return;
            #if UNITY_2023_2_OR_NEWER
            _profile.crossTalkMode.value = _crossTalkMode;
            _profile.gaussianBlurWidth.value = _gaussianBlurWidth;
            #else
            _profile.crossTalkMode.value = (int)_crossTalkMode;
            _profile.gaussianBlurWidth.value = (int)_gaussianBlurWidth;
            #endif
            _profile.curvature.value = _curvature;
        }

        #endif
    }
}
using UnityEngine;
#if UNITY_PIPELINE_URP
using UnityEngine.Rendering.Universal;
#endif

namespace CRTPostprocess.Sample
{
    public class SampleForURP : MonoBehaviour
    {
        #if UNITY_PIPELINE_URP
        
        public GameObject[] sampleData;
        public UniversalRendererData rendererData;
        public UnityEngine.UI.Text textForInfo;
        public bool quickImageChange = false;
        public bool animParam = true;

        private const float _secToNextDefault = 1f;
        private int _index = 0;
        private float _secToNext = _secToNextDefault;

        private int _currentCrossTalkMode = 0;
        private int _currentGaussianBlurWidthMode = 0;
        private bool _currentCurvature = true;
        private NTSCFeature _feature;

        private bool _featureActive;
        private NTSCPass.CrossTalkMode _crossTalkModeDefault;
        private NTSCPass.BlurWidth _blurWidthDefault;
        private bool _curvatureDefault;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Application.targetFrameRate = 60;
            
            #if UNITY_6000_0_OR_NEWER
            rendererData.TryGetRendererFeature(out _feature);
            #else
            foreach (var f in rendererData.rendererFeatures)
            {
                if (f.GetType() == typeof(NTSCFeature))
                {
                    _feature = (NTSCFeature)f;
                    break;
                }
            }
            #endif
            if (_feature != null)
            {
                _crossTalkModeDefault = _feature.crossTalkMode;
                _blurWidthDefault = _feature.blurWidth;
                _curvatureDefault = _feature.curvature;
                _featureActive = _feature.isActive;
            }

            UpdateTextForInfo();
            
            
        }

        private void OnApplicationQuit()
        {
            if (_feature != null)
            {
                _feature.crossTalkMode = _crossTalkModeDefault;
                _feature.blurWidth = _blurWidthDefault;
                _feature.curvature = _curvatureDefault;
                _feature.SetActive(_featureActive);
            }
        }

        private void UpdateTextForInfo()
        {
            if (_feature != null && !_feature.isActive)
            {
                textForInfo.text = "No Effects";
            }
            else
            {
                // Update Text for Info
                string t = "Cross-Talk: " + (NTSCPass.CrossTalkMode)(_currentCrossTalkMode + 1);
                t += ", Curvature: " + (_currentCurvature ? "ON" : "OFF");
                t += ", GaussWidth: " + (NTSCPass.BlurWidth)_currentGaussianBlurWidthMode;
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
                    
                    if (_feature != null) _feature.SetActive(false);
                    _currentCrossTalkMode = 0;
                }
                else
                {
                    if (_feature != null) _feature.SetActive(true);
                    SetNTSC(_currentCurvature, (NTSCPass.CrossTalkMode)(_currentCrossTalkMode + 1),
                        (NTSCPass.BlurWidth)_currentGaussianBlurWidthMode);
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

        void SetNTSC(bool _curvature, NTSCPass.CrossTalkMode _crossTalkMode, NTSCPass.BlurWidth blurWidth)
        {
            if (_feature == null) return;
            _feature.crossTalkMode = _crossTalkMode;
            _feature.blurWidth = blurWidth;
            _feature.curvature = _curvature;
        }

        #endif
    }
}
using UnityEngine;

namespace CRTPostprocess.Sample
{
    public class SampleForLegacy : MonoBehaviour
    {
        public GameObject[] sampleData;
        public NTSCPostprocess postProcess;
        public UnityEngine.UI.Text textForInfo;
        public bool quickImageChange = false;
        public bool animParam = true;

        private const float _secToNextDefault = 1f;
        private int _index = 0;
        private float _secToNext = _secToNextDefault;

        private int _currentCrossTalkMode = 0;
        private int _currentGaussianBlurWidthMode = 0;
        private bool _currentCurvature = true;

        private bool _featureActive;
        private NTSCPostprocess.CrossTalkMode _crossTalkModeDefault;
        private NTSCPostprocess.GaussianBlurWidth _gaussianBlurWidthDefault;
        private bool _curvatureDefault;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Application.targetFrameRate = 60;
            
            if (postProcess != null)
            {
                _crossTalkModeDefault = postProcess.crossTalkMode;
                _gaussianBlurWidthDefault = postProcess.gaussianBlurWidth;
                _curvatureDefault = postProcess.curvature;
                _featureActive = postProcess.enabled;
            }

            UpdateTextForInfo();
        }

        private void OnApplicationQuit()
        {
            if (postProcess != null)
            {
                postProcess.crossTalkMode = _crossTalkModeDefault;
                postProcess.gaussianBlurWidth = _gaussianBlurWidthDefault;
                postProcess.curvature = _curvatureDefault;
                postProcess.enabled = _featureActive;
                postProcess.UpdateParameters();
            }
        }

        private void UpdateTextForInfo()
        {
            if (postProcess != null && !postProcess.enabled)
            {
                textForInfo.text = "No Effects";
            }
            else
            {
                // Update Text for Info
                string t = "Cross-Talk: " + (NTSCPostprocess.CrossTalkMode)(_currentCrossTalkMode + 1);
                t += ", Curvature: " + (_currentCurvature ? "ON" : "OFF");
                t += ", GaussWidth: " + (NTSCPostprocess.GaussianBlurWidth)_currentGaussianBlurWidthMode;
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

                    if (postProcess != null) postProcess.enabled = false;
                    _currentCrossTalkMode = 0;
                }
                else
                {
                    if (postProcess != null) postProcess.enabled = true;
                    SetNTSC(_currentCurvature, (NTSCPostprocess.CrossTalkMode)(_currentCrossTalkMode + 1),
                        (NTSCPostprocess.GaussianBlurWidth)_currentGaussianBlurWidthMode);
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

        void SetNTSC(bool _curvature, NTSCPostprocess.CrossTalkMode _crossTalkMode, NTSCPostprocess.GaussianBlurWidth _gaussianBlurWidth)
        {
            if (postProcess == null) return;
            postProcess.crossTalkMode = _crossTalkMode;
            postProcess.gaussianBlurWidth = _gaussianBlurWidth;
            postProcess.curvature = _curvature;
            postProcess.UpdateParameters();
        }
    }
}
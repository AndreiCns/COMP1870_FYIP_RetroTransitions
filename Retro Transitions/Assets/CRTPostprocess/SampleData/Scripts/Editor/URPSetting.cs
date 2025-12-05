using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

namespace CRTPostprocess.Sample.Editor
{
    [InitializeOnLoad]
    public class URPSetting
    {
        static URPSetting()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            switch (scene.name)
            {
                case "SampleSceneForHDRP":
                {
                    var currentPipeline = QualitySettings.renderPipeline;
                    if (currentPipeline == null || currentPipeline.name != "PC_HDRP_NTSCRPAsset")
                    {
                        if (EditorUtility.DisplayDialog(
                                "GraphicsSettings",
                                "Current RenderPipeline is not suitable for SampleSceneForHDRP,\ncan I switch RenderPipeline?\nIf not switched, it will not look correct.",
                                "OK", "NO"))
                        {
                            var newPipeline =
                                AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(
                                    "Assets/CRTPostprocess/SampleData/Settings/PC_HDRP_NTSCRPAsset.asset");
                            QualitySettings.renderPipeline = newPipeline;
                            AssetDatabase.ImportAsset("Assets/CRTPostprocess/SampleData/Scripts/Editor/URPSetting.cs", ImportAssetOptions.ForceUpdate);
                        }
                    }
                    //EditorApplication.isPlaying = true;
                    break;
                }
                case "SampleSceneForURP":
                {
                    var currentPipeline = QualitySettings.renderPipeline;
                    if (currentPipeline == null || currentPipeline.name != "PC_NTSCRPAsset")
                    {
                        if (EditorUtility.DisplayDialog(
                                "GraphicsSettings",
                                "Current RenderPipeline is not suitable for SampleSceneForURP,\ncan I switch RenderPipeline?\nIf not switched, it will not look correct.",
                                "OK", "NO"))
                        {
                            var newPipeline =
                                AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(
                                    "Assets/CRTPostprocess/SampleData/Settings/PC_NTSCRPAsset.asset");
                            QualitySettings.renderPipeline = newPipeline;
                            AssetDatabase.ImportAsset("Assets/CRTPostprocess/SampleData/Scripts/Editor/URPSetting.cs", ImportAssetOptions.ForceUpdate);
                        }
                    }
                    //EditorApplication.isPlaying = true;
                    break;
                }
                case "SampleSceneForURPMobile":
                {
                    var currentPipeline = QualitySettings.renderPipeline;
                    if (currentPipeline == null || currentPipeline.name != "Mobile_NTSCRPAsset")
                    {
                        if (EditorUtility.DisplayDialog(
                                "GraphicsSettings",
                                "Current RenderPipeline is not suitable for SampleSceneForURPMobile,\ncan I switch RenderPipeline?\nIf not switched, it will not look correct.",
                                "OK", "NO"))
                        {
                            var newPipeline =
                                AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(
                                    "Assets/CRTPostprocess/SampleData/Settings/Mobile_NTSCRPAsset.asset");
                            QualitySettings.renderPipeline = newPipeline;
                            AssetDatabase.ImportAsset("Assets/CRTPostprocess/SampleData/Scripts/Editor/URPSetting.cs", ImportAssetOptions.ForceUpdate);
                        }
                    }
                    //EditorApplication.isPlaying = true;
                    break;
                }
                case "SampleSceneForLegacy":
                {
                    if (GraphicsSettings.defaultRenderPipeline != null || QualitySettings.renderPipeline != null)
                    {
                        if (EditorUtility.DisplayDialog(
                                "GraphicsSettings",
                                "Current RenderPipeline is not suitable for SampleSceneForLegacy,\ncan I switch RenderPipeline?\nIf not switched, it will not look correct.",
                                "OK", "NO"))
                        {
                            GraphicsSettings.defaultRenderPipeline = null;
                            QualitySettings.renderPipeline = null;
                            AssetDatabase.ImportAsset("Assets/CRTPostprocess/SampleData/Scripts/Editor/URPSetting.cs", ImportAssetOptions.ForceUpdate);
                        }
                    }
                    //EditorApplication.isPlaying = true;
                    break;
                }
            }
        }
    }
}

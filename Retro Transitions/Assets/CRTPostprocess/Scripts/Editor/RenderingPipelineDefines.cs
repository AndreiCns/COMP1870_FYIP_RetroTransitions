using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Rendering;

namespace CRTPostprocess.Editor
{
    [InitializeOnLoad]
    public class RenderingPipelineDefines
    { 
        static RenderingPipelineDefines()
        {
            UpdateDefines();
            CheckShader();
        }

        private static void UpdateDefines()
        {
            GetPipelineType(out var isURP, out var isHDRP);
            if (isURP)
            {
                AddDefine("UNITY_PIPELINE_URP");
            }
            else
            {
                RemoveDefine("UNITY_PIPELINE_URP");
            }
            if (isHDRP)
            {
                AddDefine("UNITY_PIPELINE_HDRP");
            }
            else
            {
                RemoveDefine("UNITY_PIPELINE_HDRP");
            }
            CreateHDRPCheckerFile(isHDRP);
        }

        private static void GetPipelineType(out bool isURP, out bool isHDRP)
        {
            isURP = false;
            isHDRP = false;
            Assembly[] playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);
            foreach (var assembly in playerAssemblies) {
                if (assembly.name.StartsWith("Unity.RenderPipelines.Universal"))
                {
                    // URP installed.
                    isURP = true;
                }
                if (assembly.name.StartsWith("Unity.RenderPipelines.HighDefinition"))
                {
                    // HDRP installed.
                    isHDRP = true;
                }
            }
        }

        private static void AddDefine(string define)
        {
            var definesList = GetDefines();
            if (!definesList.Contains(define))
            {
                definesList.Add(define);
                SetDefines(definesList);
            }
        }

        private static void RemoveDefine(string define)
        {
            var definesList = GetDefines();
            if (definesList.Contains(define))
            {
                definesList.Remove(define);
                SetDefines(definesList);
            }
        }

        #if UNITY_6000_0_OR_NEWER
        private static NamedBuildTarget GetBuildTarget(BuildTarget target)
        {
            NamedBuildTarget t = new NamedBuildTarget();
            switch (target)
            {
                case BuildTarget.iOS:
                    t = NamedBuildTarget.iOS;
                    break;
                case BuildTarget.Android:
                    t = NamedBuildTarget.Android;
                    break;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.StandaloneOSX:
                    t = NamedBuildTarget.Standalone;
                    break;
            }
            return t;
        }
        #endif

        private static List<string> GetDefines()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            #if UNITY_6000_0_OR_NEWER
            var defines = PlayerSettings.GetScriptingDefineSymbols(GetBuildTarget(target));
            #else
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            #endif
            return defines.Split(';').ToList();
        }

        private static void SetDefines(List<string> definesList)
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var defines = string.Join(";", definesList.ToArray()).Trim(';');
            #if UNITY_6000_0_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(GetBuildTarget(target), defines);
            #else
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
            #endif
        }

        private static void CheckShader()
        {
            const string shaderPath = "Hidden/NTSCPass";
            AddAlwaysIncludedShader(shaderPath);
        }
        
        private static void AddAlwaysIncludedShader(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
                return;

            var graphicsSettingsObj = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            var serializedObject = new SerializedObject(graphicsSettingsObj);
            var arrayProp = serializedObject.FindProperty("m_AlwaysIncludedShaders");
            bool hasShader = false;
            for (int i = 0; i < arrayProp.arraySize; ++i)
            {
                var arrayElem = arrayProp.GetArrayElementAtIndex(i);
                if (shader == arrayElem.objectReferenceValue)
                {
                    hasShader = true;
                    break;
                }
            }

            if (!hasShader)
            {
                int arrayIndex = arrayProp.arraySize;
                arrayProp.InsertArrayElementAtIndex(arrayIndex);
                var arrayElem = arrayProp.GetArrayElementAtIndex(arrayIndex);
                arrayElem.objectReferenceValue = shader;

                serializedObject.ApplyModifiedProperties();

                AssetDatabase.SaveAssets();
            }
        }
        
        private static string AssetsToAbsolutePath(string source)
        {
            #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                return source.Replace("Assets", Application.dataPath).Replace("/", "\\");
            #else
                return source.Replace("Assets", Application.dataPath);
            #endif
        }

        private static void CreateHDRPCheckerFile(bool isHDRP)
        {
            var shader = Shader.Find("Hidden/NTSCPass");
            if (shader == null) return;
            string path = AssetDatabase.GetAssetPath(shader).Replace("NTSC.shader", "HDRPChecker.hlsl");
            string fullPath = AssetsToAbsolutePath(path);
            string text = "#ifndef _HDRP_INSTALLED\n#define _HDRP_INSTALLED ";
            text += isHDRP ? "1\n" : "0\n";
            text += "#endif";
            System.IO.File.WriteAllText(fullPath, text, Encoding.UTF8);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }
}
#if !NET_4_6 && !NET_STANDARD_2_0
    #error Loom SDK requires .NET 4.x. Please go to Build Settings -> Player Settings -> Configuration and set Scripting Runtime Version to .NET 4.x Equivalent
#endif

using UnityEditor;
using UnityEditor.Build;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif

namespace Loom.Client.Unity.Editor.Internal
{
    internal class CheckProject :
#if UNITY_2018_1_OR_NEWER
        IPreprocessBuildWithReport
#else
        IPreprocessBuild
#endif
    {
        public int callbackOrder { get; }

#if UNITY_2018_1_OR_NEWER
        public void OnPreprocessBuild(BuildReport report)
        {
            CheckForBuildTarget(report.summary.platform);
        }
#else
        public void OnPreprocessBuild(BuildTarget target, string path)
        {
            CheckForBuildTarget(target);
        }
#endif

        private void CheckForBuildTarget(BuildTarget target)
        {
            if (target == BuildTarget.WebGL)
            {
                CheckWebGLTemplate();
                CheckWebGLPrebuiltEngine();
            }
        }

        private void CheckWebGLPrebuiltEngine()
        {
            if (!EditorUserBuildSettings.webGLUsePreBuiltUnityEngine)
                return;

            bool result =
                EditorUtility.DisplayDialog(
                    "Loom - Incompatible Build Settings",
                    "The 'Use pre-built Engine' WebGL build option is enabled, which is not compatible with Loom.\n\n" +
                    "Would you like to disable it?",
                    "Disable 'Use pre-built Engine'",
                    "Ignore");

            if (result)
            {
                EditorUserBuildSettings.webGLUsePreBuiltUnityEngine = false;
            }
        }

        private void CheckWebGLTemplate()
        {
            if (PlayerSettings.WebGL.template.StartsWith("APPLICATION"))
            {
                bool result =
                    EditorUtility.DisplayDialog(
                    "Loom - Incompatible WebGL Template",
                    "You are using a standard Unity WebGL template, which is not compatible with Loom SDK.\n\n" +
                    "Would you like to use a provided Loom template?",
                    "Set Loom Template",
                    "Ignore");

                if (result)
                {
                    PlayerSettings.WebGL.template = "PROJECT:Loom";
                }
            }
        }
    }
}

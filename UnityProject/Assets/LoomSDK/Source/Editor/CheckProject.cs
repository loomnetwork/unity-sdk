using UnityEditor;
using UnityEditor.Build;

namespace Loom.Client.Unity.Editor.Internal
{
    internal class CheckProject : IPreprocessBuild
    {
        public int callbackOrder { get; }

        public void OnPreprocessBuild(BuildTarget target, string path)
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
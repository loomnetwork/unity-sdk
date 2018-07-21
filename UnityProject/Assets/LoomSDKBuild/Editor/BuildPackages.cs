using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Loom.Client.Unity.Editor.Build {
    public class BuildPackages {
        private const string kAssetNameShort = "Loom SDK";
        private const string kPackageName = "loom-unity-sdk";

        private const string kMenuRoot = "Tools/[Build] " + kAssetNameShort + "/";

        [MenuItem(kMenuRoot + "Build Package")]
        public static void BuildPackage() {
            Debug.Log("[Build] - Building package");
            List<string> paths = CollectPackagePaths();

            if (!AssetDatabase.IsValidFolder("Assets/~NonVersioned")) {
                AssetDatabase.CreateFolder("Assets", "~NonVersioned");
            }

            ExportPackage(paths, $"Assets/~NonVersioned/{kPackageName}.unitypackage");
        }

        private static List<string> CollectPackagePaths() {
            return PackageBuildUtility.CollectPaths(
                new[] {
                    "Assets/LoomSDK",
                    "Assets/WebGLTemplates"
                },
                null
            );
        }

        private static void ExportPackage(List<string> paths, string packagePath) {
            AssetDatabase.ExportPackage(paths.ToArray(), packagePath, ExportPackageOptions.Default);
            AssetDatabase.Refresh();
        }
    }
}
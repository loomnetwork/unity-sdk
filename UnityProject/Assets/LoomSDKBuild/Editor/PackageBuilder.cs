using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Loom.Client.Unity.Editor.Build {
    public static class PackageBuilder {
        private const string kPackageName = "loom-unity-sdk";

        public static void BuildPackage() {
            AttemptPotentiallyFailingOperation(SamplesDownloader.DownloadSamples, delayBetweenAttempts: 5000);

            Debug.Log("[Build] - Building package");
            List<string> paths = CollectPackagePaths();

            if (!AssetDatabase.IsValidFolder("Assets/~NonVersioned")) {
                AssetDatabase.CreateFolder("Assets", "~NonVersioned");
            }

            string packagePath = $"Assets/~NonVersioned/{kPackageName}.unitypackage";
            ExportPackage(paths, packagePath);

            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(packagePath));
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

        /// <summary>
        /// Attempts to execute <paramref name="action"/> <paramref name="maxAttempts"/> number of times with pauses between attempts.
        /// Rethrows the original exception if attemps are depleted.
        /// </summary>
        /// <param name="action">Action to execute.</param>
        /// <param name="maxAttempts">Maximum amount of attempts before throwing an exception.</param>
        /// <param name="delayBetweenAttempts">Delay between attempts.</param>
        private static void AttemptPotentiallyFailingOperation(Action action, int maxAttempts = 3, int delayBetweenAttempts = 400) {
            int failCounter = 0;
            Exception originalException = null;
            while (true) {
                try {
                    action();
                    return;
                } catch (Exception e) {
                    if (originalException == null) {
                        originalException = e;
                    }

                    if (failCounter < maxAttempts) {
                        failCounter++;
                        Thread.Sleep(delayBetweenAttempts);
                        continue;
                    }

                    throw originalException;
                }
            }
        }
    }
}
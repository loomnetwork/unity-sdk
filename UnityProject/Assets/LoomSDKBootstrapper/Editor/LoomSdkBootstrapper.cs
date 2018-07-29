using System;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Loom.Client.Unity.Editor.Internal {
    [InitializeOnLoad]
    internal static class LoomSdkBootstrapper {
        static LoomSdkBootstrapper() {
            EditorApplication.delayCall += () => {
                if (IsLoomSdkImported())
                    return;

                bool result = EditorUtility.DisplayDialog(
                    "Loom SDK - Installation",
                    "Loom SDK is required in this project, but is not present.\n\n" +
                    "Download and install Loom SDK now?",
                    "Download and install",
                    "Cancel"
                );

                if (!result)
                    return;

                DownloadAndImportSdkPackage();
            };
        }

        [MenuItem("Tools/Loom SDK/Install SDK", true)]
        public static bool InstallValidate() {
            return !IsLoomSdkImported();
        }

        [MenuItem("Tools/Loom SDK/Install SDK")]
        public static void Install() {
            if (IsLoomSdkImported())
                return;

            DownloadAndImportSdkPackage();
        }

        [MenuItem("Tools/Loom SDK/Update SDK", true)]
        public static bool UpdateValidate() {
            return IsLoomSdkImported();
        }

        [MenuItem("Tools/Loom SDK/Update SDK")]
        public static void Update() {
            DownloadAndImportSdkPackage();
        }

        private static bool IsLoomSdkImported() {
            string[] loomSdkAsmDef = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset LoomSDK");
            if (loomSdkAsmDef.Length > 0)
                return true;

            return false;
        }

        private static void DownloadAndImportSdkPackage() {
            string tempPackageDownloadPath = FileUtil.GetUniqueTempPathInProject() + ".unitypackage";
            try {
                // Fetch release
                UnityWebRequest request = UnityWebRequest.Get("https://api.github.com/repos/loomnetwork/unity3d-sdk/releases/latest");
                request.timeout = 10;
                ExecuteSendWebRequestSync(request, (requestAsyncOperation) =>
                    !EditorUtility.DisplayCancelableProgressBar(
                    "Downloading Latest Loom SDK Package",
                    "Fetching package info...",
                    requestAsyncOperation.progress
                ));

                string text = request.downloadHandler.text;
                GitHubReleaseItem gitHubRelease = JsonUtility.FromJson<GitHubReleaseItem>(text);
                if (gitHubRelease.assets.Length == 0)
                    throw new Exception("No Loom SDK releases found");

                GitHubReleaseItem.Asset unityPackageAsset =
                    gitHubRelease.assets
                        .FirstOrDefault(asset => asset.name.EndsWith(".unitypackage"));
                if (unityPackageAsset == null)
                    throw new Exception("No Loom SDK releases with .unitypackage found");

                // Download the package
                request = UnityWebRequest.Get(unityPackageAsset.browser_download_url);
                request.timeout = 10;
                DownloadHandlerFile downloadHandlerFile = new DownloadHandlerFile(tempPackageDownloadPath);
                downloadHandlerFile.removeFileOnAbort = true;
                request.downloadHandler = downloadHandlerFile;
                ExecuteSendWebRequestSync(request, (requestAsyncOperation) =>
                    !EditorUtility.DisplayCancelableProgressBar(
                        "Downloading Latest Loom SDK Package",
                        String.Format(
                            "Downloading package... {0} MB/{1} MB",
                            Math.Round(requestAsyncOperation.webRequest.downloadedBytes / 1000f / 1000f, 2),
                            Math.Round((double) Convert.ToUInt32(requestAsyncOperation.webRequest.GetResponseHeader("Content-Length")) / 1000f / 1000f, 2)
                        ),
                        requestAsyncOperation.progress
                    ));

                AssetDatabase.ImportPackage(tempPackageDownloadPath, false);
            } catch (OperationCanceledException) {
                // Ignored
            } finally {
                EditorUtility.ClearProgressBar();
                if (File.Exists(tempPackageDownloadPath)) {
                    try {
                        File.Delete(tempPackageDownloadPath);
                    } catch {
                        // Ignored
                    }
                }
            }
        }

        private static void ExecuteSendWebRequestSync(UnityWebRequest request, Func<UnityWebRequestAsyncOperation, bool> onUpdate) {
            UnityWebRequestAsyncOperation requestAsyncOperation = request.SendWebRequest();
            while (!requestAsyncOperation.isDone) {
                if (!onUpdate(requestAsyncOperation)) {
                    request.Abort();
                    throw new OperationCanceledException();
                }

                Thread.Sleep(50);
            }

            if (request.isNetworkError)
                throw new Exception($"Network error while getting {request.url}");

            if (request.isHttpError)
                throw new Exception($"HTTP error {request.responseCode} while getting {request.url}");
        }

#pragma warning disable 0649
        [Serializable]
        private class GitHubReleaseResponse {
            public GitHubReleaseItem[] items;
        }

        [Serializable]
        private class GitHubReleaseItem {
            public string url;
            public Asset[] assets = new Asset[0];

            [Serializable]
            public class Asset {
                public string name;
                public string browser_download_url;
            }
        }
#pragma warning restore 0649
    }
}
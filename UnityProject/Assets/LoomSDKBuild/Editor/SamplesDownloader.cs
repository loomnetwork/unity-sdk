using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Loom.Client.Unity.Editor.Build {
    public static class SamplesDownloader {
        private static string kSamplesPath = "Assets/LoomSDK/Samples/";

        private static Tuple<string, string>[] kSamplesUrls = {
            Tuple.Create("https://github.com/loomnetwork/loom-unity-project-template/archive/master.zip", "loom-unity-project-template.zip"),
            Tuple.Create("https://github.com/loomnetwork/unity-evm-blackjack/archive/master.zip", "unity-evm-blackjack.zip"),
            Tuple.Create("https://github.com/loomnetwork/unity-tiles-chain-evm/archive/master.zip", "unity-tiles-chain-evm.zip"),

            // Private
            //Tuple.Create("https://github.com/loomnetwork/unity-tiles-chain/archive/master.zip", "unity-tiles-chain.zip"),
            //Tuple.Create("https://github.com/loomnetwork/memory_game_unity/archive/master.zip", "unity-memory-game.zip"),
        };

        public static void DownloadSamples() {
            Debug.Log("[Build] - Downloading samples");
            try {
                for (int i = 0; i < kSamplesUrls.Length; i++) {
                    Tuple<string, string> sample = kSamplesUrls[i];
                    string filePath = Path.Combine(kSamplesPath, sample.Item2);

                    UnityWebRequest request = UnityWebRequest.Get(sample.Item1);
                    request.timeout = 10;
                    DownloadHandlerFile downloadHandlerFile = new DownloadHandlerFile(filePath);
                    downloadHandlerFile.removeFileOnAbort = true;
                    request.downloadHandler = downloadHandlerFile;

                    UnityWebRequestAsyncOperation requestAsyncOperation = request.SendWebRequest();
                    while (!requestAsyncOperation.isDone) {
                        bool result = EditorUtility.DisplayCancelableProgressBar(
                            $"Downloading Sample {i + 1}/{kSamplesUrls.Length}",
                            String.Format(
                                "Downloading archive... {0} MB/{1} MB",
                                Math.Round(requestAsyncOperation.webRequest.downloadedBytes / 1000f / 1000f, 2),
                                Math.Round((double) Convert.ToUInt32(requestAsyncOperation.webRequest.GetResponseHeader("Content-Length")) / 1000f / 1000f, 2)
                            ),
                            requestAsyncOperation.progress
                        );

                        if (result) {
                            request.Abort();
                            throw new OperationCanceledException();
                        }

                        Thread.Sleep(50);
                    }

                    if (request.isNetworkError)
                        throw new Exception($"Network error while downloading {sample.Item1}");

                    if (request.isHttpError)
                        throw new Exception($"HTTP error {request.responseCode} while downloading {sample.Item1}");
                }
            } catch (OperationCanceledException) {
                // Ignored
            } finally {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}

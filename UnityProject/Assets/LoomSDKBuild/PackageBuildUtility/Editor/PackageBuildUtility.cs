using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Loom.Client.Unity.Editor.Build {
    public static class PackageBuildUtility {
        public static List<string> CollectPaths(string[] includedPaths, string[] excludedPaths) {
            if (excludedPaths == null) {
                excludedPaths = new string[0];
            }

            List<string> paths =
                AssetDatabase
                    .FindAssets("t:Object", includedPaths)
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Where(path => !excludedPaths.Any(path.StartsWith))
                    .Distinct()
                    .ToList();

            return paths;
        }

        public static int RunProcess(string path, string arguments = null) {
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo(path);
            process.StartInfo.Arguments = arguments ?? "";
            process.Start();
            process.WaitForExit();
            return process.ExitCode;
        }
    }
}
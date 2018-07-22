using UnityEditor;

namespace Loom.Client.Unity.Editor.Build {
    public static class MenuItems {
        public const string kAssetNameShort = "Loom SDK";
        public const string kMenuRoot = "Tools/[Build] " + kAssetNameShort + "/";

        [MenuItem(MenuItems.kMenuRoot + "Build Package")]
        public static void BuildPackage() {
            PackageBuilder.BuildPackage();
        }

        [MenuItem(MenuItems.kMenuRoot + "Download Samples .zip's")]
        public static void DownloadSamples() {
            SamplesDownloader.DownloadSamples();
        }
    }
}
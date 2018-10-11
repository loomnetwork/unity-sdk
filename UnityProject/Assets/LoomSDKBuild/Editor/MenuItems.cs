using UnityEditor;

namespace Loom.Client.Unity.Editor.Build {
    public static class MenuItems {
        public const string AssetNameShort = "Loom SDK";
        public const string MenuRoot = "Tools/[Build] " + AssetNameShort + "/";

        [MenuItem(MenuRoot + "Build Package")]
        public static void BuildPackage() {
            PackageBuilder.BuildPackage();
        }

        [MenuItem(MenuRoot + "Download Samples .zip's")]
        public static void DownloadSamples() {
            SamplesDownloader.DownloadSamples();
        }
    }
}
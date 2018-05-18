using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;

namespace UnitySwift {
    public static class PostProcessor {
        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath) {
            if(buildTarget == BuildTarget.iOS) {
                // var projPath = PBXProject.GetPBXProjectPath(buildPath);
                var projPath = buildPath + "/Unity-iPhone.xcodeproj/project.pbxproj";
                var proj = new PBXProject();
                proj.ReadFromFile(projPath);

                string targetGuid = proj.TargetGuidByName(PBXProject.GetUnityTargetName());

                //// Configure build settings
                proj.SetBuildProperty(targetGuid, "ENABLE_BITCODE", "NO");
                proj.SetBuildProperty(targetGuid, "SWIFT_OBJC_BRIDGING_HEADER", "Libraries/LoomSDK/ios/LoomSDKSwift-Bridging-Header.h");
				proj.SetBuildProperty(targetGuid, "SWIFT_OBJC_INTERFACE_HEADER_NAME", "LoomSDKSwift.h");
                proj.AddBuildProperty(targetGuid, "LD_RUNPATH_SEARCH_PATHS", "@executable_path/Frameworks");
				proj.AddBuildProperty(targetGuid, "LD_RUNPATH_SEARCH_PATHS", "@executable_path/Libraries/LoomSDK/Frameworks");
				proj.SetBuildProperty(targetGuid, "SWIFT_VERSION", "3.0");

				//frameworks
				DirectoryInfo projectParent = Directory.GetParent(Application.dataPath);
				char divider = Path.DirectorySeparatorChar;
				DirectoryInfo destinationFolder =
					new DirectoryInfo(buildPath + divider + "Frameworks/LoomSDK/Frameworks");

				foreach(DirectoryInfo file in destinationFolder.GetDirectories()) {
					string filePath = "Frameworks/LoomSDK/Frameworks/"+ file.Name;
					//proj.AddFile(filePath, filePath, PBXSourceTree.Source);
					string fileGuid =proj.AddFile(filePath, filePath, PBXSourceTree.Source);
					proj.AddFrameworkToProject (targetGuid, file.Name, false);

					PBXProjectExtensions.AddFileToEmbedFrameworks(proj, targetGuid, fileGuid);

				}
                proj.WriteToFile(projPath);

				//info.plist
				var plistPath = buildPath+ "/Info.plist";
				var plist = new PlistDocument();
				plist.ReadFromFile(plistPath);
				// Update value
				PlistElementDict rootDict = plist.root;
				//rootDict.SetString("CFBundleIdentifier","$(PRODUCT_BUNDLE_IDENTIFIER)");
				PlistElementArray urls = rootDict.CreateArray ("CFBundleURLTypes");
				PlistElementDict dic =  urls.AddDict ();
				PlistElementArray scheme = dic.CreateArray ("CFBundleURLSchemes");
				scheme.AddString (PlayerSettings.applicationIdentifier);
				dic.SetString ("CFBundleURLName", "auth0");
				// Write plist
				File.WriteAllText(plistPath, plist.WriteToString());
            }
        }
    }
}

#endif
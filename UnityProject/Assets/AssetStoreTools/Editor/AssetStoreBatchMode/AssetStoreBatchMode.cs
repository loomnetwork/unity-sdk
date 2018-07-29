using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Thinksquirrel.Development.Integration;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using NDesk.Options;
using Debug = UnityEngine.Debug;

#region Main class
public static class AssetStoreBatchMode
{
    // Input
    static string s_Username;
    static string s_Password;
    static string s_PackageName;
    static string s_RootPath;
    // ReSharper disable once NotAccessedField.Local
    #pragma warning disable 414
    static string[] s_MainAssets; // TODO
    #pragma warning restore 414
    static int s_LoginTimeout;
    static int s_MetadataTimeout;
    static int s_UploadTimeout;
    static bool s_SkipProjectSettings;

    static readonly AssetStorePublisher s_PublisherAccount = new AssetStorePublisher();
    static readonly PackageDataSource s_PackageDataSource = new PackageDataSource();

    static bool s_LoginDone;
    static bool s_GetMetadataDone;
    static bool s_AssetsUploadedDone;
    static readonly Stopwatch s_Stopwatch = new Stopwatch();

    public delegate string[] GuidListPostprocessCallback(string[] guids);
    static GuidListPostprocessCallback s_GuidListPostprocessCallback;

    /// <summary>
    /// Upload a package, using the command line arguments of the current environment.
    /// </summary>
    /// <remarks>
    /// The Asset Store account password must be provided via the "ASSET_STORE_PASSWORD" environment variable.
    /// </remarks>
    public static void UploadAssetStorePackage()
    {
        UploadAssetStorePackage(Environment.GetCommandLineArgs());
    }

    /// <summary>
    /// Upload a package, using the specified command line arguments.
    /// </summary>
    /// <remarks>
    /// The Asset Store account password must be provided via the "ASSET_STORE_PASSWORD" environment variable.
    /// </remarks>
    public static void UploadAssetStorePackage(params string[] args)
    {
        var username = Environment.GetEnvironmentVariable("ASSET_STORE_USERNAME");
        var password = Environment.GetEnvironmentVariable("ASSET_STORE_PASSWORD");
        var packageName = Environment.GetEnvironmentVariable("ASSET_STORE_PACKAGE_NAME");
        var rootPath = Environment.GetEnvironmentVariable("ASSET_STORE_ROOT_PATH");
        var loginTimeout = 10;
        var metadataTimeout = 300;
        var uploadTimeout = 36000;
        var skipProjectSettings = false;

        var mainAssets = new List<string>();

        var mainAssetsStr = Environment.GetEnvironmentVariable("ASSET_STORE_MAIN_ASSETS");
        if (mainAssetsStr != null)
        {
            var mainAssetsSplit = mainAssetsStr.Split(':');
            for(var i = 0; i < mainAssetsSplit.Length; ++i)
            {
                mainAssets.Add(mainAssetsSplit[i]);
            }
        }

        var assets = mainAssets;
        var opt = new OptionSet
        {
            { "asset_store_username=",
                "The username credentials to use for package uploading.",
                o => username = o },

            { "asset_store_package_name=",
                "The package name. The package must be set to draft status in the Publisher Administration.",
                o => packageName = o },

            { "asset_store_root_path=",
                "The root path of the package (relative to Application.dataPath). If not present, use the project Assets folder.",
                o => rootPath = o },

            { "asset_store_main_asset=",
                "A main asset for the package (relative to Application.dataPath). Multiple options are allowed. If not present, do not upload or change any main assets.",
                assets.Add },

            { "asset_store_login_timeout=",
                "The maximum amount of time to wait (in seconds) when logging in. Defaults to 10 seconds. Must be within 2 and 36000 seconds. Login is attempted twice.",
                (int o) => loginTimeout = o },

            { "asset_store_metadata_timeout=",
                "The maximum amount of time to wait (in seconds) when getting metadata. Defaults to 300 seconds. Must be within 2 and 36000 seconds.",
                (int o) => metadataTimeout = o },

            { "asset_store_upload_timeout=",
                "The maximum amount of time to wait (in seconds) when uploading. Defaults to 36000 seconds. Must be within 2 and 36000 seconds.",
                (int o) => uploadTimeout = o },

            { "skip_project_settings",
                "If true, always skip project settings export. This only applies to assets in the Complete Projects category.",
                o => skipProjectSettings = o != null }
        };

        opt.Parse(args);

        UploadAssetStorePackage(username, password, packageName, rootPath, mainAssets.ToArray(), null, loginTimeout, metadataTimeout, uploadTimeout, skipProjectSettings);
    }
    /// <summary>
    /// Upload a package, using the specified options.
    /// </summary>
    /// <param name="username">The username credentials to use for package uploading.</param>
    /// <param name="password">The password credentials to use for package uploading.</param>
    /// <param name="packageName">The package name. The package must be set to draft status in the Publisher Administration.</param>
    /// <param name="rootPath">The root path of the package (relative to Application.dataPath). If null, use the project Assets folder.</param>
    /// <param name="mainAssets">An array of the main assets for the package (relative to Application.dataPath). If null, do not upload or change any main assets.</param>
    /// <param name="loginTimeout">The maximum amount of time to wait (in seconds) when logging in. Defaults to 90 seconds. Must be within 2 and 36000 seconds. Login is attempted twice.</param>
    /// <param name="metadataTimeout">The maximum amount of time to wait (in seconds) when getting metadata. Defaults to 600 seconds. Must be within 2 and 36000 seconds.</param>
    /// <param name="uploadTimeout">The maximum amount of time to wait (in seconds) when uploading. Defaults to 36000 seconds. Must be within 2 and 36000 seconds.</param>
    public static void UploadAssetStorePackage(string username, string password, string packageName, string rootPath = null, string[] mainAssets = null, GuidListPostprocessCallback guidListPostprocessCallback = null, int loginTimeout = 90, int metadataTimeout = 600, int uploadTimeout = 36000, bool skipProjectSettings = false)
    {
        if (string.IsNullOrEmpty(username))
            throw new ArgumentNullException("username");

        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException("password");

        if (string.IsNullOrEmpty(packageName))
            throw new ArgumentNullException("packageName");

        s_Username = username;
        s_Password = password;
        s_PackageName = packageName;
        s_RootPath = rootPath;
        s_MainAssets = mainAssets;
        s_GuidListPostprocessCallback = guidListPostprocessCallback;
        s_LoginTimeout = Mathf.Clamp(loginTimeout, 2, 36000);
        s_MetadataTimeout = Mathf.Clamp(metadataTimeout, 2, 36000);
        s_UploadTimeout = Mathf.Clamp(uploadTimeout, 2, 36000);
		s_SkipProjectSettings = skipProjectSettings;

        Finish();

#if !UNITY_5_5_OR_NEWER
        if (Application.webSecurityEnabled)
        {
            Debug.Log("[Asset Store Batch Mode] Switching from Web Player platform...");

            EditorUserBuildSettings.SwitchActiveBuildTarget(EditorUserBuildSettings.selectedStandaloneTarget);
        }
#endif

        Debug.Log("[Asset Store Batch Mode] Logging into the Asset Store...");

        AssetStoreClient.LoginWithCredentials(s_Username, s_Password, false, OnLogin);

        if (!WaitForUpdate(ref s_LoginDone, s_LoginTimeout))
        {
            Finish();

            // Try again
            s_LoginDone = false;
            AssetStoreClient.LoginWithCredentials(s_Username, s_Password, false, OnLogin);

            if (!WaitForUpdate(ref s_LoginDone, s_LoginTimeout))
            {
                Finish();
                return;
            }
        }

        AssetStoreAPI.GetMetaData(s_PublisherAccount, s_PackageDataSource, OnGetMetadata);

        Debug.Log("[Asset Store Batch Mode] Getting package metadata...");

        if (!WaitForUpdate(ref s_GetMetadataDone, s_MetadataTimeout))
        {
            Finish();
            return;
        }

        var packages = s_PackageDataSource.GetAllPackages();
        var package = packages.FirstOrDefault(p => p.Name == s_PackageName && p.Status == Package.PublishedStatus.Draft);

        if (package == null)
        {
            Debug.LogError("[Asset Store Batch Mode] Draft package: " + s_PackageName + " not found!");
            Finish();
            return;
        }

        // Validate root project folder
        var projectFolder = Path.Combine(Application.dataPath, s_RootPath ?? string.Empty);

        // Convert to unix path style
        projectFolder = projectFolder.Replace("\\", "/");

        if (!IsValidProjectFolder(projectFolder))
        {
            Debug.LogError("[Asset Store Batch Mode] Project folder is invalid");
            Finish();
            return;
        }

        // Set root asset path
        var localRootPath = SetRootPath(package, projectFolder);

        // Set main assets
        if (MainAssetsUtil.CanGenerateBundles)
        {
            // TODO: Set main assets
        }

        // Verify content
        var checkContent = CheckContent(package, localRootPath);
        if (!string.IsNullOrEmpty(checkContent))
        {
            Debug.LogError("[Asset Store Batch Mode] " + checkContent);
            Finish();
            return;
        }

        var draftAssetsPath = GetDraftAssetsPath(localRootPath);

        Export(package, localRootPath, draftAssetsPath);

        // Upload assets
        AssetStoreAPI.UploadAssets(
            package,
            AssetStorePackageController.GetLocalRootGUID(package),
            localRootPath,
            Application.dataPath,
            draftAssetsPath,
            OnAssetsUploaded, null);

        Debug.Log("[Asset Store Batch Mode] Uploading asset...");

        if (!WaitForUpdate(ref s_AssetsUploadedDone, s_UploadTimeout))
        {
            Finish();
            return;
        }

        if (MainAssetsUtil.CanGenerateBundles)
        {
            // TODO: Upload main assets
        }

        Debug.Log("[Asset Store Batch Mode] Asset successfully uploaded");

        Finish();
    }


    static void OnLogin(string errorMessage)
    {
        s_LoginDone = true;

        if (errorMessage == null) return;

        Debug.LogError("[Asset Store Batch Mode] " + errorMessage);
        Finish();
    }

    static void OnGetMetadata(string errorMessage)
    {
        s_GetMetadataDone = true;

        if (errorMessage == null) return;

        Debug.LogError("[Asset Store Batch Mode] " + errorMessage);
        Finish();
    }

    static void OnAssetsUploaded(string errorMessage)
    {
        s_AssetsUploadedDone = true;

        if (errorMessage == null) return;

        Debug.LogError("[Asset Store Batch Mode] " + errorMessage);
        Finish();
    }

    // -----------------------------------------------
    // Helper functions
    // -----------------------------------------------

    static void Finish()
    {
        AssetStoreClient.Logout();
        s_LoginDone = false;
        s_GetMetadataDone = false;
        s_AssetsUploadedDone = false;
        AssetStoreClient.Update();
    }
    static bool WaitForUpdate(ref bool isDone, int timeout)
    {
        s_Stopwatch.Reset();
        s_Stopwatch.Start();

        do
        {
            AssetStoreClient.Update();
            Thread.Sleep(10);
            if (!isDone && s_Stopwatch.Elapsed.TotalSeconds > timeout)
                throw new TimeoutException("Asset Store batch mode operation timed out.");
        } while (!isDone);

        return isDone;
    }
    static string GetDraftAssetsPath(string localRootPath)
    {
        var chars = new[] {(char) 47};
        var fileName = localRootPath.Trim(chars).Replace('/', '_');
        return "Temp/uploadtool_" + fileName + ".unitypackage";
    }
    static void Export(Package package, string localRootPath, string toPath)
    {
        File.Delete(toPath);

        var guids = GetGUIDS(package, localRootPath);
        if (s_GuidListPostprocessCallback != null)
        {
            guids = s_GuidListPostprocessCallback(guids);
        }
        Debug.Log("[Asset Store Batch Mode] Number of assets to export: " + guids.Length);

        var sb = new StringBuilder();
        sb.AppendLine("[Asset Store Batch Mode] Exported asset list:");

        // Note - implementation here differs from Asset Store tools, in order to work properly in batch mode
        var paths = new string[guids.Length];

        for (var i = 0; i < paths.Length; ++i)
        {
            paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            sb.AppendLine(paths[i]);
        }

        Debug.Log(sb.ToString());

        AssetDatabase.ExportPackage(paths, toPath);
    }
    static bool IsValidProjectFolder(string directory)
    {
        return Application.dataPath.Length <= directory.Length &&
                directory.Substring(0, Application.dataPath.Length) == Application.dataPath &&
                Directory.Exists(directory);
    }

    static string SetRootPath(Package package, string path)
    {
        var localRootPath = path.Substring(Application.dataPath.Length);

        if (localRootPath == string.Empty)
            localRootPath = "/";

        package.RootPath = path;

        return localRootPath;
    }

    static string CheckContent(Package package, string localRootPath)
    {
        var errorMessage = string.Empty;
        var disallowedFileTypes = new[]
        {
            ".mb",
            ".ma",
            ".max",
            ".c4d",
            ".blend",
            ".3ds",
            ".jas",
            ".fbm",
            ".dds",
            ".pvr"
        };
        foreach (var guid in GetGUIDS(package, localRootPath))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (var fileType in disallowedFileTypes)
            {
                if (path.EndsWith(fileType))
                {
                    if (errorMessage != string.Empty)
                        errorMessage += "\n";
                    if (fileType == ".fbm")
                        errorMessage += "Disallowed file type:" + fileType +
                                        " - disable embedded media when exporting to .fbx";
                    else if (fileType == "jpg" || fileType == "jpeg")
                        Debug.LogWarning("[Asset Store Batch Mode] It is strongly encouraged to use PNG format instead of JPEG for " + path);
                    else
                        errorMessage += "Disallowed file type: " + errorMessage;
                }
            }
        }
        return errorMessage;
    }
    static string[] GetGUIDS(Package package, string localRootPath)
    {
        var includeProjectSettings = package.IsCompleteProjects && !s_SkipProjectSettings;
        var str1 = "Assets" + (localRootPath ?? string.Empty);
        var chars = new[] { (char)47 };
        var path1 = str1.Trim(chars);
        string[] guidArray = null;
        object[] assetsItemArray = null;

        if (typeof(DebugUtils).Assembly.GetType("UnityEditor.AssetServer") != null)
        {
            assetsItemArray = AssetServer.BuildExportPackageAssetListAssetsItems(AssetServer.CollectAllChildren(AssetDatabase.AssetPathToGUID(path1), new string[0]), true);
        }
        else
        {
            guidArray = Packager.BuildExportPackageAssetListGuids(Packager.CollectAllChildren(AssetDatabase.AssetPathToGUID(path1), new string[0]), true);
        }
        var list = new List<string>();
        var str2 = path1.ToLower();
        if (assetsItemArray != null)
        {
            foreach (var assetsItem in assetsItemArray)
            {
                var assetGuid = assetsItem.GetFieldValue<string>("guid");
                var str3 = AssetDatabase.GUIDToAssetPath(assetGuid).ToLower();
                if (str3.StartsWith("assets/plugins") || str3.Contains("standard assets") || str3.StartsWith(str2))
                    list.Add(assetGuid);
            }
        }
        else
        {
            foreach (var guid in guidArray)
            {
                var str3 = AssetDatabase.GUIDToAssetPath(guid).ToLower();
                if (str3.StartsWith("assets/plugins") || str3.Contains("standard assets") || str3.StartsWith(str2))
                    list.Add(guid);
            }
        }
        if (includeProjectSettings)
        {
            foreach (var path2 in Directory.GetFiles("ProjectSettings"))
            {
                var str3 = AssetDatabase.AssetPathToGUID(path2);
                if (str3.Length > 0)
                    list.Add(str3);
            }
        }
        var array = new string[list.Count];
        list.CopyTo(array);
        return array;
    }
}
#endregion

#region Proxy classes
internal interface IReflectedType
{
    Type GetRuntimeType();
    object GetRuntimeObject();

}

abstract class AssetStoreToolsReflectedType : IReflectedType
{
    Type m_RuntimeType;
    object m_RuntimeObject;

    public Type GetRuntimeType()
    {
        return m_RuntimeType;
    }
    public object GetRuntimeObject()
    {
        return m_RuntimeObject;
    }
    protected void SetRuntimeType(Type value)
    {
        m_RuntimeType = value;
    }
    protected void SetRuntimeObject(object value)
    {
        m_RuntimeObject = value;
    }
    protected AssetStoreToolsReflectedType() {}
    protected AssetStoreToolsReflectedType(bool createObject)
    {
        var typeName = GetType().Name;
        var assembly = typeof (DebugUtils).Assembly;

        SetRuntimeType(assembly.GetType(typeName, true));

        if (createObject)
        {
            SetRuntimeObject(Activator.CreateInstance(m_RuntimeType));
        }
    }
}

internal class AssetStorePublisher : AssetStoreToolsReflectedType
{
    public AssetStorePublisher() : base(true) { }
}

internal class PackageDataSource : AssetStoreToolsReflectedType
{
    public PackageDataSource() : base(true) {}

    public IList<Package> GetAllPackages()
    {
        var packages = GetRuntimeObject().Invoke("GetAllPackages") as IList;

        if (packages == null)
        {
            throw new TargetException("GetAllPackages returned an invalid value");
        }

        var packageList = new List<Package>();

        for (var i = 0; i < packages.Count; ++i)
        {
            packageList.Add(new Package(packages[i]));
        }

        return packageList;
    }
}

internal class Package : AssetStoreToolsReflectedType
{
    public Package(object package) : base(false)
    {
        SetRuntimeObject(package);
    }
    public string Name
    {
        get { return GetRuntimeObject().GetFieldValue<string>("Name"); }
    }
    public PublishedStatus Status
    {
        get { return (PublishedStatus)((int)GetRuntimeObject().GetPropertyValue("Status")); }
    }
    public bool IsCompleteProjects
    {
        get { return GetRuntimeObject().GetFieldValue<bool>("IsCompleteProjects"); }
    }
    public string RootPath
    {
        get { return GetRuntimeObject().GetFieldValue<string>("RootPath"); }
        set { GetRuntimeObject().SetFieldValue("RootPath", value); }
    }

    internal enum PublishedStatus
    {
        Draft,
        Disabled,
        Published,
        PendingReview,
    }
}

internal class AssetStoreClient : AssetStoreToolsReflectedType
{
    static AssetStoreClient s_Instance;

    static AssetStoreClient()
    {
        s_Instance = new AssetStoreClient();
        s_Instance.GetRuntimeType().Assembly.GetType("AssetStoreManager").SetFieldValue("sDbg", true);
    }

    private AssetStoreClient() : base(false) { }

    public static bool LoggedIn()
    {
        return s_Instance.GetRuntimeType().Invoke<bool>("LoggedIn");
    }
    public static void LoginWithCredentials(string username, string password, bool rememberMe, DoneLoginCallback callback)
    {
        Type doneLoginCallbackType;
        var doneLoginCallback = CreateCallbackDelegate("DoneLoginCallback", callback, out doneLoginCallbackType);
        s_Instance.GetRuntimeType().Invoke("LoginWithCredentials", username.Param(), password.Param(), rememberMe.Param(), new Parameter(doneLoginCallback, doneLoginCallbackType));
    }
    public static void Update()
    {
        s_Instance.GetRuntimeType().Invoke("Update");
    }
    public static void Logout()
    {
        s_Instance.GetRuntimeType().Invoke("Logout");
    }

    internal static Delegate CreateCallbackDelegate(string name, Delegate del, out Type type)
    {
        type = s_Instance.GetRuntimeType().GetNestedType(name);
        return del == null ? null : Delegate.CreateDelegate(type, del.Method);
    }
    public delegate void DoneLoginCallback(string errorMessage);
    public delegate void ProgressCallback(double pctUp, double pctDown);
    public delegate void DoneCallback(string errorMessage);
}

internal class AssetStoreAPI : AssetStoreToolsReflectedType
{
    static AssetStoreAPI s_Instance;

    static AssetStoreAPI()
    {
        s_Instance = new AssetStoreAPI();
    }

    private AssetStoreAPI() : base(false) { }

    public static void GetMetaData(AssetStorePublisher publisherAccount, PackageDataSource packageDataSource, DoneCallback callback)
    {
        Type doneCallbackType;
        var doneCallback = CreateCallbackDelegate("DoneCallback", callback, out doneCallbackType);
        s_Instance.GetRuntimeType().Invoke("GetMetaData", publisherAccount.GetRuntimeObject().Param(), packageDataSource.GetRuntimeObject().Param(), new Parameter(doneCallback, doneCallbackType));
    }
    public static void UploadAssets(Package package, string localRootGuid, string localRootPath, string projectPath, string draftAssetsPath, DoneCallback callback, AssetStoreClient.ProgressCallback progressCallback)
    {
        Type doneCallbackType, clientProgressCallbackType;
        var doneCallback = CreateCallbackDelegate("DoneCallback", callback, out doneCallbackType);
        var clientProgressCallback = AssetStoreClient.CreateCallbackDelegate("ProgressCallback", progressCallback, out clientProgressCallbackType);
        s_Instance.GetRuntimeType().Invoke("UploadAssets", package.GetRuntimeObject().Param(), localRootGuid.Param(), localRootPath.Param(), projectPath.Param(), draftAssetsPath.Param(), new Parameter(doneCallback, doneCallbackType), new Parameter(clientProgressCallback, clientProgressCallbackType));
    }

    internal static Delegate CreateCallbackDelegate(string name, Delegate del, out Type type)
    {
        type = s_Instance.GetRuntimeType().GetNestedType(name);
        return del == null ? null : Delegate.CreateDelegate(type, del.Method);
    }
    public delegate void DoneCallback(string errorMessage);
}

internal class AssetStorePackageController : AssetStoreToolsReflectedType
{
    static AssetStorePackageController s_Instance;

    static AssetStorePackageController()
    {
        s_Instance = new AssetStorePackageController();
    }

    private AssetStorePackageController() : base(false) { }

    public static string GetLocalRootGUID(Package package)
    {
        return s_Instance.GetRuntimeType().Invoke<string>("GetLocalRootGUID", package.GetRuntimeObject().Param());
    }
}

internal class AssetServer : AssetStoreToolsReflectedType
{
    static AssetServer s_Instance;

    static AssetServer()
    {
        s_Instance = new AssetServer();
    }

    private AssetServer()
    {
        var assembly = typeof(DebugUtils).Assembly;
        SetRuntimeType(assembly.GetType("UnityEditor.AssetServer", true));
     }

    public static string[] CollectAllChildren(string guid, string[] collection)
    {
        return s_Instance.GetRuntimeType().Invoke<string[]>("CollectAllChildren", guid.Param(), collection.Param());
    }

    public static object[] BuildExportPackageAssetListAssetsItems(string[] guids, bool dependencies)
    {
        return s_Instance.GetRuntimeType().Invoke<object[]>("BuildExportPackageAssetListAssetsItems", guids.Param(), dependencies.Param());
    }

    public static void ExportPackage(string[] guids, string path)
    {
        s_Instance.GetRuntimeType().Invoke("ExportPackage", guids.Param(), path.Param());
    }
}

internal class Packager : AssetStoreToolsReflectedType
{
    static Packager s_Instance;

    static Packager()
    {
        s_Instance = new Packager();
    }

    private Packager()
    {
        var assembly = typeof(DebugUtils).Assembly;
        SetRuntimeType(assembly.GetType("Packager", true));
     }

    public static string[] CollectAllChildren(string guid, string[] collection)
    {
        return s_Instance.GetRuntimeType().Invoke<string[]>("CollectAllChildren", guid.Param(), collection.Param());
    }

    public static string[] BuildExportPackageAssetListGuids(string[] guids, bool dependencies)
    {
        return s_Instance.GetRuntimeType().Invoke<string[]>("BuildExportPackageAssetListGuids", guids.Param(), dependencies.Param());
    }

    public static void ExportPackage(string[] guids, string path)
    {
        s_Instance.GetRuntimeType().Invoke("ExportPackage", guids.Param(), path.Param());
    }
}

#endregion

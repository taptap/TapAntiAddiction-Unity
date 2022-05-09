using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
# if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using System.IO;
using System.Collections.Generic;
using System.Linq;

# if UNITY_IOS
public class BuildPostProcessor
{

    static string[] resourceExts = { ".png", ".jpg", ".jpeg", ".storyboard",".bundle",".json"};

    public static void DeleteDirectory(string target_dir)
    {
        string[] files = Directory.GetFiles(target_dir);
        string[] dirs = Directory.GetDirectories(target_dir);

        foreach (string file in files)
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (string dir in dirs)
        {
            DeleteDirectory(dir);
        }

        Directory.Delete(target_dir, false);
    }


    internal static void CopyAndReplaceDirectory(string srcPath, string dstPath, string[] enableExts)
    {
        if (Directory.Exists(dstPath))
            DeleteDirectory(dstPath);
        if (File.Exists(dstPath))
            File.Delete(dstPath);

        Directory.CreateDirectory(dstPath);

        foreach (var file in Directory.GetFiles(srcPath))
        {
            if (enableExts.Contains(System.IO.Path.GetExtension(file)))
            {
                File.Copy(file, Path.Combine(dstPath, Path.GetFileName(file)));
            }
        }

        foreach (var dir in Directory.GetDirectories(srcPath))
        {
            CopyAndReplaceDirectory(dir, Path.Combine(dstPath, Path.GetFileName(dir)), enableExts);
        }
    }

    public static void GetDirFileList(string dirPath, ref List<string> dirs, string[] enableExts, string subPathFrom = "")
    {
        foreach (string path in Directory.GetFiles(dirPath))
        {
            if (enableExts.Contains(System.IO.Path.GetExtension(path)))
            {
                if (subPathFrom != "")
                {
                    int index = path.IndexOf(subPathFrom);
                    if (index > 0)
                    {
                        dirs.Add(path.Substring(path.IndexOf(subPathFrom)));
                    }
                }
                else
                {
                    dirs.Add(path);
                }
            }
        }

        if (Directory.GetDirectories(dirPath).Length > 0)
        {
            foreach (string path in Directory.GetDirectories(dirPath))
            {
                GetDirFileList(path, ref dirs, enableExts, subPathFrom);
            }
        }

    }

    public static string GetUnityTarget(PBXProject proj)
    {
#if UNITY_2019_3_OR_NEWER
        Debug.Log("UNITY_2019_3_OR_NEWER here");
        string target = proj.GetUnityMainTargetGuid();
        return target;
#endif
        Debug.Log("UNITY_2019_3_OR_NEWER beyond");
        var unityPhoneTarget = proj.TargetGuidByName("Unity-iPhone");
        return unityPhoneTarget;
    }
    
    public static string FilterFileByPrefix(string srcPath, string filterName)
    {
        if (!Directory.Exists(srcPath))
        {
            return null;
        }

        foreach (var dir in Directory.GetDirectories(srcPath))
        {
            string fileName = Path.GetFileName(dir);
            if (fileName.StartsWith(filterName))
            {
                return Path.Combine(srcPath, Path.GetFileName(dir));
            }
        }

        return null;
    }


    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        if (target == BuildTarget.iOS)
        {
            var projectPath = PBXProject.GetPBXProjectPath(path);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);

            // Get targetGUID
#if UNITY_2019_3_OR_NEWER
            string unityFrameworkTarget = project.TargetGuidByName("UnityFramework");
            string targetGUID = GetUnityTarget(project);
#else
                string unityFrameworkTarget = project.TargetGuidByName("Unity-iPhone");
                string targetGUID = project.TargetGuidByName("Unity-iPhone");
#endif
            if (targetGUID == null)
            {
                Debug.Log("target is null ?");
                return;
            }

            // Built in Frameworks
            project.AddFrameworkToProject(targetGUID, "Foundation.framework", false);
            project.AddFrameworkToProject(targetGUID, "libz.tbd", false);

            Debug.Log("Added iOS frameworks to project");

            // Add Shell Script to copy folders and files after running successfully
            

            // Add '-Objc' to "Other Linker Flags"
            project.AddBuildProperty(targetGUID, "OTHER_LDFLAGS", "-Objc");
            project.SetBuildProperty(targetGUID, "SWIFT_VERSION", "5.0");

            Debug.Log("AntiAddiction start add resource to target:" + path + ",dirs:" + Path.Combine(path, "resources"));
            List<string> resources = new List<string>();

            var appDataPath = Application.dataPath;
            var parentFolder = Directory.GetParent(appDataPath).FullName;
            var tdsResourcePath = "";
            var remotePackagePath = FilterFileByPrefix(parentFolder + "/Library/PackageCache/", $"com.tapsdk.antiaddiction@");
            var assetLocalPackagePath = FilterFileByPrefix(parentFolder + "/Assets/", "Plugins");
            var localPackagePath = FilterFileByPrefix(parentFolder, "AntiAddiction");

            if (!string.IsNullOrEmpty(remotePackagePath))
            {
                tdsResourcePath = remotePackagePath + "/Plugins/iOS/resources";
            } else if (!string.IsNullOrEmpty(localPackagePath)){
                tdsResourcePath = localPackagePath + "/Plugins/iOS/resources";
            } else {
                tdsResourcePath = assetLocalPackagePath + "/iOS/resources";
            }

            Debug.Log("tdsResourcePath:" + tdsResourcePath);

            CopyAndReplaceDirectory(tdsResourcePath, Path.Combine(path, "resources"), resourceExts);
            Debug.Log("AntiAddiction resource exists:" + string.Join(", ", resourceExts.ToArray()));
            GetDirFileList(tdsResourcePath, ref resources, resourceExts, "resources");

            foreach (string resource in resources)
            {
                string resourcesBuildPhase = project.GetResourcesBuildPhaseByTarget(targetGUID);
                string resourcesFilesGuid = project.AddFile(resource, resource, PBXSourceTree.Source);
                project.AddFileToBuildSection(targetGUID, resourcesBuildPhase, resourcesFilesGuid);

            }

            // Overwrite
            project.WriteToFile(projectPath);
        }
    }
}
#endif

using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace TapTap.AntiAddiction {
    public class AntiAddictionExporter {
        [MenuItem("TapTap/Export PC Anti-Addiction SDK")]
        static void Export() {
            string path = EditorUtility.OpenFolderPanel("Export path", "", "");
            string[] assetPaths = new string[] {
                "Assets/TapTap/AntiAddiction"
            };
            string exportPath = Path.Combine(path, "pc-anti-addiction.unitypackage");
            AssetDatabase.ExportPackage(assetPaths, exportPath,
                ExportPackageOptions.Recurse);
            Debug.Log("Export done.");
        }
        
        [MenuItem("TapTap/Open Anti-Addiction Save Folder")]
        static void OpenAntiAddictionSaveFolder()
        {
            string folderPath = Path.Combine(Application.persistentDataPath,
                "tap-anti-addiction");
            if (Directory.Exists(folderPath))
            {
                Debug.LogFormat($"{folderPath} ! Directory does not exist!");
                #if UNITY_EDITOR_OSX
                Process.Start( "/usr/bin/open", string.Format($"\"{folderPath}\""));
                #elif UNITY_EDITOR_64
                path = path.Replace("/", "\\");
                Process.Start("Explorer.exe", "/select, \"" +folderPath+ "\"");
                #endif
            }
            //
            else
            {
                Debug.LogFormat($"{folderPath} ! Directory does not exist!");
            }
            
        }
    }
}

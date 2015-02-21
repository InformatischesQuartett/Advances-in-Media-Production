using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CustomBuild : MonoBehaviour
{

    [MenuItem("Build/Windows Build With Postprocess")]
    public static void BuildGame()
    {
        // Get filename.
        string path = EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");
        string[] levels = new string[] {"Assets/Scenes/inputSelector.unity", "Assets/Scenes/default.unity"};

        string[] copyfilestoroot = new string[] {@"Assets/Plugins/x86/cudart32_55.dll", @"Assets/Plugins/x86/opencv_core290.dll", @"Assets/Plugins/x86/opencv_ffmpeg290.dll", @"Assets/Plugins/x86/opencv_gpucodec290.dll", @"Assets/Plugins/x86/opencv_highgui290.dll", @"Assets/Plugins/x86/opencv_imgproc290.dll"};

        // Build player.
        BuildPipeline.BuildPlayer(levels, path + "/AMP.exe", BuildTarget.StandaloneWindows, BuildOptions.None);

        // Copy a file from the project folder to the build folder, alongside the built game.
        for (int i = 0; i < copyfilestoroot.Length; i++)
        {
            FileUtil.CopyFileOrDirectory(copyfilestoroot[i], path + "/" + Path.GetFileName(copyfilestoroot[i]));
        }
    }
}

using UnityEngine;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Callbacks;

/// <summary>
/// Post Processor for webgl builds due to an error in the UnityLoader.js file that searches for OSX version 10.
/// Upon BigSur's release OSX version moved to 11, and the old regex lookup for version loading was broken
/// This can be removed in later versions of unity containing the fix (Assuming it will be in a 2020 version or beyond)
/// </summary>
public class WebGLRegexFixPostBuildProcessor
{
    [PostProcessBuild(100)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.WebGL)
        {
            return;
        }

        string[] filePaths = Directory.GetFiles(pathToBuiltProject, "*.js", SearchOption.AllDirectories);
 
        foreach(string file in filePaths)
        {
            if(file.ToLower().Contains("loader.js"))
            {
                string text = File.ReadAllText(file);
                text = text.Replace(@"Mac OS X (10[\.\_\d]+)", @"Mac OS X (1[\.\_\d][\.\_\d]+)");
                File.WriteAllText(file, text);
                Debug.Log($"Replaced {file} with updated regex");
            }
        }
    }
}

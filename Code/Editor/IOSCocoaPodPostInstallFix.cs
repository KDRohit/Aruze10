using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System.Text;

public class IOSCocoaPodPostInstallFix
{
    //IOSResolver generates the podfile at callback order 40
    //then runs the install at callback order 50.   We must be between these two values
    [PostProcessBuildAttribute(49)]
    public static void OnPostprocessBuild(BuildTarget target, string projectPath)
    {
#if UNITY_IOS
        if (target == BuildTarget.iOS)
        {
            string podFile = projectPath + "/Podfile";
            
            
            if (File.Exists(podFile))
            {
                System.Console.WriteLine($"Updating Podfile for IOS at : " + podFile);
                
                //read file in
                string fileContents = File.ReadAllText(podFile);
                
                //replace use_frameworks with use_modular_headers to static link library (Requires cocoa pods 1.6+)
                fileContents = fileContents.Replace("use_frameworks!", "use_modular_headers!");
                
                //take code signing off individual pods
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(fileContents);
                sb.AppendLine("post_install do |installer|");
                sb.AppendLine("\tinstaller.pods_project.targets.each do |target|");
                sb.AppendLine("\t\ttarget.build_configurations.each do |config|");
                sb.AppendLine("\t\t\tconfig.build_settings['EXPANDED_CODE_SIGN_IDENTITY'] = \"\"");
                sb.AppendLine("\t\t\tconfig.build_settings['CODE_SIGNING_REQUIRED'] = \"NO\"");
                sb.AppendLine("\t\t\tconfig.build_settings['CODE_SIGNING_ALLOWED'] = \"NO\"");
                sb.AppendLine("\t\tend");
                sb.AppendLine("\tend");
                sb.AppendLine("end");
                sb.AppendLine();

                //overwrite file
                File.WriteAllText(podFile, sb.ToString());
            }
            else
            {
                Debug.LogError("Can't update cocoapods pod file -- it doesn't exist.  Expected at: " + podFile);
            }
        }
#endif
    }
}

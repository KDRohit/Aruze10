using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public class SKAdNetworkPlistInjectPostProcess
{
    private const string SKAD_ROOT_KEY = "SKAdNetworkItems";
    private const string SKAD_ID_KEY = "SKAdNetworkIdentifier";
    
    [MenuItem("Zynga/Ad Services/Run SKAdNetworkIneject")]
    public static void openMotdViewer()
    {
        ChangeXcodePlist(BuildTarget.iOS, null);
    }
    
    [PostProcessBuild]
    public static void ChangeXcodePlist(BuildTarget buildTarget, string pathToBuiltProject)
    {
        if (buildTarget != BuildTarget.iOS)
        {
            return;
        }
        
        //we replace our generated info.plist with a replacement file.  Merge our skadnetwork ids into this replacement file
        string rootProjectDirectory = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));
        string plistReplacementFile = rootProjectDirectory + "/info_replace.plist";
        string skadFile = rootProjectDirectory + "/SKAdNetwork_IDs.plist";

        if (!File.Exists(plistReplacementFile))
        {
            Debug.LogError("Can't find plist replacement file at: " + plistReplacementFile);
            return;
        }

        if (!File.Exists(skadFile))
        {
            Debug.LogError("Can't find skad file at: " + skadFile);
            return;
        }

        //read file in
        PlistDocument plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistReplacementFile));
        
        // Get root
        PlistElementDict rootDict = plist.root;
   
        //create skadnetworkitems array
        PlistElementArray skadNodes = null;
        if (!rootDict.values.ContainsKey(SKAD_ROOT_KEY))
        {
            skadNodes = rootDict.CreateArray(SKAD_ROOT_KEY);
        }
        else
        {
            skadNodes = rootDict.values[SKAD_ROOT_KEY].AsArray();
        }

        //copy skadnetwork ids from plist file
        PlistDocument skadPlist = new PlistDocument();
        skadPlist.ReadFromString(File.ReadAllText(skadFile));

        PlistElementDict skadRoot = skadPlist.root;
        if (skadRoot.values.ContainsKey(SKAD_ROOT_KEY))
        {
            PlistElementArray nodesToCopy = skadRoot.values[SKAD_ROOT_KEY].AsArray();
            if (nodesToCopy != null)
            {
                foreach (PlistElement element in nodesToCopy.values)
                {
                    if (element == null)
                    {
                        Debug.LogError("Invalid skad id");
                        continue;
                    }
                    
                    PlistElementDict dict = element.AsDict();

                    if (dict == null || !dict.values.ContainsKey(SKAD_ID_KEY))
                    {
                        Debug.LogError("skad id is in wrong format");
                        continue;
                    }

                    PlistElementDict newNode = skadNodes.AddDict();
                    newNode.SetString(SKAD_ID_KEY, dict.values[SKAD_ID_KEY].AsString());
                }
            }
            else
            {
                Debug.LogError("Could not load skadnetwork ids");
            }
        }
        else
        {
            Debug.LogError("No network ids root node in skadnetwork plist");
        }
        
        // Write to file
        File.WriteAllText(plistReplacementFile, plist.WriteToString());
    }

}

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

/*
 * Enter the list of bundle names you want to build
 * Makes it possible to test new bundles quickly through the "Use Local Bundles" option in Login Setting
 */
 
public class BuildSelectBundles : ScriptableWizard
{
    public string[] bundleNames;
    public string manifestName = "";
    public bool HD = false;
    public bool SD = false;
    public bool uploadBundles = false;

    [MenuItem ("Zynga/BundlesV2/Create Select Bundles")] static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard<BuildSelectBundles>("Create Designated Bundles", "Create Bundles");
    }
 
    public void OnWizardCreate()
    {
        string whatBundles = "";

        if (bundleNames.Length == 0)
        {
            Debug.Log("No bundles listed to build.");
            return;
        }
        
        foreach (string bundleName in bundleNames)
        {
            whatBundles += bundleName + ",";
        }

        List<Variant> variantsToBuild = new List<Variant>();
        if (HD)
        {
            variantsToBuild.Add(Variant.HD);
        }

        if (SD)
        {
            variantsToBuild.Add(Variant.SD);
        }

        if (variantsToBuild.Count == 0)
        {
            Debug.Log("Skipping. No selected bundle variants to build");
            return;
        }

        if (string.IsNullOrEmpty(manifestName))
        {
            Debug.Log("Merging new bundle into current manifest");
        }
        else
        {
            manifestName += "_" + AssetBundleManager.PLATFORM;
            Debug.Log("Creating new manifest: " + manifestName);
        }

        CreateAssetBundlesV2.BuildBundles(SkuId.HIR, EditorUserBuildSettings.activeBuildTarget, variantsToBuild.ToArray(), whatBundles, manifestNameOverride: manifestName);

        if (uploadBundles)
        {
            System.Diagnostics.ProcessStartInfo proc = new System.Diagnostics.ProcessStartInfo();
            proc.FileName = "make";
            proc.WorkingDirectory = Application.dataPath + "/../../";
            proc.Arguments = string.Format("PLATFORM={0} publish_bundles BUILD_TAG=fakejenkins", AssetBundleManager.PLATFORM);
            proc.UseShellExecute = false;
            proc.RedirectStandardOutput = true;
            Process uploadProc = System.Diagnostics.Process.Start(proc);
            if (uploadProc != null)
            {
                string output = uploadProc.StandardOutput.ReadToEnd();
                Debug.Log(output);

                uploadProc.WaitForExit();
            }
            else
            {
                Debug.Log("Proc failed");
            }

        }
    }
}
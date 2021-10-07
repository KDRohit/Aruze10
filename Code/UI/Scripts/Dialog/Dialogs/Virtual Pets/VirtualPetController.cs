using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualPetController : MonoBehaviour, IResetGame
{
    [SerializeField] private Camera petCamera;

    public VirtualPet currentPet{ get; private set; }

    public static VirtualPetController instance { get; private set; }
    
    private const string PET_PREFAB_PATH = "Features/Virtual Pets/Prefabs/Instanced Prefabs/Pet/Dog";
    private const string PET_CONTROLLER_PREFAB_PATH = "Features/Virtual Pets/Prefabs/Instanced Prefabs/Dog Overlay";

    private static bool loading = false;

    void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("Shouldn't be more than 1 pet controller. Destroying 2nd instance");
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void toggleCamera(bool enabled)
    {
        petCamera.enabled = enabled;
    }

    public void showPet(Transform parent)
    {
        toggleCamera(true);
        if (currentPet == null)
        {
            AssetBundleManager.load(this, PET_PREFAB_PATH, petLoadSuccess, petLoadFailed, Dict.create(D.TRANSFORM, parent), isSkippingMapping: true, fileExtension: ".prefab");
        }
        else
        {
            currentPet.transform.SetParent(parent, false);
        }
    }

    public void unloadPet()
    {
        toggleCamera(false);
        Destroy(currentPet);
        //AssetBundleManager.Instance.markBundleForUnloading();
    }

    private void petLoadSuccess(string path, Object obj, Dict args)
    {
        Transform parent = (Transform)args.getWithDefault(D.TRANSFORM, petCamera.transform);
        GameObject petObj = NGUITools.AddChild(parent, obj as GameObject);
        currentPet = petObj.GetComponent<VirtualPet>();
    }
    
    private void petLoadFailed(string path, Dict args)
    {
        Debug.LogWarning("Pet failed to load from: " + path);
    }

    public static void createPetController(Transform parentDialog = null)
    {
        if (instance == null && !loading)
        {
            AssetBundleManager.load(PET_CONTROLLER_PREFAB_PATH, controllerLoadSuccess, controllerLoadFailed, Dict.create(D.TRANSFORM, parentDialog), isSkippingMapping:true, fileExtension:".prefab", blockingLoadingScreen:false);
        }
    }
    
    private static void controllerLoadSuccess(string path, Object obj, Dict args)
    {
        GameObject petParentObj = NGUITools.AddChild(Dialog.instance.transform.parent, obj as GameObject);
        instance = petParentObj.GetComponent<VirtualPetController>();

        Transform parentDialog = (Transform) args.getWithDefault(D.TRANSFORM, null);
        if (parentDialog != null)
        {
            instance.showPet(parentDialog);
        }

        loading = false;
    }
    
    private static void controllerLoadFailed(string path, Dict args)
    {
        loading = false;
        Debug.LogWarning("Pet Controller failed to load from: " + path);
    }
}

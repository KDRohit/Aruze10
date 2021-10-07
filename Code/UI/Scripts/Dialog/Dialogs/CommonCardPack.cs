using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommonCardPack : MonoBehaviour
{
    [SerializeField] private CollectablePack pack;
    
    private const string PACK_PREFAB_PATH = "Features/Common/Collections Packs/Prefabs/Collections Pack Item";

    public void init(string packKey = "", bool useGenericLogo = false, bool grayOutPack = false)
    {
        pack.init(packKey, useGenericLogo, grayOutPack);
    }

    public void grayOutPack()
    {
        pack.grayOut();
    }

    public static void loadCardPack(object caller, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict args = null)
    {
        AssetBundleManager.load(caller, PACK_PREFAB_PATH, successCallback, failCallback, args, isSkippingMapping:true, fileExtension:".prefab");
    }
}

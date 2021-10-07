using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CommonAnimatedTieredChest : TICoroutineMonoBehaviour
{
    [SerializeField] private LabelWrapperComponent headerLabel;
    [SerializeField] private LabelWrapperComponent subHeaderLabel;
    [SerializeField] private LabelWrapperComponent creditsLabel;
    public AnimatedParticleEffect coinParticle;
    [SerializeField] private CollectablePack cardPack;
    public AnimationListController.AnimationInformationList openChestAnimInfo;
    public AnimationListController.AnimationInformationList introChestAnimInfo;
    public AnimationListController.AnimationInformationList outroChestAnimInfo;

    private const string CHEST_PREFABS_PATH = "Features/Common/Prize Chests/Prefabs/Common Animated Chests/Chest {0}";
    
    public enum CHEST_TYPE
    {
        Common = 0,
        Bronze = 1,
        Silver = 2,
        Gold = 3,
        Epic = 4
    }

    public IEnumerator playOpenChest()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(openChestAnimInfo));
    }
    
    public IEnumerator playChestOutro()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(outroChestAnimInfo));
    }
    
    public IEnumerator playChestIntro()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(introChestAnimInfo));
    }

    public void initChest(string headerText = "", string subHeaderText = "", long creditsAmount = 0, bool showPack = false, string packKey = "")
    {
        headerLabel.text = headerText;
        subHeaderLabel.text = subHeaderText;
        creditsLabel.text = CreditsEconomy.convertCredits(creditsAmount);
        cardPack.gameObject.SetActive(showPack);

        if (showPack)
        {
            cardPack.init(packKey, true);
        }
    }

    public static void loadChest(CHEST_TYPE chestType, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict args = null)
    {
        AssetBundleManager.load(string.Format(CHEST_PREFABS_PATH, chestType), successCallback, failCallback, args, isSkippingMapping:true, fileExtension:".prefab");
    }
    
    public static void loadChest(string chestType, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict args = null)
    {
        AssetBundleManager.load(string.Format(CHEST_PREFABS_PATH, chestType), successCallback, failCallback, args, isSkippingMapping:true, fileExtension:".prefab");
    }
    
    public static void loadChest(int chestType, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict args = null)
    {
        CHEST_TYPE chest = (CHEST_TYPE)chestType;
        AssetBundleManager.load(string.Format(CHEST_PREFABS_PATH, chest), successCallback, failCallback, args, isSkippingMapping:true, fileExtension:".prefab");
    }
}

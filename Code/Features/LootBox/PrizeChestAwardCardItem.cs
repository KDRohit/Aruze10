using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrizeChestAwardCardItem : MonoBehaviour
{
    [SerializeField] private GameObject cardPackContainer;
    [SerializeField] private GameObject powerupIconContainer;
    [SerializeField] private GameObject eliteIconContainer;
    [SerializeField] private GameObject richpassIconContainer;
    [SerializeField] private GameObject petsTreatIconContainer;
    [SerializeField] private AnimationListController.AnimationInformationList introAnimList;
    [SerializeField] private CollectablePack collectablePack;
    [SerializeField] private MultiLabelWrapperComponent label;
    [SerializeField] private PowerupTimer powerupTimer;

    public enum PrizeChestAwardCardItemTypes { cardPack,powerup,elite,richPass,pets };

    public void setIcon(PrizeChestAwardCardItemTypes itemType, string keyName, string labelText)
    {
        switch (itemType)
        {
            case PrizeChestAwardCardItemTypes.cardPack:
                cardPackContainer.SetActive(true);
                collectablePack.init(keyName);
                break;
            case PrizeChestAwardCardItemTypes.powerup:
                powerupIconContainer.SetActive(true);
                PowerupBase activePowerup = PowerupsManager.getActivePowerup(keyName);
                if (activePowerup != null)
                {
                    powerupTimer.init(activePowerup, destroyOnExpire: false, clickable:false);
                }
                else
                {
                    Debug.LogError("Active powerup not found for rewarded powerup!");
                }
                break;
            case PrizeChestAwardCardItemTypes.elite:
                eliteIconContainer.SetActive(true);
                break;
            case PrizeChestAwardCardItemTypes.richPass:
                richpassIconContainer.SetActive(true);
                break;
            case PrizeChestAwardCardItemTypes.pets:
                petsTreatIconContainer.SetActive(true);
                break;
        }

        label.text = labelText;
    }

    public void startIntroAnimation()
    {
        StartCoroutine(AnimationListController.playListOfAnimationInformation(introAnimList));
    }

}

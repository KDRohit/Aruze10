using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PowerupCollectionSetUI : CollectableSet
{
    [SerializeField] private UIGrid powerupsGrid;
    [SerializeField] private TextMeshPro streakText;
    [SerializeField] private ButtonHandler infoButton;

    private GameObject powerupContainer;
    private List<PowerupBase> powerups = new List<PowerupBase>();

    private const int NUM_POWERUPS_TO_DISPLAY = 3;

    public override void setup(CollectableSetData dataToUse, UIAtlas atlas = null,
        Dictionary<string, Texture2D> setTextures = null)
    {
        data = dataToUse;
        AssetBundleManager.load(PowerupBase.POWERUP_ICON_CONTAINER_UI_PATH, timerLoadSuccess, timerLoadFailure);
        setCardsCount();
        setButton.registerEventDelegate(onClickSet);
        setStreakText();
        infoButton.registerEventDelegate(onInfoClick);
    }

    private void onInfoClick(Dict args = null)
    {
        DoSomething.now("powerups_ftue");
    }

    private void timerLoadSuccess(string assetPath, object loadedObj, Dict data = null)
    {
        powerupContainer = loadedObj as GameObject;
        for (int i = 0; i < PowerupsManager.activePowerups.Count; i++)
        {
            //only displaying 3 powerups in this panel
            if (i >= NUM_POWERUPS_TO_DISPLAY)
            {
                break;
            }
            
            PowerupBase powerup = PowerupsManager.activePowerups[i];
            if (powerup != null && powerup.isDisplayablePowerup && !powerup.runningTimer.isExpired)
            {
                addPowerup(powerup);
            }
        }

        powerupsGrid.repositionNow = true;
    }
    
    private void timerLoadFailure(string assetPath, Dict data = null)
    {
        Debug.LogError("Failed to load powerup icon at path " + assetPath);
    }

    private void addPowerup(PowerupBase powerup)
    {
        GameObject container = CommonGameObject.instantiate(powerupContainer, powerupsGrid.transform) as GameObject;
        PowerupTimer timerReference = container.GetComponent<PowerupTimer>();
        powerup.runningTimer.registerFunction(onPowerupExpire);
        timerReference.init(powerup);
        powerups.Add(powerup);
    }
    
    private void onPowerupExpire(Dict args, GameTimerRange sender)
    {
        setStreakText();
    }
    
    private void OnDestroy()
    {
        if (powerups != null)
        {
            for (int i = 0; i < powerups.Count; i++)
            {
                if (powerups[i] != null && powerups[i].runningTimer != null)
                {
                    powerups[i].runningTimer.removeFunction(onPowerupExpire);
                }
            }
        }
    }

    private void setStreakText()
    {
        if (PowerupsManager.isPowerupStreakActive)
        {
            streakText.text = Localize.text("powerup_streak_text_active");
        }
        else
        {
            streakText.text = Localize.text("powerup_streak_text_not_active");
        }
    }
    
    protected override void setCardsCount()
    {
        int newCount = 0;
        CollectableCardData cardData;
        int cardsOwned = 0;
        if (!data.isComplete)
        {
            for (int i = 0; i < data.cardsInSet.Count; i++)
            {
                cardData = Collectables.Instance.findCard(data.cardsInSet[i]);
                if (cardData.isNew)
                {
                    newCount++;
                }

                if (cardData.isCollected)
                {
                    cardsOwned++;
                }
            }

            count = cardsOwned + newCount;
        }

        if (completeParent != null && inProgressParent != null)
        {
            completeParent.SetActive(cardsOwned == data.cardsInSet.Count);
            inProgressParent.SetActive(cardsOwned < data.cardsInSet.Count);
			
            //Turn on the new badge if we have new cards but haven't completed the set
            if (newCount > 0 && !completeParent.activeSelf) 
            {
                notifParent.SetActive(true);
                newCardsLabel.text = CommonText.formatNumber(newCount);
            }
        }

        cardCount.text = string.Format("{0}/{1}", cardsOwned, data.cardsInSet.Count);
    }
}

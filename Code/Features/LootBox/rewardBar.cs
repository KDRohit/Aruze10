using UnityEngine;
using TMPro;

public class rewardBar : MonoBehaviour
{
    [Header("Chest")]
    [SerializeField] private GameObject chestParent;
    [SerializeField] private GameObject bronzeChest;
    [SerializeField] private GameObject silverChest;
    [SerializeField] private GameObject goldChest;
    [SerializeField] private GameObject commonChest;
    [SerializeField] private GameObject epicChest;

    [Header("Coin")] 
    [SerializeField] private GameObject coinSprite;
    
    [Header("Chest Name or Coin Amout")]
    [SerializeField] private TextMeshPro chestNameLabel;

    private const string CHEST_NAME_LOC = "chest_name";
    public void showJackPot(MissionReward reward)
    {
        if (reward == null)
        {
            return;
        }

        // We display either loot box or coins
        bool isLootBox = (reward.type == ChallengeReward.RewardType.LOOT_BOX);
        if (chestParent != null)
        {
            chestParent.SetActive(isLootBox);
        }

        if (coinSprite != null)
        {
            coinSprite.SetActive(!isLootBox);
        }

        // Display loot box
        if (isLootBox)
        {
            string imageName = reward.image;
            switch (imageName.ToLower())
            {
                case "common":
                    showChest(commonChest, chestNameLabel, imageName);
                    break;

                case "bronze":
                    showChest(bronzeChest, chestNameLabel, imageName);
                    break;

                case "silver":
                    showChest(silverChest, chestNameLabel, imageName);
                    break;

                case "gold":
                    showChest(goldChest, chestNameLabel, imageName);
                    break;

                case "epic":
                    showChest(epicChest, chestNameLabel, imageName);
                    break;
            }
        }
        // Display coin ammount
        else
        {
            if (chestNameLabel != null)
            {
                chestNameLabel.text = CreditsEconomy.convertCredits(reward.amount);
            }
        }
    }

    private void showChest(GameObject chestObj, TextMeshPro nameLabel, string imageName)
    {
        if (chestObj != null && nameLabel != null)
        {
            chestObj.SetActive(true);
            nameLabel.text = Localize.text(CHEST_NAME_LOC, imageName);;
        }
    }
}

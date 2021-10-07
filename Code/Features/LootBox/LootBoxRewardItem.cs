using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Com.Rewardables;
using Com.Scheduler;
using UnityEngine;

public class LootBoxRewardItem
{
    public enum LootRewardItemType
    {
        none,
        coin,
        cardPack,
        powerup,
        elite,
        richPass,
        pets
    };

    public LootRewardItemType rewardItemType { get; set; }
    public string description {get; set;}
    public long addedValue { get; set; }
    public long oldValue { get; set; }
    public long newValue { get; set; }
    public string keyName { get; set; }
    public JSON jsonData { get; set; }
}

public class LootBoxRewardBundle
{
    private const string OLD_POINTS = "old_points";
    private const string NEW_POINTS = "new_points";
    private const string OLD_VALUE = "old_value";
    private const string NEW_VALUE = "new_value";
    private const string OLD_CREDITS = "old_credits";
    private const string NEW_CREDITS = "new_credits";
    private const string POINTS = "points";
    private const string VALUE = "value";

    private const string REWARD_TYPE = "reward_type";
    private const string TYPE = "type";
    private const string GRANT_DATA = "grant_data";
    
    private const string CARD_PACK_TEXT = "loot_box_card_pack";
    private const string PACK_KEY = "pack_key";
    private const string BUFF_KEYNAME = "buff_keyname";

    private const string EXTRA_PROPS = "extra_props";
    private const string SOURCE = "source";
    private const string FEATURE_NAME = "feature_name";

    private const string IMAGE = "image";
    private const string COMMON = "common";
    
    private const string POWERUP_TEXT = "loot_box_powerup";
    private const string PTS_TEXT = "{0}_points";

    public string Source => source;
    private string source = "";

    public string Rarity => rarity;
    private string rarity = COMMON;

    public List<LootBoxRewardItem> LootBoxRewardItems => lootBoxRewardItems;
    private List<LootBoxRewardItem> lootBoxRewardItems;
    
    private static JSON cardPackData;
    
    public void ParseChallengeRewardBundle(JSON data)
    {
        Debug.LogError(data.ToString());


        if (data.getString(FEATURE_NAME, "").Equals(LootBoxFeature.LOOT_BOX))
        {
            JSON displayPropsJson = data.getJSON(EXTRA_PROPS);
            if (displayPropsJson != null)
            {
                rarity = displayPropsJson.getString(IMAGE, COMMON);
                source = displayPropsJson.getString(SOURCE, "");
            }

            lootBoxRewardItems = new List<LootBoxRewardItem>();

            JSON[] grantedEventsArray = data.getJsonArray("granted_events");
            foreach (JSON entry in grantedEventsArray)
            {
                LootBoxRewardItem rewardItem = new LootBoxRewardItem();
                lootBoxRewardItems.Add(rewardItem);

                if (entry.getString(TYPE, "").Equals("coin_reward"))
                {
                    rewardItem.rewardItemType = LootBoxRewardItem.LootRewardItemType.coin;
                    rewardItem.addedValue = entry.getLong("added_value", 0);
                    rewardItem.oldValue = entry.getLong(OLD_VALUE, 0);
                    rewardItem.newValue = entry.getLong(NEW_VALUE, 0);
                }
                else if (entry.getString(TYPE, "").Equals("collectibles_pack_reward"))
                {
                    rewardItem.rewardItemType = LootBoxRewardItem.LootRewardItemType.cardPack;
                    rewardItem.keyName = entry.getString(PACK_KEY, "");
                    rewardItem.description = Localize.text(CARD_PACK_TEXT);
                    rewardItem.jsonData = entry;
                }
                else if (entry.getString(TYPE, "").Equals("reward_granted"))
                {
                    JSON grantData = entry.getJSON(GRANT_DATA);
                    string typeString = grantData.getString(REWARD_TYPE, "").ToLower();

                    switch (typeString)
                    {
                        case "buff":
                            rewardItem.rewardItemType = LootBoxRewardItem.LootRewardItemType.powerup;
                            rewardItem.keyName = grantData.getString(BUFF_KEYNAME, "");
                            rewardItem.description = Localize.text(POWERUP_TEXT);
                            rewardItem.jsonData = grantData;
                            break;
                        case "elite_pass_points":
                            rewardItem.rewardItemType = LootBoxRewardItem.LootRewardItemType.elite;
                            rewardItem.addedValue = grantData.getLong(POINTS, 0);
                            rewardItem.oldValue = grantData.getLong(OLD_POINTS, 0);
                            rewardItem.newValue = grantData.getLong(NEW_POINTS, 0);
                            rewardItem.description = Localize.text(PTS_TEXT, rewardItem.addedValue);
                            rewardItem.jsonData = grantData;
                            break;
                        case "pass_points":
                            rewardItem.rewardItemType = LootBoxRewardItem.LootRewardItemType.richPass;
                            rewardItem.addedValue = grantData.getLong(VALUE, 0);
                            rewardItem.oldValue = grantData.getLong(OLD_POINTS, 0);
                            rewardItem.newValue = grantData.getLong(NEW_POINTS, 0);
                            rewardItem.description = Localize.text(PTS_TEXT, rewardItem.addedValue);
                            break;
                        case "pets": //TODO - Add in v2
                            rewardItem.rewardItemType = LootBoxRewardItem.LootRewardItemType.pets;
                            break;

                    }
                }
            }
        }
    }

    public void ParseRichPassRewardBundle(JSON data)
    {
        JSON displayPropsJson = data.getJSON(EXTRA_PROPS);
        if (displayPropsJson != null)
        {
            rarity = displayPropsJson.getString(IMAGE, COMMON);
            source = displayPropsJson.getString(SOURCE, "");
        }

        lootBoxRewardItems = new List<LootBoxRewardItem>();

        JSON[] rewardablesArray = data.getJsonArray("rewardables");
        foreach (JSON entry in rewardablesArray)
        {
            LootBoxRewardItem rewardItem = new LootBoxRewardItem();
            lootBoxRewardItems.Add(rewardItem);

            string typeString = entry.getString(REWARD_TYPE, "").ToLower();

            switch (typeString)
            {
                case "coin":
                    rewardItem.rewardItemType = LootBoxRewardItem.LootRewardItemType.coin;
                    rewardItem.addedValue = entry.getLong(VALUE, 0);
                    rewardItem.oldValue = entry.getLong(OLD_CREDITS, 0);
                    rewardItem.newValue = entry.getLong(NEW_CREDITS, 0);
                    break;
                case "buff":
                    rewardItem.rewardItemType = LootBoxRewardItem.LootRewardItemType.powerup;
                    rewardItem.keyName = entry.getString(BUFF_KEYNAME, "");
                    rewardItem.description = Localize.text(POWERUP_TEXT);
                    rewardItem.jsonData = entry;
                    break;

                case "elite_pass_points":
                    rewardItem.rewardItemType = LootBoxRewardItem.LootRewardItemType.elite;
                    rewardItem.addedValue = entry.getLong(POINTS, 0);
                    rewardItem.oldValue = entry.getLong(OLD_POINTS, 0);
                    rewardItem.newValue = entry.getLong(NEW_POINTS, 0);
                    rewardItem.description = Localize.text(PTS_TEXT, rewardItem.addedValue);
                    rewardItem.jsonData = entry;
                    break;
                case "pass_points":
                    rewardItem.rewardItemType = LootBoxRewardItem.LootRewardItemType.richPass;
                    rewardItem.addedValue = entry.getLong(VALUE, 0);
                    rewardItem.oldValue = entry.getLong(OLD_POINTS, 0);
                    rewardItem.newValue = entry.getLong(NEW_POINTS, 0);
                    rewardItem.description = Localize.text(PTS_TEXT, rewardItem.addedValue);
                    break;
                case "collectible_pack":
                    rewardItem.rewardItemType = LootBoxRewardItem.LootRewardItemType.cardPack;
                    rewardItem.keyName = entry.getString(PACK_KEY, "");
                    rewardItem.description = Localize.text(CARD_PACK_TEXT);
                    rewardItem.jsonData = entry;
                    break;
            }
        }
    }

    public Dictionary<string, JSON> getLookBoxCardPackDrops()
    {
        if (lootBoxRewardItems == null)
        {
            return null;
        }
        
        LootBoxRewardItem cardPackDrop = lootBoxRewardItems.Find((o)=>
        {
            return o.rewardItemType == LootBoxRewardItem.LootRewardItemType.cardPack;
        });

        if (cardPackDrop == null || cardPackDrop.jsonData == null)
        {
            return null;
        }

        return cardPackDrop.jsonData.getStringJSONDict("pack_dropped_events");
    }
}
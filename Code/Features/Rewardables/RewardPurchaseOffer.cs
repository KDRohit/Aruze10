using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.Rewardables;

public class RewardPurchaseOffer : Rewardable
{
    public CreditPackage package { get; private set; }
    public int boardGameDice { get; private set; }
    public string cardPackKey { get; set; }
    
    /// <inheritdoc/>
    public override void init(JSON data)
    {
        this.data = data;
		
        JSON perks = data.getJSON("extra_perks");
        int salePercent = 0;
        if (perks != null)
        {
            boardGameDice = perks.getInt("bgDice", 0);
            salePercent = perks.getInt("salePercent", 0);
        }
		
        string packageKey = data.getString("purchase_offer_key", "");
        PurchasablePackage purchasePackage = PurchasablePackage.find(packageKey);
        if (purchasePackage != null)
        {
            package = new CreditPackage(purchasePackage, salePercent, false);
        }
    }

    /// <inheritdoc/>
    public override string type
    {
        get { return "purchase_offer"; }
    }

    public RewardPurchaseOffer getClone()
    {
        return this.MemberwiseClone() as RewardPurchaseOffer;
    }
}
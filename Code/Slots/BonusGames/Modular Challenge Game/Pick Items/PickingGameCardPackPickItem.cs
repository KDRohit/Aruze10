using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickingGameCardPackPickItem : PickingGameBasePickItemAccessor
{
    [SerializeField] private CollectablePack cardPack;
    public void setCardPack(string packKey)
    {
        //Just incase when the pick is revealed, we want to show the actaul pack rarity of the pack picked
        if (cardPack != null)
        {
            cardPack.init(packKey, true);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PurchasePerksDrawer : MonoBehaviour
{
    [SerializeField] private UICenteredGrid iconsCenteredGrid;
    [SerializeField] private UIGrid iconsGrid;
    [SerializeField] private GameObject iconContainerPrefab;
    [SerializeField] private UISprite spriteContainer;

    public ButtonHandler closeButton;

    private bool needsToCheckBounds = false;
    
    public void addObjectToGrid(GameObject objectToAdd, string containerSwapperState, CreditPackage package = null, int packageIndex = -1, bool isPurchase = false, RewardPurchaseOffer offer = null)
    {
        GameObject container;
        if (iconsGrid != null)
        {
            container = NGUITools.AddChild(iconsGrid.transform, iconContainerPrefab);
        }
        else if (iconsCenteredGrid != null)
        {
            container = NGUITools.AddChild(iconsCenteredGrid.transform, iconContainerPrefab);
        }
        else
        {
            container = NGUITools.AddChild(transform, iconContainerPrefab);
        }

        PurchasePerksIconContainer objectContainer = container.GetComponent<PurchasePerksIconContainer>();
        objectContainer.containerSwapper.setState(containerSwapperState);
        
        GameObject perkIconObj = NGUITools.AddChild(objectContainer.iconParent, objectToAdd);
        if (package != null)
        {
            PurchasePerksIcon perkIcon = perkIconObj.GetComponent<PurchasePerksIcon>();
            perkIcon.init(package, packageIndex, isPurchase, offer);
        }

        needsToCheckBounds = true;
    }

    public void checkBounds()
    {
        if (spriteContainer != null && needsToCheckBounds && iconsCenteredGrid != null)
        {
            Vector3[] corners = NGUIMath.CalculateWidgetCorners(spriteContainer); //In order UR, BR, BL, UL
            
            float cellHeightBuffer = iconsCenteredGrid.cellHeight * iconsCenteredGrid.transform.lossyScale.y; //Extra buffer based on cell height. Adjusting by lossyScale to convert to world units
            float halfCellHeighBuffer = cellHeightBuffer / 2;
            
            // Find out if the bottom part of perk panel/drawer goes over bottom bound
            float bottomBound = corners[1].y;
            float gridBottom = iconsCenteredGrid.getBottomBound();
            float bottomDifference = (bottomBound + halfCellHeighBuffer) - gridBottom;

            // Find out if the close button on the perk panel/drawer goes over top bound.
            // NOTE: closeButton.transform.position.y has correct value here after the above
            //       iconsCenteredGrid.getBottomBound() call which invokes iconsCenteredGrid.reAnchor() to get close button at proper position.
            float topBound = corners[0].y;
            float topDifference = closeButton.transform.position.y - topBound;

            // Make sure close button will not go over the top bound first, then make sure the panel bottom will not go below bottom bound.
            bool overTop = topDifference > 0;
            float difference = overTop ? topDifference : bottomDifference;
            if (difference > 0)
            {
                // If over the top bound, we move it down by difference + halfCellHeighBuffer which is a little buffer so that we can see complete close button
                // If over the bottome bound, we move it up by difference + halfCellHeighBuffer which is a little buffer so that we can see complete bottom part of panel
                Vector3 amountToOffset = overTop ? new Vector3(0, -(difference + halfCellHeighBuffer), 0) : new Vector3(0, difference + halfCellHeighBuffer, 0);
                iconsCenteredGrid.gameObject.transform.position += amountToOffset;
                iconsCenteredGrid.adjustBackgroundPosition(amountToOffset);
            }

            needsToCheckBounds = false; //Should only need to recheck these when the grid has new objects added to it
        }
    }
}
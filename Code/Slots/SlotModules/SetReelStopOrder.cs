using UnityEngine;

// Hybrid game setup follows ReelGame.StopInfo[][],
// could be used for games that has a jagged array of stopOrder with different numbers of stop infos (Ainsworth04FreeSpins)
// or just hybrid reel games (Ainsworth14FreeSpins). 
// Non hybrid game setup mimic real reel looks, used for regular (Batman01FreeSpins), independent (Vip01FreeSpinGame), and layered (Batman01) games. 
// 
// Author : Xueer Zhu xzhu@zynga.com
// Date : Nov 10th 2020
//
public class SetReelStopOrder : SlotModule
{
    // Reel Type Info
    [SerializeField] private bool isHybridGame;
    [SerializeField] private int stopOrderSize;
    [SerializeField] private int cols;  // number of reels, col=0 is at the left
    [SerializeField] private int rows; // rows per reel, row=0 is at the bottom
    [SerializeField] private int layers; 
    [SerializeField] private bool isIndependentReelGame;
    [SerializeField] private bool isLayeredGame;
    
    // Reel Layout Info
    [SerializeField] private int hybridStopOrderListSize;
    [SerializeField] private int[] stopIndexArray;
    [SerializeField] private Vector3Int[] hybridStopArray;
    [SerializeField] private int[] hybridStopJaggedArraySize;
    
    // Stop Order
    private ReelGame.StopInfo[][] stopOrder;
    
    public override bool needsToSetReelStopOrder()
    {
        return true;
    }
    
    public override ReelGame.StopInfo[][] setReelStopOrder()
    {
        // parse text input according to reel game type, apply to below 3 cases
        // regular reels, not layered independent reels, and layered regular and layered independent
        if (!isHybridGame)
        {
            SetNonHybridStopOrder();
        }
        else
        {
            SetHybridStopOrder();
        }

        return stopOrder;
    }

    private void SetHybridStopOrder()
    {
        stopOrder = new ReelGame.StopInfo[hybridStopOrderListSize][];
        int hybridIndex = 0;
        for (int listIndex = 0; listIndex < hybridStopOrderListSize; listIndex++)
        {
            stopOrder[listIndex] = new ReelGame.StopInfo[hybridStopJaggedArraySize[listIndex]];
            for (int arrayIndex = 0; arrayIndex < hybridStopJaggedArraySize[listIndex]; arrayIndex++)
            {
                Vector3Int currentStop = hybridStopArray[hybridIndex];
                stopOrder[listIndex][arrayIndex] = new ReelGame.StopInfo(currentStop.x, currentStop.y, currentStop.z);
                hybridIndex++;
            }
        }
    }

    private void SetNonHybridStopOrder()
    {
        stopOrder = new ReelGame.StopInfo[stopOrderSize][];
        int stopIndex = 0;
        for (int layer = 0; layer < layers; layer++)
        {
            for (int col = 0; col < cols; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    int currentStopOrder = stopIndexArray[stopIndex];
                    if (currentStopOrder > 0 && currentStopOrder <= stopOrderSize)
                    {
                        stopOrder[currentStopOrder - 1] = new ReelGame.StopInfo[] {new ReelGame.StopInfo(col, rows - row - 1, layer)};
                    }

                    stopIndex++;
                }
            }
        }
    }
}

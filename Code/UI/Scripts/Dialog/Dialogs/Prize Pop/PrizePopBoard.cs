using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrizePop;
using Com.Rewardables;

public class PrizePopBoard : MonoBehaviour
{
    [SerializeField] private GameObject[] pickObjectPrefabs;
    [SerializeField] private PrizePopBoardRound[] boardRounds;
    
    public GameObject[] generateBoard(int roundIndex, Transform[] containerParents, int columns, List<PrizePopFeature.PrizePopPickData> previousPicks)
    {
        List<int> previousPickedIndices = new List<int>(previousPicks.Count);
        for (int i = 0; i < previousPicks.Count; i++)
        {
            previousPickedIndices.Add(previousPicks[i].pickedIndex);
        }
        int rows = containerParents.Length / columns;
        PrizePopBoardRound currentBoardRound = boardRounds[roundIndex];
        
        GameObject[] currentBoardPicks = new GameObject[currentBoardRound.boardItems.Length];
        
        if (currentBoardPicks.Length == previousPickedIndices.Count)
        {
            previousPickedIndices.Clear();
            Debug.LogErrorFormat("Prize Pop number of board items matches number of previous picks. Check configuration for round {0}", roundIndex);
        }
        
        for (int i = 0; i < currentBoardRound.boardItems.Length; i++)
        {
            int containerIndex = rows * (int)currentBoardRound.boardItems[i].location.x + (int)currentBoardRound.boardItems[i].location.y;
            if (previousPickedIndices.Contains(i))
            {
                continue; //Don't instantiate an object for an index we've already picked
            }
            
            currentBoardPicks[i] = NGUITools.AddChild(containerParents[containerIndex], pickObjectPrefabs[currentBoardRound.boardItems[i].objectToUse]);
        }

        return currentBoardPicks;
    }

    [System.Serializable]
    private class PrizePopBoardRound
    {
        public PrizePopPickItemContainer[] boardItems;
    }
    
    [System.Serializable]
    private class PrizePopPickItemContainer
    {
        public int objectToUse;
        public Vector2 location;
    }
}



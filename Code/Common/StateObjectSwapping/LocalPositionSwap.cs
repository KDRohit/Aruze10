using System.Collections.Generic;
using UnityEngine;
using System.Text;

[System.Obsolete("Animation list should be used instead of object swapper")]
public class LocalPositionSwap : ObjectSwap
{
    [SerializeField] protected Transform objectToMove;
    [SerializeField] protected List<LocalPositionState> swapperStates;
    
    /// <summary>
    /// Repositions the localPosition of the target transform based on the state
    /// </summary>
    public override void swap(string state)
    {
        if (objectToMove == null)
        {
            objectToMove = transform;
        }

        if (objectToMove != null && swapperStates != null)
        {
            for (int i = 0; i < swapperStates.Count; ++i)
            {
                if (swapperStates[i].state == state)
                {
					if (swapperStates[i].useRelativePosition)
					{
						objectToMove.localPosition += swapperStates[i].localPosition; //offsets the position of the object by a given value instead of setting it to that value
					}
					else
					{
						objectToMove.localPosition = swapperStates[i].localPosition; //sets the position of the object to the given value
					}
                }
            }
        }
    }

    public override string ToString()
    {
        if (swapperStates != null)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < swapperStates.Count; ++i)
            {
                sb.Append(swapperStates[i].state);
                if (i < swapperStates.Count - 1)
                {
                    sb.Append(",");
                }
            }

            return sb.ToString();
        }

        return "";
    }
}

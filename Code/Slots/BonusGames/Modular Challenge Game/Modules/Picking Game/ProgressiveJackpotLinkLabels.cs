using System.Collections.Generic;
using UnityEngine;


/*
 * For linking a LabelWrapper to a ProgressiveJackpot value and is updated by
 * the progressive jackpot data from the server. The label registers OnEnable and
 * unregisters the label OnDisable
 *
 * first used: zynga06 picking game
 */
public class ProgressiveJackpotLinkLabels : MonoBehaviour
{
    [Tooltip("Must be the same amount of labels and jackpot key names")]
    [SerializeField] private LabelWrapperComponent[] labels;
    [SerializeField] private string[] jackpotKeyNames;

    private readonly List<ProgressiveJackpot> jackpots = new List<ProgressiveJackpot>(); 
        
    private void OnEnable()
    {
        if (labels.Length != jackpotKeyNames.Length)
        {
            Debug.LogError("labels and jackpotKeyNames count must be the same.");
        }
        
        for(int i = 0; i < jackpotKeyNames.Length; i++)
        {
            ProgressiveJackpot jackpot = ProgressiveJackpot.find(jackpotKeyNames[i]);

            if (jackpot == null)
            {
                Debug.LogError("Couldn't find Progressive Jackpot: " + jackpotKeyNames[i]);
            }
            jackpots.Add(jackpot);
            jackpots[i].registerLabel(labels[i].labelWrapper);
        }
    }

    public void setValueAndUnlink(string value)
    {
        for(int i = 0; i < jackpotKeyNames.Length; i++)
        {
            jackpots[i].unregisterLabel(labels[i].labelWrapper);
            labels[i].labelWrapper.text = value;
        }
    }

    private void OnDisable()
    {
        for(int i = 0; i < jackpotKeyNames.Length; i++)
        {
            jackpots[i].unregisterLabel(labels[i].labelWrapper);
        }

        jackpots.Clear();
    }
}

using System.Collections.Generic;
using UnityEngine;
using Com.HitItRich.Feature.VirtualPets;

public class VirtualPetsDialogTabTricks : VirtualPetsDialogTab
{
    [SerializeField] private SerializableDictionaryTrickTypeToObject trickTypeObjects;
    [SerializeField] private UIGrid panelGrid;
    
    public enum TRICK_TYPE
    {
        ROLLOVER_RESPIN,
        FETCH_DB
    }
    
    public override void init(VirtualPet pet)
    {
        if (!isInitialized)
        {
            foreach (KeyValuePair<TRICK_TYPE, VirtualPetsTrickPanel> kvp in trickTypeObjects)
            {
                kvp.Value.init(kvp.Key);
                kvp.Value.panelButton.registerEventDelegate(treatPanelClicked, Dict.create(D.TYPE, kvp.Key.ToString().ToLower()));
            }
        }
        base.init(pet);
    }

    private void treatPanelClicked(Dict args = null)
    {
        string trickType = (string) args.getWithDefault(D.TYPE, "");
        StatsManager.Instance.LogCount("dialog", "pet", "tricks", trickType, "", "click", VirtualPetsFeature.instance.currentEnergy);
    }
    
    [System.Serializable] private class KeyValuePairOfTrickTypeToObject : CommonDataStructures.SerializableKeyValuePair<TRICK_TYPE, VirtualPetsTrickPanel> {}
    [System.Serializable] private class SerializableDictionaryTrickTypeToObject : CommonDataStructures.SerializableDictionary<KeyValuePairOfTrickTypeToObject, TRICK_TYPE, VirtualPetsTrickPanel> {}

}

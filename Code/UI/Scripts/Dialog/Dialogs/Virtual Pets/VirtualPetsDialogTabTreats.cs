using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.Feature.VirtualPets;
using UnityEngine;

public class VirtualPetsDialogTabTreats : VirtualPetsDialogTab
{
    [SerializeField] private GameObject panelPrefab;
    [SerializeField] private UIGrid panelGrid;

    public override void init(VirtualPet pet)
    {
        if (!isInitialized)
        {
            base.init(pet);
            CampaignDirector.FeatureTask[] tasks = VirtualPetsFeature.instance.getTreatTasks();
            for (int i = 0; i < tasks.Length; i++)
            {
                createTreatPanel(tasks[i]);
            }
        }
    }

    private void createTreatPanel(CampaignDirector.FeatureTask task)
    {
        if (task != null)
        {
            GameObject panel = NGUITools.AddChild(panelGrid.gameObject.transform, panelPrefab);
            VirtualPetsTreatPanel treatPanel = panel.GetComponent<VirtualPetsTreatPanel>();
            treatPanel.init(task, playerPet);
        }
    }

}

using System;
using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.Feature.VirtualPets;
using UnityEngine;

public class VirtualPetsDialogTabMyPet : VirtualPetsDialogTab
{
    [SerializeField] private UIInput nameInput;
    [SerializeField] private LabelWrapperComponent buttonLabel;

    public override void init(VirtualPet pet)
    {
        base.init(pet);
        nameInput.text = VirtualPetsFeature.instance.petName;
    }

    public void OnHideKeyboard(UIInput input)
    {
        buttonLabel.text = Localize.text("rename");

        //Save Dog Name
        if (!string.IsNullOrEmpty(input.text))
        {
            VirtualPetsFeature.instance.setDogName(input.text);
            StartCoroutine(playerPet.playNameChangedAnimations());
        }

        //If they didn't type anything and we don't have the default name go back to showing the previous name
        if (string.IsNullOrEmpty(input.text) && VirtualPetsFeature.instance.petName != Localize.text(VirtualPetsFeature.PET_DEFAULT_NAME_LOC))
        {
            input.text = VirtualPetsFeature.instance.petName;
        }

        StatsManager.Instance.LogCount("dialog", "pet", "my_pet", "edit_name", VirtualPetsFeature.instance.petName, "confirm", VirtualPetsFeature.instance.currentEnergy);
    }

    public void OnShowKeyboard(UIInput input)
    {
        buttonLabel.text = Localize.text("confirm");
        nameInput.text = VirtualPetsFeature.instance.petName == Localize.text(VirtualPetsFeature.PET_DEFAULT_NAME_LOC) ? "" : VirtualPetsFeature.instance.petName;
    }

    public void OnBecameVisible()
    {
        StatsManager.Instance.LogCount("dialog", "pet", "my_pet", "edit_name", VirtualPetsFeature.instance.petName, "view",
            VirtualPetsFeature.instance.currentEnergy);
    }

    /*
     * UIInput assumes these functions exist. Not implementing them causes a MissingMethod exception
     */
    
    public void OnInputChanged(UIInput input)
    {
    }
    
    public void OnInputChanged(string text)
    {
    }
}

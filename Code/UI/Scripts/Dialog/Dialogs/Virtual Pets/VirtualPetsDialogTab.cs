using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualPetsDialogTab : TICoroutineMonoBehaviour
{
    public StateImageButtonHandler tabButton;
    [SerializeField] private GameObject contentParent;
    
    protected VirtualPet playerPet;

    protected bool isInitialized = false; 
    public virtual void init(VirtualPet pet)
    {
        isInitialized = true;
        contentParent.SetActive(true);
        tabButton.stateImageButton.SetSelected(true);
        playerPet = pet;
    }

    public virtual void hideTab()
    {
        contentParent.SetActive(false);
        tabButton.stateImageButton.SetSelected(false);
    }
}

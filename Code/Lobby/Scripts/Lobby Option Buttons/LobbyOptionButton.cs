using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
/**
Controls UI behavior of a menu option button in the main lobby.

Here is the class hierarchy:

LobbyOptionButton (abstract)
	LobbyOptionButtonComingSoon
	LobbyOptionButtonActive (abstract)
		LobbyOptionButtonPremium
		LobbyOptionButtonVIP
		LobbyOptionButtonLockable (abstract)
			LobbyOptionButtonGeneric
			LobbyOptionButtonMysteryGift
			LobbyOptionButtonProgressive
*/

public abstract class LobbyOptionButton : TICoroutineMonoBehaviour
{
	[SerializeField] public GameObject[] objectsToHideWhenOffsecreen;

	[System.NonSerialized] public int page;
	[System.NonSerialized] public LobbyOption option = null;

	protected bool isSetup = false;
	private Transform parentTransform;
	private Transform currentTransform;
	
	protected virtual void Update()
	{
		if (gameObject == null)
		{
			//if the dialog has already been dismissed don't run
			return;
		}
		
		if (isSetup && objectsToHideWhenOffsecreen != null && objectsToHideWhenOffsecreen.Length > 0)
		{
			if (parentTransform != null && currentTransform != null)
			{
				if (parentTransform.localPosition.x + currentTransform.localPosition.x <= -1400)
				{
					for (int i = 0; i < objectsToHideWhenOffsecreen.Length; i++)
					{
						GameObject currentObj = objectsToHideWhenOffsecreen[i];
						if (currentObj != null && currentObj.activeSelf)
						{
							currentObj.SetActive(false);
						}
					}
				}
				else if (parentTransform.localPosition.x + currentTransform.localPosition.x > -1400)
				{
					if (objectsToHideWhenOffsecreen.Length > 0)
					{
						for (int i = 0; i < objectsToHideWhenOffsecreen.Length; i++)
						{
							GameObject currentObj = objectsToHideWhenOffsecreen[i];
							if (currentObj != null && !currentObj.activeSelf)
							{
								currentObj.SetActive(true);
							}
						}
					}
				}
			}
		}
	}

	// Overload for lobby styles that don't use the same kind of lobby paging as the HIR main lobby,
	// so most of the arguments are ignored.
	public void setup(LobbyOption option)
	{
		setup(option, -1, 0, 0);
	}
	
	// Overload for lobby styles that don't need a page, but do need width and height.
	public void setup(LobbyOption option, float width, float height)
	{
		setup(option, -1, width, height);
	}

	public virtual void setup(LobbyOption option, int page, float width, float height)
	{
		// width and height is provided for possible use in overrides, but isn't used in this base method.
		this.option = option;
		this.page = page;
		if (option != null)
		{
			option.button = this;
		}
		TextMeshPro[] textMeshes = GetComponentsInChildren<TextMeshPro>();
		if (textMeshes != null && MainLobby.hirV3 != null)
		{
			MainLobby.hirV3.masker.addObjectArrayToList(textMeshes);
		}

		currentTransform = transform;
		parentTransform = currentTransform.parent;

		isSetup = true;
	}

	public virtual void setupLTL(LobbyOption option) 
	{
	}
		
	protected void setSelectable(bool isSelectable)
	{
		Collider[] colliders = gameObject.GetComponentsInChildren<Collider>();

		foreach (Collider col in colliders) 
		{
			col.enabled = isSelectable;
		}
	}

	// enable/disable the bottom 'stand' mesh of the slot machine
	public void enableSlotMachineBaseMesh(bool bEnable)
	{
		GameObject slotBaseMesh = CommonGameObject.findChild(this.gameObject, "Base");
		if (slotBaseMesh != null)
		{
			slotBaseMesh.SetActive(bEnable);
		}
	}
		
	protected virtual void OnClick()
	{
		if (option != null)
		{
			option.click();	
		}
		
	}

	/// Force a refresh of some visible element, initially going to be used to control 
	/// lock icons on options that need to be displayed or hidden based on using the old or new wager system
	public virtual void refresh()
	{
		// handle refreshing something if data can change at runtime while the option is visible
	}

	public virtual void reset()
	{
		// handle refreshing something if data can change at runtime while the option is visible
	}
}

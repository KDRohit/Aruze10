using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NetworkAvatarSelect : MonoBehaviour
{
	private SocialMember member;
	public SlideController slideController;
	public UIGrid profileGrid;
	public ImageButtonHandler backButton;
	public ImageButtonHandler saveButton;
	public GameObject panelPrefab;
	public SlideContent slideContent;
	public MeshRenderer selectedPicture;

	
	private NetworkProfile profile;
	private NetworkProfileEditor editor;
	private List<NetworkAvatarSelectPanel> panels;
	private string selectedUrl;
	
	public void OnEnable()
	{
	    profileGrid.Reposition();
	}

	public void onView()
	{
		StatsManager.Instance.LogCount("dialog", "ll_profile", "edit_photo", "view", "", member.networkID.ToString());
	}
	
	public void init(SocialMember member, NetworkProfileEditor editor)
	{
		this.member = member;
		this.profile = member.networkProfile;
		this.editor = editor;

		// Setting the top image.
		RoutineRunner.instance.StartCoroutine(SlotsPlayer.instance.socialMember.setPicOnRenderer(selectedPicture));

		// Setting up the scrolling panels				
		GameObject panelObject;
		NetworkAvatarSelectPanel panel;
		
		panels = new List<NetworkAvatarSelectPanel>();
		bool isSelected = false;

		List<string> availableUrls = null;
		if (NetworkProfileFeature.instance.avatarList != null)
		{
			availableUrls = new List<string>(NetworkProfileFeature.instance.avatarList);
		}
		else
		{
			availableUrls = new List<string>();
		}
		
		if (SlotsPlayer.isFacebookUser && SlotsPlayer.instance.socialMember != null)
		{
			string facebookURL = FacebookMember.getImageUrl(SlotsPlayer.instance.socialMember.id, 512);
			if (!string.IsNullOrEmpty(facebookURL))
			{
				availableUrls.Insert(0, facebookURL);
			}
			else
			{
				Debug.LogErrorFormat("NetworkAvatarSelect.cs -- init() -- user is facebook connected but the generated facebook URL is empty...");
			}

		}

		CommonGameObject.destroyChildren(profileGrid.gameObject); // Make sure to kill all the children before we have more.
		if (availableUrls != null && panelPrefab != null)
		{
			for (int i = 0; i < availableUrls.Count; i++)
			{
				string url = availableUrls[i];
				bool isFacebook = url.Contains("facebook") && i == 0;
				// Now make each of the panels from the urls.
				panelObject = CommonGameObject.instantiate(panelPrefab, Vector3.zero, Quaternion.identity, profileGrid.transform) as GameObject;
				if (panelObject == null)
				{
					Debug.LogErrorFormat("NetworkAvatarSelect.cs -- init -- created object was null, continuing.");
					continue;
				}
				panel =	panelObject.GetComponent<NetworkAvatarSelectPanel>();
				if (panel != null)
				{
					isSelected = (member.photoSource.getUrl(PhotoSource.Source.PROFILE) == url);
					panel.init(url, this, isSelected, isFacebook);
					panels.Add(panel);
				}
				else
				{
					Debug.LogErrorFormat("NetworkAvatarSelect.cs -- init -- no NetworkAvatarSelectPanel on this created object.");
				}
			}
		}
		else if (panelPrefab == null)
		{
			Debug.LogErrorFormat("NetworkAvatarSelect.cs -- init -- panelPrefab was null somewhow even though its linked in the inspector...");
		}
		profileGrid.maxPerLine = (panels.Count + 1)/ 2; // Add one to make sure its a max of two lines.
		profileGrid.Reposition();

		// Setup the slide content
		if (availableUrls != null)
		{
			slideContent.width = availableUrls.Count * profileGrid.cellWidth;
			slideController.setBounds(-(slideController.transform.localPosition.x + slideContent.width/2), slideContent.width);
		}
		else
		{
			slideController.setBounds(-profileGrid.cellWidth, profileGrid.cellWidth);
		}

		backButton.registerEventDelegate(backClicked);
		saveButton.registerEventDelegate(saveClicked);
	}

	public void fade(bool isFading)
	{
		if (panels != null)
		{
			for (int i = 0; i < panels.Count; i++)
			{
				if (panels[i] != null)
				{
					panels[i].fade(isFading);
				}
			}
		}
	}
	
    private void backClicked(Dict args = null)
	{
		RoutineRunner.instance.StartCoroutine(editor.avatarBackAnimation());
		// Clear out the selection when we close it.
		selectedUrl = "";		
	}
	
	private void saveClicked(Dict args = null)
	{
		// Tell the editor the new url.
		editor.selectPicture(selectedUrl);
		RoutineRunner.instance.StartCoroutine(editor.avatarSavedAnimation());
		// Clear out the selection when we close it.
		selectedUrl = "";
	}

	public void selectPicture(NetworkAvatarSelectPanel panel)
	{
		// Unselect all the other ones.
		for (int i = 0; i < panels.Count; i++)
		{
			if (panels[i] != panel)
			{
				panels[i].isSelected = false;
			}
		}
		selectedUrl = panel.url;
		// Update the top photo
		selectedPicture.material = panel.profileRenderer.sharedMaterial;
	}
}

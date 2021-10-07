using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;
using Com.States;

public class NetworkProfileEditor : NetworkProfileTabBase
{
	public MeshRenderer animationPictureRenderer; // Used during the animation transition to edit.
	public MeshRenderer pictureRenderer;
	public UIInput statusInput;
	public UIInput nameInput;
	public UIInput locationInput;
	public ToggleHandler maleToggle;
	public ToggleHandler femaleToggle;

	public ImageButtonHandler saveButton;
	public ImageButtonHandler cancelButton;
	public ImageButtonHandler changeAvatarButton;

	public UISprite statusInputBackground;
	public UISprite nameInputBackground;
	public UISprite locationInputBackground;

	public NetworkAvatarSelect avatarSelector;
	public GameObject textEditorParent;
	public GameObject saveAnimationParent;

	[HideInInspector] public NetworkProfileDialog dialog;
	public GameObject editingInputPosition;

	public TextMeshPro editProfileTitle;

	private NetworkProfile profile;
	private string photoUrl;
	private string gender;
	private StateMachine stateMachine;

	private const string INPUT_SELECTED_SPRITE = "GoldFrame";
	private const string INPUT_UNSELECTED_SPRITE = "SilverFrame";
	private const string SAVED_FANFARE_KEY = "ProfileSavedFlourishLL";

	private const string SAVED_ANIMATION = "saved";
	private const string AVATAR_OUTRO_ANIMATION = "edit avatar to editor";
	private const string AVATAR_INTRO_ANIMATION = "to edit avatar intro";
	private const string AVATAR_SAVED_ANIMATION = "edit avatar saved";
	private const string EDIT_PROFILE = "edit profile";
	private const string EDIT_IMAGE = "edit image";

	public override IEnumerator onIntro(NetworkProfileDialog.ProfileDialogState fromState, string extraData = "")
	{
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_profile",
			phylum: "edit_profile",
			klass: "view",
			family: "",
			genus: member.networkID.ToString());
			editProfileTitle.enabled = true;
			yield return null;
	}

	public override IEnumerator onOutro(NetworkProfileDialog.ProfileDialogState toState, string extraData = "")
	{
		editProfileTitle.enabled = false;
		yield return null;
	}

	public void init(SocialMember member, MeshRenderer profilePic, NetworkProfileDialog dialog)
	{
		this.member = member;
		this.profile = member.networkProfile;
		this.dialog = dialog;

		setInputText(nameInput, profile.name, Localize.textTitle("whats_your_name_q"));
		setInputText(statusInput, profile.status, Localize.textTitle("whats_on_your_mind_q"));
		setInputText(locationInput, profile.location, Localize.textTitle("where_are_you_from_q"));

		gender = profile.gender;
		photoUrl = member.photoSource.getPrimaryUrl(true);

		maleToggle.init(onMaleClicked, null, true);
		maleToggle.setToggle(gender == "male");
		femaleToggle.init(onFemaleClicked, null, true);
		femaleToggle.setToggle(gender == "female");

		DisplayAsset.loadTextureToRenderer(pictureRenderer, photoUrl);
		DisplayAsset.loadTextureToRenderer(animationPictureRenderer, photoUrl);
		saveButton.registerEventDelegate(saveClicked);
		changeAvatarButton.registerEventDelegate(changeAvatarClicked);
		cancelButton.registerEventDelegate(cancelClicked);
		editProfileTitle.enabled = true;

		avatarSelector.init(member, this);// Initialize this now so that we start downloading any textures and they are ready.
		stateMachine = new StateMachine("network_profiles_editor");
		stateMachine.addState(EDIT_PROFILE);
		stateMachine.addState(EDIT_IMAGE);
		stateMachine.updateState(EDIT_PROFILE);
	}

	private void setInputText(UIInput input, string text, string defaultText)
	{
		input.text = defaultText; // Calling this here so that the UIInput Init() will get called.

		// Setting these here so that we ensure they have the correct value,
		// regardless of what the initial values in the prefab are.
		
	    input.defaultText = defaultText;
		input.defaultColor = Color.gray;
		
		if (!string.IsNullOrEmpty(text))
		{
		    input.text = text;
		    input.tmPro.color = Color.white;
		}
		else
		{
			// This needs to be called again to force the UIInput to color it properly at load.
			// Otherwise it functions properly once it gets editted.
		    input.text = defaultText;
		    input.tmPro.color = Color.gray;
		}
	}

	public void OnShowKeyboard(UIInput input)
	{
#if !UNITY_EDITOR && !UNITY_WEBGL
		// Center the screen on the desired input.
		float yDistance = editingInputPosition.transform.position.y - input.transform.position.y;
		Vector3 dialogPosition = dialog.transform.position;
		dialogPosition.y += yDistance;
		dialog.transform.position = dialogPosition;
#endif
	}

	public void OnHideKeyboard(UIInput input)
	{
#if !UNITY_EDITOR && !UNITY_WEBGL
		// Reset the dialog to normal position.
		dialog.transform.localPosition = Vector3.zero;
#endif
	}

	public void OnInputChanged(UIInput input)
	{
	}

	public void OnInputChanged(string text)
	{
		// This sometimes gets fired, and since its passing a string through we
		// don't know what to update, so do nothing.
	}

	public void saveClicked(Dict args = null)
	{
		Dictionary<string, string> updates = new Dictionary<string, string>();
		bool nameHasChanged = profile.name != nameInput.text;
		bool statusHasChanged = profile.status != statusInput.text;
		bool locationHasChanged = profile.location != locationInput.text;

		string newName = nameInput.text != nameInput.defaultText ? nameInput.text : "";
		string newStatus = statusInput.text != statusInput.defaultText ? statusInput.text : "";
		string newLocation = locationInput.text != locationInput.defaultText ? locationInput.text : "";
		
		if (nameHasChanged)
		{
			updates.Add("name", newName);
		}
		
		if (statusHasChanged)
		{
			updates.Add("status", newStatus);
		}

		if (locationHasChanged)
		{
			updates.Add("location", newLocation);
		}

		updates.Add("photo_url", photoUrl);
		updates.Add("gender", gender);

		// Send up the update network action.
		NetworkProfileAction.updateProfile(profile.networkID, updates, updateCallback);

		// Update the local profile.
		if (nameHasChanged)
		{
			profile.name = newName;
		}
		
		if (statusHasChanged)
		{
			profile.status = newStatus;
		}

		if (locationHasChanged)
		{
			profile.location = newLocation;
		}
		member.photoSource.setUrl(photoUrl, PhotoSource.Source.PROFILE);

		profile.gender = gender;
		if (LobbyLoader.lastLobby == LobbyInfo.Type.VIP && ExperimentWrapper.VIPLobbyRevamp.isInExperiment)
		{
			VIPLobbyHIRRevamp vipRevamp = VIPLobbyHIRRevamp.instance;
			if (vipRevamp != null)
			{
				vipRevamp.refreshUI();
				vipRevamp.benefitsDialog.refresh();
			}
		}
		
		dialog.profileDisplay.setupProfile();
		dialog.switchState(NetworkProfileDialog.ProfileDialogState.PROFILE_DISPLAY, "saved");
		StatsManager.Instance.LogCount("dialog", "ll_profile", "edit_profile", "save", "", profile.networkID.ToString());
		stateMachine.updateState(EDIT_PROFILE);
	}

	public void cancelClicked(Dict args = null)
	{
		if (photoUrl != member.photoSource.getPrimaryUrl())
		{
			// Even if they hit cancel, if they changed the profile picture we want to update it.
			Dictionary<string, string> updates = new Dictionary<string, string>();
			updates.Add("photo_url", photoUrl);
			member.photoSource.setUrl(photoUrl, PhotoSource.Source.PROFILE);
			NetworkProfileAction.updateProfile(profile.networkID, updates, updateCallback);
			dialog.profileDisplay.setupProfile();
		}

		editProfileTitle.enabled = false;

		dialog.switchState(NetworkProfileDialog.ProfileDialogState.PROFILE_DISPLAY);
		stateMachine.updateState(EDIT_PROFILE);
	}

	public void changeAvatarClicked(Dict args = null)
	{
		if (stateMachine.can(EDIT_PROFILE))
		{
			stateMachine.updateState(EDIT_IMAGE);
			// Load Avatar Select ModeavatarIntroAnimation();
			RoutineRunner.instance.StartCoroutine(avatarIntroAnimation());
		}
	}

	public IEnumerator avatarIntroAnimation()
	{
		avatarSelector.init(member, this);
		avatarSelector.fade(true);
		yield return RoutineRunner.instance.StartCoroutine(CommonAnimation.playAnimAndWait(animator, AVATAR_INTRO_ANIMATION));
	}

	public IEnumerator avatarBackAnimation()
	{
		stateMachine.updateState(EDIT_PROFILE);
		avatarSelector.fade(false);
		yield return RoutineRunner.instance.StartCoroutine(CommonAnimation.playAnimAndWait(animator, AVATAR_OUTRO_ANIMATION));
	}

	public IEnumerator avatarSavedAnimation()
	{
		stateMachine.updateState(EDIT_PROFILE);
		avatarSelector.fade(false);		
		yield return RoutineRunner.instance.StartCoroutine(CommonAnimation.playAnimAndWait(animator, AVATAR_SAVED_ANIMATION));
		yield return RoutineRunner.instance.StartCoroutine(CommonAnimation.playAnimAndWait(animator, "intro"));
	}	
	
	public void selectPicture(string url)
	{
		if (photoUrl != url && url != null)
		{
			StatsManager.Instance.LogCount("dialog", "ll_profile", "edit_photo", "save", grabStatNameFromUrl(url), profile.networkID.ToString());
			// If the url changed, then set the displayed pic to be the newly selected one.
			DisplayAsset.loadTextureToRenderer(pictureRenderer, url, "", true);
			DisplayAsset.loadTextureToRenderer(animationPictureRenderer, url, "" ,true); // Also update the animation one.
		}
		// Set the edit data to reflect this change.
		if (url != null)
		{
			photoUrl = url;
		}
	}

	private string grabStatNameFromUrl(string url)
	{
		string result = "";
		int keyIndex = url.IndexOf("Avatars_");
		if (keyIndex > 0)
		{
			result = url.Substring(keyIndex).Replace(".png", "").ToLower();;
		}
		else
		{
			result = "fb_photo";
		}
		return result;
	}

	private void onMaleClicked(Dict args = null)
	{
		if (maleToggle.isToggled)
		{
			// If it was already on, then we are turning it off so set it to off and gender to nothing.
			maleToggle.setToggle(false);
			gender = "";
		}
		else
		{
			// If we are turning this on, then turn off the female.
			femaleToggle.setToggle(false);
			maleToggle.setToggle(true);
			gender = "male";
		}
	}

	private void onFemaleClicked(Dict args = null)
	{
		if (femaleToggle.isToggled)
		{
			// If it was already on, then we are turning it off so set it to off and gender to nothing.
			femaleToggle.setToggle(false);
			gender = "";
		}
		else
		{
			// If we are turning this on, then turn off the male.
			maleToggle.setToggle(false);
			femaleToggle.setToggle(true);
			gender = "female";

		}
	}

	public static void updateCallback(JSON data)
	{
		Debug.LogFormat("NetworkProfileEdit.cs -- updateCallback -- recieved JSON: {0}", data.ToString());
		if (data.getBool("success", false))
		{
			// Huzzah
		}
		else
		{
			Scheduler.addDialog("generic", Dict.create(D.TITLE, "FAILURE", D.MESSAGE, "Sorry, we could not update your profile"));
		}

	}
}

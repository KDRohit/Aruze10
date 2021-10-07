using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Class for easily adding sharing to dialogs, facebook opengraph and native sharing is supported
best examples on use is to look at BigWinDialogSIR.cs or SweeetSurprizeDialog.cs
*/
public class SharingButtons : MonoBehaviour 
{
	public ButtonHandler facebookFeedButton;
	public ButtonHandler nativeShareButton;
	public ButtonHandler closeButton;			// optional, usuually users only see facebook and native buttons and dialog does not close after using them
	public UIInput messageInput;				// the little blue text box in the dialog that lets users type in body text default text is set by UILabelStaticText
	public GameObject originalUI;				// if user is not in native sharing experiment then we use this UI instead, usually it's a "Share" button
	public GameObject iosUI;					// ios specific UI
	public GameObject androidUI;				// android specific UI
	public DialogBase myDialog;					// the dialog this component is attached to
	public bool	isOneShotFacebookButton = false; // if true the facebook button will be hidden if used
	public FadeInAnOut faceBookButtonFader;		//  component to fade out one shot facebook button

	public string subjectScatId = "";			// SCAT id of sharing subject, such as email subject
	public string eventName = "";				// optional name of event that gets inserted into subject 
	public string bodyScatId = "";				// SCAT id of sharing body, such as email body
	public string statFamily = "";				// stat tracking family for dialog gui click stat		

	private const float  FADE_TIME = 1.0f;
	private const float  LABEL_TWEEN_TIME = 1.5f;
	private const float  TWEEN_TIME = 1.5f;

	public string userInputMessage
	{
		get
		{
			string postMessage = "";

			if (messageInput != null && messageInput.text != messageInput.defaultText)
			{
				postMessage = messageInput.text;
			}

			return postMessage;		
		}
	}	

	public void init(ButtonHandler.onClickDelegate facebookDelegate = null, ButtonHandler.onClickDelegate nativeShareDelegate = null, ButtonHandler.onClickDelegate closeDelegate = null) 
	{
		// check if in experiment, unfortunatly if we are not we have to revert to the old look, this can be removed when 100% ramped I hope
		if (Sharing.isAvailable)
		{
			// make sure we are active
			gameObject.SetActive(true);

			if (facebookFeedButton != null)
			{
				if (SlotsPlayer.isFacebookUser)
				{
					setButtonDelegate(facebookFeedButton, facebookDelegate);

					setButtonDelegate(facebookFeedButton, facebookClickTrack);

					if (isOneShotFacebookButton)
					{
						setButtonDelegate(facebookFeedButton, fadeButtonHandler);
					}
				}
				else if (nativeShareButton != null)
				{
					// hide the facebook button if they aren't logged in
					facebookFeedButton.gameObject.SetActive(false);					

					// center the share button which is the only button now
					CommonTransform.setX(nativeShareButton.gameObject.transform, 0.0f);
				}
			}

			setButtonDelegate(nativeShareButton, nativeShareDelegate);
			setButtonDelegate(nativeShareButton, nativeClickTrack);

			setButtonDelegate(closeButton, closeDelegate);

			initPlatfromUI();
		}
		else
		{
			gameObject.SetActive(false);     // deactivate ourselves
		}

		checkUI();

	}

	public void update()
	{
		if (messageInput != null && messageInput.selected)
		{
			myDialog.resetIdle();
		}
	}

	public void checkUI()
	{
		if (originalUI != null)
		{
			originalUI.SetActive(!Sharing.isAvailable);
		}		
	}

	public void setButtonDelegate(ButtonHandler handler, ButtonHandler.onClickDelegate clickDelegate)
	{
		if (handler != null && clickDelegate != null)
		{
			handler.registerEventDelegate(clickDelegate);
		}
	}

	public void fadeButtonHandler(Dict args = null)
	{
		StartCoroutine(fadeOutFacebookButton());
	}

	private IEnumerator fadeOutFacebookButton()
	{
		if (faceBookButtonFader != null)
		{
			faceBookButtonFader.init();
			yield return new WaitForSeconds(FADE_TIME);
			
			iTween.ScaleTo(facebookFeedButton.gameObject,
				iTween.Hash(
					"scale", new Vector3(0.6f, 0.8f, 1f),
					"time", LABEL_TWEEN_TIME,
					"easetype", "easeOutBounce"
			));			
			yield return new WaitForSeconds(LABEL_TWEEN_TIME);

			facebookFeedButton.gameObject.SetActive(false);
			if (nativeShareButton != null)
			{
				iTween.MoveTo(nativeShareButton.gameObject, iTween.Hash("x", 0.0f, "islocal", true, "time", TWEEN_TIME, "easetype", iTween.EaseType.easeInOutQuad));
			}
		}	

		yield return null;	
	}

	public void nativeClickTrack(Dict args = null)
	{
		StatsManager.Instance.LogCount("dialog", statFamily, StatsManager.getGameTheme(), StatsManager.getGameName(), "click", "share_sheet_button");
	}	

	public void facebookClickTrack(Dict args = null)
	{
		StatsManager.Instance.LogCount("dialog", statFamily, StatsManager.getGameTheme(), StatsManager.getGameName(), "click", "facebook_button");
	}		

	public void nativeShareClick(Dict args = null)
	{
		myDialog.cancelAutoClose();
		Sharing.shareGameEventWithScreenShot(subjectScatId, eventName, userInputMessage, bodyScatId, statFamily);
	}

	private void initPlatfromUI()
	{
		iosUI.SetActive(false);
		androidUI.SetActive(false);

#if UNITY_IPHONE
		iosUI.SetActive(true);
#elif UNITY_ANDROID
		androidUI.SetActive(true);
#endif
	}	

	public void shareWithGameName(string gameName)
	{
		Sharing.shareGameEventWithScreenShot(subjectScatId, eventName, userInputMessage, bodyScatId, statFamily, gameName);
	}		

}

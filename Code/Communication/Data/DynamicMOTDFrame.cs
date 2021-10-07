using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class DynamicMOTDFrame : MonoBehaviour
{
	private int frameNumber;
	public JSON frameJSON; // uneeded?
	public ButtonHandler closeButtonReference;
	public Vector2 frameSize = Vector2.zero;
	private static int startTimeStamp = 0;
	private static int endTimeStamp = 0;

	private List<ButtonHandler> buttonCache; // An ask fro UI was to have buttons fade in rather than clip in. So we need to cache money them

	public void setMOTDTimeStamps(int startTime, int endTime)
	{
		startTimeStamp = startTime;
		endTimeStamp = endTime;
	}

	public void drawFrame(int frameNumberToUse, JSON jsonToUse = null, Texture2D[] downloadedTextures = null, Vector2 spriteBackgroundSize = new Vector2())
	{
		buttonCache = new List<ButtonHandler>();

		// We're using frame number to pull textures out of the downloaded textures, 
		frameNumber = frameNumberToUse;
		int objectIterator = 0;
		Vector3 reusableVector = new Vector3();
		GameObject loadingTarget;
		Dictionary<string, GameObject> cachedObjects = DynamicMOTDFeature.instance.cachedObjects;

		if (cachedObjects == null)
		{
			cachedObjects = new Dictionary<string, GameObject>();
		}

		JSON objectSpecificJSON;

		if (jsonToUse != null)
		{
			frameJSON = jsonToUse;
		}

		if (frameJSON == null)
		{
			Debug.LogError("Frame json was null");
		}

		if (cachedObjects == null)
		{
			cachedObjects = new Dictionary<string, GameObject>();
		}

		// Create Text
		while (frameJSON.hasKey("text_" + objectIterator))
		{
			objectSpecificJSON = frameJSON.getJSON("text_" + objectIterator);

			if (!cachedObjects.ContainsKey(DynamicMOTDFeature.LABEL_NAME))
			{
				cachedObjects[DynamicMOTDFeature.LABEL_NAME] = SkuResources.getObjectFromMegaBundle<GameObject>(DynamicMOTDFeature.LABEL_NAME);
			}

			// Don't instantiate just add to a relevant list
			loadingTarget = CommonGameObject.instantiate(cachedObjects[DynamicMOTDFeature.LABEL_NAME], transform) as GameObject;
			TextMeshPro textToManipulate = loadingTarget.GetComponent<TextMeshPro>();

			Color newColor;
			if (ColorUtility.TryParseHtmlString("", out newColor))
			{
				textToManipulate.faceColor = newColor;
			}

			textToManipulate.text = objectSpecificJSON.getString("text", "");

			textToManipulate.transform.localPosition = CommonTransform.setObjectLocationWithJSON(objectSpecificJSON, reusableVector); 
			textToManipulate.rectTransform.sizeDelta = CommonTransform.setObjectScaleWithJSON(objectSpecificJSON, reusableVector);

			objectIterator++;
		}

		// Create Images
		objectIterator = 0;
		while (frameJSON.hasKey("image_" + objectIterator))
		{
			if (!cachedObjects.ContainsKey(DynamicMOTDFeature.RENDERER_NAME))
			{
				cachedObjects[DynamicMOTDFeature.RENDERER_NAME] = SkuResources.getObjectFromMegaBundle<GameObject>(DynamicMOTDFeature.RENDERER_NAME);
			}

			objectSpecificJSON = frameJSON.getJSON("image_" + objectIterator);

			loadingTarget = CommonGameObject.instantiate(cachedObjects[DynamicMOTDFeature.RENDERER_NAME], gameObject.transform) as GameObject;

			UITexture imageToManipulate = loadingTarget.GetComponent<UITexture>();

			// Map the paths to the loaded renderers somehow. 
			imageToManipulate.transform.localPosition = CommonTransform.setObjectLocationWithJSON(objectSpecificJSON, reusableVector); 
			imageToManipulate.transform.localScale = CommonTransform.setObjectScaleWithJSON(objectSpecificJSON, reusableVector);

			// If this iamge has greater area then what we know about, it's probably what we should consider a background
			if (frameSize.x * frameSize.y < imageToManipulate.transform.localScale.x * imageToManipulate.transform.localScale.y)
			{
				frameSize.x = imageToManipulate.transform.localScale.x;
				frameSize.y = imageToManipulate.transform.localScale.y;
			}

			if (downloadedTextures != null && downloadedTextures.Length > frameNumber + objectIterator)
			{
				imageToManipulate.material.mainTexture = downloadedTextures[frameNumber + objectIterator];
			}
			else
			{
				Bugsnag.LeaveBreadcrumb("Could not access image at downloaded texture index " + (frameNumber + objectIterator));
			}

			objectIterator++;
		}

		// Create buttons
		objectIterator = 0;
		Color alphaSetting;
		while (frameJSON.hasKey("button_" + objectIterator))
		{
			if (!cachedObjects.ContainsKey(DynamicMOTDFeature.BUTTON_NAME))
			{
				cachedObjects[DynamicMOTDFeature.BUTTON_NAME] = SkuResources.getObjectFromMegaBundle<GameObject>(DynamicMOTDFeature.BUTTON_NAME);
			}

			objectSpecificJSON = frameJSON.getJSON("button_" + objectIterator);

			loadingTarget = CommonGameObject.instantiate(cachedObjects[DynamicMOTDFeature.BUTTON_NAME], gameObject.transform) as GameObject;

			ButtonHandler buttonToManipulate = loadingTarget.GetComponent<ButtonHandler>();
			buttonCache.Add(buttonToManipulate);

			string buttonText = objectSpecificJSON.getString("text", "Missing Text");
			buttonToManipulate.text = buttonText;
		
			if (frameNumber > 0)
			{
				buttonToManipulate.setAllAlpha(0);
			}
			else
			{
				buttonToManipulate.setAllAlpha(1);
			}

			buttonToManipulate.transform.localPosition = CommonTransform.setObjectLocationWithJSON(objectSpecificJSON, reusableVector); 
			buttonToManipulate.transform.localScale = CommonTransform.setObjectScaleWithJSON(objectSpecificJSON, reusableVector);
			Dict args = Dict.create(D.ACTIVE, objectSpecificJSON.getString("action", ""));
			buttonToManipulate.registerEventDelegate(onClickButton, args);
			objectIterator++;
		}

		if (frameJSON.hasKey("timer"))
		{
			objectSpecificJSON = frameJSON.getJSON("timer");
			int yLocation = objectSpecificJSON.getInt("y", 0);

			string TIMER_TO_LOAD = DynamicMOTDFeature.TIMER;
			if (spriteBackgroundSize.y > 0 && spriteBackgroundSize.y / 2 < yLocation)
			{
				TIMER_TO_LOAD = DynamicMOTDFeature.TIMER_ATTACHED;
			}

			if (!cachedObjects.ContainsKey(TIMER_TO_LOAD))
			{
				cachedObjects[TIMER_TO_LOAD] = SkuResources.getObjectFromMegaBundle<GameObject>(TIMER_TO_LOAD);
			}

			loadingTarget = CommonGameObject.instantiate(cachedObjects[TIMER_TO_LOAD], gameObject.transform) as GameObject;

			TimerWithBackground timerToManipulate = loadingTarget.GetComponent<TimerWithBackground>();

			GameTimerRange rangeToRegisterTo = GameTimerRange.createWithTimeRemaining((endTimeStamp - GameTimer.currentTime));
			rangeToRegisterTo.registerLabel(timerToManipulate.timerLabel);

			timerToManipulate.backgroundSprite.color = new Color(1, 1, 1, 1);

			timerToManipulate.transform.localPosition = CommonTransform.setObjectLocationWithJSON(objectSpecificJSON, reusableVector); 
			timerToManipulate.backgroundSprite.transform.localScale = CommonTransform.setObjectScaleWithJSON(objectSpecificJSON, reusableVector); 

			objectIterator++;
		}

		if (frameJSON.hasKey("close_button"))
		{
			if (!cachedObjects.ContainsKey(DynamicMOTDFeature.CLOSE_BUTTON_NAME))
			{
				cachedObjects[DynamicMOTDFeature.CLOSE_BUTTON_NAME] = SkuResources.getObjectFromMegaBundle<GameObject>(DynamicMOTDFeature.CLOSE_BUTTON_NAME);
			}

			objectSpecificJSON = frameJSON.getJSON("close_button");

			loadingTarget = CommonGameObject.instantiate(cachedObjects[DynamicMOTDFeature.CLOSE_BUTTON_NAME], gameObject.transform) as GameObject;

			loadingTarget.transform.localPosition = CommonTransform.setObjectLocationWithJSON(objectSpecificJSON, reusableVector); 
			loadingTarget.transform.localScale  = CommonTransform.setObjectScaleWithJSON(objectSpecificJSON, reusableVector);

			ButtonHandler closeButton = loadingTarget.GetComponent<ButtonHandler>();
			buttonCache.Add(closeButton);

			if (closeButton != null)
			{
				closeButtonReference = closeButton;

				if (frameNumber > 0)
				{
					alphaSetting = new Color(1, 1, 1, 0);
					closeButton.sprite.color = alphaSetting;
				}
				else
				{
					alphaSetting = new Color(1, 1, 1, 1);
					closeButton.sprite.color = alphaSetting;
				}
			}
		}
	}

	public void fadeInButtons()
	{
		GameObject[] buttonGameObjects = new GameObject[buttonCache.Count];

		for (int i = 0; i < buttonGameObjects.Length; i++)
		{
			buttonGameObjects[i] = buttonCache[i].gameObject;

			Color defaultColorUnmodified = buttonCache[i].button.defaultColor;
			defaultColorUnmodified.a = 1;
			buttonCache[i].button.defaultColor = defaultColorUnmodified;
		}
		
		StartCoroutine(CommonGameObject.fadeGameObjectsTo(buttonGameObjects, 0f, 1.0f, 0.25f, false));
	}

	private void onClickButton(Dict args = null)
	{
		string doSomethingString = args[D.ACTIVE] as string;
		DoSomething.now(doSomethingString);
		Dialog.close();

		if (DynamicMOTD.audioPack != null)
		{
			DialogBase.playAudioFromEos(DynamicMOTD.audioPack.getAudioKey(DialogAudioPack.OK));
		}
	}

	private Vector3 setObjectLocation(JSON objectJSON, Vector3 vectorToModify)
	{
		vectorToModify.x = objectJSON.getInt("x", 0);
		vectorToModify.y = objectJSON.getInt("y", 0);
		vectorToModify.z = objectJSON.getInt("z", 0);

		return vectorToModify;
	}

	private Vector3 setObjectScale(JSON objectJSON, Vector3 vectorToModify)
	{
		vectorToModify.x = objectJSON.getInt("width", 0);
		vectorToModify.y = objectJSON.getInt("height", 0);
		vectorToModify.z = 1;

		return vectorToModify;
	}
}

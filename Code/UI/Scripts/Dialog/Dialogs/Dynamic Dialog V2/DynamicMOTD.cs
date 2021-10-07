using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.IO;

public class DynamicMOTD : DialogBase
{
	private string templateString;
	private DynamicMOTDFrame tempFrame; // holder for when we reference swap the frames
	private DynamicMOTDPageController frameController; // Only created if in the config, since we couldn't possible place it correctly otherwise?
	private int currentFrameNumber = 0;
	private int maxFrames = -1;
	private int tweenLeftLocation = 0; // Cached tween target location for when we click next frame
	private int tweenRightLocation = 0; // Cached tween target location for when we click previous frame
	private int completedFrameTweens = 0;
	private JSON dialogJSON;
	private Vector2 frameSize = Vector2.zero; // So we know how far out to put frames so they seemlessly transition
	private Vector2 backgroundSize = Vector2.zero; // We use the background size to determine what time background to display should we have one

	public TextMeshProMasker masker;
	public UIPanel panel; // Link to the panel so we can turn off clipping for full screen
	public DynamicMOTDFrame currentFrame; // frame one
	public DynamicMOTDFrame nextFrame; // frame two

	public static DialogAudioPack audioPack;

	/// Initialization
	public override void init()
	{
		audioPack = null;
		templateString = dialogArgs.getWithDefault(D.DATA, "") as string;

		if (DynamicMOTDFeature.instance.validMOTDConfigs.ContainsKey(templateString) && DynamicMOTDFeature.instance.validMOTDConfigs[templateString] != null)
		{
			dialogJSON = DynamicMOTDFeature.instance.validMOTDConfigs[templateString];
		}
		else
		{
			Dialog.close();
			Debug.LogError("Dynamic MOTD had to close due to a missing/invalid template string!");
		}
		if (dialogJSON == null || !dialogJSON.isValid)
		{
			Dialog.close();
			Debug.LogError("Dynamic MOTD Frame Json wasn't formatted correctly!");
		}

		if (DynamicMOTDFeature.instance.audioPacks.ContainsKey(templateString))
		{
			audioPack = DynamicMOTDFeature.instance.audioPacks[templateString];
		}

		int i = 0; // Iterator to pull all the frame data out that we need or at least get the proper frame count
		while (dialogJSON.hasKey("frame_" + i))
		{
			i++;
			maxFrames++;
		}

		// Start and end times for timers come through via the start and end time for the template as a whole
		int timerStartStamp = dialogJSON.getInt("start_time", 0);
		int timerEndStamp = dialogJSON.getInt("end_time", 0);

		int frameSizeX = dialogJSON.getInt("width", 0);
		int frameSizeY = dialogJSON.getInt("height", 0);

		currentFrame.setMOTDTimeStamps(timerStartStamp, timerEndStamp);

		frameSize.x = frameSizeX;
		frameSize.y = frameSizeY;

		drawStaticElements();
		drawStartingFrame();

		playAudio(DialogAudioPack.OPEN);
		playAudio(DialogAudioPack.MUSIC);
	}

	private void playAudio(string key)
	{
		if (audioPack != null)
		{
			playAudioFromEos(audioPack.getAudioKey(key));
		}
	}

	protected override void adjustScaleForScreenOrientation()
	{
		// This was breaking this dialog so don't do it.	
	}
	
#if UNITY_EDITOR
	public void initWithJSON(JSON testJson)
	{
		dialogJSON = testJson;
		int i = 0; // Iterator to pull all the frame data out that we need or at least get the proper frame count
		while (dialogJSON.hasKey("frame_" + i))
		{
			i++;
			maxFrames++;
		}

		drawStaticElements();

		drawStartingFrame();
	}

#endif

	private void drawStaticElements()
	{
		Dictionary<string, GameObject> cachedObjects = DynamicMOTDFeature.instance.cachedObjects;
		GameObject loadingTarget;
		Vector3 reusableVector = Vector3.zero;
		JSON objectSpecificJSON;

		// This shouldn't happen but if it does, just re-make the dict
		if (cachedObjects == null)
		{
			cachedObjects = new Dictionary<string, GameObject>();
		}

		if (dialogJSON.hasKey("background"))
		{
			if (cachedObjects == null || !cachedObjects.ContainsKey(DynamicMOTDFeature.BOUNDED_BACKGROUND_NAME))
			{
				cachedObjects[DynamicMOTDFeature.BOUNDED_BACKGROUND_NAME] = SkuResources.getObjectFromMegaBundle<GameObject>(DynamicMOTDFeature.BOUNDED_BACKGROUND_NAME);
			}

			objectSpecificJSON = dialogJSON.getJSON("background");

			if (objectSpecificJSON != null)
			{
				loadingTarget = CommonGameObject.instantiate(cachedObjects[DynamicMOTDFeature.BOUNDED_BACKGROUND_NAME], sizer.transform) as GameObject;

				UISprite spriteToManipulate = loadingTarget.GetComponent<UISprite>();

				spriteToManipulate.transform.localPosition = CommonTransform.setObjectLocationWithJSON(objectSpecificJSON, reusableVector);
				spriteToManipulate.transform.localScale = CommonTransform.setObjectScaleWithJSON(objectSpecificJSON, reusableVector);

				if (frameSize == Vector2.zero)
				{
					frameSize.x = spriteToManipulate.transform.localScale.x;
					frameSize.y = spriteToManipulate.transform.localScale.y;
				}

				backgroundSize = frameSize;
				tweenLeftLocation = (int)reusableVector.x;
				tweenRightLocation = (int)reusableVector.x;
			}
		}

		if (dialogJSON.hasKey("page_controller"))
		{
			if (cachedObjects == null || !cachedObjects.ContainsKey(DynamicMOTDFeature.PAGE_CONTROLLER_PATH))
			{
				cachedObjects[DynamicMOTDFeature.PAGE_CONTROLLER_PATH] = SkuResources.getObjectFromMegaBundle<GameObject>(DynamicMOTDFeature.PAGE_CONTROLLER_PATH);
			}

			objectSpecificJSON = dialogJSON.getJSON("page_controller");

			if (objectSpecificJSON != null)
			{
				loadingTarget = CommonGameObject.instantiate(cachedObjects[DynamicMOTDFeature.PAGE_CONTROLLER_PATH], sizer.transform) as GameObject;

				DynamicMOTDPageController controller = loadingTarget.GetComponent<DynamicMOTDPageController>();

				controller.transform.localPosition = CommonTransform.setObjectLocationWithJSON(objectSpecificJSON, reusableVector);
				controller.transform.localScale = CommonTransform.setObjectScaleWithJSON(objectSpecificJSON, reusableVector);

				frameController = controller;
				frameController.setFrameCount(maxFrames + 1);
				frameController.goToFrame(currentFrameNumber);
				frameController.nextButton.registerEventDelegate(onClickNextFrame);
				frameController.previousButton.registerEventDelegate(onClickPreviousFrame);
				frameController.previousButton.SetActive(false);
			}
		}
	}

	// If we didn't want to draw the next buffer frame for "reasons" we don't have to I guess.
	private void drawStartingFrame(bool shouldPrepareNext = true)
	{
		currentFrame.gameObject.SetActive(true);
		currentFrame.drawFrame(currentFrameNumber, dialogJSON.getJSON("frame_" + currentFrameNumber), downloadedTextures, backgroundSize);

		// Make sure we stretch the clipping area and whatnot to fit everything neatly
		if (frameSize == Vector2.zero && frameSize.x * frameSize.y <= currentFrame.frameSize.x * currentFrame.frameSize.y)
		{
			frameSize = currentFrame.frameSize;
			// Grab largest image on frame 1
		}

		Vector4 currentClipRange = panel.clipRange;
		currentClipRange.z = frameSize.x;
		currentClipRange.w = float.MaxValue; // We don't care about vertical masking, so just make it screen size.
		panel.clipRange = currentClipRange;

		if (currentFrame.closeButtonReference != null)
		{
			currentFrame.closeButtonReference.registerEventDelegate(onClickClose);
		}

		if (currentFrameNumber != maxFrames)
		{
			CommonTransform.setX(nextFrame.transform, frameSize.x);
		}
		else
		{
			if (frameController != null)
			{
				frameController.nextButton.SetActive(false);
				frameController.previousButton.SetActive(false);
			}
		}

		List<TextMeshPro> textMeshPros = new List<TextMeshPro>();
		textMeshPros.AddRange(currentFrame.GetComponentsInChildren<TextMeshPro>());
		textMeshPros.AddRange(nextFrame.GetComponentsInChildren<TextMeshPro>());
		masker.addObjectArrayToList(textMeshPros.ToArray());
		masker.totalWidth = (int)currentClipRange.z;
	}

	private void onClickNextFrame(Dict args = null)
	{
		goToNextFrame();
	}

	private void onClickPreviousFrame(Dict args = null)
	{
		goToPreviousFrame();
	}

	private void goToNextFrame()
	{
		currentFrameNumber++;
		if (frameController != null)
		{
			frameController.nextButton.SetActive(false);
			frameController.previousButton.SetActive(false);
			if (currentFrameNumber == maxFrames)
			{
				StartCoroutine(CommonGameObject.fadeGameObjectTo(frameController.gameObject, 1f, 0f, 0.25f, false));
			}
	
		}

		nextFrame.gameObject.SetActive(true);

		nextFrame.drawFrame(currentFrameNumber, dialogJSON.getJSON("frame_" + currentFrameNumber), downloadedTextures, backgroundSize);
		masker.addObjectArrayToList(nextFrame.GetComponentsInChildren<TextMeshPro>());
		completedFrameTweens = 0; // Always reset here just in case
		iTween.MoveTo(currentFrame.gameObject, 
		            iTween.Hash(
			        "x", -frameSize.x,
					"time", 1,
					"oncompletetarget", gameObject,
			        "oncomplete", "onCurrentFrameFinishScroll",
					"islocal", true
					)
			   );

		CommonTransform.setX(nextFrame.transform, frameSize.x);
		iTween.MoveTo(nextFrame.gameObject,
					iTween.Hash(
					"x", 0,
					"time", 1,
					"oncompletetarget", gameObject,
					"oncomplete", "onCurrentFrameFinishScroll",
					"islocal", true
					)
			   );
	}

	private void goToPreviousFrame()
	{
		currentFrameNumber--;

		if (frameController != null)
		{
			frameController.nextButton.SetActive(false);
			frameController.previousButton.SetActive(false);
		}
		nextFrame.gameObject.SetActive(true);

		nextFrame.drawFrame(currentFrameNumber, dialogJSON.getJSON("frame_" + currentFrameNumber), downloadedTextures, backgroundSize);
		masker.addObjectArrayToList(nextFrame.GetComponentsInChildren<TextMeshPro>());
		completedFrameTweens = 0; // Always reset here just in case
		iTween.MoveTo(currentFrame.gameObject,
				iTween.Hash(
			    "x", frameSize.x,
				"time", 1,
				"oncompletetarget", gameObject,
				"oncomplete", "onCurrentFrameFinishScroll",
				"islocal", true
				)
		   );

		CommonTransform.setX(nextFrame.transform, -frameSize.x);
		iTween.MoveTo(nextFrame.gameObject,
					iTween.Hash(
					"x", 0,
					"time", 1,
					"oncompletetarget", gameObject,
					"oncomplete", "onCurrentFrameFinishScroll",
					"islocal", true
					)
			   );
	}

	private void onCurrentFrameFinishScroll()
	{
		//deactivate current frame
		// move it back to where next frame would start
		// draw
		completedFrameTweens++;

		if (completedFrameTweens == 2)
		{
			trySwapReferences();
		}
	
	}

	private void trySwapReferences()
	{
		// In case we can't rely on the order of the tweens finishing, we'll just 
		// keep count and if they both finish, do the ol swap
		tempFrame = currentFrame;
		currentFrame = nextFrame;
		nextFrame = tempFrame;
		completedFrameTweens = 0;

		CommonGameObject.destroyChildren(nextFrame.gameObject);

		currentFrame.fadeInButtons();

		if (frameController != null)
		{
			frameController.nextButton.SetActive(currentFrameNumber < maxFrames);
			frameController.previousButton.SetActive(currentFrameNumber > 0 && currentFrameNumber < maxFrames);
			frameController.goToFrame(currentFrameNumber);
		}

		// Cache next frame and preapre for masking
		if (currentFrameNumber != maxFrames && currentFrameNumber > 1)
		{
			masker.addObjectArrayToList(nextFrame.GetComponentsInChildren<TextMeshPro>());
		}

		if (currentFrame.closeButtonReference != null)
		{
			currentFrame.closeButtonReference.registerEventDelegate(onClickClose);
		}
	}

	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		string knownTemplates = PlayerPrefsCache.GetString(Prefs.SEEN_DYNAMIC_MOTD_TEMPLATES, "");

		// If we have some templates to go through
		if (!string.IsNullOrEmpty(knownTemplates))
		{
			string[] templateDataAsArray = knownTemplates.Split(',');
			int parsedViewCount = 0;

			for (int i = 0; i < templateDataAsArray.Length; i++)
			{
				if (templateDataAsArray[i] == templateString && (i + 2 < templateDataAsArray.Length))
				{
					int.TryParse(templateDataAsArray[i + 2], out parsedViewCount);
					parsedViewCount++;
					templateDataAsArray[i + 1] = GameTimer.currentTime.ToString();
					templateDataAsArray[i + 2] = parsedViewCount.ToString();
					knownTemplates = string.Join(",", templateDataAsArray);
					break;
				}
				else if (i == templateDataAsArray.Length - 1)
				{
					// Tack this on to the end
					knownTemplates += ",";
					templateString += "," + GameTimer.currentTime;
					templateString += "," + 1;
					knownTemplates += templateString;
				}
			}
		}
		else
		{
			// It's the first record
			templateString += "," + GameTimer.currentTime;
			templateString += "," + 1;
			knownTemplates += templateString;
		}

		PlayerPrefsCache.SetString(Prefs.SEEN_DYNAMIC_MOTD_TEMPLATES, knownTemplates);
	}

	private void onClickClose(Dict args = null)
	{
		playAudio(DialogAudioPack.CLOSE);
		Dialog.close();
	}

	public static void showDialog(string motdKey, string templateToUse)
	{
		Dict args = Dict.create(D.MOTD_KEY, motdKey,
		                        D.DATA, templateToUse);

		Dialog.instance.showDialogAfterDownloadingTextures("dynamic_motd", DynamicMOTDFeature.instance.templateTexturePaths[templateToUse].ToArray(), args);
	}
}
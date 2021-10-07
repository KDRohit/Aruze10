using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Text;
using System.Diagnostics;
using System.Linq;
using TMPro;

public class DynamicMOTDJSONCreator : EditorWindow
{
	GameObject parentObject;
	GameObject selectedObject;
	string pastedJSON = "Input JSON here, or click generate with a dialog in the scene and selected. This will fill with json, then click build to test.";

	private const string WIDGET_PATH = "assets/data/hir/bundles/initialization/prefabs/misc/Dynamic MOTD Widgets/";
	private const string BASE_PATH = "assets/data/hir/bundles/initialization/prefabs/dialogs/Dynamic MOTD/";

	public const string BUTTON_NAME = WIDGET_PATH + "Button.prefab";
	public const string LABEL_NAME = WIDGET_PATH + "Label Body.prefab";
	public const string CLOSE_BUTTON_NAME = WIDGET_PATH + "Close Button.prefab";
	public const string RENDERER_NAME = WIDGET_PATH + "Texture.prefab";
	public const string BOUNDED_BACKGROUND_NAME = WIDGET_PATH + "Bounded Background.prefab";
	public const string FRAME_NAME = WIDGET_PATH + "Dynamic MOTD Frame.prefab";
	public const string TIMER = WIDGET_PATH + "Timer Detached.prefab";
	public const string PAGE_CONTROLLER_PATH = WIDGET_PATH + "Pagination Bar.prefab";
	public const string TIMER_ATTACHED = WIDGET_PATH + "Timer Attached.prefab";
	private const string BASE_TEMPLATE_NAME = BASE_PATH + "Dynamic MOTD Base.prefab";

	private const string BOUNDED_BACKGROUND_SPRITE_NAME = "Dialog Background 00 Stretchy";

	private Dictionary<string, GameObject> cachedObjects;

	[MenuItem("Zynga/Dynamic MOTD JSON Creator")]

	public static void ShowWindow()
	{
		EditorWindow.GetWindow<DynamicMOTDJSONCreator>("Dynamic JSON Creator");
	}

	void OnGUI()
	{
		if (GUILayout.Button("Generate JSON "))
		{
			generateJSON();
		}


		pastedJSON = GUILayout.TextArea(pastedJSON);

		if (GUILayout.Button("Try proper build"))
		{
			tryBuildInFrame();
		}

		if (GUILayout.Button("Copy JSON"))
		{
			EditorGUIUtility.systemCopyBuffer = pastedJSON;
		}
	}

	private void generateJSON()
	{
		if (Selection.activeGameObject == null)
		{
			UnityEngine.Debug.LogError("Selected game object was null!");
		}
		else
		{
			selectedObject = Selection.activeGameObject;
			string workingJSONString = "{";
			List<TextMeshPro> tmproList = selectedObject.GetComponentsInChildren<TextMeshPro>().ToList();
			List<ButtonHandler>buttonList = selectedObject.GetComponentsInChildren<ButtonHandler>().ToList();
			List<UITexture> renderList = selectedObject.GetComponentsInChildren<UITexture>().ToList();
			List<UISprite> spriteList = selectedObject.GetComponentsInChildren<UISprite>().ToList(); // Not used?
			List<TimerWithBackground> timers = selectedObject.GetComponentsInChildren<TimerWithBackground>().ToList();
			List<DynamicMOTDPageController> pageControllers = selectedObject.GetComponentsInChildren<DynamicMOTDPageController>().ToList();

			// Find static elements like close buttosn and backgrounds, whatever. Thes selectedObject.GetComponentsInChildren<UISprite>().ToList();e should be outside the frame data

			// There should only ever be 1 sprite that's relevant to us and it's the background.
			// Eventually we may support more than one so we may have to parse an array of usable background sprite names or something.
			// If we used a UITexture as the background it would have been caught earlier, so this feels ok.
			bool backgroundFound = false;
			for (int i = 0; i < spriteList.Count; i++)
			{
				// If we found the background sprite
				if (spriteList[i].spriteName == BOUNDED_BACKGROUND_SPRITE_NAME)
				{
					backgroundFound = true;
					workingJSONString += generateBackgroundJson(spriteList[i]);
					workingJSONString += ",";
				}
			}

			if (pageControllers.Count > 0)
			{
				for (int i = 0; i < pageControllers.Count; i++)
				{
					if (buttonList.Contains(pageControllers[i].nextButton))
					{
						buttonList.Remove(pageControllers[i].nextButton);
					}

					if (buttonList.Contains(pageControllers[i].previousButton))
					{
						buttonList.Remove(pageControllers[i].previousButton);
					}

					workingJSONString += generatePageControllerJson(pageControllers[i]);
					workingJSONString += ",";
				}			
			}
			

			// Pull any timer labels out of our known TMPros
			for (int i = timers.Count - 1; i >= 0; i--)
			{
				// If we found the background sprite
				if (tmproList.Contains(timers[i].timerLabel))
				{
					tmproList.Remove(timers[i].timerLabel);
				}
			}

			workingJSONString += "\"frame_0\":{";
			// Loop through buttons
			int closeButtonOffset = 0; // We number buttons in a specific way so if we're going to do that, we need to make sure
									   // we offset the index when we find a close button to account for this
			for (int i = 0; i < buttonList.Count; i++)
			{
				// Special case for a close button
				if (buttonList[i].sprite.spriteName.Contains("Close"))
				{
					workingJSONString += generateCloseButtonJSON(buttonList[i]);
					closeButtonOffset--;
				}
				else
				{
					workingJSONString += generateButtonJson(buttonList[i], i + closeButtonOffset);
				}

				// Once we hit the last element, stop adding the comma for the next piece.
				if (i != buttonList.Count - 1)
				{
					workingJSONString += ",";
				}

				// Don't double process TMPros
				if (buttonList[i].label != null && tmproList.Contains(buttonList[i].label))
				{
					tmproList.Remove(buttonList[i].label);
				}
			}

			//If we put buttons in, and we have renderers, put a comma in
			if (buttonList.Count > 0)
			{
				workingJSONString += ",";
			}

			for (int i = 0; i < renderList.Count; i++)
			{
				workingJSONString += generateImageJson(renderList[i], i);

				if (i != renderList.Count - 1)
				{
					workingJSONString += ",";
				}
			}

			// if we put renderers in, add a comma
			if (renderList.Count > 0)
			{
				workingJSONString += ",";
			}

			if(timers.Count > 0)
			{
				for (int i = 0; i < timers.Count; i++)
				{
					workingJSONString += generateTimerJSON(timers[i]);
				}
			}
		
			// if we put a timer in, add a comma
			if (timers.Count > 0)
			{
				workingJSONString += ",";
			}

			for (int i = 0; i < tmproList.Count; i++)
			{
				workingJSONString += generateTextJSON(tmproList[i], i);

				if (i != tmproList.Count - 1)
				{
					workingJSONString += ",";
				}
			}

			workingJSONString += "}}";
			pastedJSON = workingJSONString;
		}
	}

	private void tryBuildInFrame()
	{
		if ((string.IsNullOrEmpty(pastedJSON)))
		{
			UnityEngine.Debug.LogError("Could not create prefab since we didn't have any JSON");
			return;
		}

		JSON jsonToUse = new JSON(pastedJSON);

		// Reload who cares it's an editor script.
		cachedObjects = new Dictionary<string, GameObject>();
		cachedObjects[BUTTON_NAME] = SkuResources.getObjectFromMegaBundle<GameObject>(BUTTON_NAME);
		cachedObjects[LABEL_NAME] = SkuResources.getObjectFromMegaBundle<GameObject>(LABEL_NAME);
		cachedObjects[CLOSE_BUTTON_NAME] = SkuResources.getObjectFromMegaBundle<GameObject>(CLOSE_BUTTON_NAME);
		cachedObjects[RENDERER_NAME] = SkuResources.getObjectFromMegaBundle<GameObject>(RENDERER_NAME);
		cachedObjects[BOUNDED_BACKGROUND_NAME] = SkuResources.getObjectFromMegaBundle<GameObject>(BOUNDED_BACKGROUND_NAME);
		cachedObjects[BASE_TEMPLATE_NAME] = SkuResources.getObjectFromMegaBundle<GameObject>(BASE_TEMPLATE_NAME);
		cachedObjects[PAGE_CONTROLLER_PATH] = SkuResources.getObjectFromMegaBundle<GameObject>(PAGE_CONTROLLER_PATH);
		cachedObjects[TIMER] = SkuResources.getObjectFromMegaBundle<GameObject>(TIMER);
		cachedObjects[TIMER_ATTACHED] = SkuResources.getObjectFromMegaBundle<GameObject>(TIMER_ATTACHED);
		cachedObjects[FRAME_NAME] = SkuResources.getObjectFromMegaBundle<GameObject>(FRAME_NAME);

		DynamicMOTDFeature.instance.cachedObjects = cachedObjects;

		parentObject = GameObject.Find("Dialog Panel");
		GameObject dialogBase = CommonGameObject.instantiate(SkuResources.getObjectFromMegaBundle<GameObject>(BASE_TEMPLATE_NAME), parentObject.transform) as GameObject;
		dialogBase.transform.parent = parentObject.transform;
		dialogBase.transform.localPosition = Vector3.zero;

		DynamicMOTD motdReference = dialogBase.GetComponent<DynamicMOTD>();
		motdReference.initWithJSON(jsonToUse);
	}

	private string generateImageJson(UITexture targetObject, int iterator = 0)
	{
		Transform targetTransform = targetObject.transform;
		Vector3 adjustedPosition = getPositionRelativeToParents(targetTransform);

		string json = "\"image_"+ iterator + "\":{" +
			JSON.createJsonString("image_path", "") + ", " +
			appendPositionData(adjustedPosition) +
			appendWidthAndHeight(targetTransform.localScale);

		int rotation = Mathf.RoundToInt(targetTransform.localEulerAngles.z);
		if (rotation != 0)
		{
			json += ", " + JSON.createJsonString("rotation", rotation);
		}

		json += "}";
		
		return json;
	}

	private string generateTextJSON(TextMeshPro tmproObject, int iterator = 0)
	{
		Transform targetTransform = tmproObject.transform;
		Vector3 adjustedPosition = getPositionRelativeToParents(targetTransform);
		string json = "\"text_" + iterator + "\":{" +
			JSON.createJsonString("text", tmproObject.text) + ", " +
		        JSON.createJsonString("color",tmproObject.color.ToString()) + ", " +
		        appendPositionData(adjustedPosition) +
		        appendWidthAndHeight(new Vector2(tmproObject.rectTransform.rect.width, tmproObject.rectTransform.rect.height));
		json += "}";


		return json;
	}

	private string generateButtonJson(ButtonHandler buttonObject, int iterator = 0)
	{
		Transform targetTransform = buttonObject.transform;
		Vector3 adjustedPosition = getPositionRelativeToParents(targetTransform);

		string json = "";
		json = "\"button_" + iterator + "\":{" +
		JSON.createJsonString("text", buttonObject.text) + ", " +
		JSON.createJsonString("action", "") + ", " + // Will needto be updated after the fact?
		JSON.createJsonString("sprite", buttonObject.sprite.spriteName) + ", " +  // Will needto be updated after the fact?
		appendPositionData(adjustedPosition) +
		    appendWidthAndHeight(targetTransform.localScale);
		json += "}";

		return json;
	}

	private string generateBackgroundJson(UISprite backgroundSprite)
	{
		Transform targetTransform = backgroundSprite.transform;
		Vector3 adjustedPosition = getPositionRelativeToParents(targetTransform);
		string json = "\"background\":{" +
			appendPositionData(adjustedPosition) +
			appendWidthAndHeight(targetTransform.localScale);
			json += "}";

		return json;
	}

	private string generatePageControllerJson(DynamicMOTDPageController controllerObject)
	{
		Transform targetTransform = controllerObject.transform;
		Vector3 adjustedPosition = getPositionRelativeToParents(targetTransform);
		string json = "\"page_controller\":{" +
			appendPositionData(adjustedPosition) +
			appendWidthAndHeight(targetTransform.localScale);
		json += "}";

		return json;
	}


	private string generateCloseButtonJSON(ButtonHandler closeButton)
	{
		Transform targetTransform = closeButton.transform;
		Vector3 adjustedPosition = getPositionRelativeToParents(targetTransform);
		string json = "\"close_button\":{" +
		appendPositionData(adjustedPosition) +
		appendWidthAndHeight(targetTransform.localScale);
		json += "}";

		return json;
	}

	private string generateTimerJSON(TimerWithBackground timer)
	{
		Transform targetTransform = timer.transform;
		Vector3 adjustedPosition = getPositionRelativeToParents(targetTransform);
		string json = "\"timer\":{" +
		appendPositionData(adjustedPosition) +
		appendWidthAndHeight(timer.backgroundSprite.transform.localScale);
		json += "}";

		return json;
	}

	private string appendPositionData(Vector3 position)
	{
		string appendedPositionData = "";
		appendedPositionData +=
		JSON.createJsonString("x", Mathf.RoundToInt(position.x)) + ", " +
		JSON.createJsonString("y", Mathf.RoundToInt(position.y)) + ", " +
		JSON.createJsonString("z", Mathf.RoundToInt(position.z)) + ", ";

		return appendedPositionData;
	}

	private string appendWidthAndHeight(Vector2 scale)
	{
		string appendedPositionData = "";
		appendedPositionData +=
		JSON.createJsonString("width", Mathf.RoundToInt(scale.x)) + ", " +
		JSON.createJsonString("height", Mathf.RoundToInt(scale.y));

		return appendedPositionData;
	}

	private Vector3 getPositionRelativeToParents(Transform t)
	{
		Transform workingTransform = t;
		Vector3 startingPosition = t.localPosition;
		while (workingTransform != selectedObject.transform)
		{
			startingPosition += workingTransform.parent.transform.localPosition;
			workingTransform = workingTransform.parent.transform;
		}

		return startingPosition;
	}
}


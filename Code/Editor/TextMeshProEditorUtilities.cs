using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor.Experimental.SceneManagement;

/*
Contains editor menu items to help with things related to TextMeshPro.
*/

public static class TextMeshProEditorUtilities
{
	private const string STANDARD_FONT_NAME = "OpenSans-Bold SDF";
	private const string NUMBERS_FONT_NAME = "monofonto numbers SDF";

	// Convert all the UILabels in the selected game objects over to TextMeshPros
	[MenuItem("Zynga/Assets/Replace All UILabels with TextMeshPros")]
	public static void convertAllUILabelsToTextMeshPro()
	{
		if (Selection.gameObjects != null)
		{
			foreach (GameObject selectedObject in Selection.gameObjects)
			{
				UILabel[] nguiLabels = selectedObject.GetComponentsInChildren<UILabel>(true);

				foreach (UILabel nguiLabel in nguiLabels)
				{
					makeTextMeshProFromUILabel(nguiLabel.gameObject, true, false);
				}
			}
		}
	}

	[MenuItem("Zynga/Assets/Force Mesh Update All TextMeshPro Containers")]
	public static void forceUpdateAllTextMeshProContainers()
	{
		if (Selection.gameObjects != null)
		{
			foreach (GameObject selectedObject in Selection.gameObjects)
			{
				TextMeshPro[] textMeshPros = selectedObject.GetComponentsInChildren<TextMeshPro>(true);

				foreach (TextMeshPro textMeshPro in textMeshPros)
				{
					textMeshPro.ForceMeshUpdate();
				}
			}
		}
	}

	// Convert a legacy TextMeshPro object over to the format that will work in 1.4.1
	// return bool telling if the TextMeshPro object was modified
	private static bool convertTextMeshProTo_1_4_1(TextMeshPro textMeshPro, StringBuilder outputLog)
	{
		string textMeshProConvertLog = "\t" + textMeshPro.gameObject.name + ": ";
		bool isTextMeshProChanged = false;

		TextAlignmentOptions convertedAlign = TMProExtensions.TMProExtensionFunctions.convertLegacyTextAlignmentEnumValue(textMeshPro.alignment);

		if ((int)textMeshPro.alignment != (int)convertedAlign)
		{
			textMeshProConvertLog += "Alignment Updated from: " + (int)textMeshPro.alignment + "; to " + (int)convertedAlign + "; ";
			textMeshPro.alignment = convertedAlign;
			isTextMeshProChanged = true;
		}
				
		TextContainer textContainer = textMeshPro.GetComponent<TextContainer>();
		if (textContainer != null)
		{
			bool isAlignmentMatched = TMProExtensions.TMProExtensionFunctions.isTextContainerAlignmentSameAsTextMeshPro(textMeshPro, textContainer);

			if (!isAlignmentMatched)
			{
				textMeshProConvertLog += "TextContainer had different alignment: " + textContainer.anchorPosition + "; from textMeshPro.alignment = " + textMeshPro.alignment;
			}
					
			Object.DestroyImmediate(textContainer, true);
			textMeshProConvertLog += " Destroyed TextContainer.";
			isTextMeshProChanged = true;
		}
				
		// Append info about the TextMeshPro changes to our results
		if (isTextMeshProChanged && outputLog != null)
		{
			outputLog.Append(textMeshProConvertLog + "\n");
		}

		return isTextMeshProChanged;
	}
	
	// Convert the selected object(s) to be in TextMeshPro 1.4.1 format
	[MenuItem("Zynga/Assets/TextMeshPro/Report Objects Using Sprite Mask On Text Mesh Pro Objects (WARNING: Could take a bit)")]
	public static void reportAllObjectsUsingSpriteMaskOnTextMeshProObjects()
	{
		List<GameObject> prefabsWithSpriteMaskScript = getAllGameObjectsContainingSpriteMaskScript();

		string outputStr = "TextMeshProEditorUtilities.reportAllObjectsUsingSpriteMaskOnTextMeshProObjects() - List of objects that use SpriteMask on Text Mesh Pro Objects:\n";

		foreach (GameObject prefab in prefabsWithSpriteMaskScript)
		{
			SpriteMask[] spriteMaskArray = prefab.GetComponentsInChildren<SpriteMask>(true);
			foreach (SpriteMask spriteMask in spriteMaskArray)
			{
				bool isSpriteMaskTouchingTextMeshPro = doesSpriteMaskAffectAnyTextMeshProObjects(spriteMask);
				if (isSpriteMaskTouchingTextMeshPro)
				{
					outputStr += prefab.name + " : (" + spriteMask.gameObject.name + ")" + "\n";
				}
			}
		}
		
		Debug.Log(outputStr);
	}

	private static bool doesSpriteMaskAffectAnyTextMeshProObjects(SpriteMask spriteMask)
	{
		// First check the SpriteMask itself to see if anything under it is TextMeshPro, since SpriteMask applies to anything nested under the game object
		TextMeshPro textMeshPro = spriteMask.gameObject.GetComponentInChildren<TextMeshPro>();
		if (textMeshPro == null)
		{
			// We need to check if any extra linked transforms on SpriteMask include TextMeshPro
			if (spriteMask.maskedObjects != null)
			{
				foreach (Transform linkedTransform in spriteMask.maskedObjects)
				{
					if (linkedTransform != null)
					{
						textMeshPro = linkedTransform.gameObject.GetComponentInChildren<TextMeshPro>();
						if (textMeshPro != null)
						{
							return true;
						}
					}
				}
			}
		}
		else
		{
			// Under the SpriteMask object has a TextMeshPro object in its own hierarchy
			return true;
		}

		return false;
	}
	
	private static List<GameObject> getAllGameObjectsContainingSpriteMaskScript()
	{
		List<GameObject> prefabsWithSpriteMaskScript = new List<GameObject>();

		List<GameObject> allPrefabs = CommonEditor.gatherPrefabs("Assets/");
		foreach (GameObject prefab in allPrefabs)
		{
			SpriteMask spriteMask = prefab.GetComponentInChildren<SpriteMask>(true);
			if (spriteMask != null)
			{
				prefabsWithSpriteMaskScript.Add(prefab);
			}
		}

		return prefabsWithSpriteMaskScript;
	}
	
	// Convert the selected object(s) to be in TextMeshPro 1.4.1 format
	[MenuItem("Zynga/Assets/TextMeshPro/Convert Selected Game Objects To TextMeshPro 1.4.1")]
	public static void convertSelectedGameObjectsToTextMeshPro_1_4_1()
	{
		if (Selection.gameObjects != null)
		{
			StringBuilder outputLog = new StringBuilder();
			outputLog.Append("TextMeshProEditorUtilities.convertSelectedGameObjectsToTextMeshPro_1_4_1() - Results:\n");

			foreach (GameObject go in Selection.gameObjects)
			{
				try
				{
					bool isModifyingPrefab = false;
					bool isPrefabChanged = false;
					string prefabAssetPath = null;
					GameObject rootObject = go;

					bool isObjectAPrefab = false;
					bool isInPrefabStage = false;
					if (PrefabStageUtility.GetCurrentPrefabStage() != null && PrefabStageUtility.GetPrefabStage(go) != null)
					{
						isObjectAPrefab = true;
						isInPrefabStage = true;
					}
					else
					{
						isObjectAPrefab = PrefabUtility.IsPartOfAnyPrefab(go);
					}

					if (isObjectAPrefab)
					{
						bool isSceneInstance = PrefabUtility.IsPartOfNonAssetPrefabInstance(go);

						if (!isSceneInstance)
						{
							// Either part of prefab asset or nested prefab instance.  Unpack prefab to edit and save it
							// afterward.
							bool isImmutablePrefab = PrefabUtility.IsPartOfImmutablePrefab(go);
							bool isVariantPrefab = PrefabUtility.IsPartOfVariantPrefab(go);
							if (isImmutablePrefab || isVariantPrefab)
							{
								Debug.LogWarningFormat(go, "Cannot modify immutable/variant prefab {0}.", go);
								continue;
							}
							isModifyingPrefab = true;

							if (isInPrefabStage)
							{
								prefabAssetPath = PrefabStageUtility.GetPrefabStage(go).prefabAssetPath;
							}
							else
							{
								// Check for whether go is itself a prefab.
								PrefabInstanceStatus instanceStatus = PrefabUtility.GetPrefabInstanceStatus(go);
								PrefabAssetType assetType = PrefabUtility.GetPrefabAssetType(go);
								// This API is confusing but this seems to be the only way to figure this out now.
								bool goIsPrefab = assetType != PrefabAssetType.NotAPrefab && instanceStatus == PrefabInstanceStatus.NotAPrefab;
								GameObject prefabObject = goIsPrefab ? go : PrefabUtility.GetCorrespondingObjectFromSource(go);
								if (prefabObject == null)
								{
									Debug.LogWarningFormat(go, "Cannot find prefab for {0}.", go);
									continue;
								}
								prefabAssetPath = AssetDatabase.GetAssetPath(prefabObject);
							}
						
							rootObject = PrefabUtility.LoadPrefabContents(prefabAssetPath);
						}
					}

					TextMeshPro[] textMeshPros = rootObject.GetComponentsInChildren<TextMeshPro>(true);
				
					if (textMeshPros.Length > 0)
					{
						outputLog.Append("rootObject.name = " + rootObject.name + "; Prefab: " + prefabAssetPath + "\n");
					}

					foreach (TextMeshPro textMeshPro in textMeshPros)
					{
						isPrefabChanged = convertTextMeshProTo_1_4_1(textMeshPro, outputLog);
					}

					if (isModifyingPrefab)
					{
						if (isPrefabChanged)
						{
							PrefabUtility.SaveAsPrefabAsset(rootObject, prefabAssetPath);
						}

						PrefabUtility.UnloadPrefabContents(rootObject);
					}
				}
				catch (System.ArgumentException e)
				{
					outputLog.AppendLine(e.Message);
				}
			}
			// Output would be too long to output to the console, so will output to a file
			CommonEditor.outputStringToFile("Assets/-Temporary Storage-/Tool Output/TextMeshPro_1_4_1_Convert_Results.txt", outputLog.ToString(), "Convert Selected TextMeshPro Instances To 1.4.1", LogType.Log, true, true);
		}
	}

	// Function for converting all TextMeshPro objects in our entire project over to work
	// with TextMeshPro 1.4.1. This includes updating old legacy alignment values that are
	// no longer supported and removing TextContainer which was dropped
	[MenuItem("Zynga/Assets/TextMeshPro/Convert ALL TextMeshPro Instances To 1.4.1 (WARNING: Can take 15+ minutes)")]
	public static void convertAllTextMeshProInstancesTo_1_4_1()
	{
		List<GameObject> allPrefabs = CommonEditor.gatherPrefabs("Assets/");

		StringBuilder outputLog = new StringBuilder();
		outputLog.Append("TextMeshProEditorUtilities.convertAllTextMeshProInstancesTo_1_4_1() - Results:\n");
		
		foreach (GameObject prefab in allPrefabs)
		{
			try 
			{
				string prefabAssetPath = AssetDatabase.GetAssetPath(prefab);
				GameObject loadedPrefab = PrefabUtility.LoadPrefabContents(prefabAssetPath);
				bool isPrefabChanged = false;
			
				TextMeshPro[] textMeshPros = loadedPrefab.GetComponentsInChildren<TextMeshPro>(true);

				if (textMeshPros.Length > 0)
				{
					outputLog.Append("Prefab: " + prefabAssetPath + "\n");
				}

				foreach (TextMeshPro textMeshPro in textMeshPros)
				{
					isPrefabChanged = convertTextMeshProTo_1_4_1(textMeshPro, outputLog);
				}
			
				// Now save our changes to the prefab
				if (isPrefabChanged)
				{
					PrefabUtility.SaveAsPrefabAsset(loadedPrefab, prefabAssetPath);
				}
				PrefabUtility.UnloadPrefabContents(loadedPrefab);
			}
			catch (System.ArgumentException e)
			{
				outputLog.AppendLine(e.Message);
			}
		}
		
		// Output would be too long to output to the console, so will output to a file
		CommonEditor.outputStringToFile("Assets/-Temporary Storage-/Tool Output/TextMeshPro_1_4_1_Convert_Results.txt", outputLog.ToString(), "Convert All TextMeshPro Instances To 1.4.1", LogType.Log, true, true);
	}

	[MenuItem("Zynga/Assets/Remove All TextMeshPro TextContainer components")]
	public static void removeAllTextMeshProTextContainers()
	{
		if (Selection.gameObjects != null)
		{
			foreach (GameObject go in Selection.gameObjects)
			{
				Debug.LogFormat(go, "Processing {0} to remove TextContainers", go);

				bool isModifyingPrefab = false;
				string prefabAssetPath = null;
				GameObject rootObject = go;

				if (PrefabUtility.IsPartOfAnyPrefab(go))
				{
					bool isSceneInstance = PrefabUtility.IsPartOfNonAssetPrefabInstance(go);
					if (!isSceneInstance)
					{
						// Either part of prefab asset or nested prefab instance.  Unpack prefab to edit and save it
						// afterward.
						bool isImmutablePrefab = PrefabUtility.IsPartOfImmutablePrefab(go);
						bool isVariantPrefab = PrefabUtility.IsPartOfVariantPrefab(go);
						if (isImmutablePrefab || isVariantPrefab)
						{
							Debug.LogWarningFormat(go, "Cannot modify immutable/variant prefab {0}.", go);
							continue;
						}
						isModifyingPrefab = true;
						// Check for whether go is itself a prefab.
						PrefabInstanceStatus instanceStatus = PrefabUtility.GetPrefabInstanceStatus(go);
						PrefabAssetType assetType = PrefabUtility.GetPrefabAssetType(go);
						// This API is confusing but this seems to be the only way to figure this out now.
						bool goIsPrefab = assetType != PrefabAssetType.NotAPrefab && instanceStatus == PrefabInstanceStatus.NotAPrefab;
						GameObject prefabObject = goIsPrefab ? go : PrefabUtility.GetCorrespondingObjectFromSource(go);
						if (prefabObject == null)
						{
							Debug.LogWarningFormat(go, "Cannot find prefab for {0}.", go);
							continue;
						}
						prefabAssetPath = AssetDatabase.GetAssetPath(prefabObject);
						rootObject = PrefabUtility.LoadPrefabContents(prefabAssetPath);
					}
				}

				TextMeshPro[] textMeshPros = rootObject.GetComponentsInChildren<TextMeshPro>(true);

				foreach (TextMeshPro textMeshPro in textMeshPros)
				{
					TextContainer textContainer = textMeshPro.GetComponent<TextContainer>();
					if (textContainer != null)
					{
						Debug.LogFormat(textMeshPro.gameObject, "Removing TextContainer from {0}", textMeshPro.gameObject);
						TMProExtensions.TMProExtensionFunctions.SetPivotAndAlignmentFromTextContainer(textMeshPro);
					}
				}

				if (isModifyingPrefab)
				{
					PrefabUtility.SaveAsPrefabAsset(rootObject, prefabAssetPath);
					PrefabUtility.UnloadPrefabContents(rootObject);
				}
			}
		}
	}

	[MenuItem("Zynga/Assets/Create TextMeshPro from UILabel %#t")]
	public static void makeTextMeshProFromUILabels()
	{
		foreach (GameObject uiLabelObject in Selection.gameObjects)
		{
			makeTextMeshProFromUILabel(uiLabelObject);
		}
	}

	// Convert a UILabel to a TexteMeshPro
	private static void makeTextMeshProFromUILabel(GameObject uiLabelObject, bool useSameObject = false, bool selectObjectWhenDone = true)
	{
		UILabel uiLabel = uiLabelObject.GetComponent<UILabel>();

		if (uiLabel == null)
		{
			Debug.LogWarning("The selected GameObject has no UILabel component.", uiLabelObject);
			return;
		}

		GameObject tmProObject;

		if (useSameObject)
		{
			// disabled the uiLabel so it can't do anything while we replace it
			uiLabel.enabled = false;

			tmProObject = uiLabelObject;
		}
		else
		{
			tmProObject = new GameObject();
			tmProObject.transform.parent = uiLabelObject.transform.parent;
			tmProObject.transform.localPosition = uiLabelObject.transform.localPosition;
			tmProObject.transform.SetSiblingIndex(uiLabelObject.transform.GetSiblingIndex() + 1);
		}

		// track if the object was active at the start, if using the same object this will be restored at the end, but will be set active while we screw with the text elements
		bool isObjectActive = tmProObject.activeSelf;
		tmProObject.SetActive(true);

		TextMeshPro tmPro = tmProObject.AddComponent<TextMeshPro>();
		
		if (!useSameObject)
		{
			// Set the name and layer.
			if (uiLabelObject.name.FastEndsWith(" Old"))
			{
				// If the original object already has the "Old" suffix, strip it first to name the new object, then put it back on.
				uiLabelObject.name = uiLabelObject.name.Substring(0, uiLabelObject.name.Length - 4);
			}
			tmProObject.name = uiLabelObject.name;
			uiLabelObject.name += " Old";
				
			tmProObject.layer = uiLabelObject.layer;
		}

		// regardless of if this is a new or the same object always default the scale to Vector3.one
		tmProObject.transform.localScale = Vector3.one;
		
		// Set some standard stuff on the new object.
		tmPro.isOrthographic = true;
		
		// Set the font.
		switch (uiLabel.font.name.ToLower())
		{
			case "small":
			case "medium":
			case "large":
			case "title":
				tmPro.font = TMProFontLoader.getFont(STANDARD_FONT_NAME);
				break;
			
			case "numbers small":
			case "numbers large":
			case "numbers huge":
				tmPro.font = TMProFontLoader.getFont(NUMBERS_FONT_NAME);
				break;
		}
		
		// Set word wrapping and font sizing.
		tmPro.enableWordWrapping = (uiLabel.lineWidth > 0);
		tmPro.enableAutoSizing = (uiLabel.shrinkToFit && uiLabel.lineWidth > 0 && uiLabel.lineHeight > 0 && uiLabel.maxLineCount == 0);
		tmPro.lineSpacing = uiLabel.lineSpacing;
		
		if (uiLabel.lineWidth == 0)
		{
			// Give these some size, since there seems to be some strange issues if set to 0.
			tmPro.textContainer.width = 100.0f;
		}
		else
		{
			tmPro.textContainer.width = uiLabel.lineWidth;
		}

		if (uiLabel.lineHeight == 0)
		{
			// Give these some size, since there seems to be some strange issues if set to 0.
			tmPro.textContainer.height = 100.0f;
		}
		else
		{
			tmPro.textContainer.height = uiLabel.lineHeight;
		}

		int fontSize = 72;
		switch (uiLabel.font.name.ToLower())
		{
			case "small":
				fontSize = 32;
				break;
			case "medium":
				fontSize = 47;
				break;
			case "large":
				fontSize = 72;
				tmPro.fontStyle = FontStyles.Bold;
				break;
			case "title":
				fontSize = 134;
				tmPro.fontStyle = FontStyles.Bold;
				break;			
			case "numbers small":
				fontSize = 40;
				break;
			case "numbers large":
				fontSize = 72;
				break;
			case "numbers huge":
				fontSize = 144;
				break;
		}
		tmPro.fontSize = fontSize;		// Just in case autosizing isn't enabled.
		tmPro.fontSizeMax = fontSize;	// Just in case autosizing is enabled, which will be most common.
		
		// Set text.
		tmPro.text = uiLabel.text;
		
		// TODO:UNITY2018:obsoleteTextContainer:confirm
		// Set alignment.
		TMProExtensions.TMProExtensionFunctions.SetPivotAndAlignmentFromUIPivot(tmPro, uiLabel.pivot);

		// Set color.
		tmPro.color = uiLabel.color;
		if (uiLabel.colorMode == UILabel.ColorMode.Gradient)
		{

			if (uiLabel.gradientSteps.Count > 2) {
				tmPro.enableVertexGradient = false;
				tmPro.color = Color.white;   // color will come from texture applied by material to TMPro Face
				Debug.LogWarningFormat("{0}'s UILabel has {1} gradient steps, must use a texture with a new TMPro material because TMPro doesnt support multi-gradients yet", uiLabel.gameObject.name, uiLabel.gradientSteps.Count);
			} else {
				// At this time, TextMeshPro doesn't have support for multiple gradient steps.
				tmPro.enableVertexGradient = true;
				// For some reason the endGradientColor has 0.0 for alpha, so we need to manually make sure it's 1.0 before using it.
				Color bottomColor = uiLabel.endGradientColor;
				bottomColor.a = 1.0f;
				tmPro.colorGradient = new VertexGradient (uiLabel.color, uiLabel.color, bottomColor, bottomColor);
			}
		}
		
		// Set standard effects (outline, shadow).
		switch (uiLabel.effectStyle)
		{
			case UILabel.Effect.Shadow:
				switch (uiLabel.font.name.ToLower())
				{
			case "small":
			case "medium":
			case "large":
			case "title":
						string TMProDropShadowMaterialName = "OpenSans-Bold/Text Dropshadow";
						if(uiLabel.color.CompareRGB(Color.black))
							TMProDropShadowMaterialName = "OpenSans-Bold/Text Dropshadow White";	// black text on black dropshadows looks terrible
						tmPro.fontSharedMaterial = TMProFontLoader.getGeneralMaterial(TMProDropShadowMaterialName);
						break;
					case "numbers small":
					case "numbers large":
					case "numbers huge":
						tmPro.fontSharedMaterial = TMProFontLoader.getGeneralMaterial("monofonto Numbers/Numbers Dropshadow");
						break;
				}
				break;

			case UILabel.Effect.Outline:
				switch (uiLabel.font.name.ToLower())
				{
					case "small":
					case "medium":
					case "large":
					case "title":
						tmPro.fontSharedMaterial = TMProFontLoader.getGeneralMaterial("OpenSans-Bold/Text Outlined");
						break;
					case "numbers small":
					case "numbers large":
					case "numbers huge":
						tmPro.fontSharedMaterial = TMProFontLoader.getGeneralMaterial("monofonto Numbers/Numbers Outlined");
						break;
				}
				break;
		}
		
		// Copy special components, or their equivalents.
		CommonEditor.copyComponent<UILabelStaticText>(uiLabelObject, tmProObject);
		CommonEditor.copyComponent<UIAnchor>(uiLabelObject, tmProObject);

		// Wire up the UILabelStaticText field to the new tmPro object.
		// Technically this is unnecessary since UILabelStaticText can also do this in Awake(), 
		// but this saves a GetComponent() call Awake().
		UILabelStaticText staticTextComponent = tmProObject.GetComponent<UILabelStaticText>();
		if(staticTextComponent != null) 
		{
			staticTextComponent.label = null;	// set UILabel ref to null since we're getting rid of it
			staticTextComponent.labelTMPro = tmPro;
		}
		
		UILabelMaxSizeStretcher sourceMSS = uiLabelObject.GetComponent<UILabelMaxSizeStretcher>();
		if (sourceMSS != null)
		{
			TextMeshProMaxSizeStretcher newMSS = tmProObject.AddComponent<TextMeshProMaxSizeStretcher>();
			newMSS.label = tmPro;
			newMSS.targetSprite = sourceMSS.targetSprite;
			newMSS.direction = (TextMeshProMaxSizeStretcher.Direction)sourceMSS.direction;
			newMSS.pixelOffset = sourceMSS.pixelOffset;
		}

		// force the textContainer to update, because it doesn't always seem to be fully updated when leaving here
		tmPro.ForceMeshUpdate();

		// search for sibling 'LabelShadow' object and link it to new TMPro object, and unlink the old UILabel
		// this is useful for titles, which often have 'Title Label' and 'Title Shadow' siblings
		if(tmPro.gameObject.name.FastEndsWith(" Label")) 
		{
			string labelShadowObjName = tmPro.gameObject.name.Substring(0, tmPro.gameObject.name.Length - 6) + " Shadow";

			LabelShadow labelShadow = tmPro.transform.parent.gameObject.GetComponentInChildren<LabelShadow>();
			if ((labelShadow != null) && (labelShadow.gameObject.name == labelShadowObjName))
			{
				labelShadow.label = null;
				labelShadow.tmPro = tmPro;
			}
		}

		if (useSameObject) {
			// remove the NGUI label now
			if (Application.isEditor) {
				Object.DestroyImmediate (uiLabel);
			} else {
				Object.Destroy (uiLabel);
			}

			// restore the active state of the gameobject
			tmProObject.SetActive (isObjectActive);
		} else {
			uiLabel.gameObject.SetActive(false);	// hide the old uiLabel so we can see how the new one looks
		}
		
		if (selectObjectWhenDone)
		{
			// Select the new object.
			Selection.activeGameObject = tmProObject;
		}
	}
}	

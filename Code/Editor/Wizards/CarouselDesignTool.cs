using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using TMPro;
using TMProExtensions;

/**
   ScriptableWizard to create generate JSON for the selected label to be used in the carousel admin tool.
*/
public class CarouselDesignTool : ScriptableWizard
{
	private const int WINDOW_WIDTH = 560;
	private const int WINDOW_HEIGHT = 350;
	private const string SETUP_SCENE_PATH = "Assets/Data/Common/Scenes/Carousel Setup.unity";
	private const string LABEL_NAME = "Label";
	private const string IMAGE_NAME = "Image";
	
	private List<Transform> labels = new List<Transform>();
	private List<Transform> images = new List<Transform>();

	public enum LabelFont
	{
		// If adding fonts here, also add them to TMProFontLoader.cs
		OPENSANS,
		TEKO,
		POLLERONE,
		MONOFONTO_NUMBERS
		// If adding fonts here, also add them to TMProFontLoader.cs
	}
	
	// These must match the font asset names.
	private List<string> fontNames = new List<string>()
	{
		"OpenSans-Bold SDF",
		"Teko-Bold SDF",
		"PollerOne SDF",
		"monofonto numbers SDF"
	};

	public enum FontFaceTexture
	{
		NONE,
		DIALOG_TITLE
	}
	
	// These must match the texture asset names.
	private List<string> fontFaceTextureNames = new List<string>()
	{
		"",		// No texture used in this index.
		"Dialog Title Gradient"
	};

	private int centerX
	{
		get { return Mathf.RoundToInt(setupScript.imageTemplate.transform.localScale.x * 0.5f); }
	}

	private int centerY
	{
		get { return Mathf.RoundToInt(setupScript.imageTemplate.transform.localScale.y * 0.5f); }
	}
	
	// Returns the selected label.
	private TextMeshPro selectedLabel
	{
		get
		{
			if (Selection.activeTransform != null)
			{
				return Selection.activeTransform.GetComponentInChildren<TextMeshPro>();
			}
			return null;
		}
	}

	// Returns the selected image.
	private Renderer selectedImage
	{
		get
		{
			if (Selection.activeTransform != null)
			{
				// Since TextMeshPro also uses renderers, we need to make sure we're not including that here.
				Renderer renderer = Selection.activeTransform.GetComponent<Renderer>();
				if (renderer != null && renderer.gameObject.GetComponent<TextMeshPro>() == null)
				{
					return renderer;
				}
			}
			return null;
		}
	}
	
	private CarouselSetupScript setupScript
	{
		get
		{
			if (_setupScript == null)
			{
				GameObject go = GameObject.Find("Carousel Slide");
				if (go != null)
				{
					_setupScript = go.GetComponent<CarouselSetupScript>();
				}
			}
			return _setupScript;
		}
	}
	private CarouselSetupScript _setupScript = null;

	[MenuItem ("Zynga/Art Tools/Carousel Design Tool")]static void CreateWizard()
	{
		CarouselDesignTool window = ScriptableWizard.DisplayWizard<CarouselDesignTool>("Carousel Design Tool");

		window.position = new Rect(
			(Screen.currentResolution.width - WINDOW_WIDTH) / 2,
			(Screen.currentResolution.height - WINDOW_HEIGHT) / 2,
			WINDOW_WIDTH,
			WINDOW_HEIGHT
		);
		
		if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path != SETUP_SCENE_PATH)
		{
			EditorApplication.isPlaying = false;
			UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SETUP_SCENE_PATH);
		}

		// Look for existing labels and images when first launching the wizard, just in case it was closed from previous usage.
		window.findExistingLabels();
		window.findExistingImages();
	}
	
	void OnGUI()
	{
		if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path != SETUP_SCENE_PATH)
		{
			if (GUILayout.Button("Load the Carousel Setup Scene"))
			{
				EditorApplication.isPlaying = false;
				UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SETUP_SCENE_PATH);
				findExistingLabels();
				findExistingImages();
			}
			return;
		}

		GUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("", "Labels:", GUILayout.Width(50));
		
		if (labels.Count > 0)
		{
			string[] options = new string[labels.Count];
			for (int i = 0; i < labels.Count; i++)
			{
				options[i] = string.Format("Label {0}", i + 1);
			}
		
			int selectedIndex = GUILayout.SelectionGrid(labels.IndexOf(Selection.activeTransform), options, options.Length);

			if (selectedIndex > -1)
			{
				Selection.activeTransform = labels[selectedIndex];
			}
		}

		if (GUILayout.Button("New Label"))
		{
			addLabel();
		}

		GUILayout.EndHorizontal();
		
		//////////////////////////////////////////////////////////////////////////////////////////////////////
		
		GUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("", "Images:", GUILayout.Width(50));
		
		if (images.Count > 0)
		{
			string[] options = new string[images.Count];
			for (int i = 0; i < images.Count; i++)
			{
				options[i] = string.Format("Image {0}", i + 1);
			}
		
			int selectedIndex = GUILayout.SelectionGrid(images.IndexOf(Selection.activeTransform), options, options.Length);

			if (selectedIndex > -1)
			{
				Selection.activeTransform = images[selectedIndex];
			}
		}

		if (GUILayout.Button("New Image"))
		{
			addImage();
		}

		GUILayout.EndHorizontal();
		GUILayout.Space(10);
		
		//////////////////////////////////////////////////////////////////////////////////////////////////////
		
		TextMeshPro label = selectedLabel;
		Renderer image = selectedImage;
		
		if (label != null)
		{
			UILabelStaticText staticText = label.transform.GetComponent<UILabelStaticText>();

			GUILayout.BeginHorizontal();
	
			if (GUILayout.Button("Copy Label JSON To Clipboard"))
			{
				generateJson();
			}

			JSON labelJson = getCopyBufferJson();
			if (labelJson != null)
			{
				if (labelJson.isValid &&
					labelJson.hasKey("x") &&
					labelJson.hasKey("y") &&
					labelJson.hasKey("width") &&
					labelJson.hasKey("height") &&
					!labelJson.hasKey("z") &&	// The label JSON must NOT have a z property.
					GUILayout.Button("Apply Label JSON From Clipboard"))
				{
					CarouselPanelCustom.applyLabelDesignData(label, labelJson);
					// Also set the allCaps property.
					setAllCaps(staticText, labelJson.getBool("is_all_caps", false));
				}
			}

			LabelFont font = (LabelFont)fontNames.IndexOf(label.font.name);
			
			FontFaceTexture fontFaceTex = FontFaceTexture.NONE;
			Texture tex = label.getFaceTexture();
			if (tex != null)
			{
				fontFaceTex = (FontFaceTexture)fontFaceTextureNames.IndexOf(tex.name);
			}
			
			float scaleX = Mathf.Clamp(label.transform.localScale.x, 0.5f, 2.0f);
			int maxFontSize = Mathf.RoundToInt(label.fontSizeMax);
			int x = Mathf.RoundToInt(label.transform.localPosition.x);
			int y = Mathf.RoundToInt(label.transform.localPosition.y);
			int width = (int)label.textContainer.width;
			int height = (int)label.textContainer.height;
			int rotation = Mathf.RoundToInt(label.transform.localEulerAngles.z);
			bool allCaps = staticText.allCaps;
		
			GUILayout.EndHorizontal();
			GUILayout.Space(10);
			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Delete Label"))
			{
				deleteLabel();
				return;
			}
		
			GUILayout.EndHorizontal();
			GUILayout.Space(10);
			GUILayout.BeginHorizontal();

			x = EditorGUILayout.IntField("Position X:", x, GUILayout.ExpandWidth(false));
			y = EditorGUILayout.IntField("Position Y:", y, GUILayout.ExpandWidth(false));
		
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();

			width = EditorGUILayout.IntField("Width:", width, GUILayout.ExpandWidth(false));
			height = EditorGUILayout.IntField("Height:", height, GUILayout.ExpandWidth(false));
		
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();

			rotation = EditorGUILayout.IntField("Rotation:", rotation, GUILayout.ExpandWidth(false));
			scaleX = Mathf.Clamp(EditorGUILayout.FloatField("Scale X:", scaleX, GUILayout.ExpandWidth(false)), 0.5f, 2.0f);

			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();

			maxFontSize = Mathf.Clamp(EditorGUILayout.IntField("Max Font Size:", maxFontSize, GUILayout.ExpandWidth(false)), 24, 200);

			if (maxFontSize != label.fontSizeMax)
			{
				label.enableAutoSizing = true;	// Just making sure.
				label.fontSizeMax = maxFontSize;
				// For some reason, TextMeshPro doesn't update the font size change until deselecting the object.
			}

			LabelFont newFont = (LabelFont)EditorGUILayout.EnumPopup("Font:", font);

			if (font != newFont)
			{
				label.font = TMProFontLoader.getFont(fontNames[(int)newFont]);
				CarouselPanelCustom.setExtraPadding(label);
				label.makeMaterialInstance();	// Must do whenever changing the font.
			}
			
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();

			// TODO:UNITY2018:obsoleteTextContainer:confirm
			TextAlignmentOptions alignment = (TextAlignmentOptions)EditorGUILayout.EnumPopup("Alignment", label.alignment);
			label.alignment = alignment;

			FontFaceTexture newFontFaceTex = (FontFaceTexture)EditorGUILayout.EnumPopup("Texture:", fontFaceTex);
			
			if (fontFaceTex != newFontFaceTex)
			{
				label.setFaceTexture(CarouselPanelCustom.getFontFaceTexture(fontFaceTextureNames[(int)newFontFaceTex]));
			}

			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();

			allCaps = EditorGUILayout.ToggleLeft("All Caps:", allCaps, GUILayout.ExpandWidth(false));
			
			GUILayout.EndHorizontal();

			label.transform.localEulerAngles = new Vector3(0, 0, rotation);
			label.transform.localPosition = new Vector3(x, y, -10);
			label.textContainer.width = width;
			label.textContainer.height = height;
			setAllCaps(staticText, allCaps);

			CarouselPanelCustom.applyLabelSize(label, scaleX, 1.0f);

			GUILayout.Space(10);

			drawGUILabel("To set color, bold, italic, underline, and line spacing, edit the properties in the Font Settings inspector.");
			drawGUILabel("To set outline and shadow, use the Underlay tab on the material inspector.");
			drawGUILabel("The settings on the Face and Outline tabs are ignored.");

			CarouselPanelCustom.enforceAlignment(label);
		}
		
		if (image != null)
		{
			Transform imageTransform = image.transform;
			
			GUILayout.BeginHorizontal();
	
			if (GUILayout.Button("Copy Image JSON To Clipboard"))
			{
				generateJson();
			}

			// Only show the Paste button if the copy buffer contains valid JSON.
			JSON imageJson = getCopyBufferJson();
			if (imageJson != null)
			{
				if (imageJson.isValid &&
					imageJson.hasKey("x") &&
					imageJson.hasKey("y") &&
					imageJson.hasKey("z") &&
					imageJson.hasKey("width") &&
					imageJson.hasKey("height") &&
					GUILayout.Button("Apply Image JSON From Clipboard"))
				{
					CarouselPanelCustom.applyImageDesignData(imageTransform, imageJson);
				}
			}

			int x = Mathf.RoundToInt(imageTransform.localPosition.x);
			int y = Mathf.RoundToInt(imageTransform.localPosition.y);
			int z = Mathf.RoundToInt(imageTransform.localPosition.z);
			int rotation = Mathf.RoundToInt(imageTransform.localEulerAngles.z);
			int width = Mathf.RoundToInt(imageTransform.localScale.x);
			int height = Mathf.RoundToInt(imageTransform.localScale.y);

			GUILayout.EndHorizontal();
			GUILayout.Space(10);
			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Delete Image"))
			{
				deleteImage();
				return;
			}
		
			GUILayout.EndHorizontal();
			GUILayout.Space(10);
			GUILayout.BeginHorizontal();

			x = EditorGUILayout.IntField("Position X:", x, GUILayout.ExpandWidth(false));
			y = EditorGUILayout.IntField("Position Y:", y, GUILayout.ExpandWidth(false));
		
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();

			width = EditorGUILayout.IntField("Width:", width, GUILayout.ExpandWidth(false));
			height = EditorGUILayout.IntField("Height:", height, GUILayout.ExpandWidth(false));

			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();

			rotation = EditorGUILayout.IntField("Rotation:", rotation, GUILayout.ExpandWidth(false));
			z = EditorGUILayout.IntField("Position Z:", z, GUILayout.ExpandWidth(false));

			GUILayout.EndHorizontal();
		
			imageTransform.localEulerAngles = new Vector3(0, 0, rotation);
			imageTransform.localPosition = new Vector3(x, y, z);
			imageTransform.localScale = new Vector3(width, height, 1.0f);
		}
	}
	
	private void drawGUILabel(string text)
	{
		GUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("", text);
		GUILayout.EndHorizontal();
	}
	
	private void setAllCaps(UILabelStaticText staticText, bool allCaps)
	{
		staticText.allCaps = allCaps;
		
		// Set the preview text capitalization.
		if (allCaps)
		{
			staticText.text = Localize.toUpper(staticText.text);
		}
		else
		{
			staticText.text = Localize.toTitle(staticText.text);
		}
	}
	
	private JSON getCopyBufferJson()
	{
		string buffer = EditorGUIUtility.systemCopyBuffer.Trim();
		
		if (buffer != "" &&
			buffer.FastStartsWith("{") &&
			buffer.FastEndsWith("}")
			)
		{
			JSON json = new JSON(buffer);
			if (json.isValid)
			{
				return json;
			}
		}
		return null;
	}
	
	private void generateJson()
	{
		string json = "";

		if (selectedLabel != null)
		{
			json = generateLabelJson();
		}
		else if (selectedImage != null)
		{
			json = generateImageJson();
		}
		
		EditorGUIUtility.systemCopyBuffer = json;
	}
	
	private string generateLabelJson()
	{
		TextMeshPro label = selectedLabel;
		UILabelStaticText staticText = label.transform.GetComponent<UILabelStaticText>();

		string alignment = "";
	
		// TODO:UNITY2018:obsoleteTextContainer:confirm
		switch (label.alignment)
		{
			case TextAlignmentOptions.Center:
				alignment = "center";
				break;
			case TextAlignmentOptions.Left:
				alignment = "left";
				break;
			case TextAlignmentOptions.Right:
				alignment = "right";
				break;
			case TextAlignmentOptions.Top:
				alignment = "top";
				break;
			case TextAlignmentOptions.Bottom:
				alignment = "bottom";
				break;
			case TextAlignmentOptions.TopLeft:
				alignment = "top_left";
				break;
			case TextAlignmentOptions.TopRight:
				alignment = "top_right";
				break;
			case TextAlignmentOptions.BottomLeft:
				alignment = "bottom_left";
				break;
			case TextAlignmentOptions.BottomRight:
				alignment = "bottom_right";
				break;
		}
	
		string fontName = CarouselPanelCustom.DEFAULT_FONT_NAME;
		if (label.font != null)
		{
			fontName = label.font.name;
		}

		string fontFaceTextureName = "";
		Texture tex = label.getFaceTexture();
		if (tex != null)
		{
			fontFaceTextureName = tex.name;
		}
	
		float scaleX = Mathf.Clamp(label.transform.localScale.x, 0.5f, 2.0f);
		float effectDistance = 0.0f;
		string effectStyle = "";
		float effectSoftness = 0.0f;
		
		if (label.isUnderlayEnabled())
		{
			Vector2 offset = label.getUnderlayOffset();
			if (offset.x != 0.0f || offset.y != 0.0f)
			{
				effectDistance = label.getUnderlayOffset().y;
				effectStyle = "shadow";
			}
			else
			{
				float dilate = label.getUnderlayDilate();
				if (dilate != 0.0f)
				{
					effectStyle = "outline";
					effectDistance = dilate;
				}
			}
			
			effectSoftness = label.getUnderlaySoftness();
		}

		string json = "{" +
			JSON.createJsonString("x", Mathf.RoundToInt(label.transform.localPosition.x)) + ", " +
			JSON.createJsonString("y", Mathf.RoundToInt(label.transform.localPosition.y)) + ", " +
			JSON.createJsonString("width", Mathf.RoundToInt(label.textContainer.width)) + ", " +
			JSON.createJsonString("height", Mathf.RoundToInt(label.textContainer.height)) + ", ";
	
		int rotation = Mathf.RoundToInt(label.transform.localEulerAngles.z);
		if (rotation != 0)
		{
			json += JSON.createJsonString("rotation", rotation) + ", ";
		}
	
		if (scaleX != 1.0f)
		{
			json += JSON.createJsonString("scale_x", scaleX) + ", ";
		}
		
		json +=
			JSON.createJsonString("alignment", alignment) + ", " +
			JSON.createJsonString("data", "") + ", " +
			JSON.createJsonString("line_spacing", Mathf.RoundToInt(label.lineSpacing)) + ", " +
			JSON.createJsonString("font", fontName) + ", " +
			JSON.createJsonString("max_font_size", (int)label.fontSizeMax) + ", " +
			JSON.createJsonString("style", (int)label.fontStyle) + ", " +
			JSON.createJsonString("effect", effectStyle) + ", " +
			JSON.createJsonString("effect_distance", effectDistance) + ", " +
			JSON.createJsonString("effect_softness", effectSoftness) + ", " +
			JSON.createJsonString("effect_color", CommonColor.colorToHexWithAlpha(label.getUnderlayColor())) + ", " +
			JSON.createJsonString("is_all_caps", staticText.allCaps);
	
		if (fontFaceTextureName != "")
		{
			json += ", " +
				JSON.createJsonString("font_face_texture", fontFaceTextureName);		
		}
	
		if (label.enableVertexGradient)
		{
			json += ", " +
				JSON.createJsonString("is_gradient", true) + ", " +
				JSON.createJsonString("color", CommonColor.colorToHex(label.colorGradient.topLeft)) + ", " +
				JSON.createJsonString("end_gradient_color", CommonColor.colorToHex(label.colorGradient.bottomLeft));
		}
		else
		{
			json += ", " +
				JSON.createJsonString("color", CommonColor.colorToHexWithAlpha(label.color));
		}
	
		json += "}";
		
		return json;
	}

	private string generateImageJson()
	{
		Transform imageTransform = selectedImage.transform;
		
		string json = "{" +
			JSON.createJsonString("x", Mathf.RoundToInt(imageTransform.localPosition.x)) + ", " +
			JSON.createJsonString("y", Mathf.RoundToInt(imageTransform.localPosition.y)) + ", " +
			JSON.createJsonString("z", Mathf.RoundToInt(imageTransform.localPosition.z)) + ", " +
			JSON.createJsonString("width", Mathf.RoundToInt(imageTransform.localScale.x)) + ", " +
			JSON.createJsonString("height", Mathf.RoundToInt(imageTransform.localScale.y));

		int rotation = Mathf.RoundToInt(imageTransform.localEulerAngles.z);
		if (rotation != 0)
		{
			json += ", " + JSON.createJsonString("rotation", rotation);
		}

		json += "}";
		
		return json;
	}
	
	// Adds a new label and selects it. All new labels will use TextMeshPro
	private void addLabel()
	{		
		GameObject go = NGUITools.AddChild(setupScript.elementsParent, setupScript.labelTemplate);
		go.SetActive(true);
		go.transform.localPosition = new Vector3(centerX, centerY, -10.0f);
		go.name = LABEL_NAME;
		
		// Make an instance of the material so changing color, etc. won't affect the original material.
		// This does cause a red Unity warning about leaking materials into the scene when in edit mode,
		// but those can be ignored.
		TextMeshPro label = go.GetComponent<TextMeshPro>();
		if (label != null)
		{
			label.makeMaterialInstance();
		}
		else
		{
			Debug.LogError("Did not find TextMeshPro component on the new label object!");
		}
		
		CarouselPanelCustom.setExtraPadding(label);
		
		Selection.activeTransform = go.transform;
		
		labels.Add(go.transform);
	}
	
	// Removes the currently selected label.
	private void deleteLabel()
	{
		TextMeshPro label = selectedLabel;
		
		if (label == null)
		{
			return;
		}
		
		// Just in case the label has a scaler parent, remove both from the list.
		labels.Remove(label.transform);
		
		DestroyImmediate(label.gameObject);
		
		if (labels.Count > 0)
		{
			// If there are still labels, select the first one.
			Selection.activeTransform = labels[0].transform;
		}
	}

	// Adds a new image and selects it.
	private void addImage()
	{		
		GameObject go = NGUITools.AddChild(setupScript.elementsParent, setupScript.imageTemplate);
		go.SetActive(true);
		go.transform.localScale = setupScript.imageTemplate.transform.localScale;
		go.transform.localPosition = new Vector3(centerX, centerY, 0.0f);
		go.name = IMAGE_NAME;
				
		Selection.activeTransform = go.transform;
				
		images.Add(go.transform);
	}
	
	// Removes the currently selected image.
	private void deleteImage()
	{
		Renderer image = selectedImage;
		
		if (image == null)
		{
			return;
		}
		
		images.Remove(image.transform);
		
		DestroyImmediate(image.gameObject);
		
		if (images.Count > 0)
		{
			// If there are still labels, select the first one.
			Selection.activeTransform = images[0].transform;
		}
	}
	
	private void findExistingLabels()
	{
		labels.Clear();

		if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path != SETUP_SCENE_PATH)
		{
			return;
		}
		
		TextMeshPro[] labelScripts = setupScript.elementsParent.GetComponentsInChildren<TextMeshPro>();
		
		foreach (TextMeshPro label in labelScripts)
		{
			labels.Add(label.transform);
		}
	}

	private void findExistingImages()
	{
		images.Clear();

		if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path != SETUP_SCENE_PATH)
		{
			return;
		}
		
		Renderer[] renderers = setupScript.elementsParent.GetComponentsInChildren<Renderer>();
		
		foreach (Renderer renderer in renderers)
		{
			// Since TextMeshPro also uses renderers, we need to make sure we're not including those here.
			if (renderer.gameObject.GetComponent<TextMeshPro>() == null)
			{
				images.Add(renderer.transform);
			}
		}
	}
}

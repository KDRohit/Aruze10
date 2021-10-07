using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Helpful functions to extend NGUI functionality without changing the NGUI source files just in case we need to upgrade NGUI.
*/

public class NGUIExt : IResetGame
{
	public enum SceneAnchor
	{
		NONE,
		CENTER,
		OVERLAY_CENTER,
		DIALOG
	}

	public static UIRoot uiRoot = null;	///< The root NGUI component. Set by NGUILoader.

	public static Dictionary<GameObject, bool> disabledButtons = new Dictionary<GameObject, bool>();	///< List of buttons that have been disabled, to be enabled again later.
	public static Dictionary<Transform, Vector3> hiddenPositions = new Dictionary<Transform, Vector3>();		///< List of positions of hidden objects, to be shown again later.

	// mobile specific - list of all buttons
	public static List<GameObject> allButtonsCached = new List<GameObject>();

	public const float BASE_SCREEN_WIDTH = 2048f;	// The screen height that the NGUI stuff is based on [2048x1536 for iPad 4].

	public const string UI_ROOT = "UI Root (2D)";

	public const string UI_CAMERA_PATH = UI_ROOT + "/0 Camera";
	public const string UI_OVERLAY_PATH = UI_ROOT + "/2 Overlay Camera";

	public static bool areButtonsEnabled = true;							///< Are buttons enabled or not?
	public static List<GameObject> allExceptions = new List<GameObject>();	///< Button exceptions to disabling

	public static Shader guiShader
	{
		get
		{
			if (_guiShader == null)
			{
				_guiShader = ShaderCache.find("Unlit/Transparent Colored");
			}
			return _guiShader;
		}
	}
	private static Shader _guiShader = null;

	/// Returns a multiplier that represents pixel DPI relative to the NGUI manual height,
	/// which is constant at 1536 (iPad retina height).
	public static float pixelFactor
	{
		get
		{
			if (!Application.isPlaying || uiRoot == null)
			{
				// When in the editor, default to low res pixel factor.
				return .5f;
			}
			return (float)Screen.height / NGUIExt.uiRoot.manualHeight;
		}
	}

	/// Returns the basic aspect ratio of the screen, rounded to 2 decimal places for more accurate float comparison.
	public static float aspectRatio
	{
		get
		{
			return CommonMath.round((float)Screen.width / Screen.height, 2);
		}
	}

	// Returns the screen width that NGUI sees, based on the base screen resolution and current aspect ratio.
	public static float effectiveScreenWidth
	{
		get
		{
			return (float)Screen.width / pixelFactor;
		}
	}
	
	public static float effectiveScreenHeight
	{
		get
		{
			return (float)Screen.height / pixelFactor;
		}
	}

	// Disables mouse activity for all UICamera objects.
	// Internally to NGUI, this disables raycasting.
	// Since Android devices have an external back button,
	// we need to disable that here too, to prevent unexpected input.
	public static void disableAllMouseInput()
	{
		AndroidUtil.isBackEnabled = false;
		UICamera[] uiCameras = Object.FindObjectsOfType(typeof(UICamera)) as UICamera[];
		foreach (UICamera uiCam in uiCameras)
		{
			uiCam.disablePlatformInput();
		}
	}

	// Enables mouse activity for all UICamera objects.
	// Internally to NGUI, this re-enables raycasting.
	public static void enableAllMouseInput()
	{
		if (Glb.isQuitting)
		{
			return;
		}

		AndroidUtil.isBackEnabled = true;

		UICamera[] uiCameras = Object.FindObjectsOfType(typeof(UICamera)) as UICamera[];
		foreach (UICamera uiCam in uiCameras)
		{
			uiCam.enablePlatformInput();
		}
	}

	/// This method can be used to attach an NGui panel to the proper object in the scene.
	public static void attachToAnchor(GameObject child, SceneAnchor anchor, Vector3 offset)
	{
		if (child != null)
		{
			if (uiRoot == null && Application.isPlaying)
			{
				Debug.LogError(string.Format("Could not attach '{0}', NGui root object was not assigned from NGUILoader", child.name));
			}

			GameObject parent = null;

			switch (anchor)
			{
				case SceneAnchor.CENTER:
					parent = GameObject.Find(UI_CAMERA_PATH + "/Anchor Center");
					break;
				case SceneAnchor.OVERLAY_CENTER:
					parent = GameObject.Find(UI_OVERLAY_PATH + "/Anchor Center");
					break;
				case SceneAnchor.DIALOG:
					parent = Dialog.instance.gameObject;
					break;
			}

			if (parent == null)
			{
				Debug.LogWarning("Couldn't find NGUI anchor " + anchor + " for GameObject " + child.name);
			}
			else
			{
				child.transform.parent = parent.transform;
				child.transform.localScale = Vector3.one;
				child.transform.localPosition = offset;
			}
		}
	}


	/// Get the pixel size of the text on the label.
	public static Vector2 getLabelPixelSize(UILabel label)
	{
		if (label == null || label.processedText == null || label.font == null)
		{
			return Vector2.one;
		}

		Vector2 size = !string.IsNullOrEmpty(label.processedText) ? label.font.CalculatePrintedSize(label.processedText, label.supportEncoding, UIFont.SymbolStyle.None, label.lineSpacing) : Vector2.one;
		size.x *= label.transform.localScale.x;
		size.y *= label.transform.localScale.y;
		return size;
	}


/*
	/// Streams an asset bundle with a texture to apply to a UITexture for drawing directly without using an atlas.
	public static IEnumerator useAssetTexture(UITexture uiTexture, string assetKey, int textureIndex = 0, int pixelWidth = 0, int pixelHeight = 0)
	{
		// Start downloading the image asset and wait for it to finish.
		AssetDownloadManager.linkAsset(assetKey);

		while (AssetDownloadManager.getDownloadStatus(assetKey) == DownloadStatus.DOWNLOADING)
		{
			yield return new WaitForSeconds(0);
		}

		if (AssetDownloadManager.getDownloadStatus(assetKey) == DownloadStatus.COMPLETE)
		{
			// Success. Assign the texture and show the element.
			AssetInfo asset = AssetDownloadManager.getAssetInfo(assetKey);
			if (textureIndex >= asset.textureUpdates.Length)
			{
				Debug.LogError(string.Format("Asset bundle texture index {0} was requested but only {1} textures exist in the bundle.", textureIndex, asset.textureUpdates.Length));
			}
			else
			{
				Texture texture = asset.textureUpdates[textureIndex].texture;

				Material mat = new Material(guiShader);
				mat.mainTexture = texture;

				uiTexture.material = mat;

				// Don't do MakePixelPerfect. The mobile versions rely on the image to be
				// scaled to fit the area defined by the UITexture's localScale. If that
				// scale's width and height are already matching the image, then it will
				// be pixel perfect on the web version without needing to make this
				// mobile-breaking MakePixelPerfect call.
				// uiTexture.MakePixelPerfect();

				Vector3 scale = uiTexture.transform.localScale;
				if (pixelWidth != 0)
				{
					scale.x = pixelWidth;
				}
				if (pixelHeight != 0)
				{
					scale.y = pixelHeight;
				}
				uiTexture.transform.localScale = scale;
			}
		}
	}
*/

	/// Applies a given texture to a NGUI texture object.
	public static void applyUITexture(UITexture uiTexture, Texture texture, bool useOriginalMaterial = false)
	{
		Material mat = null;
		if (useOriginalMaterial && uiTexture.material != null)
		{
			mat = new Material(uiTexture.material);
		}
		else
		{
			mat = new Material(guiShader);
		}
		mat.mainTexture = texture;

		uiTexture.material = mat;

		// Don't do MakePixelPerfect. The mobile versions rely on the image to be
		// scaled to fit the area defined by the UITexture's localScale. If that
		// scale's width and height are already matching the image, then it will
		// be pixel perfect on the web version without needing to make this
		// mobile-breaking MakePixelPerfect call.
		// uiTexture.MakePixelPerfect();
	}

	/// Applies a texture from a symbol, which may be uv mapped from a large texture, to an NGUI texture object.
	public static void applyUITextureFromSymbol(UITexture uiTexture, SymbolInfo info)
	{
		Material mat = new Material(guiShader);
		info.applyTextureToMaterial(mat);

		uiTexture.material = mat;
		
		// Only setup our own UVs if we don't have a uvMappedMaterial
		if(!info.isUVMappedMaterial())
		{
			uiTexture.uvRect = new Rect(mat.mainTextureOffset.x , mat.mainTextureOffset.y, mat.mainTextureScale.x, mat.mainTextureScale.y);
		}

		// Don't do MakePixelPerfect. The mobile versions rely on the image to be
		// scaled to fit the area defined by the UITexture's localScale. If that
		// scale's width and height are already matching the image, then it will
		// be pixel perfect on the web version without needing to make this
		// mobile-breaking MakePixelPerfect call.
		// uiTexture.MakePixelPerfect();
	}

	/// Returns the screen position of an object based on a given camera.
	public static Vector2int screenPositionOfWorld(Camera theCamera, Vector3 worldPosition)
	{
		if (theCamera == null)
		{
			// Commented out the below validation because it spams errors with the tutorial spotlight and arrows.

			// validate camera to ensure against bad implementations
			//Debug.LogError("The camera passed in is Null.");

			return Vector2int.zero;
		}

		Vector3 screenPos = Vector3.zero;

		// We find the pixel position within the viewport...
		Vector3 vPort = theCamera.WorldToViewportPoint(worldPosition);
		vPort.x *= theCamera.pixelRect.width;
		vPort.y *= theCamera.pixelRect.height;

		// ...add the viewport's screen position.
		screenPos.x = vPort.x + theCamera.pixelRect.x;
		screenPos.y = vPort.y + theCamera.pixelRect.y;

		return new Vector2int((int)screenPos.x, (int)screenPos.y);
	}
	
	/// Takes world position coords and returns the local position relative to the given transform.
	/// Only deals with x and y coords.
	public static Vector2 localPositionOfPosition(Transform transform, Vector3 position)
	{
		Vector3 worldScale = CommonTransform.getWorldScale(transform);
		
		float diffX = position.x - transform.position.x;
		float diffY = position.y - transform.position.y;
		
		return new Vector2(diffX / worldScale.x, diffY / worldScale.y);
	}

	/// Returns the screen coordinate bounds from the bounds of the given object. z property is ignored.
	public static Bounds screenBounds(Camera theCamera, GameObject obj, bool usePixelFactor = true)
	{
		// This function is a duplicate of the one in UIPixelPositioner, since the old UI stuff will eventually be obsolete.
		Bounds bounds = CommonGameObject.getObjectBounds(obj, false, usePixelFactor);

		Vector2int center = screenPositionOfWorld(theCamera, bounds.center);
		if (usePixelFactor)
		{
			center.x = (int)(pixelFactor * center.x);
			center.y = (int)(pixelFactor * center.y);
		}

		Vector2int point1 = screenPositionOfWorld(theCamera, bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z));
		Vector2int point2 = screenPositionOfWorld(theCamera, bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, bounds.extents.z));
		Vector2int point3 = screenPositionOfWorld(theCamera, bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, -bounds.extents.z));
		Vector2int point4 = screenPositionOfWorld(theCamera, bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z));
		Vector2int point5 = screenPositionOfWorld(theCamera, bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z));
		Vector2int point6 = screenPositionOfWorld(theCamera, bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z));
		Vector2int point7 = screenPositionOfWorld(theCamera, bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, -bounds.extents.z));
		Vector2int point8 = screenPositionOfWorld(theCamera, bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z));

		Vector2int max = new Vector2int(point1.x, point1.y);

		max.x = Mathf.Max(max.x, point2.x);
		max.x = Mathf.Max(max.x, point3.x);
		max.x = Mathf.Max(max.x, point4.x);
		max.x = Mathf.Max(max.x, point5.x);
		max.x = Mathf.Max(max.x, point6.x);
		max.x = Mathf.Max(max.x, point7.x);
		max.x = Mathf.Max(max.x, point8.x);

		max.y = Mathf.Max(max.y, point2.y);
		max.y = Mathf.Max(max.y, point3.y);
		max.y = Mathf.Max(max.y, point4.y);
		max.y = Mathf.Max(max.y, point5.y);
		max.y = Mathf.Max(max.y, point6.y);
		max.y = Mathf.Max(max.y, point7.y);
		max.y = Mathf.Max(max.y, point8.y);

		Vector2int min = new Vector2int(point1.x, point1.y);

		min.x = Mathf.Min(min.x, point2.x);
		min.x = Mathf.Min(min.x, point3.x);
		min.x = Mathf.Min(min.x, point4.x);
		min.x = Mathf.Min(min.x, point5.x);
		min.x = Mathf.Min(min.x, point6.x);
		min.x = Mathf.Min(min.x, point7.x);
		min.x = Mathf.Min(min.x, point8.x);

		min.y = Mathf.Min(min.y, point2.y);
		min.y = Mathf.Min(min.y, point3.y);
		min.y = Mathf.Min(min.y, point4.y);
		min.y = Mathf.Min(min.y, point5.y);
		min.y = Mathf.Min(min.y, point6.y);
		min.y = Mathf.Min(min.y, point7.y);
		min.y = Mathf.Min(min.y, point8.y);

		return new Bounds(new Vector3(center.x, center.y, 0), new Vector3(max.x - min.x, max.y - min.y, 0));
	}

/*
	/// Return the bounds of objects in a hierarchy rooted at "go", accounting for NGUI label size.
	/// Optionally exclude the root node pivot.
	public static Bounds findNGUIPixelBounds(GameObject go, bool excludeRootObjectLocal = false)
	{
		Bounds bounds = new Bounds();

		foreach (GameObject current in CommonGameObject.findAllChildren(go))
		{
			Vector3 center = current.transform.localPosition;
			Vector3 scale = current.transform.localScale;

			// Include root local tranforms?
			if (excludeRootObjectLocal && current == go)
			{
				center = Vector3.zero;
				scale = Vector3.one;
			}

			UILabel label = current.GetComponent<UILabel>();
			if (label != null)
			{
				Vector2 labelSize = NGUIExt.getLabelPixelSize(label);

				center.x += (label.pivotOffset.x + 0.5f) * labelSize.x;
				center.y += (label.pivotOffset.y - 0.5f)  * labelSize.y;

				scale = new Vector3(labelSize.x, labelSize.y, 1);
			}

			Bounds newBounds = new Bounds(center, scale);

			bounds.Encapsulate(newBounds);

			//Debug.Log(string.Format("t:{0} b:{1}", bounds.size, newBounds.size));
		}
		return bounds;
	}
*/
/*
	public static void revealText(UILabel label, string text, int revealSpeed)
	{
		RevealTextNGUIScript reveal = label.gameObject.GetComponent<RevealTextNGUIScript>();
		if (reveal != null)
		{
			// As a component, reveal should be safe for DestroyImmediate
			GameObject.DestroyImmediate(reveal);
		}
		reveal = label.gameObject.AddComponent<RevealTextNGUIScript>();
		reveal.startReveal(text, revealSpeed);
	}
*/
/*
	// COMMENTED OUT BUTTON DISABLING
	// This needs some review and care/planning if we decide to use it.

	/// Disables all button colliders except for the one provided. May provide null to disable all of them.
	public static void disableButtons()
	{
		disableButtonsExcept(new List<GameObject>());
	}

	/// Disables all button colliders except for the one provided. May provide null to disable all of them.
	public static void disableButtonsExcept(GameObject except)
	{
		List<GameObject> list = new List<GameObject>();
		list.Add(except);
		disableButtonsExcept(list);
	}

	public static void addButton(GameObject obj)
	{
		if(obj == null || obj.collider == null || obj.tag == "Always Enabled Button")
		{
			return;
		}

		if(!allButtonsCached.Contains(obj))
		{
			allButtonsCached.Add(obj);
		}
	}

	public static void removeButton(GameObject obj)
	{
		allButtonsCached.Remove(obj);
	}

	/// Disables all button colliders except for the ones provided. May provide empty list to disable all of them.
	public static void disableButtonsExcept(List<GameObject> except)
	{
		// Before disabling the buttons, re-enable any that were already disabled so they don't get stuck disabled.
		enableButtons();

		// We need to make a copy of the passed in list, so we don't add the AlwaysEnabledButton objects
		// to the actual focusObjects list that was passed in by reference.
		allExceptions = new List<GameObject>(except);

		foreach (GameObject button in allButtonsCached)
		{
			if (!except.Contains(button.gameObject))
			{
				// Only disable it if it is currently enabled, so it doesn't get enabled when enableButtons() is called.
				if (!disabledButtons.ContainsKey(button.gameObject))
				{
					disabledButtons.Add(button.gameObject, true);
				}
			}
		}

		areButtonsEnabled = false;
	}

	/// Refreshes any disabled button colliders. This is used when other things might be changing the enabled state of
	/// button colliders. It uses the most recent set of disabled things, and does nothing if buttons aren't disabled.
	public static void refreshDisabledButtons()
	{
		if (!areButtonsEnabled)
		{
			disableButtonsExcept(allExceptions);
		}
	}

	/// Enables all button colliders.
	public static void enableButtons()
	{
		foreach (KeyValuePair<GameObject, bool> p in disabledButtons)
		{
			GameObject obj = p.Key;
			if (obj != null && obj.collider != null)
			{
				obj.collider.enabled = true;
			}
		}

		disabledButtons.Clear();
		areButtonsEnabled = true;
	}

	public static bool isDisabledButton(GameObject obj)
	{
		if (obj == null || obj.collider == null)
		{
			return false;
		}

		return disabledButtons.ContainsKey(obj);
	}
*/
	/// Returns the camera that is being used to render a given object in the NGUI tree.
	public static Camera getObjectCamera(GameObject obj)
	{
		if (obj == null)
		{
			// no camera to find for a null object
			Debug.LogWarning(" NGUIExt.getObjectCamera() - obj passed was null!");
			return null;
		}

		// Optimization: Use a dictionary to find the camera component associated with the UI camera.
		// The dictionary saves the transform => Camera (from the game object holding the UICamera, see UICamera.cs)
		Dictionary<Transform,Camera> uiCameras = UICamera.sUICameraTransformDic;
		Camera cam;
		Transform _parent = obj.transform.parent;
		do
		{
			if (uiCameras.TryGetValue(_parent, out cam))
			{
				// make sure the camera actually targets the same layer as the passed in object
				UICamera currentUICamera = cam.GetComponent<UICamera>();
				if ((currentUICamera.eventReceiverMask.value & (1 << obj.layer)) != 0 || obj.layer == Layers.ID_HIDDEN)
				{
					return cam;
				}
			}
			_parent = _parent.parent;
		}
		while ( !System.Object.ReferenceEquals(_parent, null) );

		// in case the uiCameras dictionary was not filled , use slow path
		UICamera camParent = CommonGameObject.findComponentInParent("UICamera", obj) as UICamera;
		if (camParent != null)
		{
			if ((camParent.eventReceiverMask.value & (1 << obj.layer)) != 0 || obj.layer == Layers.ID_HIDDEN)
			{
				return camParent.GetComponent<Camera>();
			}
		}

		// if those fail, the camera may not be a parent of this object, 
		// so let's look for a UICamera that is marked as handling the same layer as the passed obj
		// NOTE: There is the possibility this might fail if two cameras handle the same layer, as this will return the first one found that the object is seen by
		Dictionary<Transform, Camera>.ValueCollection uiCameraValues = uiCameras.Values;
		foreach (Camera currentCamera in uiCameraValues)
		{
			UICamera currentUICamera = currentCamera.GetComponent<UICamera>();
			if (currentUICamera != null)
			{
				if ((currentUICamera.eventReceiverMask.value & (1 << obj.layer)) != 0 || obj.layer == Layers.ID_HIDDEN)
				{
					// check if the object is inside of the camera (just using center point, so may not be the most accurate,
					// but this is more of a fallback section anyways, and we should try to make sure the object is inside
					// of whatever camera we are going to return).
					if (CommonCamera.isPointInCamera(obj.transform.position, currentCamera))
					{
						// found layer match for obj, so returning this camera
						return currentCamera;
					}
				}
			}
		}

		Debug.LogWarning(" NGUIExt.getObjectCamera() - No UICamera was found that can click on this object: obj.name = " + obj.name, obj);
		return null;
	}

	/// Changes the alpha channel on all UIWidgets on this object
	public static void fadeGameObject(GameObject obj, float alpha)
	{
		if (obj == null)
		{
			return;
		}
		List<UIWidget> list = new List<UIWidget>();
		list.AddRange(obj.GetComponentsInChildren<UIWidget>());
		fadeGameObject(list, alpha);
	}

	public static void fadeGameObject(List<UIWidget> list, float alpha)
	{
		if (list == null)
		{
			return;
		}

		float a = Mathf.Clamp01(alpha);

		foreach(UIWidget widget in list)
		{
			Color c = widget.color;
			c.a = a;
			widget.color = c;
		}
	}

	/// Returns a mapping of the alpha values to UIWidgets on a GameObject, allowing them to be restored back to default if they are changed
	public static Dictionary<UIWidget, float> getAlphaValueMapForGameObject(GameObject gameObject)
	{
		Dictionary<UIWidget, float> alphaMap = new Dictionary<UIWidget, float>();

		foreach (UIWidget widget in gameObject.GetComponentsInChildren<UIWidget>())
		{
			alphaMap.Add(widget, widget.alpha);
		}

		return alphaMap;
	}

	/// Restores alpha values from a map created by calling getAlphaValueMapForUIGameObject()
	public static void restoreAlphaValuesToGameObjectFromMap(GameObject gameObject, Dictionary<UIWidget, float> alphaMap, float multiplier = 1.0f)
	{
		if (gameObject == null)
		{
			return;
		}
		
		foreach (UIWidget widget in gameObject.GetComponentsInChildren<UIWidget>(true))
		{
			if (alphaMap.ContainsKey(widget))
			{
				widget.alpha = alphaMap[widget] * multiplier;
			}
		}
	}

	/// Restores alpha values from a map created by calling getAlphaValueMapForGameObject(), performed over a set duration
	public static IEnumerator restoreAlphaValuesToGameObjectFromMapOverTime(GameObject gameObject, Dictionary<UIWidget, float> alphaMap, float duration)
	{
		float elapsedTime = 0;

		while (elapsedTime < duration)
		{
			if (gameObject == null)
			{
				yield break; //Return early if the gameObject is destroyed in the middle of this coroutine. 
			}
			elapsedTime += Time.deltaTime;
			restoreAlphaValuesToGameObjectFromMap(gameObject, alphaMap, (elapsedTime / duration));
			yield return null;
		}

		// Make sure everything is set to it's final alpha value.
		restoreAlphaValuesToGameObjectFromMap(gameObject, alphaMap);
	}


	/// Returns the mouse position in NGUI coord space.
	public static Vector2int touchPosition
	{
		get
		{
			return new Vector2int(TouchInput.position.x - Screen.width / 2, (Screen.height - TouchInput.position.y) - Screen.height / 2);
		}
	}

	/// Should NGUI be enabled, based on what TouchInput.cs is doing.
	/// Called by NGUI source code.
	public static bool shouldProcessMouse()
	{
		return !TouchInput.isDragging;
	}

	/// Resets an image button so the normal button state is displayed.
	/// Sometimes you have to do this manually when clicking the button opens a dialog because the button gets stuck in over state.
	public static void resetUIImageButton(UIImageButton button)
	{
		// This is the NGUI equivalent of UIElement.clear() for old GUI stuff.
		button.target.spriteName = button.normalSprite;
	}

	/// Calls the CheckParent() method on every UIWidget that is a child of parent.
	/// This is necessary after changing the parenting of widgets, to keep the UIPanel meshes correct.
	public static void checkParents(Transform parent)
	{
		foreach (UIWidget widget in parent.GetComponentsInChildren<UIWidget>())
		{
			widget.ParentHasChanged();
		}
	}

	/// Get local scale bounds for NGUI widgets by using the scale of the widgets.
	public static Bounds getObjectBounds(GameObject root)
	{
		Bounds bounds = new Bounds();

		UIWidget[] widgets = root.GetComponentsInChildren<UIWidget>(true);

		foreach (UIWidget widget in widgets)
		{
			Vector3 center = CommonTransform.localPositionToParent(widget.transform, root.transform);
			Vector3 scale = CommonTransform.localScaleToParent(widget.transform, root.transform);

			switch (widget.pivot)
			{
				case UIWidget.Pivot.TopLeft:
				case UIWidget.Pivot.Left:
				case UIWidget.Pivot.BottomLeft:
					center.x += scale.x * .5f;
					break;
				case UIWidget.Pivot.TopRight:
				case UIWidget.Pivot.Right:
				case UIWidget.Pivot.BottomRight:
					center.x -= scale.x * .5f;
					break;
			}
			switch (widget.pivot)
			{
				case UIWidget.Pivot.TopLeft:
				case UIWidget.Pivot.Top:
				case UIWidget.Pivot.TopRight:
					center.y -= scale.y * .5f;
					break;
				case UIWidget.Pivot.BottomLeft:
				case UIWidget.Pivot.Bottom:
				case UIWidget.Pivot.BottomRight:
					center.y += scale.y * .5f;
					break;
			}

			Bounds widgetBounds = new Bounds(center, scale);
			bounds.Encapsulate(widgetBounds);
		}
		return bounds;
	}
	
	/// Takes a list of UILabels that should have "shrink to fit" enabled,
	/// analyzes the font size of all of them, then applies the smallest
	/// font size among them to all of the labels, and disables "shrink to fit".
	/// This is so that a collection of similar labels can all use a consistent
	/// font size, regardless of the amount of content in each of them.
	/// Make sure this is called AFTER dialog init() and Awake() functions,
	/// to make sure UILabelStaticText has had a chance to happen first.
	public static void useSmallestFontSize(List<UILabel> labels)
	{
		float smallest = 99999f;
		
		// One loop to find the smallest size.
		foreach (UILabel label in labels)
		{
			if (label.transform.localScale.y < smallest)
			{
				smallest = label.transform.localScale.y;
			}
		}
				
		// Another loop to set all labels to the smallest size.
		foreach (UILabel label in labels)
		{
			label.shrinkToFit = false;
			label.transform.localScale = new Vector3(smallest, smallest, 1f);
		}		
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		disabledButtons = new Dictionary<GameObject, bool>();	///< List of buttons that have been disabled, to be enabled again later.
		hiddenPositions = new Dictionary<Transform, Vector3>();		///< List of positions of hidden objects, to be shown again later.
		areButtonsEnabled = true;							///< Are buttons enabled or not?
		allExceptions = new List<GameObject>();	///< Button exceptions to disabling
		_guiShader = null;
		//uiRoot = null;
	}

	//Coppies the contents of the other uiLabel into the Original
	public static void copyUILabel(UILabel original, UILabel other)
	{
		original.text = other.text;
		original.color = other.color;
		original.lineWidth = other.lineWidth;
		original.lineHeight = other.lineHeight;
		original.maxLineCount = other.maxLineCount;
		original.effectStyle = other.effectStyle;
		original.effectColor = other.effectColor;
		original.symbolStyle = other.symbolStyle;
		original.effectDistance = other.effectDistance;
		original.shrinkToFit = other.shrinkToFit;
		original.colorMode = other.colorMode;
		original.endGradientColor = other.endGradientColor;
		original.gradientSteps = other.gradientSteps;
		original.gradientScale = other.gradientScale;
		original.gradientOffset = other.gradientOffset;
		original.font = other.font;
		original.pivot = other.pivot;
		original.lineSpacing = other.lineSpacing;
	}
	
	// Returns the UIWidget pivot equivalent of a TextMeshPro object's anchor.
	public static UIWidget.Pivot textMeshProAnchorToPivot(TextMeshPro tmPro)
	{
		Vector2 pivot = tmPro.rectTransform.pivot;

		if (pivot == new Vector2(0, 1))
			return UIWidget.Pivot.TopLeft;
		else if (pivot == new Vector2(0.5f, 1))
			return UIWidget.Pivot.Top;
		else if (pivot == new Vector2(1f, 1))
			return UIWidget.Pivot.TopRight;
		else if (pivot == new Vector2(0, 0.5f))
			return UIWidget.Pivot.Left;
		else if (pivot == new Vector2(0.5f, 0.5f))
			return UIWidget.Pivot.Center;
		else if (pivot == new Vector2(1, 0.5f))
			return UIWidget.Pivot.Right;
		else if (pivot == new Vector2(0, 0))
			return UIWidget.Pivot.BottomLeft;
		else if (pivot == new Vector2(0.5f, 0))
			return UIWidget.Pivot.Bottom;
		else if (pivot == new Vector2(1, 0))
			return UIWidget.Pivot.BottomRight;
		else
			return UIWidget.Pivot.Center; // Need a default.
	}
}

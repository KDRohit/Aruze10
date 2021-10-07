using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TMProExtensions;

/**
This is a purely static class of generic useful functions that relate to GameObjects.
*/
public static class CommonGameObject
{
	// Custom instantiate calls (which everyone should use instead of the built in Unity one, 
	// that should ensure Animators are working as expected still)
	public static Object instantiate(Object original)
	{
		Object createdObject = UnityEngine.Object.Instantiate(original);
		GameObject createdGameObject = createdObject as GameObject;
		if (createdGameObject != null)
		{
			CommonGameObject.addAnimatorUpdateOnEnableScriptToAllAnimators(createdGameObject);
		}

		return createdObject;
	}

	public static Object instantiate(Object original, Transform parent)
	{
		Object createdObject = UnityEngine.Object.Instantiate(original, parent);
		GameObject createdGameObject = createdObject as GameObject;
		if (createdGameObject != null)
		{
			CommonGameObject.addAnimatorUpdateOnEnableScriptToAllAnimators(createdGameObject);
		}

		return createdObject;
	}

	public static Object instantiate(Object original, Transform parent, bool instantiateInWorldSpace)
	{
		Object createdObject = UnityEngine.Object.Instantiate(original, parent, instantiateInWorldSpace);
		GameObject createdGameObject = createdObject as GameObject;
		if (createdGameObject != null)
		{
			CommonGameObject.addAnimatorUpdateOnEnableScriptToAllAnimators(createdGameObject);
		}

		return createdObject;
	}

	public static Object instantiate(Object original, Vector3 position, Quaternion rotation)
	{
		Object createdObject = UnityEngine.Object.Instantiate(original, position, rotation);
		GameObject createdGameObject = createdObject as GameObject;
		if (createdGameObject != null)
		{
			CommonGameObject.addAnimatorUpdateOnEnableScriptToAllAnimators(createdGameObject);
		}

		return createdObject;
	}

	public static Object instantiate(Object original, Vector3 position, Quaternion rotation, Transform parent)
	{
		Object createdObject = UnityEngine.Object.Instantiate(original, position, rotation, parent);
		GameObject createdGameObject = createdObject as GameObject;
		if (createdGameObject != null)
		{
			CommonGameObject.addAnimatorUpdateOnEnableScriptToAllAnimators(createdGameObject);
		}

		return createdObject;
	}

	// Adds AnimatorUpdateOnEnable scripts to every animator in a game object.  This is forced on all objects
	// called using our CommonGameObject.instantiate calls, which should be perfered for consistant animator
	// funcitonality.  Note: when upgrading Unity we can comment out the calls to this function to test
	// if Unity has fixed the issue where the Animators aren't updating right away when enabled and
	// if it ever is fixed we can remove this code.
	private static void addAnimatorUpdateOnEnableScriptToAllAnimators(GameObject obj)
	{
		Animator[] allAnimators = obj.GetComponentsInChildren<Animator>(true);
		for (int i = 0; i < allAnimators.Length; i++)
		{
			GameObject animatorGameObject = allAnimators[i].gameObject;

			// Check if an AnimatorUpdateOnEnable component was somehow saved to this object,
			// and don't bother creating another if it is already attached.
			AnimatorUpdateOnEnable onEnableComponent = animatorGameObject.GetComponent<AnimatorUpdateOnEnable>();
			if (onEnableComponent == null)
			{
				animatorGameObject.AddComponent<AnimatorUpdateOnEnable>();
			}
		}
	}

	/// Disables all cameras found in the game object provided.
	public static void disableCameras(GameObject obj)
	{
		foreach (Camera camera in obj.GetComponentsInChildren<Camera>())
		{
			camera.gameObject.SetActive(false);
		}
	}
	
	/// Enables all cameras found in the game object provided.
	public static void enableCameras(GameObject obj)
	{
		foreach (Camera camera in obj.GetComponentsInChildren<Camera>(true))
		{
			camera.gameObject.SetActive(true);
		}
	}

	/// Get a mapping of all the camera layers so they can be restored back to what they were
	public static Dictionary<GameObject, int> getCameraLayerMap(GameObject gameObject)
	{
		Dictionary<GameObject, int> cameraLayerMap = new Dictionary<GameObject, int>();
		
		getCameraLayerMapRecursive(ref cameraLayerMap, gameObject);
		
		return cameraLayerMap;
	}

	/// Recursive function to get camera layers, because we need every single game object
	private static void getCameraLayerMapRecursive(ref Dictionary<GameObject, int> cameraLayerMap, GameObject gameObject)
	{
		if (gameObject != null)
		{
			cameraLayerMap.Add(gameObject, gameObject.layer);

			foreach (Transform child in gameObject.transform)
			{
				getCameraLayerMapRecursive(ref cameraLayerMap, child.gameObject);
			}
		}
	}

	/// Restore all camera layers using the passed in map which should be obtained by calling getCameraLayerMap()
	public static void restoreCameraLayerMap(Dictionary<GameObject, int> cameraLayerMap, GameObject gameObject)
	{
		restoreCameraLayerMapRecursive(cameraLayerMap, gameObject);
	}

	/// Recursive function to restore the camera layer properties from a map obtained by calling getCameraLayerMap()
	private static void restoreCameraLayerMapRecursive(Dictionary<GameObject, int> cameraLayerMap, GameObject gameObject)
	{
		if (gameObject != null)
		{
			if (cameraLayerMap.ContainsKey(gameObject))
			{
				gameObject.layer = cameraLayerMap[gameObject];
			}

			foreach (Transform child in gameObject.transform)
			{
				restoreCameraLayerMapRecursive(cameraLayerMap, child.gameObject);
			}
		}
	}

	// this will rotate object on z axis using euler angles to have it face the target vector
	public static Vector3 lookAt(GameObject gameObject, Vector3 target)
	{
		// Calculate an appropriate lookat target vector on our current z-plane.
		Vector3 targetPosition = new Vector3(target.x, target.y, gameObject.transform.position.z);

		// Align our forward transform vector with this target
		gameObject.transform.LookAt(targetPosition);

		// Correct rotation to point z-axis away from camera.
		gameObject.transform.Rotate(Vector3.up, 90.0f, Space.Self); 

		return gameObject.transform.rotation.eulerAngles;
	}		
	
	/// Returns a mapping of the UIAnchor enabled states, allowing them to be restored back to default if they are changed
	public static Dictionary<UIAnchor, bool> getUIAnchorEnabledMapForGameObject(GameObject gameObject)
	{
		Dictionary<UIAnchor, bool> anchorEnabledMap = new Dictionary<UIAnchor, bool>();
		
		foreach (UIAnchor anchor in gameObject.GetComponentsInChildren<UIAnchor>(true))
		{
			anchorEnabledMap.Add(anchor, anchor.enabled);
		}
		
		return anchorEnabledMap;
	}
	
	/// Disables all UIAnchors on a gameobject
	public static void disableUIAnchorsForGameObject(GameObject gameObject)
	{
		foreach (UIAnchor anchor in gameObject.GetComponentsInChildren<UIAnchor>(true))
		{
			anchor.enabled = false;
		}
	}
	
	/// Resotre the UIAnchor enabled states based on a snapshot taken by calling getUIAnchorEnabledMapForGameObject()
	public static void restoreUIAnchorActiveMapToGameObject(GameObject gameObject, Dictionary<UIAnchor, bool> anchorEnabledMap)
	{
		foreach (UIAnchor anchor in gameObject.GetComponentsInChildren<UIAnchor>(true))
		{
			if (anchorEnabledMap != null && anchorEnabledMap.ContainsKey(anchor))
			{
				anchor.enabled = anchorEnabledMap[anchor];
			}
		}
	}
	
	/// Returns a mapping of the alpha values to materials on a GameObject, allowing them to be restored back to default if they are changed
	public static Dictionary<Material, float> getAlphaValueMapForGameObject(GameObject gameObject)
	{
		Dictionary<Material, float> alphaMap = new Dictionary<Material, float>();
		
		foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>(true))
		{
			Material[] materials = renderer.materials;
			
			foreach (Material material in materials)
			{

				if (CommonMaterial.canAlphaMaterial(material))
				{
					alphaMap.Add(material, CommonMaterial.getAlphaOnMaterial(material));
				}
			}
		}
		
		return alphaMap;
	}

	/// Restores alpha values from a map created by calling getAlphaValueMapForGameObject()
	public static void restoreAlphaValuesToGameObjectFromMap(GameObject gameObject, Dictionary<Material, float> alphaMap)
	{
		if (gameObject == null)
		{
			Debug.LogWarning("Trying to set alpha on null gameObject");
			return;
		}
		else if (alphaMap == null)
		{
			Debug.LogWarningFormat("Trying to set alpha for gameObject {0} using null alphaMap", gameObject.name);
			return;
		}

		foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>(true))
		{
			Material[] materials = renderer.materials;

			foreach (Material material in materials)
			{
				if (alphaMap.ContainsKey(material))
				{
					CommonMaterial.setAlphaOnMaterial(material, alphaMap[material]);
				}
			}
		}
	}
	
	/// Restores alpha values from a map created by calling getAlphaValueMapForGameObject(), performed over a set duration
	public static IEnumerator restoreAlphaValuesToGameObjectFromMapOverTime(GameObject gameObject, Dictionary<Material, float> alphaMap, float duration)
	{
		// gather a list of materials so we don't have to grab stuff every update loop
		List<Material> materialList = new List<Material>();
		foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>(true))
		{
			Material[] materials = renderer.materials;
			
			foreach (Material material in materials)
			{
				if (CommonMaterial.canAlphaMaterial(material))
				{
					materialList.Add(material);
				}
			}
		}
		
		float elapsedTime = 0;
		
		while (elapsedTime < duration)
		{
			elapsedTime += Time.deltaTime;
			
			foreach (Material material in materialList)
			{
				if (alphaMap.ContainsKey(material))
				{
					CommonMaterial.setAlphaOnMaterial(material, alphaMap[material] * (elapsedTime / duration));
				}
			}
			
			yield return null;
		}
		
		// ensure all values are set to final amounts
		foreach (Material material in materialList)
		{
			if (alphaMap.ContainsKey(material))
			{
				CommonMaterial.setAlphaOnMaterial(material, alphaMap[material]);
			}
		}
	}

	/// Sets only the alpha value of every material in a game object. Useful for fading stuff.
	public static void alphaGameObject(GameObject gameObject, float alpha, List<GameObject> objectsToIgnore = null, bool includeUIObjects = false)
	{
		Renderer [] rendererList = gameObject.GetComponentsInChildren<Renderer>(true);
		for (int i=0; i<rendererList.Length; i++)
		{
			Renderer renderer = rendererList[i];
			bool shouldAlphaRender = true;
			if (objectsToIgnore != null && objectsToIgnore.Contains(renderer.gameObject))
			{
				shouldAlphaRender = false;
			}
			if (shouldAlphaRender)
			{
				CommonRenderer.alphaRenderer(renderer, alpha);
			}
		}

		if (includeUIObjects)
		{
			alphaUIGameObject(gameObject, alpha);
		}
	}

	// Overload that just accepts arrays of the objects we want
	public static void alphaGameObject(List<Renderer> gameObjects, float alpha, List<GameObject> objectsToIgnore = null)
	{
		for (int i=0; i<gameObjects.Count; i++)
		{
			Renderer renderer = gameObjects[i];

			// make sure all renderers still exist, in case something was destroyed on this object during the same frame we are trying to fade
			if (renderer != null)
			{
				bool shouldAlphaRender = true;
				if (objectsToIgnore != null && objectsToIgnore.Contains(renderer.gameObject))
				{
					shouldAlphaRender = false;
				}
				if (shouldAlphaRender)
				{
					CommonRenderer.alphaRenderer(renderer, alpha);
				}
			}
		}
	}
	
	// Interpolates to the alpha value for a LabelWrapperComponent.  For the most part there are probably better
	// functions to use that do more alpha control of more complex objects, but when you are only dealing with 
	// LabelWrapperComponent this function can be used.
	public static IEnumerator interpolateLabelWrapperComponentAlphaOverTime(LabelWrapperComponent label, float endAlpha, float fadeDuration, bool doEaseOutCubic = false)
	{
		if (label != null)
		{
			float startingAlphaValue = label.alpha;

			float elapsedTime = 0;
			while (elapsedTime < fadeDuration)
			{
				elapsedTime += Time.deltaTime;
				float t = (elapsedTime / fadeDuration);

				label.alpha = CommonMath.getInterpolatedFloatValue(startingAlphaValue, endAlpha, t, doEaseOutCubic);

				yield return null;
			}
		}
		else
		{
			Debug.LogWarning("CommonGameObject.interpolateLabelWrapperComponentAlphaOverTime() - Called with null label!");
		}
	}

	/// Sets only the alpha value of every UI widget and TextMeshPro label in a game object.
	public static void alphaUIGameObject(GameObject gameObject, float alpha)
	{
		// Note: this does a per-frame allocation of these arrays if called every frame, which is not desirable.
		//       would be better to call GetComponentsInChildren() once at start of a coroutine (for example)

		alphaUIGameObject(gameObject.GetComponentsInChildren<UIWidget>(), gameObject.GetComponentsInChildren<TextMeshPro>(), alpha);
	}

	/// Overload that just accepts arrays of the components we want
	public static void alphaUIGameObject(UIWidget[] widgets, TextMeshPro[] textMeshes, float alpha)
	{
		for(int i=0; i<widgets.Length; i++)
		{
			widgets[i].alpha = alpha;
		}

		for(int i=0; i<textMeshes.Length; i++)
		{
			textMeshes[i].alpha = alpha;
		}
	}

	/// Overload that just accepts Lists of the components we want
	public static void alphaUIGameObject(List<UIWidget> widgets, List<TextMeshPro> textMeshes, float alpha)
	{
		for(int i=0; i<widgets.Count; i++)
		{
			if (widgets[i] != null)
			{
				widgets[i].alpha = alpha;
			}
		}
			
		for(int i=0; i<textMeshes.Count; i++)
		{
			if (textMeshes[i] != null)
			{
				textMeshes[i].alpha = alpha;
			}
		}
	}

	/// Sets the color value of every material in a game object.
	public static void colorGameObject(GameObject gameObject, Color color, int ignoreMask = 0)
	{
		if (gameObject == null)
		{
			Debug.LogWarning("ERROR: Common.ColorGameObject(...) failed because gameObject is null");
			return;
		}

		foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>(true))
		{
			CommonRenderer.colorRenderer(renderer, color, ignoreMask);
		}
	}

	/// Sets only the color value of every UI widget in a game object.
	public static void colorUIGameObject(GameObject gameObject, Color color, bool shouldMultiply = false)
	{
		foreach (UIWidget widget in gameObject.GetComponentsInChildren<UIWidget>(true))
		{
			Color newColor = widget.color;
			if (shouldMultiply)
			{
				newColor *= color;
			}
			else
			{
				newColor = color;
			}
			newColor.a = widget.color.a;
			widget.color = newColor;
		}
	}
	
	/// Sets only the color value of a UI widget in a game object.
	public static void colorUIWidget(UIWidget widget, Color color)
	{
		if (widget != null)
		{
			color.a = widget.color.a;
			widget.color = color;
		}
	}

	/// Swaps a shader with some other shader by name.
	public static void swapShader(GameObject gameObject, string fromShader, string toShader)
	{
		Shader replacement = ShaderCache.find(toShader);
		if (replacement != null)
		{
			foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>(true))
			{
				Material[] materials = renderer.materials;
				foreach (Material material in materials)
				{
					if (material.shader.name == fromShader)
					{
						material.shader = replacement;
					}
				}
				renderer.materials = materials;
			}
		}
		else
		{
			Debug.LogWarning("The desired replacement shader does not exist: " + toShader);
		}
	}

	/// Find a child game object by name.
	public static GameObject findChild(GameObject parent, string childName, bool includeInactives = true)
	{
		Transform[] children = parent.GetComponentsInChildren<Transform>(includeInactives);
		
		foreach (Transform child in children)
		{
			if (child.gameObject.name == childName)
			{
				return child.gameObject;
			}
		}
		
		return null;
	}
	
	/// Find a child's transform by name.
	public static Transform findChildTransform(GameObject parent, string childName)
	{
		Transform[] children = parent.GetComponentsInChildren<Transform>(true);
		
		foreach (Transform child in children)
		{
			if (child.gameObject.name == childName)
			{
				return child;
			}
		}
		
		return null;
	}
	
	/// Find a direct child game object by name.
	public static GameObject findDirectChild(GameObject parent, string childName)
	{
		foreach (Transform child in parent.transform)
		{
			if (child.gameObject.name == childName)
			{
				return child.gameObject;
			}
		}
		
		return null;
	}
	
	/// Returns all children directly beneath the given parent, but not grandchildren.
	public static List<GameObject> findDirectChildren(GameObject parent, bool includeInactives = false)
	{
		List<GameObject> returnChildren = new List<GameObject>();
		foreach (Transform child in parent.transform)
		{
			if (includeInactives || child.gameObject.activeSelf)
			{
				returnChildren.Add(child.gameObject);
			}
		}
		
		return returnChildren;
	}
	
	/// Returns a list with the gameobject and all children beneath it, including grandchildren, great-grandchildren, etcetera.
	public static List<GameObject> findAllChildren(GameObject parent, bool includeInactives = false)
	{
		Transform[] children = parent.GetComponentsInChildren<Transform>(includeInactives);
		List<GameObject> returnChildren = new List<GameObject>();
		
		foreach (Transform child in children)
		{
			returnChildren.Add(child.gameObject);
		}
		return returnChildren;
	}

	public static bool isGameObjectChildOf(GameObject possibleChild, GameObject parent)
	{
		if (possibleChild == null || parent == null)
		{
			return false;
		}
		Transform possibleParent = possibleChild.transform;
		while (possibleParent != null)
		{
			if (parent.transform == possibleParent)
			{
				return true;
			}
			possibleParent = possibleParent.parent;
		}
		return false;
	}
	
	/// Returns all child descendants with the given name (will also include the given gameobject if its name matches)
	public static List<GameObject> findChildrenWithName(GameObject parent, string name, bool includeInactives = false)
	{
		Transform[] children = parent.GetComponentsInChildren<Transform>(includeInactives);
		List<GameObject> returnChildren = new List<GameObject>();
		
		foreach (Transform child in children)
		{
			if (child.name == name)
			{
				returnChildren.Add(child.gameObject);
			}
		}
		return returnChildren;
	}
	
	/// Finds a component of the given type that is a parent object of gameObject
	public static Component findComponentInParent(string component, GameObject gameObject)
	{
		while (gameObject.transform.parent != null)
		{
			gameObject = gameObject.transform.parent.gameObject;
			Component componentObj = gameObject.GetComponent(component);
			
			if (componentObj != null)
			{
				return componentObj;
			}
		}
		return null;
	}

	/// Finds and removes unneeded components from a loot prefab object - strips inactive objects too
	public static void prepareGameObjectForUI(GameObject gameObject)
	{
		// We don't want the item's colliders, so remove them all.
		Collider[] colliders = gameObject.GetComponentsInChildren<Collider>(true);
		foreach (Collider itemCollider in colliders)
		{
			Object.Destroy(itemCollider);
		}
		
		// We don't want the item's particle systems, so remove them all.
		ParticleSystem[] systems = gameObject.GetComponentsInChildren<ParticleSystem>(true);
		foreach (ParticleSystem system in systems)
		{
			if (system.main.playOnAwake)
			{
				// Only destroy the system if it is set to play automatically,
				// which gives us the ability to have manually triggered emitters
				// on loot objects for special effects on animated consumable loot.
				Object.Destroy(system.gameObject);
			}
		}
	}

	/// Sets the layer of all children in a given GameObject to the input layer, leaving anything with a layer keepLayer untouched
	public static void setLayerRecursively(GameObject parent, int layer, int keepLayer = -1)
	{
		if (parent == null || parent.transform == null)
		{
			// Nothing to do here.
			return;
		}
		
		Profiler.BeginSample("setLayerRecursively");
		Transform[] children = parent.GetComponentsInChildren<Transform>(true);
		
		foreach (Transform child in children)
		{
			// only set the layer if we aren't keeping it as is
			if (keepLayer == -1 || keepLayer != child.gameObject.layer)
			{
				child.gameObject.layer = layer;
			}
		}
		Profiler.EndSample();
	}

	// Get a map of the layers of an object and it's children so it can be restored
	public static Dictionary<Transform, int> getLayerRestoreMap(GameObject parent)
	{
		if (parent == null || parent.transform == null)
		{
			// Nothing to do here, return an empty map
			return new Dictionary<Transform, int>();
		}

		Dictionary<Transform, int> layerRestoreMap = new Dictionary<Transform, int>();
		
		Transform[] children = parent.GetComponentsInChildren<Transform>(true);
		
		foreach (Transform child in children)
		{
			layerRestoreMap.Add(child, child.gameObject.layer);
		}

		return layerRestoreMap;
	}

	// Restore the layers of an object from a map that was created using getLayerRestoreMap()
	public static void restoreLayerMap(GameObject parent, Dictionary<Transform, int> layerRestoreMap)
	{
		if (parent == null || parent.transform == null)
		{
			// Nothing to do here
			return;
		}
		
		Profiler.BeginSample("restoreLayerMap");
		Transform[] children = parent.GetComponentsInChildren<Transform>(true);
		
		foreach (Transform child in children)
		{
			if (layerRestoreMap.ContainsKey(child))
			{
				child.gameObject.layer = layerRestoreMap[child];
			}
		}
		Profiler.EndSample();
	}

	/// Gets all children in the gameObject that have a specific layer defined by the mask.
	public static List<Transform> getObjectsByLayerMask(GameObject parent, int mask)
	{
		List<Transform> objectsByLayer = new List<Transform>();
		if (parent == null || parent.transform == null)
		{
			Debug.LogError("No parent");
			// Nothing to do here.
			return objectsByLayer;
		}

		Transform[] children = parent.GetComponentsInChildren<Transform>(true);
		foreach (Transform child in children)
		{
			int layerMask = 1 << child.gameObject.layer;
			// Check and see if the child game object has the right mask.
			if((layerMask & mask) != 0)
			{
				objectsByLayer.Add(child);
			}
		}

		return objectsByLayer;
	}

	/// Sets the given text mesh and all child textmeshes to the given text.
	/// Typically the child textmeshes are drop shadows of the same text.
	public static void setShadowedTextMesh(TextMesh textMesh, string text)
	{
		foreach (TextMesh child in textMesh.GetComponentsInChildren<TextMesh>())
		{
			child.text = text;
		}
	}

	/// Turns on or off all renderers in the given GameObject.
	public static void setObjectRenderersEnabled(GameObject gameObject, bool status, bool includeInactive = false)
	{
		Profiler.BeginSample("setObjectRenderersEnabled");

		if (gameObject != null)
		{
			foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>(includeInactive))
			{
				renderer.enabled = status;
			}
		}

		Profiler.EndSample();
	}
	
	/// Turns on or off all shadows (cast/receive) in the given GameObject.
	public static void setObjectShadowsEnabled(GameObject gameObject, bool cast, bool receive, bool includeInactive = false)
	{
		Profiler.BeginSample("setObjectShadowsEnabled");

		if (gameObject != null)
		{
			foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>(includeInactive))
			{
				renderer.shadowCastingMode = cast ? ShadowCastingMode.On : ShadowCastingMode.Off;
				renderer.receiveShadows = receive;
			}
		}

		Profiler.EndSample();
	}
	
	/// Turns on or off all colliders in the given GameObject.
	public static void setObjectCollidersEnabled(GameObject gameObject, bool status, bool includeInactive = false)
	{
		if (gameObject != null)
		{
			foreach (Collider collider in gameObject.GetComponentsInChildren<Collider>(includeInactive))
			{
				collider.enabled = status;
			}
		}
	}
	
	/// Returns true if any renderer in the given GameObject's children are enabled
	public static bool hasEnabledObjectRenderer(GameObject gameObject)
	{
		foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>())
		{
			if (renderer.enabled)
			{
				return true;
			}
		}
		return false;
	}

	/// Activates an object and children opposite the way SetActiveRecursively does it, which is needed for proper ragdoll activation.
	public static void parentsFirstSetActive(GameObject root, bool isActive)
	{
		root.SetActive(isActive);
		foreach (Transform transform in root.transform)
		{
			parentsFirstSetActive(transform.gameObject, isActive);
		}
	}

	/// Returns a component at the given scene-based path, or null
	public static Component findComponentByPath(string component, string path)
	{
		GameObject gameObject = GameObject.Find(path);
		if (gameObject != null)
		{
			return gameObject.GetComponent(component);
		}
		return null;
	}
	
	/// Returns the full scene path of a GameObject
	public static string getObjectPath(GameObject gameObject)
	{
		if (gameObject == null)
		{
			return "null";
		}
		
		string path = "/" + gameObject.name;
		while (gameObject.transform.parent != null)
		{
			gameObject = gameObject.transform.parent.gameObject;
			path = "/" + gameObject.name + path;
		}
		return path;
	}

	/// Fits a game object proportionally into a given size. If the object already fits, it uses the maxScale.
	public static void fitGameObject(GameObject gameObject, Vector3 maxScale, Vector3 fitSize)
	{
		// Default to the max size allowed. Do before getting bounds.
		gameObject.transform.localScale = maxScale;
		
		Bounds bounds = CommonGameObject.getObjectBounds(gameObject);
		float factor = 1f;
		
		if (bounds.size.x > 0)
		{
			factor = Mathf.Min(factor, fitSize.x / bounds.size.x);
		}
		if (bounds.size.y > 0)
		{
			factor = Mathf.Min(factor, fitSize.y / bounds.size.y);
		}
		if (bounds.size.z > 0)
		{
			factor = Mathf.Min(factor, fitSize.z / bounds.size.z);
		}
		
		if (factor < 1f)
		{
			// Needs to shrink.
			gameObject.transform.localScale = maxScale * factor;
		}
	}
	
	
	/// Fits a game object's local scale to fit into a given size in pixels and then scales it up or down based on a given scale.
	/// Make sure to run this AFTER the gameObject has been parented to the NGUI structure.
	public static Bounds fitGameObjectToPanel(GameObject gameObject, Vector2 panelPixelSize, float scaleValue = 1.0f)
	{

		// In order to get accurate results from the bounds calculation,
		// the object needs to be set to world scale of 1,1,1 first.
		CommonTransform.setWorldScale(gameObject.transform, Vector3.one);
		Bounds bounds = CommonGameObject.getObjectBounds(gameObject);
		float scale = 0;
		
		// Go through and each dimension and find the smallest fitted scale size between them all.
		scale = panelPixelSize.x / bounds.size.x;
		
		scale = Mathf.Min(scale, panelPixelSize.y / bounds.size.y);
		
		// Compare the gameObject's Z to the panel's X because our objects rotate on the Y axis.
		scale = Mathf.Min (scale, panelPixelSize.x / bounds.size.z);
		
		// Adjust the scale by the scaleValue.
		scale *= scaleValue;
		
		// Fit the gameObject.
		gameObject.transform.localScale = new Vector3(scale, scale, scale);
		
		return bounds;
	}

	/// Determine if the passed in GameObject contains NGUI widgets, which may require it to display on a specific camera
	public static bool isUsingNguiElements(GameObject gameObject)
	{
		UIWidget[] widgets = gameObject.GetComponentsInChildren<UIWidget>(true);
		return widgets.Length > 0;
	}

	/// Does this given GameObject or any of its children have any MonoBehaviours?
	public static bool hasScript(GameObject root)
	{
		MonoBehaviour script = root.GetComponent<MonoBehaviour>();
		if (script != null)
		{
			return true;
		}
		foreach (Transform child in root.transform)
		{
			if (hasScript(child.gameObject))
			{
				return true;
			}
		}
		return false;
	}

	//Checks the passed mask against all the currently enabled cameras in the scene and returns the first match
	//If there is more than one camera that renders the same bitmask it will take the first match
	//If there is no camera with the propermask then it returns null
	// NOTE FROM TODD: This is a pretty bad function. Use NGUIExt.getObjectCamera() instead.
	public static Camera getCameraByBitMask(int mask)
	{
		Camera[] cameras = Camera.allCameras;
		foreach (Camera cam in cameras)
		{
			if( (mask & cam.cullingMask) != 0)
			{
				return cam;
			}
		}
		return null;
	}
	/// Fi
	/// nd the root bone of a skinned mesh renderer
	public static Transform findRootBoneOfSkinnedMeshRenderer(SkinnedMeshRenderer skinnedMeshRenderer)
	{
		if (skinnedMeshRenderer == null || skinnedMeshRenderer.bones.Length == 0)
		{
			return null;
		}
		
		Transform root = skinnedMeshRenderer.bones[0];
		for(int i = 1; i < skinnedMeshRenderer.bones.Length; i++)
		{
			if (root.IsChildOf(skinnedMeshRenderer.bones[i]))
			{
				root = skinnedMeshRenderer.bones[i];
			}
		}
		
		return root;
	}

	// function used in many places for destroying game objects after waiting a certain amount of time
	// useful in places where we start an object on a path or some other type of tween, but don't want to handle
	// waiting for it to finish in that same coroutine
	public static IEnumerator waitThenDestroy(GameObject go, float time)
	{
		yield return new TIWaitForSeconds(time);
		GameObject.Destroy(go);
	}
	
	/// Auto release an object back into a GameObjectCacher after a set amount of time
	/// NOTE: This is not safe to use if an iTween is involved and may not finish for sure, because if it doesn't it will be stuck on the instance
	public static IEnumerator waitThenReleaseObjectToCacher(GameObject go, float time, GameObjectCacher cacher)
	{
		yield return new TIWaitForSeconds(time);
		cacher.releaseInstance(go);
	}

	/// Get the world position of a collider, returns a bool saying if it could calculate it and returns it in the variable worldCenter
	public static bool getColliderWorldCenter(Collider collider, out Vector3 worldCenter)
	{
		if (collider == null)
		{
			worldCenter = Vector3.zero;
			return false;
		}

		if (collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider)
		{
			Vector3 center;
			if (collider is BoxCollider)
			{
				BoxCollider box = collider as BoxCollider;
				center = box.center;
			}
			else if (collider is SphereCollider)
			{
				SphereCollider sphere = collider as SphereCollider;
				center = sphere.center;
			}
			else // Capsule collider
			{
				CapsuleCollider capsule = collider as CapsuleCollider;
				center = capsule.center;
			}

			worldCenter = collider.transform.TransformPoint(center);
			return true;
		}
		else
		{
			Debug.LogWarning("Passed collider: collider.name = " + collider.name + " doesn't have a center!  Returning Vector3.zero.");
			worldCenter = Vector3.zero;
			return false;
		}
	}

	/// gets the bounds of the tile object, accounting for children and dynamic changes
	public static Bounds getObjectBounds(GameObject gameObject, bool includeInactive = false, bool usePixelFactor = false)
	{
		Bounds bounds = new Bounds();
		
		if (gameObject == null)
		{
			return bounds;
		}
		
		bounds.center = gameObject.transform.position;
		bounds.extents = new Vector3(0f, 0f, 0f);
		
		bool foundBounds = false;
		Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(includeInactive);
		
		foreach (Renderer renderer in renderers)
		{
			if (shouldUseRendererForBoundsCheck(renderer))
			{
				bounds.Encapsulate(renderer.bounds);
				foundBounds = true;
			}
		}
		
		// Get bounds for NGUI widgets by using the scale of the widgets.
		UIWidget[] widgets = gameObject.GetComponentsInChildren<UIWidget>(includeInactive);

		foreach (UIWidget widget in widgets)
		{
			foundBounds = true;
			
			Vector3 worldScale = CommonTransform.getWorldScale(widget.transform);
			Vector3 center = widget.transform.position;
			
			switch (widget.pivot)
			{
			case UIWidget.Pivot.TopLeft:
			case UIWidget.Pivot.Left:
			case UIWidget.Pivot.BottomLeft:
				center.x += worldScale.x * .5f;
				break;
			case UIWidget.Pivot.TopRight:
			case UIWidget.Pivot.Right:
			case UIWidget.Pivot.BottomRight:
				center.x -= worldScale.x * .5f;
				break;
			}
			switch (widget.pivot)
			{
			case UIWidget.Pivot.TopLeft:
			case UIWidget.Pivot.Top:
			case UIWidget.Pivot.TopRight:
				center.y -= worldScale.y * .5f;
				break;
			case UIWidget.Pivot.BottomLeft:
			case UIWidget.Pivot.Bottom:
			case UIWidget.Pivot.BottomRight:
				center.y += worldScale.y * .5f;
				break;
			}
			
			if (usePixelFactor)
			{
				worldScale *= NGUIExt.pixelFactor;
			}
			Bounds widgetBounds = new Bounds(center, worldScale);
			bounds.Encapsulate(widgetBounds);
		}
		
		if (!foundBounds)
		{
			// If we haven't found bounds yet with another technique,
			// then try to use colliders as a way to get bounds.
			Collider[] colliders = gameObject.GetComponentsInChildren<Collider>(true);
			
			foreach (Collider collider in colliders)
			{
				bounds.Encapsulate(collider.bounds);
			}
		}
		
		return bounds;
	}

	private static bool shouldUseRendererForBoundsCheck(Renderer renderer)
	{
		if (renderer.gameObject.layer == Layers.ID_NGUI_IGNORE_BOUNDS)
		{
			return false;
		}

		if (renderer.bounds.size.x == 0 || renderer.bounds.size.y == 0)
		{
			return false;
		}

		if (renderer.gameObject.GetComponent<SpriteMask>() != null)
		{
			return false;
		}
		
		ParticleSystemRenderer particleSysRenderer = renderer as ParticleSystemRenderer;
		if (particleSysRenderer != null)
		{
			if (particleSysRenderer.meshCount == 0)
			{
				// In Unity 2017 if there aren't any meshes to render then just
				// ignore this ParticleSystemRenderer, otherwise it will return a weird size
				// rather than not having a size like you'd expect
				return false;
			}
		}
	
		return true;
	}

	//This will check for a component if it doesn't exist it will add in onto the provided gameobject
 	public static T getComponent<T>(GameObject toCheck, bool shouldAdd = false) where T : Component
 	{
        //Try to get the component
		T component = toCheck.GetComponent<T>();
		//If component is null add and return
		if (component == null)
		{
			if (shouldAdd)
			{
				return toCheck.AddComponent<T>();
			}
			else
			{
				Debug.LogError("No component of type [" + typeof(T).ToString() + "] was found on object [" + NGUITools.GetHierarchy(toCheck) + "]");
			}
		}
		//We have the component return it
		return component;
	}

	// Clear all particle systems and trail renderers so they appear correctly when used again
	public static void clearAllParticleSystemsAndTrailRenderers(GameObject gameObject)
	{
		foreach (ParticleSystem particleSystem in gameObject.GetComponentsInChildren<ParticleSystem>(true))
		{
			if (particleSystem != null)
			{
				particleSystem.Clear();
			}
		}

		foreach (TrailRenderer trailRenderer in gameObject.GetComponentsInChildren<TrailRenderer>(true))
		{
			if (trailRenderer != null)
			{
				trailRenderer.Clear();
			}
		}
	}

	// this will build alpha restore dictionaries for arrays of gameobjects and covers materials, NGUI UI object, and tmpro labels
	// three dictionaries are created in the end, one for materials, one for the NGUI UI objects, and one for tmpro labels.
	public static AlphaRestoreData getAlphaRestoreDataForGameObject(GameObject objectToMap)
	{
		if (objectToMap == null)
		{
			return null;
		}

		AlphaRestoreData restoreData = new AlphaRestoreData();
		restoreData.materialAlphaMap = CommonGameObject.getAlphaValueMapForGameObject(objectToMap);
		restoreData.uiAlphaMap = NGUIExt.getAlphaValueMapForGameObject(objectToMap);
		restoreData.tmProLabelAlphaMap = TMProFunctions.getAlphaValueMapForGameObject(objectToMap);

		return restoreData;
	}

	// this will build alpha restore dictionaries for arrays of gameobjects and covers materials, NGUI UI object, and tmpro labels
	// three dictionaries are created in the end, one for materials, one for the NGUI UI objects, and one for tmpro labels.
	public static Dictionary<GameObject, AlphaRestoreData> getAlphaRestoreDataMapsForGameObjects(GameObject[] objectsToFade)
	{
		Dictionary<GameObject, AlphaRestoreData> restoreDataMap = new Dictionary<GameObject, AlphaRestoreData>();

		foreach (GameObject go in objectsToFade)
		{
			AlphaRestoreData alphaRestoreForGo = getAlphaRestoreDataForGameObject(go);
			if (alphaRestoreForGo != null)
			{
				restoreDataMap.Add(go, alphaRestoreForGo);
			}
		}

		return restoreDataMap;		
	}

	// fade object back to original values covers materials, NGUI UI elements, and tmPro labels
	public static IEnumerator fadeGameObjectToOriginalAlpha(GameObject objectToFade, AlphaRestoreData restoreData, float fadeDuration)
	{
		RoutineRunner.instance.StartCoroutine(restoreAlphaValuesToGameObjectFromMapOverTime(objectToFade, restoreData.materialAlphaMap, fadeDuration));
		RoutineRunner.instance.StartCoroutine(NGUIExt.restoreAlphaValuesToGameObjectFromMapOverTime(objectToFade, restoreData.uiAlphaMap, fadeDuration));
		RoutineRunner.instance.StartCoroutine(TMProFunctions.restoreAlphaValuesToGameObjectFromMapOverTime(objectToFade, restoreData.tmProLabelAlphaMap, fadeDuration));

		yield return new TIWaitForSeconds(fadeDuration);
	}

	// fade objects back to original values covers materials, NGUI UI elements, and tmPro labels
	public static IEnumerator fadeGameObjectsToOriginalAlpha(Dictionary<GameObject, AlphaRestoreData> restoreDataMap, float fadeDuration)
	{
		List<TICoroutine> fadingObjectCoroutines = new List<TICoroutine>();

		foreach (KeyValuePair<GameObject, AlphaRestoreData> entry in restoreDataMap)
		{
			fadingObjectCoroutines.Add(RoutineRunner.instance.StartCoroutine(fadeGameObjectToOriginalAlpha(entry.Key, entry.Value, fadeDuration)));
		}

		if (fadingObjectCoroutines.Count > 0)
		{
			yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(fadingObjectCoroutines));
		}
	}

	// fade object back to original values covers materials, NGUI UI elements, and tmPro labels (non-corotuine version)
	public static void fadeGameObjectToOriginalAlpha(GameObject objectToFade, AlphaRestoreData restoreData)
	{
		CommonGameObject.restoreAlphaValuesToGameObjectFromMap(objectToFade, restoreData.materialAlphaMap);
		NGUIExt.restoreAlphaValuesToGameObjectFromMap(objectToFade, restoreData.uiAlphaMap);
		TMProFunctions.restoreAlphaValuesToGameObjectFromMap(objectToFade, restoreData.tmProLabelAlphaMap);
	}

	// fade objects back to original values covers materials, NGUI UI elements, and tmPro labels (non-corotuine version)
	public static void fadeGameObjectsToOriginalAlpha(Dictionary<GameObject, AlphaRestoreData> restoreDataMap)
	{
		foreach (KeyValuePair<GameObject, AlphaRestoreData> entry in restoreDataMap)
		{
			fadeGameObjectToOriginalAlpha(entry.Key, entry.Value);
		}
	}

	// fade a single object, starting the fade from whatever alpha value each component that can render in the objects were at when the fade started
	public static IEnumerator fadeGameObjectToFromCurrent(GameObject objectToFade, float endAlpha, float fadeDuration, bool doEaseOutCubic = false)
	{
		GameObject[] gameObjectArray = new GameObject[1];
		gameObjectArray[0] = objectToFade;
		yield return RoutineRunner.instance.StartCoroutine(fadeGameObjectsToFromCurrent(gameObjectArray, endAlpha, fadeDuration, doEaseOutCubic));
	}

	// fade a list of objects, starting the fade from whatever alpha value each component that can render in the objects were at when the fade started
	public static IEnumerator fadeGameObjectsToFromCurrent(GameObject[] objectsToFade, float endAlpha, float fadeDuration, bool doEaseOutCubic = false)
	{
		List<UIWidget> widgets = new List<UIWidget>();
		List<float> widgetStartingAlphaValues = new List<float>();
		List<TextMeshPro> textMeshObjects = new List<TextMeshPro>();
		List<float> textMeshStartingAlphaValues = new List<float>();
		List<Renderer> rendererObjects = new List<Renderer>();
		List<List<float>> rendererStartingAlphaValues = new List<List<float>>();

		// grab all the objects we will be fading, and figure out what alpha values they are starting from
		foreach (GameObject go in objectsToFade)
		{
			UIWidget[] widgetsInObject = go.GetComponentsInChildren<UIWidget>();
			for (int i = 0; i < widgetsInObject.Length; i++)
			{
				widgetStartingAlphaValues.Add(widgetsInObject[i].alpha);
			}
			widgets.AddRange(widgetsInObject);

			TextMeshPro[] textMeshProsInObject = go.GetComponentsInChildren<TextMeshPro>();
			for (int i = 0; i < textMeshProsInObject.Length; i++)
			{
				textMeshStartingAlphaValues.Add(textMeshProsInObject[i].alpha);
			}
			textMeshObjects.AddRange(textMeshProsInObject);

			Renderer[] renderersInObject = go.GetComponentsInChildren<Renderer>();
			for (int i = 0; i < renderersInObject.Length; i++)
			{
				rendererStartingAlphaValues.Add(CommonRenderer.getRendererAlphaValues(renderersInObject[i]));
			}
			rendererObjects.AddRange(renderersInObject);
		}

		float elapsedTime = 0;
		while (elapsedTime < fadeDuration)
		{		
			elapsedTime += Time.deltaTime;
			float t = (elapsedTime / fadeDuration);

			for (int i = 0; i < rendererObjects.Count; i++)
			{
				CommonRenderer.alphaRendererFromStartValues(rendererObjects[i], rendererStartingAlphaValues[i], endAlpha, t, doEaseOutCubic);
			}
			
			for (int i = 0; i < widgets.Count; i++)
			{
				// Just double check that a widget wasn't destroyed while this coroutine was running
				if (widgets[i] != null)
				{
					widgets[i].alpha = CommonMath.getInterpolatedFloatValue(widgetStartingAlphaValues[i], endAlpha, t, doEaseOutCubic);
				}
			}

			for (int i = 0; i < textMeshObjects.Count; i++)
			{
				// Just double check that a text mesh object wasn't destroyed while this coroutine was running
				if (textMeshObjects[i] != null)
				{
					textMeshObjects[i].alpha = CommonMath.getInterpolatedFloatValue(textMeshStartingAlphaValues[i], endAlpha, t, doEaseOutCubic);
				}
			}

			yield return null;
		}

		// set to final value
		alphaGameObject(rendererObjects, endAlpha);
		alphaUIGameObject(widgets, textMeshObjects, endAlpha);
	}

	// Fade one object
	public static IEnumerator fadeGameObjectTo(GameObject objectToFade, float startAlpha, float endAlpha, float fadeDuration, bool includeInActive, bool doEaseOutCubic = false)
	{
		GameObject[] array = { objectToFade };
		return fadeGameObjectsTo(array, startAlpha, endAlpha, fadeDuration, includeInActive, doEaseOutCubic);
	}

	// fade array of gameobjects to desired alpha value, handles materials and tmpro labels at the same time
	public static IEnumerator fadeGameObjectsTo(GameObject[] objectsToFade, float startAlpha, float endAlpha, float fadeDuration, bool includeInActive, bool doEaseOutCubic=false)
	{
		float elapsedTime = 0;
		float currentAlpha = startAlpha;	
		List<UIWidget> widgets = new List<UIWidget>();
		List<TextMeshPro> textMeshObjects = new List<TextMeshPro>();
		List<Renderer> rendererObjects =  new List<Renderer>();

		foreach (GameObject go in objectsToFade)
		{
			widgets.AddRange(go.GetComponentsInChildren<UIWidget>(includeInActive));
			textMeshObjects.AddRange(go.GetComponentsInChildren<TextMeshPro>(includeInActive));
			rendererObjects.AddRange(go.GetComponentsInChildren<Renderer>(includeInActive));
		}

		alphaGameObject(rendererObjects, startAlpha);
		alphaUIGameObject(widgets, textMeshObjects, startAlpha);

		while (elapsedTime < fadeDuration)
		{		
			elapsedTime += Time.deltaTime;
			float t = (elapsedTime / fadeDuration);

			currentAlpha = CommonMath.getInterpolatedFloatValue(startAlpha, endAlpha, t, doEaseOutCubic);
	
			alphaGameObject(rendererObjects, currentAlpha);
			alphaUIGameObject(widgets, textMeshObjects, currentAlpha);

			yield return null;
		}	

		// set to final value
		alphaGameObject(rendererObjects, currentAlpha);
		alphaUIGameObject(widgets, textMeshObjects, currentAlpha);
	}		

	// Fades a line renderer and its width.
	public static IEnumerator fadeLineRendererColorAndWidth(LineRenderer lineRenderer, Color startColor, Color endColor, float startWidth, float endWidth, float time)
	{
		for (float t = 0.0f; t < 1.0f; t += Time.deltaTime/time)
		{
			if (lineRenderer != null)
			{
				Color color = Color.Lerp(startColor, endColor, t);
				float width = Mathf.Lerp(startWidth, endWidth, t);
				lineRenderer.startColor = color;
				lineRenderer.endColor = color;
				lineRenderer.widthMultiplier = width;
				yield return null;
			}
		}
		if (lineRenderer != null)
		{
			lineRenderer.startColor = endColor;
			lineRenderer.endColor = endColor;
			lineRenderer.widthMultiplier = endWidth;
		}
	}

	// Draw the box, capsule or sphere collider of a game object using a line renderer.
	// Does not set the material of the line renderer.
	// Returns null if it cannot be drawn.
	public static LineRenderer drawCollider(Collider collider, float width)
	{
		if (collider == null)
		{
			return null;
		}

		Vector3 extends = Vector3.zero;
		Vector3 center = Vector3.zero;
		if (collider.GetComponent<BoxCollider>() != null)
		{
			BoxCollider boxCollider = collider.GetComponent<BoxCollider>();
			extends = boxCollider.size;
			center = boxCollider.center;
		}
		else if (collider.GetComponent<CapsuleCollider>() != null)
		{
			CapsuleCollider capsuleCollider = collider.GetComponent<CapsuleCollider>();
			// The capsule collider can be set to have its height be in a certain direction.
			// We need to make sure the line renderer points in the same direction.
			switch (capsuleCollider.direction)
			{
			case 0:
				// X-axis
				extends = new Vector3(capsuleCollider.height, capsuleCollider.radius, capsuleCollider.radius);
				break;
			case 1:
				// Y-axis
				extends = new Vector3(capsuleCollider.radius, capsuleCollider.height, capsuleCollider.radius);
				break;
			case 2:
			default:
				// Z-axis
				extends = new Vector3(capsuleCollider.radius, capsuleCollider.radius, capsuleCollider.height);
				break;
			}
			center = capsuleCollider.center;
		}
		else if (collider.GetComponent<SphereCollider>() != null)
		{
			SphereCollider sphereCollider = collider.GetComponent<SphereCollider>();
			extends = new Vector3(sphereCollider.radius, sphereCollider.radius, sphereCollider.radius);
			center = sphereCollider.center;
		}
		else
		{
			return null;
		}

		Bounds colliderBounds = new Bounds();
		colliderBounds.extents = extends;
		colliderBounds.center = center;

		return drawBounds(collider.gameObject, colliderBounds, width);
	}

	// Draw the box using a line renderer, using a bounds object.
	// Does not set the material of the line renderer.
	public static LineRenderer drawBounds(GameObject obj, Bounds bounds, float width)
	{
		LineRenderer lineRenderer = obj.GetComponent<LineRenderer>();

		if (lineRenderer == null)
		{
			lineRenderer = obj.gameObject.AddComponent<LineRenderer>();
		}
		if (lineRenderer == null)
		{
			return null;
		}

		// Set the extent of our line renderer box
		Vector3 posExtends = new Vector3(bounds.extents.x / 2.0f + bounds.center.x, bounds.extents.y / 2.0f + bounds.center.y, bounds.extents.z / 2.0f + bounds.center.z);
		Vector3 negExtends = new Vector3(-(bounds.extents.x) / 2.0f + bounds.center.x, -(bounds.extents.y) / 2.0f + bounds.center.y, -(bounds.extents.z) / 2.0f + bounds.center.z);
		float zPos = bounds.center.z - 1.0f;
		//Vector3 localScale = collider.transform.localScale;
		float lineSize = Mathf.Min(bounds.extents.x, bounds.extents.y) * 0.0025f;

		// Set the position of the vertices.
		Vector3 [] positions = new Vector3[8];
		positions[0] = new Vector3(posExtends.x, 			negExtends.y, zPos);
		positions[1] = new Vector3(negExtends.x, 			negExtends.y, zPos);
		positions[2] = new Vector3(negExtends.x - lineSize, negExtends.y, zPos);
		positions[3] = new Vector3(negExtends.x, 			posExtends.y, zPos);
		positions[4] = new Vector3(negExtends.x, 			posExtends.y + lineSize, zPos);
		positions[5] = new Vector3(posExtends.x, 			posExtends.y, zPos);
		positions[6] = new Vector3(posExtends.x + lineSize, posExtends.y, zPos);
		positions[7] = new Vector3(posExtends.x,			negExtends.y, zPos);

		// Create a curve for rendering the width
		AnimationCurve curve = new AnimationCurve();
		curve.AddKey(0, 1.0f);
		curve.AddKey(1, 1.0f);

		lineRenderer.useWorldSpace = false;
		lineRenderer.positionCount = 8;
		lineRenderer.sortingOrder = 31999; // Make sure the line renderer is on top of everything. We may need to set the sorting layer manually.
		lineRenderer.SetPositions(positions);
		lineRenderer.widthCurve = curve;
		lineRenderer.widthMultiplier = width;
		lineRenderer.enabled = true;
		return lineRenderer;
	}

	public static void destroyChildren(GameObject go)
	{
		List<Transform> toBeDeleted = new List<Transform>();
		for (int i = 0; i < go.transform.childCount; i++)
		{
		    toBeDeleted.Add(go.transform.GetChild(i));
		}
		for (int i = 0; i < toBeDeleted.Count; i++)
		{
		    GameObject.Destroy(toBeDeleted[i].gameObject);
		}
	}

}

public class AlphaRestoreData
{
	public Dictionary<Material, float>  materialAlphaMap;
	public Dictionary<UIWidget, float> uiAlphaMap;
	public Dictionary<TextMeshPro, float> tmProLabelAlphaMap;
}

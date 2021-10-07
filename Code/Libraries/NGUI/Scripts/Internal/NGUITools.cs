//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

/// <summary>
/// Helper class containing generic functions used throughout the UI library.
/// </summary>

static public class NGUITools
{
	/// <summary>
	/// Helper function -- whether the disk access is allowed.
	/// </summary>

	static public bool fileAccess
	{
		get
		{
			return true;
//			return Application.platform != RuntimePlatform.WindowsWebPlayer &&
//				Application.platform != RuntimePlatform.OSXWebPlayer;
		}
	}

	/// <summary>
	/// New WWW call can fail if the crossdomain policy doesn't check out. Exceptions suck. It's much more elegant to check for null instead.
	/// </summary>

	static public WWW OpenURL (string url)
	{
#if UNITY_FLASH
		Debug.LogError("WWW is not yet implemented in Flash");
		return null;
#else
		WWW www = null;
		try { www = new WWW(url); }
		catch (System.Exception ex) { Debug.LogError(ex.Message); }
		return www;
#endif
	}

	/// <summary>
	/// New WWW call can fail if the crossdomain policy doesn't check out. Exceptions suck. It's much more elegant to check for null instead.
	/// </summary>

	static public WWW OpenURL (string url, WWWForm form)
	{
		if (form == null) return OpenURL(url);
#if UNITY_FLASH
		Debug.LogError("WWW is not yet implemented in Flash");
		return null;
#else
		WWW www = null;
		try { www = new WWW(url, form); }
		catch (System.Exception ex) { Debug.LogError(ex != null ? ex.Message : "<null>"); }
		return www;
#endif
	}

	/// <summary>
	/// Same as Random.Range, but the returned value is between min and max, inclusive.
	/// Unity's Random.Range is less than max instead, unless min == max.
	/// This means Range(0,1) produces 0 instead of 0 or 1. That's unacceptable.
	/// </summary>

	static public int RandomRange (int min, int max)
	{
		if (min == max) return min;
		return UnityEngine.Random.Range(min, max + 1);
	}

	/// <summary>
	/// Returns the hierarchy of the object in a human-readable format.
	/// </summary>

	static public string GetHierarchy (GameObject obj)
	{
		string path = obj.name;

		while (obj.transform.parent != null)
		{
			obj = obj.transform.parent.gameObject;
			path = obj.name + "/" + path;
		}
		return "\"" + path + "\"";
	}

	/// <summary>
	/// Parse a RrGgBb color encoded in the string.
	/// </summary>

	static public void ParseColor (string text, int offset, out Color colr)
	{
		const float f = 1f / 255f;
		colr.r = f*((NGUIMath.HexToDecimal(text[offset])	 << 4) | NGUIMath.HexToDecimal(text[offset + 1]));
		colr.g = f*((NGUIMath.HexToDecimal(text[offset + 2]) << 4) | NGUIMath.HexToDecimal(text[offset + 3]));
		colr.b = f*((NGUIMath.HexToDecimal(text[offset + 4]) << 4) | NGUIMath.HexToDecimal(text[offset + 5]));
		colr.a = 1.0f;
		//return new Color(f * r, f * g, f * b);
	}

	/// <summary>
	/// The reverse of ParseColor -- encodes a color in RrGgBb format.
	/// </summary>

	static public string EncodeColor (Color c)
	{
		int i = 0xFFFFFF & (NGUIMath.ColorToInt(c) >> 8);
		return NGUIMath.DecimalToHex(i);
	}

	static Color mInvisible = new Color(0f, 0f, 0f, 0f);

	/// <summary>
	/// Parse an embedded symbol, such as [FFAA00] (set color) or [-] (undo color change)
	/// </summary>

	static public int ParseSymbol (string text, int index, List<Color> colors, bool premultiply)
	{
		int length = text.Length;

		if (index + 2 < length)
		{
			if (text[index + 1] == '-')
			{
				if (text[index + 2] == ']')
				{
					if (colors != null && colors.Count > 1) colors.RemoveAt(colors.Count - 1);
					return 3;
				}
			}
			else if (index + 7 < length)
			{
				if (text[index + 7] == ']')
				{
					if (colors != null)
					{
						//better code to validate hex color string without allocations
						for(int j=0;j<6;j++)
						{
							char ch = text[index+1+j];
							if(!( ((ch>='0') && (ch <= '9')) ||
							      ((ch>='a') && (ch <= 'f')) ||
							      ((ch>='A') && (ch <= 'F'))))
								return 0;   // not a valid color string
						}
						
						Color c;
						ParseColor(text, index + 1, out c);

#if false
						// horrible code that does allocations just to validate 6-char string is hex format!
						if (EncodeColor(c) != text.Substring(index + 1, 6).ToUpper())
							return 0;
#endif

						c.a = colors[colors.Count - 1].a;
						if (premultiply && c.a != 1f)
							c = Color.Lerp(mInvisible, c, c.a);

						colors.Add(c);
					}
					return 8;
				}
			}
		}
		return 0;
	}

	/// <summary>
	/// Runs through the specified string and removes all color-encoding symbols.
	/// </summary>

	static public string StripSymbols (string text)
	{
		if (text != null)
		{
			int startIndex = text.IndexOf('[');
			if(startIndex < 0)
				return text;

			for (int i = startIndex, imax = text.Length; i < imax; )
			{
				char c = text[i];

				if (c == '[')
				{
					int retVal = ParseSymbol(text, i, null, false);

					if (retVal > 0)
					{
						text = text.Remove(i, retVal);
						imax = text.Length;
						continue;
					}
				}
				++i;
			}
		}
		return text;
	}

	/// <summary>
	/// Find all active objects of specified type.
	/// </summary>

	static public T[] FindActive<T> () where T : Component
	{
		return GameObject.FindObjectsOfType(typeof(T)) as T[];
	}

	/// <summary>
	/// Find the camera responsible for drawing the objects on the specified layer.
	/// </summary>

	static public Camera FindCameraForLayer (int layer)
	{
		int layerMask = 1 << layer;

		Camera[] cameras = NGUITools.FindActive<Camera>();

		for (int i = 0, imax = cameras.Length; i < imax; ++i)
		{
			Camera cam = cameras[i];

			if ((cam.cullingMask & layerMask) != 0)
			{
				return cam;
			}
		}
		return null;
	}

	/// <summary>
	/// Add a collider to the game object containing one or more widgets.
	/// </summary>

	static public BoxCollider AddWidgetCollider (GameObject go)
	{
		if (go != null)
		{
			Collider col = go.GetComponent<Collider>();
			BoxCollider box = col as BoxCollider;

			if (box == null)
			{
				if (col != null)
				{
					if (Application.isPlaying) GameObject.Destroy(col);
					else GameObject.DestroyImmediate(col);
				}
				box = go.AddComponent<BoxCollider>();
			}

			int depth = NGUITools.CalculateNextDepth(go);

			Bounds b = NGUIMath.CalculateRelativeWidgetBounds(go.transform);
			box.isTrigger = true;
			box.center = b.center + Vector3.back * (depth * 0.25f);
			box.size = new Vector3(b.size.x, b.size.y, 0f);
			return box;
		}
		return null;
	}

	/// <summary>
	/// Helper function that returns the string name of the type.
	/// </summary>

	static public string GetName<T> () where T : Component
	{
		string s = typeof(T).ToString();
		if (s.FastStartsWith("UI")) s = s.Substring(2);
		else if (s.FastStartsWith("UnityEngine.")) s = s.Substring(12);
		return s;
	}

	/// <summary>
	/// Add a new child game object.
	/// </summary>

	static public GameObject AddChild (GameObject parent)
	{
		GameObject go = new GameObject();

		if (parent != null)
		{
			Transform t = go.transform;
			t.parent = parent.transform;
			t.localPosition = Vector3.zero;
			t.localRotation = Quaternion.identity;
			t.localScale = Vector3.one;
			go.layer = parent.layer;
		}
		return go;
	}

	/// <summary>
	/// Instantiate an object and add it to the specified parent.
	/// </summary>

	static public GameObject AddChild (GameObject parent, GameObject prefab, bool preserveLocalTransform=false)
	{
		if (parent != null)
		{
			return AddChild(parent.transform, prefab, preserveLocalTransform);
		}

		return CommonGameObject.instantiate(prefab) as GameObject;
	}
	
	/// <summary>
	/// Instantiate an object and add it to the specified parent transform.
	/// </summary>

	static public GameObject AddChild (Transform parent, GameObject prefab, bool preserveLocalTransform=false)
	{
		GameObject go = CommonGameObject.instantiate(prefab, parent) as GameObject;

		if (go != null)
		{
			// KrisG Zynga: added.  Note original behavior is to always overwrite localtransform with Identity, I added option to override that.
			if (!preserveLocalTransform)
			{
				Transform t = go.transform;
				t.localPosition = Vector3.zero;
				t.localRotation = Quaternion.identity;
				t.localScale = Vector3.one;
			}
			go.layer = parent.gameObject.layer;
		}
		return go;
	}

	/// <summary>
	/// Gathers all widgets and calculates the depth for the next widget.
	/// </summary>

	static public int CalculateNextDepth (GameObject go)
	{
		int depth = -1;
		UIWidget[] widgets = go.GetComponentsInChildren<UIWidget>();
		for (int i = 0, imax = widgets.Length; i < imax; ++i) depth = Mathf.Max(depth, widgets[i].depth);
		return depth + 1;
	}

	/// <summary>
	/// Add a child object to the specified parent and attaches the specified script to it.
	/// </summary>

	static public T AddChild<T> (GameObject parent) where T : Component
	{
		GameObject go = AddChild(parent);
		go.name = GetName<T>();
		return go.AddComponent<T>();
	}

	/// <summary>
	/// Add a new widget of specified type.
	/// </summary>

	static public T AddWidget<T> (GameObject go) where T : UIWidget
	{
		int depth = CalculateNextDepth(go);

		// Create the widget and place it above other widgets
		T widget = AddChild<T>(go);
		widget.depth = depth;

		// Clear the local transform
		Transform t = widget.transform;
		t.localPosition = Vector3.zero;
		t.localRotation = Quaternion.identity;
		t.localScale = new Vector3(100f, 100f, 1f);
		widget.gameObject.layer = go.layer;

		return widget;
	}

	/// <summary>
	/// Add a sprite appropriate for the specified atlas sprite.
	/// It will be sliced if the sprite has an inner rect, and a regular sprite otherwise.
	/// </summary>

	static public UISprite AddSprite (GameObject go, UIAtlas atlas, string spriteName)
	{
		UIAtlas.Sprite sp = (atlas != null) ? atlas.GetSprite(spriteName) : null;
		UISprite sprite = AddWidget<UISprite>(go);
		sprite.type = (sp == null || sp.inner == sp.outer) ? UISprite.Type.Simple : UISprite.Type.Sliced;
		sprite.atlas = atlas;
		sprite.spriteName = spriteName;
		return sprite;
	}

	/// <summary>
	/// Get the rootmost object of the specified game object.
	/// </summary>

	static public GameObject GetRoot (GameObject go)
	{
		Transform t = go.transform;

		for (; ; )
		{
			Transform parent = t.parent;
			if (parent == null) break;
			t = parent;
		}
		return t.gameObject;
	}

	/// <summary>
	/// Finds the specified component on the game object or one of its parents.
	/// </summary>

	private  static  Dictionary<GameObject, Dictionary<Type, Component>> cachedReferences = new Dictionary<GameObject, Dictionary<Type, Component>>();
	private static List<GameObject> objectsToRemove = new List<GameObject>();
	
	static public T FindInParents<T>(GameObject go, bool forceLookup = false) where T : Component
	{
		if (go == null) return null;
		Type passedType = typeof(T); 
		
		// Declare the component we intend to return and see if it's cached
		Component comp = null;

		Dictionary<Type, Component> cachedDictionary = null;
		bool objectCached = cachedReferences.TryGetValue(go, out cachedDictionary);
		bool valueCached = cachedDictionary != null && cachedDictionary.TryGetValue(passedType, out comp);
		
		// If we have the gameobject cached already, lets try to grab the cached component references off of it
		if (forceLookup || (objectCached && !valueCached))
		{
			comp = go.GetComponent<T>();
		}
		
		// Ok we didn't have that component on that game object. Try the old way
		if (comp == null)
		{
			Transform t = go.transform.parent;

			while (t != null && comp == null)
			{
				comp = t.gameObject.GetComponent<T>();
				t = t.parent;
			}

			// If we have a valid component and transform
			if (comp != null && t != null)
			{
				if (!objectCached)
				{
					// We never cached the passed game object, so add it now
					cachedReferences.Add(go, new Dictionary<Type, Component>());
					cachedReferences[go].Add(passedType, comp);
				}	
				else if (!valueCached)
				{
					// We cached the game object and its dictionary, so add the new type
					cachedDictionary.Add(passedType, comp);
				}
				else
				{
					// We're probably refreshing the data
					cachedDictionary[passedType] = comp;
				}
			}
		}

		foreach (GameObject cachedGameObject in cachedReferences.Keys)
		{
			if (cachedGameObject == null)
			{
				objectsToRemove.Add(cachedGameObject);
			}
		}

		for (int i = 0; i < objectsToRemove.Count; i++)
		{
			cachedReferences.Remove(objectsToRemove[i]);
		}

		objectsToRemove.Clear();
		return (T) comp;
	}

	/// <summary>
	/// Destroy the specified object, immediately if in edit mode.
	/// </summary>

	static public void Destroy (UnityEngine.Object obj)
	{
		if (obj != null)
		{
			if (Application.isPlaying)
			{
				if (obj is GameObject)
				{
					GameObject go = obj as GameObject;
					go.transform.parent = null;
				}

				UnityEngine.Object.Destroy(obj);
			}
			else UnityEngine.Object.DestroyImmediate(obj);
		}
	}

	/// <summary>
	/// Destroy the specified object immediately, unless not in the editor, in which case the regular Destroy is used instead.
	/// </summary>

	static public void DestroyImmediate (UnityEngine.Object obj)
	{
		if (obj != null)
		{
#if UNITY_EDITOR
			// MC -- Aug 28, 2015, The idea here is to only use 'DestroyImmediate()' when the we are editing and not playing the game.
			//	This is used to cleanup NGUI UI widgets for NGUI components. In all other cases it should just use Destroy() as is
			//	the case with the new Unity5 guidelines.
			// if (Application.isEditor) UnityEngine.Object.DestroyImmediate(obj);
			// else UnityEngine.Object.Destroy(obj);
			if (Application.isEditor && !EditorApplication.isPlaying)
			{
				UnityEngine.Object.DestroyImmediate(obj);
			}
			else
			{
				UnityEngine.Object.Destroy(obj);
			}
#else
			UnityEngine.Object.Destroy(obj);
#endif
		}
	}

	/// <summary>
	/// Call the specified function on all objects in the scene.
	/// </summary>

	static public void Broadcast (string funcName)
	{
		GameObject[] gos = GameObject.FindObjectsOfType(typeof(GameObject)) as GameObject[];
		for (int i = 0, imax = gos.Length; i < imax; ++i) gos[i].SendMessage(funcName, SendMessageOptions.DontRequireReceiver);
	}

	/// <summary>
	/// Call the specified function on all objects in the scene.
	/// </summary>

	static public void Broadcast (string funcName, object param)
	{
		GameObject[] gos = GameObject.FindObjectsOfType(typeof(GameObject)) as GameObject[];
		for (int i = 0, imax = gos.Length; i < imax; ++i) gos[i].SendMessage(funcName, param, SendMessageOptions.DontRequireReceiver);
	}

	/// <summary>
	/// Determines whether the 'parent' contains a 'child' in its hierarchy.
	/// </summary>

	static public bool IsChild (Transform parent, Transform child)
	{
		if (parent == null || child == null) return false;

		while (child != null)
		{
			if (child == parent) return true;
			child = child.parent;
		}
		return false;
	}

	/// <summary>
	/// Activate the specified object and all of its children.
	/// </summary>

	static void Activate (Transform t)
	{
		SetActiveSelf(t.gameObject, true);

		// Prior to Unity 4, active state was not nested. It was possible to have an enabled child of a disabled object.
		// Unity 4 onwards made it so that the state is nested, and a disabled parent results in a disabled child.

		// If there is even a single enabled child, then we're using a Unity 4.0-based nested active state scheme.
		for (int i = 0, imax = t.childCount; i < imax; ++i)
		{
			Transform child = t.GetChild(i);
			if (child.gameObject.activeSelf) return;
		}

		// If this point is reached, then all the children are disabled, so we must be using a Unity 3.5-based active state scheme.
		for (int i = 0, imax = t.childCount; i < imax; ++i)
		{
			Transform child = t.GetChild(i);
			Activate(child);
		}
	}

	/// <summary>
	/// Deactivate the specified object and all of its children.
	/// </summary>

	static void Deactivate (Transform t)
	{
		SetActiveSelf(t.gameObject, false);
	}

	/// <summary>
	/// SetActiveRecursively enables children before parents. This is a problem when a widget gets re-enabled
	/// and it tries to find a panel on its parent.
	/// </summary>

	static public void SetActive (GameObject go, bool state)
	{
		if (state)
		{
			Activate(go.transform);
		}
		else
		{
			Deactivate(go.transform);
		}
	}

	/// <summary>
	/// Activate or deactivate children of the specified game object without changing the active state of the object itself.
	/// </summary>

	static public void SetActiveChildren (GameObject go, bool state)
	{
		Transform t = go.transform;

		if (state)
		{
			for (int i = 0, imax = t.childCount; i < imax; ++i)
			{
				Transform child = t.GetChild(i);
				Activate(child);
			}
		}
		else
		{
			for (int i = 0, imax = t.childCount; i < imax; ++i)
			{
				Transform child = t.GetChild(i);
				Deactivate(child);
			}
		}
	}

	/// <summary>
	/// Unity4 has changed GameObject.active to GameObject.activeself.
	/// </summary>

	static public bool GetActive(GameObject go)
	{
		return go && go.activeInHierarchy;
	}

	/// <summary>
	/// Unity4 has changed GameObject.active to GameObject.SetActive.
	/// </summary>

	static public void SetActiveSelf(GameObject go, bool state)
	{
		go.SetActive(state);
	}

	/// <summary>
	/// Recursively set the game object's layer.
	/// </summary>

	static public void SetLayer (GameObject go, int layer)
	{
		go.layer = layer;

		Transform t = go.transform;
		
		for (int i = 0, imax = t.childCount; i < imax; ++i)
		{
			Transform child = t.GetChild(i);
			SetLayer(child.gameObject, layer);
		}
	}

	/// <summary>
	/// Helper function used to make the vector use integer numbers.
	/// </summary>

	static public Vector3 Round (Vector3 v)
	{
		v.x = Mathf.Round(v.x);
		v.y = Mathf.Round(v.y);
		v.z = Mathf.Round(v.z);
		return v;
	}

	/// <summary>
	/// Make the specified selection pixel-perfect.
	/// </summary>

	static public void MakePixelPerfect (Transform t)
	{
		UIWidget w = t.GetComponent<UIWidget>();

		if (w != null)
		{
			w.MakePixelPerfect();
		}
		else
		{
			t.localPosition = Round(t.localPosition);
			t.localScale = Round(t.localScale);

			for (int i = 0, imax = t.childCount; i < imax; ++i)
			{
				MakePixelPerfect(t.GetChild(i));
			}
		}
	}

	/// <summary>
	/// Save the specified binary data into the specified file.
	/// </summary>

	static public bool Save (string fileName, byte[] bytes)
	{
#if UNITY_WEBPLAYER || UNITY_FLASH || UNITY_METRO || UNITY_WEBGL
		return false;
#else
		if (!NGUITools.fileAccess) return false;

		string path = Application.persistentDataPath + "/" + fileName;

		if (bytes == null)
		{
			if (File.Exists(path)) File.Delete(path);
			return true;
		}

		FileStream file = null;

		try
		{
			file = File.Create(path);
		}
		catch (System.Exception ex)
		{
			NGUIDebug.Log(ex.Message);
			return false;
		}

		file.Write(bytes, 0, bytes.Length);
		file.Close();
		return true;
#endif
	}

	/// <summary>
	/// Load all binary data from the specified file.
	/// </summary>

	static public byte[] Load (string fileName)
	{
#if UNITY_WEBPLAYER || UNITY_FLASH || UNITY_METRO || UNITY_WEBGL
		return null;
#else
		if (!NGUITools.fileAccess) return null;

		string path = Application.persistentDataPath + "/" + fileName;

		if (File.Exists(path))
		{
			return File.ReadAllBytes(path);
		}
		return null;
#endif
	}

	/// <summary>
	/// Pre-multiply shaders result in a black outline if this operation is done in the shader. It's better to do it outside.
	/// </summary>

	static public Color ApplyPMA (Color c)
	{
		if (c.a != 1f)
		{
			c.r *= c.a;
			c.g *= c.a;
			c.b *= c.a;
		}
		return c;
	}

	/// <summary>
	/// Inform all widgets underneath the specified object that the parent has changed.
	/// </summary>

	static public void MarkParentAsChanged (GameObject go)
	{
		UIWidget[] widgets = go.GetComponentsInChildren<UIWidget>();
		for (int i = 0, imax = widgets.Length; i < imax; ++i)
			widgets[i].ParentHasChanged();
	}

	/// <summary>
	/// Clipboard access via reflection.
	/// http://answers.unity3d.com/questions/266244/how-can-i-add-copypaste-clipboard-support-to-my-ga.html
	/// </summary>

#if UNITY_WEBPLAYER || UNITY_FLASH || UNITY_METRO || UNITY_WEBGL
	/// <summary>
	/// Access to the clipboard is not supported on this platform.
	/// </summary>

	public static string clipboard
	{
		get { return null; }
		set { }
	}
#else
	static PropertyInfo mSystemCopyBuffer = null;
	static PropertyInfo GetSystemCopyBufferProperty ()
	{
		if (mSystemCopyBuffer == null)
		{
			Type gui = typeof(GUIUtility);
			mSystemCopyBuffer = gui.GetProperty("systemCopyBuffer", BindingFlags.Static | BindingFlags.NonPublic);
		}
		return mSystemCopyBuffer;
	}

	/// <summary>
	/// Access to the clipboard via a hacky method of accessing Unity's internals. Won't work in the web player.
	/// </summary>

	public static string clipboard
	{
		get
		{
			PropertyInfo copyBuffer = GetSystemCopyBufferProperty();
			return (copyBuffer != null) ? (string)copyBuffer.GetValue(null, null) : null;
		}
		set
		{
			PropertyInfo copyBuffer = GetSystemCopyBufferProperty();
			if (copyBuffer != null) copyBuffer.SetValue(null, value, null);
		}
	}
#endif
}

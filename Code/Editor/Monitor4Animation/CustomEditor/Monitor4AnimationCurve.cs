using UnityEngine;
using UnityEditor;
using HierarchyHelper;

/*
 * Paul Note: Ripped from here: https://github.com/gydisme/Unity-Game-Framwork/tree/master/Assets/Editor/CustomEditor/Monitor4AnimationCurve
 */

public class Monitor4AnimationCurve : EditorWindow
{
	private Transform _monitorTransform = null;

	private void OnEnable()
	{
		if (EditorApplication.isPlayingOrWillChangePlaymode)
		{
			return;
		}

		_monitorTransform = null;
		HierarchyChangedDetector.onHierarchyChanged += monitorGameObject;
	}

	private void OnDisable()
	{
		HierarchyChangedDetector.onHierarchyChanged -= monitorGameObject;
	}

	private string getRelativeName(Transform t, bool includeSelf)
	{
		if (t == null)
		{
			return string.Empty;
		}

		string path = includeSelf ? t.name : string.Empty;
		while (t != _monitorTransform && t.parent != null)
		{
			t = t.parent.transform;
			path = t.name + "/" + path;
		}

		int i = path.IndexOf("/");
		if (i < 0)
		{
			path = "";
		}
		else
		{
			path = path.Substring(i + 1);
		}
		
		return path;
	}

	private string getLastRelativeName(HierarchyChangedDetector.EChangeType changeType, HierarchyChangedDetector.HierarchySnapshot snapshot)
	{
		if (changeType == HierarchyChangedDetector.EChangeType.Renamed)
		{
			return getRelativeName(snapshot.me, false) + snapshot.name;
		}
		else if (changeType == HierarchyChangedDetector.EChangeType.Parented)
		{
			if (snapshot.parent == null || !snapshot.parent.IsChildOf(_monitorTransform))
			{
				return string.Empty;
			}
				
			string path = getRelativeName(snapshot.parent, true);
			if (string.IsNullOrEmpty(path))
			{
				return snapshot.me.name;
			}
			else
			{
				return path + "/" + snapshot.me.name;
			}
		}
		else if (changeType == HierarchyChangedDetector.EChangeType.Created)
		{
		}
		else if (changeType == HierarchyChangedDetector.EChangeType.Deleted)
		{
		}

		return string.Empty;
	}

	private void monitorGameObject(HierarchyChangedDetector.EChangeType changeType, HierarchyChangedDetector.HierarchySnapshot snapshot)
	{
		if (_monitorTransform == null)
		{
			return;
		}

		if (changeType == HierarchyChangedDetector.EChangeType.Deleted)
		{
			if (snapshot.parent == null)
			{
				return;
			}
				
			if (!snapshot.parent.IsChildOf(_monitorTransform) && snapshot.parent != _monitorTransform)
			{
				return;
			}
		}

		if (changeType == HierarchyChangedDetector.EChangeType.Parented)
		{
			if (snapshot.me.parent == null)
			{
				return;
			}
		}

		string oldPath = getLastRelativeName(changeType, snapshot);
		string path = getRelativeName(snapshot.me, true);

		Debug.Log(oldPath + " => " + path);

		if (string.IsNullOrEmpty(oldPath))
		{
			return;
		}

		bool changed = false;
		AnimationClip[] anims = AnimationUtility.GetAnimationClips(_monitorTransform.gameObject);

		if (anims != null && anims.Length > 0)
		{
			foreach (AnimationClip ac in anims)
			{
				EditorCurveBinding[] objectCurveBinding = AnimationUtility.GetObjectReferenceCurveBindings(ac);
				EditorCurveBinding[] curveDataBinding = AnimationUtility.GetCurveBindings(ac);

				for (int i = 0; i < objectCurveBinding.Length; i++)
				{
					if (objectCurveBinding[i].path.CompareTo(oldPath) == 0 || objectCurveBinding[i].path.StartsWith(oldPath + "/"))
					{
						int index = objectCurveBinding[i].path.IndexOf(oldPath);
						string newPath = path + objectCurveBinding[i].path.Substring(index + oldPath.Length);

						ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(ac, objectCurveBinding[i]);
						AnimationUtility.SetObjectReferenceCurve(ac, objectCurveBinding[i], null);
						objectCurveBinding[i].path = newPath;
						AnimationUtility.SetObjectReferenceCurve(ac, objectCurveBinding[i], keyframes);

						changed = true;
					}
				}

				for (int i = 0; i < curveDataBinding.Length; i++)
				{
					if (curveDataBinding[i].path.CompareTo(oldPath) == 0 || curveDataBinding[i].path.StartsWith(oldPath + "/"))
					{
						int index = curveDataBinding[i].path.IndexOf(oldPath);
						string newPath = path + curveDataBinding[i].path.Substring(index + oldPath.Length);

						AnimationCurve c = AnimationUtility.GetEditorCurve(ac, curveDataBinding[i]);
						AnimationUtility.SetEditorCurve(ac, curveDataBinding[i], null);
						curveDataBinding[i].path = newPath;
						AnimationUtility.SetEditorCurve(ac, curveDataBinding[i], c);

						changed = true;
					}
				}
			}
		}

		if (changed)
		{
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			EditorApplication.RepaintAnimationWindow();
		}
	}

	private void OnGUI()
	{
		if (_monitorTransform == null)
		{
			GUI.backgroundColor = Color.white;
		}
		else
		{
			if (_monitorTransform.gameObject.GetComponent<Animator>() == null)
			{
				GUI.backgroundColor = Color.yellow;
			}
			else
			{
				GUI.backgroundColor = Color.green;
			}
		}

		_monitorTransform = EditorGUILayout.ObjectField("Monitor", _monitorTransform, typeof(Transform), true) as Transform;

		GUI.backgroundColor = Color.white;
		if (_monitorTransform != null)
		{
			if (_monitorTransform.gameObject.GetComponent<Animator>() == null)
			{
				EditorGUILayout.HelpBox("No animator was found on the above object. Animation clips will not be affected.", MessageType.Warning);
			}
		}
		else
		{
			EditorGUILayout.HelpBox("Add a game object with an animator above to monitor it for hierarchy changes.", MessageType.Info);
		}
	}

	[MenuItem("Zynga/Animation Clips/Hierarchy Monitor for Animation Changes", false, 0)]
	public static void OpenWindow()
	{
		EditorWindow.GetWindow<Monitor4AnimationCurve>(false, "Monitor4AnimationChanges", true);
	}
}
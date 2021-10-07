#define Debug_

using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/*
 * Paul Note: Ripped from here: https://github.com/gydisme/Unity-Game-Framwork/tree/master/Assets/Editor/Utility
 */

namespace HierarchyHelper
{
#if UNITY_EDITOR
	[InitializeOnLoad]
#endif
	public static class HierarchyChangedDetector
	{
		public class HierarchySnapshot
		{
			public Transform me;
			public Transform parent;
			public string name;
		}

		public enum EChangeType
		{
			Renamed,
			Created,
			Parented,
			Deleted
		}

		private static List<HierarchySnapshot> _hierarchySnapshots = null;
		private static List<Transform> _hierarchyTransforms = null;

		public delegate void OnHierarchyChanged(EChangeType changeType, HierarchySnapshot snapshot);
		public static OnHierarchyChanged onHierarchyChanged = delegate (EChangeType changeType, HierarchySnapshot snapshot) { };

		static HierarchyChangedDetector()
		{
			_hierarchySnapshots = new List<HierarchySnapshot>();
			_hierarchyTransforms = new List<Transform>();

			Transform[] all = GameObject.FindObjectsOfType<Transform>();
			foreach (Transform t in all)
			{
				HierarchySnapshot h = createSnapshot(t);
				_hierarchySnapshots.Add(h);
				_hierarchyTransforms.Add(t);
			}

#if UNITY_2017 || UNITY_5_6
            EditorApplication.hierarchyWindowChanged += onHierarchyChangeCheck;
#else
			EditorApplication.hierarchyChanged += onHierarchyChangeCheck;
#endif

#if Debug
			OnHierarchyChanged += onHierarchyChange;
#endif
		}

		private static HierarchySnapshot createSnapshot(Transform t)
		{
			HierarchySnapshot h = new HierarchySnapshot();
			h.name = t.name;
			h.parent = t.parent;
			h.me = t;
			return h;
		}

		private static void onHierarchyChangeCheck()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
			{
				return;
			}

			bool found = false;
			for (int i = 0; i < _hierarchySnapshots.Count;)
			{
				HierarchySnapshot h = _hierarchySnapshots[i];
				if (h.me == null)
				{
					_hierarchySnapshots.RemoveAt(i);
					_hierarchyTransforms.RemoveAt(i);
					onHierarchyChanged(EChangeType.Deleted, h);
					found = true;
					continue;
				}
				else if (h.parent != h.me.parent)
				{
					onHierarchyChanged(EChangeType.Parented, h);
					h.parent = h.me.parent;
					found = true;
					break;
				}
				else if (h.name != h.me.name)
				{
					onHierarchyChanged(EChangeType.Renamed, h);
					h.name = h.me.name;
					found = true;
					break;
				}

				i++;
			}

			if (!found)
			{
				Transform[] all = GameObject.FindObjectsOfType<Transform>();
				foreach (Transform t in all)
				{
					if (!_hierarchyTransforms.Contains(t))
					{
						HierarchySnapshot h = createSnapshot(t);
						_hierarchySnapshots.Add(h);
						_hierarchyTransforms.Add(t);

						onHierarchyChanged(EChangeType.Created, h);
					}
				}
			}
		}

		private static void onHierarchyChange(EChangeType changeType, HierarchySnapshot snapshot)
		{
			System.Text.StringBuilder log = new System.Text.StringBuilder();

			log.Append(changeType.ToString());

			if (snapshot.me != null)
			{
				log.Append(" name:" + snapshot.me.name + ", parent: " + snapshot.me.parent);
			}

			log.Append(" snapshot name: " + snapshot.name + ", parent: " + snapshot.parent);

			Debug.Log(log);
		}
	}
}

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class NGUIAuditor : EditorWindow
{
	[MenuItem("Zynga/Editor Tools/Editor Window Objects/NGUIAuditor")]
	public static void openNGUIAuditor()
	{
		NGUIAuditor nuigAuditor = (NGUIAuditor)EditorWindow.GetWindow(typeof(NGUIAuditor));
		nuigAuditor.Show();
	}

	private NGUIAuditorObject nguiAuditorObject;
	public void OnGUI()
	{
		if (nguiAuditorObject == null)
		{
		    nguiAuditorObject = new NGUIAuditorObject();
		}
	    nguiAuditorObject.drawGUI(position);
	}
}

public class NGUIAuditorObject : EditorWindowObject
{
	public GameObject targetObject;

	private Dictionary<UIAtlas, List<UISprite>> atlasCount;

	protected override string getButtonLabel()
	{
		return "NGUI Auditor";
	}

	protected override string getDescriptionLabel()
	{
		return "Finds all UISprites and groups those references by Atlas.";
	}

	public override void drawGuts(Rect position)
	{
		GUILayout.BeginVertical();
		targetObject = EditorGUILayout.ObjectField("Target Object", targetObject, typeof(GameObject), allowSceneObjects:true) as GameObject;

		if (GUILayout.Button("Find References!"))
		{
			atlasCount = new Dictionary<UIAtlas, List<UISprite>>();
			UISprite[] allSprites = targetObject.GetComponentsInChildren<UISprite>(true);
			for (int i = 0; i < allSprites.Length; i++)
			{
				UISprite sprite = allSprites[i];
				if (atlasCount.ContainsKey(sprite.atlas))
				{
					atlasCount[sprite.atlas].Add(sprite);
				}
				else
				{
					atlasCount[sprite.atlas] = new List<UISprite>();
					atlasCount[sprite.atlas].Add(sprite);
				}
			}
		}

		if (atlasCount != null)
		{
			GUILayout.Label("Found References:");
			foreach (KeyValuePair<UIAtlas, List<UISprite>> pair in atlasCount)
			{
				GUILayout.Label(string.Format("{0} : {1}", pair.Key.name, pair.Value.Count));
			}

			if (GUILayout.Button("LOG IT OUT"))
			{
				string result = "";
				foreach (KeyValuePair<UIAtlas, List<UISprite>> pair in atlasCount)
				{
					result += string.Format("Atlas: {0} with {1} uses:\n", pair.Key.name, pair.Value.Count);
					for (int i = 0; i < pair.Value.Count; i++)
					{
						UISprite sprite = pair.Value[i];
						result += string.Format("{0}/{1}\n", sprite.transform.parent.name, sprite.gameObject.name);
					}
					GUILayout.Label(string.Format("{0} : {1}", pair.Key.name, pair.Value.Count));
				}
				Debug.Log(result);
			}
		}
		GUILayout.EndVertical();
	}
}
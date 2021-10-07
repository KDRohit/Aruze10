using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/*
	Class: Collection Viewer
	Class to see all the cards in the current collection and which ones are collected
*/


public class CollectionViewer : EditorWindow
{
	[MenuItem("Zynga/Editor Tools/Collection Viewer")]
	public static void openCollectionViewer()
	{
		CollectionViewer collectionViewer = (CollectionViewer)EditorWindow.GetWindow(typeof(CollectionViewer));
		collectionViewer.Show();
	}

	private CollectionViewerObject collectionViewerObject;


	public void OnGUI()
	{
		if (collectionViewerObject == null)
		{
			collectionViewerObject = new CollectionViewerObject();
		}
		collectionViewerObject.drawGUI(position);
	}
}

public class CollectionViewerObject : EditorWindowObject
{
	private Color collectedColor = new Color(0.0f, 1.0f, 0.0f);

	protected override string getButtonLabel()
	{
		return "Collection Viewer";
	}

	protected override string getDescriptionLabel()
	{
		return "(RUNTIME ONLY) This will let you see all the sets and card currently available in the collection";
	}

	private string filter = "";

	private Vector2 scrollPos = Vector2.zero;

	private Dictionary<CollectableCardData, bool> expandedListCard = new Dictionary<CollectableCardData, bool>();
	private Dictionary<CollectableSetData, bool> expandedListSet = new Dictionary<CollectableSetData, bool>();


	private void populateExpanded()
	{
		if (expandedListCard == null)
		{
			expandedListCard = new Dictionary<CollectableCardData, bool>();
		}
		if (Collectables.Instance != null && Collectables.Instance.getAllCards().Count != expandedListCard.Count)
		{
			expandedListCard.Clear();
			foreach(KeyValuePair<string, CollectableCardData> cards in Collectables.Instance.getAllCards())
			{
				expandedListCard[cards.Value] = false;
			}
		}

		if (expandedListSet == null)
		{
			expandedListSet = new Dictionary<CollectableSetData, bool>();
		}
		if (Collectables.Instance != null && Collectables.Instance.getAllSets().Count != expandedListSet.Count)
		{
			expandedListSet.Clear();
			foreach(KeyValuePair<string, CollectableSetData> cards in Collectables.Instance.getAllSets())
			{
				expandedListSet[cards.Value] = false;
			}
		}
	}


	public bool isInFilterSet(string filter, CollectableSetData cSet)
	{
		if (string.IsNullOrEmpty(filter) || cSet.keyName.Contains(filter))
		{
			return true;
		}

		foreach (string cardName in cSet.cardsInSet)
		{
			if (cardName.Contains(filter))
			{
				return true;
			}
		}

		return false;
	}

	public bool isInFilterCard(string filter, CollectableCardData card)
	{
		return string.IsNullOrEmpty(filter) || 
			card.keyName.Contains(filter);
	}
	
	public override void drawGuts(Rect position)
	{
		scrollPos = GUILayout.BeginScrollView(scrollPos);
		GUILayout.BeginVertical();
		if (Application.isPlaying)
		{
			if (Collectables.Instance != null)
			{
				populateExpanded();
				filter = GUILayout.TextField(filter);

				foreach (CollectableSetData cSet in Collectables.Instance.getAllSets().Values)
				{
					if (isInFilterSet(filter, cSet))
					{
						expandedListSet[cSet] = EditorGUILayout.Foldout(expandedListSet[cSet], cSet.keyName);
						if (expandedListSet[cSet])
						{
							EditorGUI.indentLevel++;
							foreach (string cardname in cSet.cardsInSet)
							{
								CollectableCardData card = Collectables.Instance.findCard(cardname);
								if (isInFilterCard(filter, card))
								{
									Color prev = GUI.color;
									if (card.isCollected)
									{
										GUI.color = collectedColor;
									}
									expandedListCard[card] = EditorGUILayout.Foldout(expandedListCard[card], cardname, true);
									GUI.color = prev;
									if (expandedListCard[card])
									{
										GUILayout.TextArea(card.ToString());
									}
								}
							}

							EditorGUI.indentLevel--;
						}
					}
				}
			}
			else
			{
				GUILayout.Label("Collectables has not initialized yet.");
			}
		}
		else
		{
			GUILayout.Label("Need to be playing the game for this to run");
		}
		GUILayout.EndVertical();
		GUILayout.EndScrollView();		
	}
}

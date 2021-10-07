using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

public class PanelListDisplay : EditorWindow {

	public DialogCreator.Node rootNode;
	public int panelCount;

	public PanelListDisplay(DialogCreator.Node node, int count)
	{
		rootNode = node;
		panelCount = count;
	}

	private void generateContent(DialogCreator.Node currentNode)
	{
		GUILayout.BeginHorizontal();
		for (int i = 1; i < currentNode.depth; i++)
		{
			GUILayout.Label(" >");
		}
		if(currentNode.hasPanel)
		{
			GUI.contentColor = new Color(1, 0.1f, 0);
			GUILayout.Label(" " + currentNode.name + " (UIPanel)", EditorStyles.boldLabel);
			GUI.contentColor = new Color(1, 1, 1);
		}
		else
		{
			GUILayout.Label(" " + currentNode.name);
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		for (int i = 0; i < currentNode.children.Count; i++)
		{
			generateContent(currentNode.children[i]);
		}
	}

	private void OnGUI()
	{
		if(rootNode != null)
		{
			generateContent(rootNode);
			GUILayout.Label("Current UIPanels exist under " + rootNode.name + ": " + panelCount);
		}
	}

}

using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/*
	Class: MOTDViewer
		Editor Window Object conversion of the DevGUIMenuMOTD class.
*/


public class MotdViewer : EditorWindow
{
	[MenuItem("Zynga/Editor Tools/Motd Viewer")]
	public static void openMotdViewer()
	{
		MotdViewer playerViewer = (MotdViewer)EditorWindow.GetWindow(typeof(MotdViewer));
		playerViewer.Show();
	}

	private MotdViewerObject playerViewerObject;

	public void OnGUI()
	{
		if (playerViewerObject == null)
		{
			playerViewerObject = new MotdViewerObject();
		}
		playerViewerObject.drawGUI(position);
	}
}

public class MotdViewerObject : EditorWindowObject
{
	protected override string getButtonLabel()
	{
		return "MOTD Viewer";
	}

	protected override string getDescriptionLabel()
	{
		return "(RUNTIME ONLY) This will let you see the current MOTD list";
	}

	private Vector2 scrollPosition = Vector2.zero;

	private const float BUTTON_WIDTH = 300f;

	public override void drawGuts(Rect position)
	{
		DevGUIMenuMOTD.drawInternals();
	}
}

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

//struct for custom filter histories
struct FilterObject
{
	public List<System.Type> types;
	public List<Object> listing;
	public bool show;
	public string typeString;
	public List<string> tags;
	
	public FilterObject(List<System.Type> theTypes, List<string> theTags, string theTypeString)
	{
		typeString = theTypeString;
		tags = theTags;
		types = theTypes;
		listing = new List<Object>(10);
		show = true;
	}
}

/**
 History window for the editor. Tracks the last 10 objects that were selected by default. Custom histories can be set in the editor so that objects of a 
 specific type or tag may be tracked seperately from the main history. Custom filters save to editor prefs and are reloaded on window's init.
 
 Currently if an object in the history is unloaded or destroyed it will be removed from the history window.
*/
public class History : EditorWindow 
{
	// Main object history
	static private List<Object> objectHistory = new List<Object>(10);
	// index used to avoid reording objects while using hotkeys
	static private int currentlySelectedIndex = 0;
	
	// Holds custom history filters
	private List<FilterObject> listings = new List<FilterObject>(10);
	
	// variables used by editor display
	private string typeString = "";
	private Vector2 scrollPos;
	private List<bool> showLists = new List<bool>();
	private List<int> toBeRemoved = new List<int>();
	private string currentSelection = "";
	private bool doNotUpdate = false; // used to avoid reordering lists and selected object is updated through the history window
	private bool showMainHistory = true;
	
	// Json string used to saved filters
	private List<string> jsonStrings = new List<string>();

	
	[MenuItem ("Window/History/Open %#H")]
	static void Init ()
	{
		History window = (History)EditorWindow.GetWindow(typeof (History));
		
		// Rebuild custom filters form preferences.
		string test = EditorPrefs.GetString("HistoryPref", "");
		string[] fromPrefs = test.Split('*');
		foreach(string obj in fromPrefs)
		{
			window.AddFilteredHistory(obj);
		}
	}
	
	void OnGUI()
	{
		scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
		showMainHistory = EditorGUILayout.Foldout(showMainHistory, "Object history");
		// draw each item in the main history
		if(showMainHistory)
		{
			for(int i = 0; i < objectHistory.Count; i++)
			{
				Object obj = objectHistory[i];
				// only draw if object is still loaded/exists
				if(obj != null)
				{
					GUI.SetNextControlName("historyObj,-1," + i);
					objectHistory[i] = EditorGUILayout.ObjectField(obj, typeof(Object), false);
				}
			}
		}
		
		// Back and next buttons. Only drawn when history has some objects. Using these will not reorder the history
		if(objectHistory.Count > 1)
		{
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("back"))
			{
				PreviousObject();
			}
			if (GUILayout.Button("next"))
			{
				NextObject();
			}
			EditorGUILayout.EndHorizontal();
		}
		
		GUILayout.Space(10.0f);
		
		// Text field to enter object type name for new filter category
		GUI.SetNextControlName("FilterField");
		typeString = EditorGUILayout.TextField("Enter Filter Type", typeString);
		UpdateHelp();
		if (GUILayout.Button("Add New Type"))
		{
			AddFilteredHistory(typeString);
		}
		
		toBeRemoved.Clear();
		// iterate through the custom filter sets drawing them all
		for(int i = 0; i < listings.Count; i++)
		{
			GUILayout.BeginHorizontal();
			showLists[i] = EditorGUILayout.Foldout(showLists[i], listings[i].typeString);
			if(GUILayout.Button("Remove", GUILayout.Width(60.0f)))
			{
				toBeRemoved.Add(i);
			}
			GUILayout.EndHorizontal();
			if(showLists[i])
			{
				for(int j = 0; j < listings[i].listing.Count; j++)
				{
					// only draw if the item still exists
					if(listings[i].listing[j] != null)
					{
						GUI.SetNextControlName("historyObj," + i + "," + j);
						EditorGUILayout.ObjectField(listings[i].listing[j], typeof(object), false);
					}
				}
			}
		}
		
		EditorGUILayout.EndScrollView();
		
		// Check for key inputs, if enter is pressed try to add the current filter in textfield
		if(Event.current.isKey)
		{
			switch(Event.current.keyCode)
			{
				case KeyCode.Return:
				case KeyCode.KeypadEnter:
				{
					AddFilteredHistory(typeString);
					Repaint();
				}
				break;
			}
		}
		
		// Removed the flagged filters
		if(toBeRemoved.Count > 0)
		{
			for(int i = toBeRemoved.Count - 1; i >= 0; i--)
			{
				listings.RemoveAt(toBeRemoved[i]);
			}
			// Save updated filters to preferences.
			UpdateFilterPreferences();
			Repaint();
		}
		
		SelectionCheck();
	}
	
	// adds in help dialog when certain elements are selected.
	private void UpdateHelp()
	{
		string selected = GUI.GetNameOfFocusedControl();
		if(selected == "FilterField")
		{
			EditorGUILayout.HelpBox("Enter a type to create a history for that type only, this is not case sensitive and spaces are ignored." +
				" Multiple types can be in one filter. Filters for tags can be created using \"tag:tagName\".(\"gameobject\" or " + 
				"\"Camera,material, tag:TestTag\")", MessageType.Info);
		}
	}
	
	// Try to add a custom filtered history based on the name of a type. If the string cannot be matched to a type nothing is done.
	private void AddFilteredHistory(string theString)
	{
		// some basic sanitization
		theString = theString.Replace(" ", "");
		string[] subStrings = theString.Split(',');
		
		List<System.Type> tempTypes = new List<System.Type>();
		List<string> tempTags = new List<string>();
		System.Type t = null;
		
		string jsonString = "";
		
		// parse the string looking for types/tags to be tracked
		for(int i = 0; i < subStrings.Length; i++)
		{
			// Check for tags
			if(subStrings[i].Contains("tag:"))
			{
				tempTags.Add(subStrings[i].ToString().Replace("tag:", ""));
				jsonString += subStrings[i];
			}
			// tags done, onto types
			else
			{
				t = GetType(subStrings[i]);
				if(t != null)
				{
					tempTypes.Add(t);
					if(tempTypes.Count > 0 && i > 0) 
					{
						jsonString += ",";
					}
					jsonString += subStrings[i];
				}
			}
		}
		
		if(tempTypes.Count > 0 || tempTags.Count > 0)
		{
			// use the json string for the title, it wont contain any types that werent found.
			listings.Insert(0, new FilterObject(tempTypes, tempTags, jsonString));
			showLists.Insert(0, true);
			jsonStrings.Add(jsonString);
		}
		// save the set to preferences.
		UpdateFilterPreferences();
		
		// blank out the filter type text field.
		typeString = "";
	}
	
	/// Save the current custom filter set to the editor preferences
	private void UpdateFilterPreferences()
	{
		string myString = "";
		for(int i = 0; i < listings.Count; i++)
		{
			myString += i > 0 ? "*" : "";
			myString += listings[i].typeString;
		}
		
		EditorPrefs.SetString("HistoryPref", myString);
	}
	
	[MenuItem ("Window/History/Next %#.")]
	static void NextObject()
	{
		if(objectHistory.Count ==0) return;
		currentlySelectedIndex--;
		currentlySelectedIndex = Mathf.Clamp(currentlySelectedIndex, 0, objectHistory.Count -1);
		GUI.FocusControl("historyObj,-1," + currentlySelectedIndex);
		
		Selection.objects = new Object[]{objectHistory[currentlySelectedIndex]};
		EditorGUIUtility.PingObject(objectHistory[currentlySelectedIndex]);
	}
	
	
	[MenuItem ("Window/History/Previous %#,")]
	static void PreviousObject()
	{
		if(objectHistory.Count ==0) return;
		currentlySelectedIndex++;
		currentlySelectedIndex = Mathf.Clamp(currentlySelectedIndex, 0, objectHistory.Count -1);
		GUI.FocusControl("historyObj,-1," + currentlySelectedIndex);
		
		Selection.objects = new Object[]{objectHistory[currentlySelectedIndex]};
		EditorGUIUtility.PingObject(objectHistory[currentlySelectedIndex]);
	}
	
	void OnSelectionChange()
	{
		if(doNotUpdate)
		{
			doNotUpdate = false;
			return;
		}
		
		if (objectHistory.Count <= currentlySelectedIndex || objectHistory[currentlySelectedIndex] != Selection.activeObject)
		{
			// Try to remove the current selection from history, prevents duplicate entrys and will move item to the top
			objectHistory.Remove(Selection.activeObject);
			// Add the object to the start of the history list
			objectHistory.Insert(0, Selection.activeObject);
			currentlySelectedIndex = 0;
		}
		
		// Iterate through listings checking if the new selected type matches any of the custom filter sets
		Object[] test;
		string tempTag = "";
		for(int i = 0; i < listings.Count; i++)
		{
			// Run through type filters
			for(int j = 0; j < listings[i].types.Count; j++)
			{
				test = Selection.GetFiltered(listings[i].types[j], SelectionMode.Unfiltered);
				
				// add each object found
				foreach(Object obj in test)
				{
					listings[i].listing.Remove(obj);
					listings[i].listing.Insert(0, obj);
				}
				// remove anything past 10 objects
				while(listings[i].listing.Count > 10)
				{
					listings[i].listing.RemoveAt(listings[i].listing.Count - 1);
				}
			}
			// Run through tag filters but only if the selection is a game object, otherwise it wont even have tags
			if(Selection.activeGameObject != null)
			{
				for(int k = 0; k < listings[i].tags.Count; k++)
				{
					tempTag = Selection.activeGameObject.tag;
					if(tempTag == listings[i].tags[k])
					{
						listings[i].listing.Remove(Selection.activeGameObject);
						listings[i].listing.Insert(0, Selection.activeGameObject);
					}
				}
			}
		}
		
		// remove anything past the 10th object
		while(objectHistory.Count > 10)
		{
			objectHistory.RemoveAt(objectHistory.Count - 1);
		}
		
		// repaint or the updated lists may never show up.
		Repaint();
	}
	
	/// Custom GetType looks for the type in system, UnityEngine and UnityEditor.
	System.Type GetType(string theString)
	{
		System.Type tempType = System.Type.GetType(theString, false, true);
		if(tempType != null)
		{
			return tempType;
		}
		
		tempType = System.Type.GetType("UnityEngine." + theString + ", UnityEngine", false, true);
		if(tempType != null)
		{
			return tempType;
		}
		
		tempType = System.Type.GetType("UnityEditor." + theString + ", UnityEditor", false, true);
		if(tempType != null)
		{
			return tempType;
		}
		return null;
	}
	
	
	/// Check currently selected item name, if its one of the history objects, set the object as the active object for the inspector and flag
	/// that the selection change was via history and not to reorder the lists.
	void SelectionCheck()
	{
		string tempString = GUI.GetNameOfFocusedControl();

		if(currentSelection != tempString)
		{
			currentSelection = tempString;
			// if the string is none null its likely a history object that should be set as the selected object in the inspector.
			if(currentSelection != "")
			{
				doNotUpdate = true;
				string[] historyPath = currentSelection.Split(',');
				// based on the string set the proper object as the selection
				if(historyPath[0] != null && historyPath[0] == "historyObj")
				{
					if(historyPath[1] != null && historyPath[1] == "-1")
					{
						Selection.objects = new Object[]{objectHistory[System.Convert.ToInt32(historyPath[2])]};
					}
					else if(historyPath[1] != null)
					{
						Selection.objects = new Object[]{listings[System.Convert.ToInt32(historyPath[1])].listing[System.Convert.ToInt32(historyPath[2])]};
					}
				}
			}
		}
	}
}

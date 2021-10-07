using UnityEngine;
using System.Collections;
using UnityEditor;

/**
This is pretty basic right now, so please feel free to expand it.
What this does is simply allow you to store up GameObject references,
which can be from the current selection or manually dragged over.
*/
public class ObjectHolder : ScriptableWizard
{
	public bool resetToSelection = true;	///< Toggle true to force the object holder to populate with your current Unity object selection
	public GameObject[] objects;			///< A list of the stored objects
	
	[MenuItem ("Zynga/Wizards/Object Holder")]static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<ObjectHolder>("Object Holder", "Close");
	}
	
	public void OnWizardUpdate()
	{
		if (resetToSelection)
		{
			resetToSelection = false;
			helpString = "This is only a temporary storage place for GameObject references.\nFeel free to store as many random things as you want in the provided GameObject array.";
			
			Transform[] selection = Selection.GetTransforms(SelectionMode.Editable | SelectionMode.TopLevel);
			objects = new GameObject[selection.Length];
			
			for (int i = 0; i < selection.Length; i++)
			{
				objects[i] = selection[i].gameObject;
			}
		}
	}
	
	public void OnWizardCreate()
	{
	
	}
}
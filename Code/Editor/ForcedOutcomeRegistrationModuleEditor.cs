using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/// Author: Scott Lepthien
/// This is a custom inspector to allow the ForcedOutcomeRegistrationModule to force a refresh of forced outcomes to SlotBaseGame
[CustomEditor(typeof(ForcedOutcomeRegistrationModule))]
public class ForcedOutcomeRegistrationModuleEditor : Editor 
{
	public override void OnInspectorGUI()
	{
		 ForcedOutcomeRegistrationModule forcedOutcomeModule = (ForcedOutcomeRegistrationModule)target;

		// Add a button to force the SlotBaseGame to refresh its list of forced outcomes
		if(GUILayout.Button("Refresh Outcomes In SlotBaseGame"))
        {
            forcedOutcomeModule.registerAllForcedOutcomesToSlotBaseGame();
        }

		// Show default inspector property editor
		DrawDefaultInspector ();
	}
}

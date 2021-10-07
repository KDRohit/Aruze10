using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Zap.Automation.Editor
{
	#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	public delegate void TopBarButtonMethod();
	public static class ZAPEditorHelpers
	{
		/// <summary>
		/// Will return a list file names of the test plans stored in a directory
		/// </summary>
		/// <param name="location">The directory to search.  If left blank it will search the default location.</param>
		/// <returns></returns>
		public static List<string> getTestPlansAtLocation(string location = "")
		{
			List<string> testPlans = new List<string>();
			if(string.IsNullOrEmpty(location))
			{
				//If they haven't set the location try to grab the default
				location = ZAPFileHandler.getZapSaveFileLocation();
			}

			if (string.IsNullOrEmpty(location))
			{
				Debug.LogWarning("You must set your zap directory in Preferences");
				return testPlans;
			}
			
			// Make sure that the file location exists, in case it hasn't been used yet
			// or was cleaned up using the Operating System
			CommonFileSystem.createDirectoryIfNotExisting(location);

			DirectoryInfo d = new DirectoryInfo(location);
			FileInfo[] Files = d.GetFiles("*.json"); //Getting Text files			
			foreach (FileInfo file in Files)
			{
				testPlans.Add(file.Name);
			}
			return testPlans;
		}
		
		/// <summary>
		/// A function to create an automatable for a slot game
		/// </summary>
		/// <param name="gameKey">the game key for the automatable</param>
		/// <returns></returns>
		public static AutomatableSlotBaseGame defaultBaseGameAutomatable(string gameKey = "")
		{
			//Create a blank Automatable game
			AutomatableSlotBaseGame automatable = ScriptableObject.CreateInstance(typeof(AutomatableSlotBaseGame)) as AutomatableSlotBaseGame;

			//Create a spin test give it 10 spins
			SpinTest spinTest = ScriptableObject.CreateInstance(typeof(SpinTest)) as SpinTest;
			spinTest.iterations = 10;

			//Create a cheats test
			CheatsTest cheatsTest = ScriptableObject.CreateInstance(typeof(CheatsTest)) as CheatsTest;

			//Create a free spin test 
			//FreeSpinTest freeSpinTest = ScriptableObject.CreateInstance(typeof(FreeSpinTest)) as FreeSpinTest;

			//Create a picking game test 
			//PickingGameTest pickingGameTest = ScriptableObject.CreateInstance(typeof(PickingGameTest)) as PickingGameTest;

			//Add our test to the automatable
			automatable.tests.Add(spinTest);
			automatable.tests.Add(cheatsTest);
			//automatable.tests.Add(freeSpinTest);
			//automatable.tests.Add(pickingGameTest);

			//Set the game key if provided
			if (!string.IsNullOrEmpty(gameKey))
			{
				automatable.key = gameKey;
			}

			return automatable;
		}
		
		#region Common OnGUI Functions
		public static void drawTopBar(string barTitle, Dictionary<string, TopBarButtonMethod> buttons)
		{
			Rect backgroundRect = new Rect(0,
				0,
				EditorGUIUtility.currentViewWidth,
				EditorGUIUtility.singleLineHeight);

			if (Event.current.type == EventType.Repaint)
			{
				EditorStyles.toolbar.Draw(backgroundRect, false, true, true, false);
			}

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(barTitle);
			GUILayout.FlexibleSpace();
			if (buttons != null)
			{
				foreach (string key in buttons.Keys)
				{
					if (GUILayout.Button(key, EditorStyles.toolbarButton))
					{
						buttons[key]();
					}
				}
			}
			EditorGUILayout.EndHorizontal();
		}
		#endregion Common OnGUI Functions
	}
	#endif
}

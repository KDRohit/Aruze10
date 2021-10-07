using System;
using UnityEngine;
using UnityEditor;

/*
NOTES:
- TransformInspector code from: http://wiki.unity3d.com/index.php?title=TransformInspector
*/

[CanEditMultipleObjects, CustomEditor(typeof(Transform))]
public class CustomTransformInspectorEditor : Editor
{
#region Custom Transform Inspector Variables
	private const float LABEL_WIDTH = 150.0f;
	private const float BUTTON_WIDTH = 16.0f;
	private const float BUTTON_HEIGHT = 16.0f;
	private const float BUTTON_EMPTY = BUTTON_WIDTH + 2.0f;
	private const float LINE_SPACER = 2.0f;
	private const int GROUP_SPACING = 15;
	private Vector3 tempVector = new Vector3(7777, 7777, 7777); //Used for the reset and copy/paste functionality to set a temporary value before applying the actual value we want (seems like a unity bug...)

	private const string BUTTON_IMAGE_FOLDER = "Assets/Code/Editor/zCustomTransformInspector/Textures/";
	private Texture BUTTON_TEXTURE_LOCK;
	private Texture BUTTON_TEXTURE_UNLOCK;
	private Texture BUTTON_TEXTURE_ROUND;
	private Texture BUTTON_TEXTURE_RESET;

	private Color buttonColorLock = new Color32(255,188,0,255);
	private Color buttonColorUnlock = Color.green;
	private Color buttonColorRound = new Color32(0,242,242,255);
	private Color buttonColorReset = Color.magenta;

	private const bool WIDE_MODE = true;
	private bool scaleProportionLocked = false;

	private const float POSITION_MAX = Mathf.Infinity; //original script linked at the top of this file had this set to 100000.0f; I don't know if it matters but I'm changing it to infinity for now and can change it back later if we encounter any issues
#endregion

#region Additional Tools Variables
	private bool showAdditionalTools = false;
	private bool showCopyPasteTools = false;
	private bool showAlignmentTools = false;
	private bool showScalingTools = false;
	private bool showMiscTools = false;
	private bool positionToggle = false;
	private bool rotationToggle = false;
	private bool scaleToggle = false;
	private bool isToggleAcive = false;

	private static Component[] componentList;
	#endregion

#region Prefab Wizard Variables
	private GameObject rootGO;
	private int rootGOID;
	private int immediateRootGOID;

	//UI Variables
	private Color colorGood = Color.green;
	private Color colorBad = Color.red;
	private Color colorWarning = Color.yellow;

	private bool validRootGO;
	private bool rootGOHasOverrides;
#endregion

#region Copy/Paste to clipboard function
	private static void CopyToClipboard(string str)
	{
		var textEditor = new TextEditor();
		textEditor.text = str;
		textEditor.SelectAll();
		textEditor.Copy();
	}
#endregion

#region Default transform inspector variables
	private static GUIContent positionGUIContent = new GUIContent(("Position"), ("The local position of this Game Object relative to the parent."));
	private static GUIContent rotationGUIContent = new GUIContent(("Rotation"), ("The local rotation of this Game Object relative to the parent."));
	private static GUIContent scaleGUIContent = new GUIContent(("Scale"), ("The local scaling of this Game Object relative to the parent."));

	private static string positionWarningText = "Due to floating-point precision limitations, it is recommended to bring the world coordinates of the GameObject within a smaller range.";

	private SerializedProperty positionProperty;
	private SerializedProperty rotationProperty;
	private SerializedProperty scaleProperty;
#endregion

	public void OnEnable()
	{
		this.positionProperty = this.serializedObject.FindProperty("m_LocalPosition");
		this.rotationProperty = this.serializedObject.FindProperty("m_LocalRotation");
		this.scaleProperty = this.serializedObject.FindProperty("m_LocalScale");

		Selection.selectionChanged += onSelectionChange;

		loadInspectorTextures();
	}

	public void OnDisable()
	{
		Selection.selectionChanged -= onSelectionChange;

		resetInspectorTextures();
	}

	private void loadInspectorTextures()
	{
		BUTTON_TEXTURE_LOCK = (Texture)AssetDatabase.LoadAssetAtPath(BUTTON_IMAGE_FOLDER + "button_image_lock_00.png", typeof(Texture));
		BUTTON_TEXTURE_UNLOCK = (Texture)AssetDatabase.LoadAssetAtPath(BUTTON_IMAGE_FOLDER + "button_image_lock_01.png", typeof(Texture));
		BUTTON_TEXTURE_RESET = (Texture)AssetDatabase.LoadAssetAtPath(BUTTON_IMAGE_FOLDER + "button_image_reset.png", typeof(Texture));
		BUTTON_TEXTURE_ROUND = (Texture)AssetDatabase.LoadAssetAtPath(BUTTON_IMAGE_FOLDER + "button_image_round.png", typeof(Texture));
	}

	private void resetInspectorTextures()
	{
		BUTTON_TEXTURE_LOCK = null;
		BUTTON_TEXTURE_UNLOCK = null;
		BUTTON_TEXTURE_RESET = null;
		BUTTON_TEXTURE_ROUND = null;
	}

	private void onSelectionChange() //Used for the Prefab Wizard functionality
	{
		if (Selection.activeGameObject != null)
		{
			try
			{
				if (PrefabUtility.IsPartOfAnyPrefab(Selection.activeGameObject))
				{
					rootGOID = PrefabUtility.GetOutermostPrefabInstanceRoot(Selection.activeGameObject).GetInstanceID();
					immediateRootGOID = PrefabUtility.GetNearestPrefabInstanceRoot(Selection.activeGameObject).GetInstanceID();

					validRootGO = true;
				}
				else
				{
					validRootGO = false;
				}
			}
			catch { } //without this catch{}, selecting prefabs in the project view will throw an error to the console
		}
		else
		{
			validRootGO = false;
		}

		Repaint(); //Redraw the window so it updates even if it isn't focused
	}

	private void OnSceneGUI()
	{
		if (EditorGUI.actionKey) //Need this to update the inspector while holding down the CTRL/CMD key while not hovering over the inspector (otherwise it only triggers when your mouse is over the inspector)
		{
			Repaint();
		}
	}

	private void updatePlayerPrefs()
	{
		//initalizes the starting state for some of the utility UI
		scaleProportionLocked = EditorPrefs.GetBool("CustomTransformInspector_ScaleProportionLocked", scaleProportionLocked);						//Is the scale proportion lock button toggled?
		showAdditionalTools = EditorPrefs.GetBool("CustomTransformInspector_ShowAdditionalToolsToggle", showAdditionalTools);	//Is the Additional Tools rollout toggled?
		showCopyPasteTools = EditorPrefs.GetBool("CustomTransformInspector_ShowCopyPasteToolsToggle", showCopyPasteTools);      //Is the Copy/Paste Tools button toggled?
		positionToggle = EditorPrefs.GetBool("CustomTransformInspector_CopyPasteToolsToggle_Position", positionToggle);         //Is the position copy toggle on or off?
		rotationToggle = EditorPrefs.GetBool("CustomTransformInspector_CopyPasteToolsToggle_Rotation", rotationToggle);         //Is the rotation copy toggle on or off?
		scaleToggle = EditorPrefs.GetBool("CustomTransformInspector_CopyPasteToolsToggle_Scale", scaleToggle);                  //Is the scale copy toggle on or off?
		showScalingTools = EditorPrefs.GetBool("CustomTransformInspector_ShowScaleToolsToggle", showScalingTools);              //Is the Scale Tools button toggled?
		showMiscTools = EditorPrefs.GetBool("CustomTransformInspector_ShowMiscToolsToggle", showMiscTools);						//Is the "Miscellaneous Tools button toggled?
	}

	private GameObject getTopmostPrefab(GameObject obj)
	{
		//Loop through the parents of obj until it finds the top-most parent who isn't a prefab
		while (PrefabUtility.IsPartOfAnyPrefab(PrefabUtility.GetOutermostPrefabInstanceRoot(obj).transform.parent.gameObject))
		{
			obj = PrefabUtility.GetOutermostPrefabInstanceRoot(obj).transform.parent.gameObject;
		}

		return obj;
	}

	private void openNewInstanceOfPrefab(GameObject obj, bool useImmediatePrefab)
	{
		GameObject basePrefab = PrefabUtility.GetCorrespondingObjectFromSource(obj);
		GameObject basePrefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);		
		
		Transform parent = obj.transform.parent; //Find the rootGO's parent transform
		basePrefabInstance.transform.parent = getTopmostPrefab(obj).transform.parent;

		basePrefabInstance.transform.localPosition = basePrefab.transform.localPosition;
		basePrefabInstance.transform.rotation = basePrefab.transform.rotation;
		basePrefabInstance.transform.localScale = basePrefab.transform.localScale;

		Selection.activeGameObject = basePrefabInstance;
	}

	public override void OnInspectorGUI()
	{
		EditorGUIUtility.wideMode = WIDE_MODE;
		EditorGUIUtility.labelWidth = LABEL_WIDTH; //Sets the maximum width for how long the Position/Rotation/Scale fields extend into the inspector; the default value of 0 leaves them too long, so we want to limit the width so the X/Y/Z fields can be larger

		updatePlayerPrefs();

		this.serializedObject.Update();

#region Position/Rotation/Scale Fields
		//POSITION/////////////////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();

		EditorGUILayout.PropertyField(this.positionProperty, positionGUIContent); //Position UI Element

		GUI.color = buttonColorRound;
		if (GUILayout.Button(new GUIContent(BUTTON_TEXTURE_ROUND, "Round position"), GUIStyle.none, GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT))) //Position Rounding Button Handler
		{
			roundValues(positionProperty);
		}

		GUILayout.Space(2);

		GUI.color = buttonColorReset;
		if (GUILayout.Button(new GUIContent(BUTTON_TEXTURE_RESET, "Reset position"), GUIStyle.none, GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT))) //Position Reset Button Handler
		{
			resetValues(positionProperty);
		}

		GUILayout.Space(BUTTON_EMPTY);
		GUI.color = Color.white;
		GUILayout.EndHorizontal();
		GUILayout.Space(LINE_SPACER);

		//ROTATION/////////////////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();
		this.RotationPropertyField(this.rotationProperty, rotationGUIContent); //Rotation UI Element

		GUI.color = buttonColorRound;
		if (GUILayout.Button(new GUIContent(BUTTON_TEXTURE_ROUND, "Round rotation"), GUIStyle.none, GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT))) //Rotation Rounding Button Handler
		{
			roundValues(rotationProperty);
		}

		GUILayout.Space(2);

		GUI.color = buttonColorReset;
		if (GUILayout.Button(new GUIContent(BUTTON_TEXTURE_RESET, "Reset rotation"), GUIStyle.none, GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT))) //Rotation Reset Button Handler
		{
			resetValues(rotationProperty);
		}

		GUILayout.Space(BUTTON_EMPTY);
		GUI.color = Color.white;
		GUILayout.EndHorizontal();
		GUILayout.Space(LINE_SPACER);

		//SCALE/////////////////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();

		Vector3 value = this.scaleProperty.vector3Value; //Stores the current scale for the proportional scale lock feature below
		Vector3 originalValue = value;

		EditorGUI.BeginChangeCheck();

		EditorGUILayout.PropertyField(this.scaleProperty, scaleGUIContent); //Scale UI Element

		if (EditorGUI.EndChangeCheck())
		{
			Vector3 newValue = new Vector3();

			if (scaleProportionLocked)
			{
				newValue.x = this.scaleProperty.vector3Value[0];

				if (newValue.x > 0.0f)
				{
					float difference = newValue.x / originalValue.x;

					if (scaleProperty.vector3Value[1] == 0.0f)
					{
						newValue.y = newValue.x;
					}
					else
					{
						newValue.y = originalValue.y * difference;
					}

					if (scaleProperty.vector3Value[2] == 0.0f)
					{
						newValue.z = newValue.x;
					}
					else
					{
						newValue.z = originalValue.z * difference;
					}

					scaleProperty.vector3Value = newValue;
				}
			}
		}

		GUI.color = buttonColorRound;
		if (GUILayout.Button(new GUIContent(BUTTON_TEXTURE_ROUND, "Round scale"), GUIStyle.none, GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT))) //Scale Rounding Button Handler
		{
			roundValues(scaleProperty);
		}

		GUILayout.Space(2);

		GUI.color = buttonColorReset;
		if (GUILayout.Button(new GUIContent(BUTTON_TEXTURE_RESET, "Reset scale"), GUIStyle.none, GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT))) //Scale Reset Button Handler
		{
			resetValues(scaleProperty);
		}

		GUILayout.Space(2);

		GUIContent scaleButtons;

		if (scaleProportionLocked)
		{
			scaleButtons = new GUIContent(BUTTON_TEXTURE_LOCK, "locked");
			GUI.color = buttonColorLock;
		}
		else
		{
			scaleButtons = new GUIContent(BUTTON_TEXTURE_UNLOCK, "unlocked");
			GUI.color = buttonColorUnlock;
		}

		if (GUILayout.Button(scaleButtons, GUIStyle.none, GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
		{
			scaleProportionLocked = !scaleProportionLocked;
			EditorPrefs.SetBool("CustomTransformInspector_ScaleProportionLocked", scaleProportionLocked);
		}

		GUI.color = Color.white;
		GUILayout.EndHorizontal();
		GUILayout.Space(LINE_SPACER);

#endregion

		drawUILine(Color.grey);

#region Prefab Wizard Controls
		if (validRootGO)
		{
			rootGO = (GameObject)EditorUtility.InstanceIDToObject(rootGOID);

			EditorGUILayout.ObjectField("Prefab Root:", rootGO, typeof(GameObject), true); //show the game object

			rootGOHasOverrides = PrefabUtility.HasPrefabInstanceAnyOverrides(rootGO, false); //Does the root prefab have any overrides?

			const string openNewInstanceButtonText = "Open New Instance of Prefab";
			const string applyAnywaysButtonText = "Apply Anyways";
			const string unpackButtonText = "Unpack Prefab";

			if (rootGOHasOverrides) //Selected root prefab has overrides...
			{
				EditorGUILayout.HelpBox("Root prefab has overrides applied. Applying changes to this prefab may result in other instances of it breaking depending on what changes have been made.", MessageType.Warning);

				GUI.color = colorWarning;
				if(GUILayout.Button(openNewInstanceButtonText))
				{
					openNewInstanceOfPrefab(rootGO, false);
				}

				GUI.color = colorBad;
				if (GUILayout.Button(applyAnywaysButtonText))
				{
					PrefabUtility.ApplyPrefabInstance(rootGO, InteractionMode.UserAction);
				}
			}
			else //Selected root prefab has no overrides
			{
				EditorGUILayout.HelpBox("No changes to apply to prefab", MessageType.Info);

				if (EditorGUI.actionKey) //if the user is holding down CTRL or CMD, show the alternate button to allow instancing the currently selected prefab
				{
					GUI.color = colorWarning;

					if (GUILayout.Button(openNewInstanceButtonText))
					{
						openNewInstanceOfPrefab((GameObject)EditorUtility.InstanceIDToObject(immediateRootGOID), true);
					}

					Repaint();
				}

				GUI.color = colorGood;

				if (GUILayout.Button(unpackButtonText))
				{
					PrefabUtility.UnpackPrefabInstance(rootGO, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
				}
			}

			GUI.color = Color.white;
			drawUILine(Color.grey);
		}
#endregion

#region Additional Tools Foldout Controls
		showAdditionalTools = EditorGUILayout.Foldout(showAdditionalTools, "Additional Tools");

		if (showAdditionalTools)
		{
			//Sets the player pref to remember user setting
			EditorPrefs.SetBool("CustomTransformInspector_ShowAdditionalToolsToggle", showAdditionalTools);

			//Copy paste transform tools toggle
			GUI.color = showCopyPasteTools ? Color.green : Color.white;
			
			if (GUILayout.Button(new GUIContent("Copy/Paste Tools", "")))
			{
				showCopyPasteTools = !showCopyPasteTools;
				EditorPrefs.SetBool("CustomTransformInspector_ShowCopyPasteToolsToggle", showCopyPasteTools);
			}

			if (showCopyPasteTools)
			{
				GUI.color = Color.white;
				GUILayout.BeginVertical("Select properties to copy or paste:", EditorStyles.helpBox);
				GUILayout.Space(GROUP_SPACING);

				GUILayout.BeginHorizontal();

				//Copy paste transform buttons
				GUI.color = positionToggle ? (Color.cyan) : (Color.white);
				positionToggle = GUILayout.Toggle(positionToggle, "Position");

				GUI.color = rotationToggle ? (Color.cyan) : (Color.white);
				rotationToggle = GUILayout.Toggle(rotationToggle, "Rotation");

				GUI.color = scaleToggle ? (Color.cyan) : (Color.white);
				scaleToggle = GUILayout.Toggle(scaleToggle, "Scale");

				GUI.color = Color.white;

				EditorPrefs.SetBool("CustomTransformInspector_CopyPasteToolsToggle_Position", positionToggle);
				EditorPrefs.SetBool("CustomTransformInspector_CopyPasteToolsToggle_Rotation", rotationToggle);
				EditorPrefs.SetBool("CustomTransformInspector_CopyPasteToolsToggle_Scale", scaleToggle);

				Transform t = (Transform)target;

				if (positionToggle || rotationToggle || scaleToggle)
				{
					if (Selection.objects.Length > 1)
					{
						GUI.enabled = false;
					}
					else
					{
						GUI.enabled = true;
					}
				}
				else
				{
					GUI.enabled = false; ;
				}

				if (GUILayout.Button(new GUIContent("Copy", "Copy the selected transform value(s)")))
				{
					if (positionToggle)
					{
						//Copy the position values
						EditorPrefs.SetFloat("LocalPosX", t.localPosition.x);
						EditorPrefs.SetFloat("LocalPosY", t.localPosition.y);
						EditorPrefs.SetFloat("LocalPosZ", t.localPosition.z);
					}

					if (rotationToggle)
					{
						//Copy the rotation values
						EditorPrefs.SetFloat("LocalRotX", t.localEulerAngles.x);
						EditorPrefs.SetFloat("LocalRotY", t.localEulerAngles.y);
						EditorPrefs.SetFloat("LocalRotZ", t.localEulerAngles.z);
					}

					if (scaleToggle)
					{
						//Copy the scale values
						EditorPrefs.SetFloat("LocalScaleX", t.localScale.x);
						EditorPrefs.SetFloat("LocalScaleY", t.localScale.y);
						EditorPrefs.SetFloat("LocalScaleZ", t.localScale.z);
					}
				}

				GUI.enabled = positionToggle || rotationToggle || scaleToggle ? true : false;

				if (GUILayout.Button(new GUIContent("Paste", "Paste the selected transform value(s)")))
				{
					Vector3 pasteVector3 = new Vector3();

					if (positionToggle)
					{
						positionProperty.vector3Value = tempVector; //Set it to the random tempVector first

						//Then adjust the values based on the copied values
						pasteVector3.x = EditorPrefs.GetFloat("LocalPosX", 0.0f);
						pasteVector3.y = EditorPrefs.GetFloat("LocalPosY", 0.0f);
						pasteVector3.z = EditorPrefs.GetFloat("LocalPosZ", 0.0f);
						positionProperty.vector3Value = pasteVector3;
					}

					if (rotationToggle)
					{
						Quaternion quat = new Quaternion();
						Quaternion temp = new Quaternion();
						temp.x = tempVector.x;
						temp.y = tempVector.y;
						temp.z = tempVector.z;
						temp.w = tempVector.z;

						rotationProperty.quaternionValue = temp; //Set it to the random tempVector first

						//Then adjust the values based on the copied values
						pasteVector3.x = EditorPrefs.GetFloat("LocalRotX", t.localEulerAngles.x);
						pasteVector3.y = EditorPrefs.GetFloat("LocalRotY", t.localEulerAngles.y);
						pasteVector3.z = EditorPrefs.GetFloat("LocalRotZ", t.localEulerAngles.z);
						quat.eulerAngles = pasteVector3;
						rotationProperty.quaternionValue = quat;
					}

					if (scaleToggle)
					{
						scaleProperty.vector3Value = tempVector; //Set it to the random tempVector first

						//Then adjust the values based on the copied values
						pasteVector3.x = EditorPrefs.GetFloat("LocalScaleX", 1.0f);
						pasteVector3.y = EditorPrefs.GetFloat("LocalScaleY", 1.0f);
						pasteVector3.z = EditorPrefs.GetFloat("LocalScaleZ", 1.0f);
						scaleProperty.vector3Value = pasteVector3;
					}
				}

				GUI.enabled = true;
				GUILayout.EndHorizontal();
				GUILayout.EndVertical();

				GUILayout.BeginVertical("Copy/paste components:", EditorStyles.helpBox);
				GUILayout.Space(GROUP_SPACING);

				GUI.enabled = Selection.objects.Length > 1 ? false : true;

				if (GUILayout.Button(new GUIContent("Copy All Components", "")))
				{
					copyAllComponents(Selection.activeGameObject);
				}

				if (GUILayout.Button(new GUIContent("Paste All Components", "")))
				{
					pasteAllComponents(Selection.activeGameObject);
				}
				GUI.enabled = true;

				GUILayout.EndVertical();
				drawUILine(Color.grey);
			}

			GUI.enabled = true; //enables the UI for this panel since the above copy tools can disable it
			GUI.color = showScalingTools ? Color.green : Color.white;

			if (GUILayout.Button(new GUIContent("Scale Tools", "")))
			{
				showScalingTools = !showScalingTools;
				EditorPrefs.SetBool("CustomTransformInspector_ShowScaleToolsToggle", showScalingTools);
			}

			if (showScalingTools)
			{
				GUI.color = Color.white;
				GUILayout.BeginVertical("Create geometry:", EditorStyles.helpBox);
				GUILayout.Space(GROUP_SPACING);

				GUILayout.BeginHorizontal();

				if(GUILayout.Button(new GUIContent("Create UI Texture", "")))
				{
					GameObject obj = new GameObject();
					Transform parentObj = Selection.activeTransform;

					obj.transform.parent = parentObj;
					obj.name = "---------------------------------- REF";
					obj.transform.localPosition = new Vector3(0, 0, 1000);
					obj.transform.localScale = Vector3.one;

					obj.AddComponent<UITexture>();

					Selection.activeGameObject = obj;
				}

				if (GUILayout.Button(new GUIContent("Create Quad", "")))
				{
					GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
					Transform parentObj = Selection.activeTransform;

					obj.transform.parent = parentObj; //sets the quad as the child of the currently active object
					obj.name = "---------------------------------- REF QUAD";
					obj.transform.localPosition = new Vector3(0, 0, 1000);
					obj.transform.localScale = Vector3.one;
					MeshCollider collider = obj.GetComponent<MeshCollider>();
					DestroyImmediate(collider); //Removes the Mesh Collider component on the reference quad
					obj.GetComponent<Renderer>().material = null; //deletes the material for quicker texture mapping

					Selection.activeGameObject = obj; //sets the new quad as the active object for quicker scaling
				}

				GUILayout.EndHorizontal();

				GUILayout.EndVertical();

				string plural = Selection.objects.Length == 1 ? "object" : "objects";
				GUILayout.BeginVertical("Scale selected " + plural + ":", EditorStyles.helpBox);
				GUILayout.Space(GROUP_SPACING);

				GUILayout.BeginHorizontal();

				if (GUILayout.Button(new GUIContent("2732x1536", "")))
				{
					scaleProperty.vector3Value = new Vector3(2732, 1536, 1);
				}

				if (GUILayout.Button(new GUIContent("2732x2048", "")))
				{
					scaleProperty.vector3Value = new Vector3(2732, 2048, 1);
				}

				if (GUILayout.Button(new GUIContent("3327x1682", "")))
				{
					scaleProperty.vector3Value = new Vector3(3327, 1682, 1);
				}

				if (GUILayout.Button(new GUIContent("3327x2048", "")))
				{
					scaleProperty.vector3Value = new Vector3(3327, 2048, 1);
				}

				if (GUILayout.Button(new GUIContent("3413x2048", "")))
				{
					scaleProperty.vector3Value = new Vector3(3413, 2048, 1);
				}

				GUILayout.EndHorizontal();

				GUILayout.EndVertical();
				drawUILine(Color.grey);
			}

			//Misc Tools Toggle
			GUI.color = showMiscTools ? Color.green : Color.white;

			if (GUILayout.Button(new GUIContent("Miscellaneous Tools", "")))
			{
				showMiscTools = !showMiscTools;
				EditorPrefs.SetBool("CustomTransformInspector_ShowMiscToolsToggle", showMiscTools);
			}

			if (showMiscTools)
			{
				GUI.color = Color.white;
				GUILayout.BeginVertical("Copy prefab info to clipboard:", EditorStyles.helpBox);
				GUILayout.Space(GROUP_SPACING);

				//Get Prefab Asset Path
				if (PrefabUtility.IsPartOfAnyPrefab(Selection.activeGameObject) && Selection.objects.Length == 1)
				{
					GUI.enabled = true;
				}
				else
				{
					GUI.enabled = false;
				}

				if (GUILayout.Button(new GUIContent("Copy Prefab Asset Path to Clipboard", "")))
				{
					GameObject go = PrefabUtility.GetOutermostPrefabInstanceRoot(Selection.activeGameObject);

					CopyToClipboard(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go));
				}

				//Get Current Object Path in Prefab
				GUI.enabled = Selection.activeGameObject != null && Selection.objects.Length == 1 ? true : false;

				if (GUILayout.Button(new GUIContent("Copy Current Game Object Hierarchy Path", "")))
				{
					Transform current = Selection.activeTransform;
					string path = current.gameObject.name;

					if (PrefabUtility.IsPartOfAnyPrefab(current.gameObject)) //If it's part of a prefab
					{
						while (current.parent.gameObject != PrefabUtility.GetOutermostPrefabInstanceRoot(current.gameObject)) //loop the entire hierarchy up to the selection's outer-most root prefab
						{
							current = current.parent;
							path = current.gameObject.name + "/" + path;
						}

						path = current.parent.gameObject.name + "/" + path; //Add the prefab root game object name to the path string
					}
					else //If it's not part of a prefab
					{
						while (current.parent != null) //loop the entire hierarchy
						{
							current = current.parent;
							path = current.gameObject.name + "/" + path;
						}
					}

					CopyToClipboard(path);
				}

				GUILayout.EndVertical();

				drawUILine(Color.grey);
				GUI.enabled = true;
			}
		}
		else
		{
			EditorPrefs.SetBool("CustomTransformInspector_ShowAdditionalToolsToggle", showAdditionalTools);
		}
#endregion

#region DON'T CHANGE THIS CODE
		//DO NOT CHANGE THIS CODE, IT MAKES THE NEW TRANSFORM INSPECTOR WORK/////////////////////////////////////////////////////////////////////////////////////////////////////
		this.serializedObject.ApplyModifiedProperties();
#endregion
	}

	private void roundValues(SerializedProperty property)   //Handles rounding values to nearest decimal points (for when duplicating an object, to prevent it from moving to .99999 or .00001)
	{
		int precision = 1; //the number of decimal places to round to
		double roundX;
		double roundY;
		double roundZ;

		//Rotation isn't a Vector3, so we need to filter that out from working with the Position/Scale rounder stuff
		if (property.name == "m_LocalPosition" || property.name == "m_LocalScale")
		{
			Vector3 oldValue = property.vector3Value;
			Vector3 newValue = new Vector3();

			roundX = Math.Round(oldValue[0], precision);
			roundY = Math.Round(oldValue[1], precision);
			roundZ = Math.Round(oldValue[2], precision);

			newValue[0] = (float)roundX;
			newValue[1] = (float)roundY;
			newValue[2] = (float)roundZ;

			property.vector3Value = newValue;
		}

		if (property.name == "m_LocalRotation")
		{
			Quaternion quat = new Quaternion();
			
			Vector3 oldValue = property.quaternionValue.eulerAngles;
			Vector3 newValue = new Vector3();

			roundX = Math.Round(oldValue[0], precision);
			roundY = Math.Round(oldValue[1], precision);
			roundZ = Math.Round(oldValue[2], precision);

			newValue[0] = (float)roundX;
			newValue[1] = (float)roundY;
			newValue[2] = (float)roundZ;
			
			quat.eulerAngles = newValue;
			property.quaternionValue = quat;
		}
	}

	private void resetValues(SerializedProperty property)
	{
		// If an object has an incredibly small value between 0 and 1 (ie 5.193273e-10 or 0.00014), attempting to set them to 0 first won't work
		// (because unity thinks they're already set to 0), so I have to set them to another arbitrary value first...

		// Also, it seems like randomly trying to set a Vector3.zero or Vector3.one just flat out doesn't work for some strange reason I can't figure out.
		// Maybe because one of the selected objects is already at the correct scale that's causing it to break?
		// ...But if we assign it a temp value and then set those actual zero/one values, it works fine.

		if (property.name == "m_LocalPosition") //Position Reset
		{
			property.vector3Value = tempVector; //Set to the temp vector
			property.vector3Value = Vector3.zero; //then set to the reset value
		}

		if (property.name == "m_LocalRotation") //Rotation Reset
		{
			Quaternion tempQuat = new Quaternion();
			tempQuat.x = tempVector.x;
			tempQuat.y = tempVector.y;
			tempQuat.z = tempVector.z;
			tempQuat.w = tempVector.z;

			property.quaternionValue = tempQuat; //Set to the temp vector
			property.quaternionValue = Quaternion.identity; //then set to the reset value
		}

		if (property.name == "m_LocalScale") //Scale Reset
		{
			property.vector3Value = tempVector; //Set to temp vector
			property.vector3Value = Vector3.one; //then set to the reset value
		}
	}

	private void copyAllComponents(GameObject obj)
	{
		Component[] components = obj.GetComponents(typeof(Component));

		if (components.Length > 0)
		{
			componentList = components;
		}
	}

	private void pasteAllComponents(GameObject obj)
	{
		foreach (Component component in componentList)
		{
			UnityEditorInternal.ComponentUtility.CopyComponent(component);
			UnityEditorInternal.ComponentUtility.PasteComponentAsNew(obj);
		}
	}

	private static void drawUILine(Color color, int thickness = 1, int padding = 10)
	{
		Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
		r.height = thickness;
		r.y += padding / 2;
		r.x -= 2;
		EditorGUI.DrawRect(r, color);
	}

#region "DON'T CHANGE THIS CODE"
	//DO NOT CHANGE THIS CODE, IT MAKES THE NEW TRANSFORM INSPECTOR WORK/////////////////////////////////////////////////////////////////////////////////////////////////////
	private bool ValidatePosition(Vector3 position)
	{
		if (Mathf.Abs(position.x) > CustomTransformInspectorEditor.POSITION_MAX)
		{
			return false;
		}

		if (Mathf.Abs(position.y) > CustomTransformInspectorEditor.POSITION_MAX)
		{
			return false;
		}

		if (Mathf.Abs(position.z) > CustomTransformInspectorEditor.POSITION_MAX)
		{
			return false;
		}

		return true;
	}
		
	//DO NOT CHANGE THIS CODE, IT MAKES THE NEW TRANSFORM INSPECTOR WORK/////////////////////////////////////////////////////////////////////////////////////////////////////
	private void RotationPropertyField(SerializedProperty rotationProperty, GUIContent content)
	{
		Transform transform = (Transform)this.targets[0];
		Quaternion localRotation = transform.localRotation;
		foreach (UnityEngine.Object t in (UnityEngine.Object[])this.targets)
		{
			if (!SameRotation(localRotation, ((Transform)t).localRotation))
			{
				EditorGUI.showMixedValue = true;
				break;
			}
		}

		EditorGUI.BeginChangeCheck();

		Vector3 eulerAngles = EditorGUILayout.Vector3Field(content, localRotation.eulerAngles);

		if (EditorGUI.EndChangeCheck())
		{
			Undo.RecordObjects(this.targets, "Rotation Changed");
			foreach (UnityEngine.Object obj in this.targets)
			{
				Transform t = (Transform)obj;
				t.localEulerAngles = eulerAngles;
			}
			rotationProperty.serializedObject.SetIsDifferentCacheDirty();
		}

		EditorGUI.showMixedValue = false;
	}
	

	//DO NOT CHANGE THIS CODE, IT MAKES THE NEW TRANSFORM INSPECTOR WORK/////////////////////////////////////////////////////////////////////////////////////////////////////
	private bool SameRotation(Quaternion rot1, Quaternion rot2)
	{
		if (rot1.x != rot2.x) return false;
		if (rot1.y != rot2.y) return false;
		if (rot1.z != rot2.z) return false;
		if (rot1.w != rot2.w) return false;
		return true;
	}
#endregion

}



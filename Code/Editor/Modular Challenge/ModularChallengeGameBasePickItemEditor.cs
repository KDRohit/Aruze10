using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;


/*
 * Editor class to help with assigning large numbers of base pick values
 */
[CustomEditor(typeof(PickingGameBasePickItem), true), CanEditMultipleObjects]
public class ModularChallengeGameBasePickItemEditor : Editor
{
	[SerializeField] private string pickButtonTargetName = "pickobject";
	[SerializeField] private string colliderTargetName = "pickobject";
	[SerializeField] private bool useFirstColliderFound = true;
	[SerializeField] private bool useLastColliderFound = false;
	[SerializeField] private GameObject uiButtonTarget;
	[SerializeField] private bool targetPickItem = true;

	protected List<AutoAssignmentTargetInput> autoInputs;

	protected AnimBool showSetupHelpers;

	protected virtual void OnEnable()
	{
		showSetupHelpers = new AnimBool(false);
		showSetupHelpers.valueChanged.AddListener(Repaint);

		createAutoInputs();
	}

	// set up auto input fields for the inspector
	protected virtual void createAutoInputs()
	{
		autoInputs = new List<AutoAssignmentTargetInput>();

		AutoAssignmentTargetInput pickButtonInput = ModularChallengeGameEditorDisplayHelper.createField("PickObject", typeof(GameObject), "pickButton", "Pick Button GO");
		autoInputs.Add(pickButtonInput);

		AutoAssignmentTargetInput animatorInput = ModularChallengeGameEditorDisplayHelper.createField("PickObject", typeof(Animator), "pickAnimator", "Pick Animator");
		autoInputs.Add(animatorInput);

		AutoAssignmentTargetInput colliderInput = ModularChallengeGameEditorDisplayHelper.createField("PickObject", typeof(BoxCollider), "buttonCollider", "Pick Collider");
		colliderInput.includeAddButton = true;
		autoInputs.Add(colliderInput);

		AutoAssignmentTargetInput messageInput = ModularChallengeGameEditorDisplayHelper.createField("PickObject", typeof(UIButtonMessage), "buttonMessage", "UI Button Message");
		messageInput.includeAddButton = true;
		autoInputs.Add(messageInput);
	}



	// retrieve the attached BasePickItem
	protected PickingGameBasePickItem getBasePick()
	{
		Component self = (target as Component);
		PickingGameBasePickItem attached = self.gameObject.GetComponent<PickingGameBasePickItem>();
		return attached;

	}

	// shorthand for drawing a custom list of properties
	protected void drawPropertyList(string propertyName)
	{
		EditorGUILayout.PropertyField(serializedObject.FindProperty(propertyName), true);
	}

	// Render off generic autoInput controls
	protected virtual void renderAutoInputs()
	{
		for (int i = 0; i < autoInputs.Count; i++)
		{
			autoInputs[i] = ModularChallengeGameEditorDisplayHelper.renderField(autoInputs[i], targets);
		}

	}

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		EditorGUI.indentLevel = 1;

		EditorGUILayout.Separator();
		showSetupHelpers.target = EditorGUILayout.ToggleLeft("Show Base Setup Helpers", showSetupHelpers.target);
		EditorGUILayout.Separator();



		if (EditorGUILayout.BeginFadeGroup(showSetupHelpers.faded))
		{
			Color originalBackground = GUI.backgroundColor;

			renderAutoInputs();

			EditorGUILayout.Separator();

			// Collider
			EditorGUILayout.HelpBox("Click to create box colliders on children with target name: " + pickButtonTargetName, MessageType.Info);

			colliderTargetName = EditorGUILayout.TextField("Collider Target", colliderTargetName);
			EditorGUI.indentLevel++;
			useFirstColliderFound = EditorGUILayout.Toggle("Use First Found", useFirstColliderFound);
			useLastColliderFound = EditorGUILayout.Toggle("Use Last (Deepest) Found", useLastColliderFound);
			if (useFirstColliderFound && useLastColliderFound)
			{
				EditorGUILayout.HelpBox("Cannot be both first and last!", MessageType.Error);
			}

			EditorGUI.indentLevel--;

			// Box Colliders
			if (GUILayout.Button("Get BoxColliders from PickButton Component"))
			{
				getBoxCollidersFromButton();
			}

			// Generic component clearing button: BoxCollider
			ModularChallengeGameEditorDisplayHelper.renderClearComponentsButton<BoxCollider>(targets);

			EditorGUILayout.Separator();

			// UIButtonMessage Components
			targetPickItem = EditorGUILayout.Toggle("Target Pick Item (Self)", targetPickItem);
			if (!targetPickItem)
			{
				uiButtonTarget = (GameObject)EditorGUILayout.ObjectField("Message Target", uiButtonTarget, typeof(GameObject), false);
			}

			GUI.backgroundColor = Color.green;
			if (GUILayout.Button("Auto Create UI Button Messages (On Colliders)"))
			{
				addUiButtonMessages();
			}

			GUI.backgroundColor = originalBackground;

			// clear UIButtonMessage components
			ModularChallengeGameEditorDisplayHelper.renderClearComponentsButton<UIButtonMessage>(targets);
		}

		EditorGUILayout.EndFadeGroup();
	}


	// retrieve colliders from pick buttons already assigned
	private void getBoxCollidersFromButton()
	{
		foreach (Object currentTarget in targets)
		{
			PickingGameBasePickItem targetItem = (PickingGameBasePickItem)currentTarget;
			targetItem.buttonCollider = targetItem.gameObject.GetComponent<Collider>();
		}
	}

	// add button message components to all colliders found
	private void addUiButtonMessages()
	{
		foreach (Object currentTarget in targets)
		{
			PickingGameBasePickItem targetItem = (PickingGameBasePickItem)currentTarget;
			Collider[] colliders = targetItem.gameObject.GetComponentsInChildren<Collider>(true);
			foreach (Collider collider in colliders)
			{
				UIButtonMessage buttonMessage = collider.gameObject.AddComponent<UIButtonMessage>();
				if (targetPickItem)
				{
					buttonMessage.target = targetItem.gameObject;
				}
				else
				{
					buttonMessage.target = uiButtonTarget;
				}
				buttonMessage.functionName = "pickItemPressed";
				buttonMessage.trigger = UIButtonMessage.Trigger.OnClick;

				// set this for the base script (to avoid automatic init)
				targetItem.buttonMessage = buttonMessage;
			}
		}
	}
}

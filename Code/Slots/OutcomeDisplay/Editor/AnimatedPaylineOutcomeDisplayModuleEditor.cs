using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Text;

//
// An Editor script to beautify AnimatedPaylineOutcomeDisplayModule. The main goal
// here is help with set up in the editor giving options to create the many
// animation lists needed for every possible payline
//
// Author : Nick Saito <nsaito@zynga.com>
// June 17, 2020
//

[CustomEditor(typeof(AnimatedPaylineOutcomeDisplayModule))]
public class AnimatedPaylineOutcomeDisplayModuleEditor : Editor
{
	// toggle the templated fields showing
	private bool showTemplateFields;

	// the main object that we need to serialize
	private SerializedObject animatedPaylineOutcomeDisplayObject;

	// Serialized Properties on the SerializedObject
	private Dictionary<string, SerializedProperty> serializedProperties;

	// direct link to the module so we can update lists and objects easily
	private AnimatedPaylineOutcomeDisplayModule paylineDisplayModule;

	// toggle for help boxes
	private bool showHelp;

	public override void OnInspectorGUI()
	{
		// Initialize the custom sounds property so we can show it.
		initSerializedProperties();
		animatedPaylineOutcomeDisplayObject.Update();

		// draw guts here
		drawAnimationTemplateGUI();
		drawPaylineAnimationSettingsGUI();
		drawSpecialEffectGUI();
		drawHelpButton();

		// Save it out
		if (GUI.changed)
		{
			animatedPaylineOutcomeDisplayObject.ApplyModifiedProperties();
		}
	}

	private void initSerializedProperties()
	{
		paylineDisplayModule = target as AnimatedPaylineOutcomeDisplayModule;

		if (animatedPaylineOutcomeDisplayObject == null)
		{
			AnimatedPaylineOutcomeDisplayModule animatedPaylineOutcomeDisplayModule = target as AnimatedPaylineOutcomeDisplayModule;
			animatedPaylineOutcomeDisplayObject = new SerializedObject(animatedPaylineOutcomeDisplayModule);
			serializedProperties = new Dictionary<string, SerializedProperty>();

			SerializedProperty serializedProperty = serializedObject.GetIterator();
			serializedProperty.Reset();

			do
			{
				if (!serializedProperties.ContainsKey(serializedProperty.name))
				{
					serializedProperties.Add(serializedProperty.name, animatedPaylineOutcomeDisplayObject.FindProperty(serializedProperty.name));
				}
			} while (serializedProperty.Next(true));

			serializedProperty.Reset();
		}
	}

	private void drawAnimationTemplateGUI()
	{
		drawHeader("Payline Automated Generation");
		drawHelp(@"Animation Templates used to generate animation lists for the paylines
Acquired Delay : Adds a cumulative delay to each animation.
Loop Animation Template : Template for the looped payline animation that plays when looping through the outcomes or playing the cascade
End Animation Template : Template for payline end animation when paylines are turned off");

		showTemplateFields = EditorGUILayout.Foldout(showTemplateFields, "Animation Templates");
		if (showTemplateFields)
		{
			serializedProperties["numberOfReels"].intValue = EditorGUILayout.IntField("Number Of Reels", serializedProperties["numberOfReels"].intValue);
			serializedProperties["numberOfSymbolsPerReel"].intValue = EditorGUILayout.IntField("Number Of Symbols Per Reel", serializedProperties["numberOfSymbolsPerReel"].intValue);
			serializedProperties["acquiredDelay"].floatValue = EditorGUILayout.FloatField("Acquired Delay", serializedProperties["acquiredDelay"].floatValue);

			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(serializedProperties["acquiredAnimationTemplate"], true, null);
			EditorGUILayout.PropertyField(serializedProperties["loopAnimationTemplate"], true, null);
			EditorGUILayout.PropertyField(serializedProperties["endAnimationTemplate"], true, null);
			EditorGUI.indentLevel--;

			drawHelp(@"Creates all the combinations of paylines and populates the animation lists with the templates specified above. Pressing this overwrites all the paylines and creates them anew.");
			if (GUILayout.Button("Create Animation Lists"))
			{
				createPaylineAnimationDataList();
				EditorUtility.SetDirty(target);
			}

			drawHelp(@"Updates the animation for each animator using the information in the templates above.");
			if (GUILayout.Button("Update Animation States"))
			{
				updatePaylineAnimationStates();
				EditorUtility.SetDirty(target);
			}

			drawHelp(@"Updates the sounds for each animation using the information in the templates above.");
			if (GUILayout.Button("Update Sounds"))
			{
				updatePaylineAudio();
				EditorUtility.SetDirty(target);
			}
		}
	}

	private void createPaylineAnimationDataList()
	{
		paylineDisplayModule.paylineAnimationDataList = new List<AnimatedPaylineOutcomeDisplayModule.PaylineAnimationData>();
		addPaylineAnimationData(0, new List<int>());
	}

	private void addPaylineAnimationData(int reelIndex, List<int> positions)
	{
		if (reelIndex < serializedProperties["numberOfReels"].intValue)
		{
			for (int i = 0; i < serializedProperties["numberOfSymbolsPerReel"].intValue; i++)
			{
				List<int> newPositions = new List<int>(positions);
				newPositions.Add(i);
				addPaylineAnimationData(reelIndex + 1, newPositions);
			}
		}
		else
		{
			AnimatedPaylineOutcomeDisplayModule.PaylineAnimationData paylineAnimationData = new AnimatedPaylineOutcomeDisplayModule.PaylineAnimationData();
			paylineAnimationData.paylineName = getPaylineKey(positions);
			paylineAnimationData.positions = positions;

			paylineAnimationData.acquiredAnimation = new AnimationListController.AnimationInformationList();
			paylineAnimationData.loopAnimation = new AnimationListController.AnimationInformationList();
			paylineAnimationData.endAnimation = new AnimationListController.AnimationInformationList();

			createPaylineWithPositions(paylineDisplayModule.acquiredAnimationTemplate, paylineAnimationData.acquiredAnimation, positions, serializedProperties["acquiredDelay"].floatValue);
			createPaylineWithPositions(paylineDisplayModule.loopAnimationTemplate, paylineAnimationData.loopAnimation, positions, 0f);
			createPaylineWithPositions(paylineDisplayModule.endAnimationTemplate, paylineAnimationData.endAnimation, positions, 0f);

			paylineDisplayModule.paylineAnimationDataList.Add(paylineAnimationData);
		}
	}

	private void createPaylineWithPositions(AnimationListController.AnimationInformation templateAnimationInformation, AnimationListController.AnimationInformationList animationInformationList, List<int> positions, float delay)
	{
		int reelIndex = 0;
		float currentDelay = 0f;
		foreach (int position in positions)
		{
			int endPosition = 0;

			if (reelIndex + 1 < positions.Count)
			{
				endPosition = positions[(reelIndex + 1)];
			}

			AnimatedPaylineOutcomeDisplayModule.PaylineReelPositionAnimationOverride paylineReelPositionAnimationOverride = getPaylineReelPositionAnimationOverride(reelIndex, position);
			AnimatedPaylineOutcomeDisplayModule.PaylineConnectorAnimationOverride paylineConnectorAnimationOverride = getPaylineConnectorAnimationOverride(reelIndex, position, endPosition);

			AnimationListController.AnimationInformation newAnimationInformation = templateAnimationInformation.clone();
			newAnimationInformation.targetAnimator = paylineReelPositionAnimationOverride.targetAnimator;
			newAnimationInformation.delay = currentDelay;
			animationInformationList.animInfoList.Add(newAnimationInformation);

			if (paylineConnectorAnimationOverride != null)
			{
				currentDelay += delay;
				AnimationListController.AnimationInformation newConnectorAnimationInformation = templateAnimationInformation.clone();
				newConnectorAnimationInformation.targetAnimator = paylineConnectorAnimationOverride.targetAnimator;
				newConnectorAnimationInformation.delay = currentDelay;
				animationInformationList.animInfoList.Add(newConnectorAnimationInformation);

			}

			reelIndex++;
			currentDelay += delay;
		}
	}

	// Find the connector animation overrides for this position
	private AnimatedPaylineOutcomeDisplayModule.PaylineConnectorAnimationOverride getPaylineConnectorAnimationOverride(int reelIndex, int symbolStartPosition, int symbolEndPosition)
	{
		if (paylineDisplayModule.paylineConnectorAnimationOverrides == null)
		{
			return null;
		}

		foreach (AnimatedPaylineOutcomeDisplayModule.PaylineConnectorAnimationOverride connectorOverride in paylineDisplayModule.paylineConnectorAnimationOverrides)
		{
			if (connectorOverride.reelIndex == reelIndex && connectorOverride.position == symbolStartPosition && connectorOverride.endPosition == symbolEndPosition)
			{
				return connectorOverride;
			}
		}

		return null;
	}

	// Find the animation overrides for this position
	private AnimatedPaylineOutcomeDisplayModule.PaylineReelPositionAnimationOverride getPaylineReelPositionAnimationOverride(int reelIndex, int symbolStartPosition)
	{
		foreach (AnimatedPaylineOutcomeDisplayModule.PaylineReelPositionAnimationOverride animationOverride in paylineDisplayModule.paylineReelPositionAnimationOverrides)
		{
			if (animationOverride.reelIndex == reelIndex && animationOverride.position == symbolStartPosition)
			{
				return animationOverride;
			}
		}

		return null;
	}

	private void updatePaylineAnimationStates()
	{
		foreach (AnimatedPaylineOutcomeDisplayModule.PaylineAnimationData paylineAnimationData in paylineDisplayModule.paylineAnimationDataList)
		{
			updateAnimationStates(paylineAnimationData.acquiredAnimation, paylineDisplayModule.acquiredAnimationTemplate);
			updateAnimationStates(paylineAnimationData.loopAnimation, paylineDisplayModule.loopAnimationTemplate);
			updateAnimationStates(paylineAnimationData.endAnimation, paylineDisplayModule.endAnimationTemplate);
		}
	}

	private void updateAnimationStates(AnimationListController.AnimationInformationList toAnimationInformationList, AnimationListController.AnimationInformation fromAnimationInformation)
	{
		foreach (AnimationListController.AnimationInformation animationInformation in toAnimationInformationList.animInfoList)
		{
			animationInformation.ANIMATION_NAME = fromAnimationInformation.ANIMATION_NAME;
		}
	}

	private void updatePaylineAudio()
	{
		foreach (AnimatedPaylineOutcomeDisplayModule.PaylineAnimationData paylineAnimationData in paylineDisplayModule.paylineAnimationDataList)
		{
			updateAnimationAudio(paylineAnimationData.acquiredAnimation, paylineDisplayModule.acquiredAnimationTemplate);
			updateAnimationAudio(paylineAnimationData.loopAnimation, paylineDisplayModule.loopAnimationTemplate);
			updateAnimationAudio(paylineAnimationData.endAnimation, paylineDisplayModule.endAnimationTemplate);
		}
	}

	private void updateAnimationAudio(AnimationListController.AnimationInformationList toAnimationInformationList, AnimationListController.AnimationInformation fromAnimationInformation)
	{
		foreach (AnimationListController.AnimationInformation animationInformation in toAnimationInformationList.animInfoList)
		{
			animationInformation.soundsPlayedDuringAnimation = new AudioListController.AudioInformationList();

			foreach (AudioListController.AudioInformation audioInformation in fromAnimationInformation.soundsPlayedDuringAnimation.audioInfoList)
			{
				animationInformation.soundsPlayedDuringAnimation.audioInfoList.Add(audioInformation.clone());
			}
		}
	}

	private void drawPaylineAnimationSettingsGUI()
	{
		drawHeader("Payline Animation Settings");
		drawHelp(@"Payline Animation Data List : Paylines generated in the editor
Always Draw Animated Paylines : Draw animated paylines for all paylines
Draw Animated Paylines For Multipliers : Draw animated paylines when they have a multiplier
Is Drawing Cascade : Cascade shows all the paylines first before looping through them one at a time
Payline Reel Position Animation Overrides : Animators for paylines that appear over each symbol
Payline Connector Animation Overrides : Animators that connect the paylines together from each position
Clear Animations : Use this to return any animations to their default state before starting the next spin
Show Paylines After Bet Selector Delay : Adds a delay before showing the paylines again after the bet selector animates out.");

		EditorGUILayout.PropertyField(serializedProperties["paylineAnimationDataList"], true, null);
		serializedProperties["alwaysDrawAnimatedPaylines"].boolValue = EditorGUILayout.Toggle("Always Draw Animated Paylines", serializedProperties["alwaysDrawAnimatedPaylines"].boolValue);
		serializedProperties["drawAnimatedPaylinesForMultipliers"].boolValue = EditorGUILayout.Toggle("Draw Animated Paylines For Multipliers", serializedProperties["drawAnimatedPaylinesForMultipliers"].boolValue);
		serializedProperties["isDrawingCascade"].boolValue = EditorGUILayout.Toggle("Is Drawing Cascade", serializedProperties["isDrawingCascade"].boolValue);
		EditorGUILayout.PropertyField(serializedProperties["paylineReelPositionAnimationOverrides"], true, null);
		EditorGUILayout.PropertyField(serializedProperties["paylineConnectorAnimationOverrides"], true, null);
		EditorGUILayout.PropertyField(serializedProperties["clearAnimations"], true, null);
		serializedProperties["showPaylinesAfterBetSelectorDelay"].floatValue = EditorGUILayout.FloatField("Show Paylines After Bet Selector Delay", serializedProperties["showPaylinesAfterBetSelectorDelay"].floatValue);
	}

	private void drawSpecialEffectGUI()
	{
		drawHeader("Special Effect Animations");
		drawHelp(@"Always Play Special Effect Animation : Plays the special effect animation for every payline
Play Special Effect Animation For Multipliers : Plays the special effect animation when the payline has a multiplier
Play Special Effect Animation Once : Plays the special effect animation once when triggered on a reel stop
Special Effect Animation : The animated special effect
Multiplier Label : label to set when the payline has a multiplier");

		serializedProperties["alwaysPlaySpecialEffectAnimation"].boolValue = EditorGUILayout.Toggle("Always Play Special Effect Animation", serializedProperties["alwaysPlaySpecialEffectAnimation"].boolValue);
		serializedProperties["playSpecialEffectAnimationForMultipliers"].boolValue = EditorGUILayout.Toggle("Play Special Effect Animation For Multipliers", serializedProperties["playSpecialEffectAnimationForMultipliers"].boolValue);
		serializedProperties["playSpecialEffectAnimationOnce"].boolValue = EditorGUILayout.Toggle("Play Special Effect Animation Once", serializedProperties["playSpecialEffectAnimationOnce"].boolValue);
		EditorGUILayout.PropertyField(serializedProperties["specialEffectAnimation"], true, null);
		EditorGUILayout.PropertyField(serializedProperties["multiplierLabel"], true, null);
	}

	private void drawHelpButton()
	{
		string helpText = showHelp ? "Hide Help" : "Show Help";
		if (GUILayout.Button(helpText))
		{
			showHelp = !showHelp;
		}
	}

	private void drawHelp(string helpMessage)
	{
		if (showHelp)
		{
			EditorGUILayout.HelpBox(helpMessage, MessageType.Info);
		}
	}

	private void drawHeader(string text)
	{
		EditorGUILayout.LabelField("");
		EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
	}

	private string getPaylineKey(List<int> positions)
	{
		StringBuilder paylineKey = new StringBuilder();
		paylineKey.AppendFormat("{0}x{1}", serializedProperties["numberOfReels"].intValue, serializedProperties["numberOfSymbolsPerReel"].intValue);
		foreach (int i in positions)
		{
			paylineKey.AppendFormat("_{0}", i);
		}

		return paylineKey.ToString();
	}
}

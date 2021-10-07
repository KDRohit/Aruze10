using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.AnimatedValues;
using System;

// An Editor script to beautify AnimatedParticleEffect. The main goal
// here is help with set up in the editor by hiding unused options, and
// auto selecting cameras when using spin panel elements. We also have
// a lot of built in help here.
//
// Author : Nick Saito <nsaito@zynga.com>
// Sept 16, 2018
//
[CustomEditor(typeof(AnimatedParticleEffect))]
public class AnimatedParticleEffectEditor : CustomEditorBase<AnimatedParticleEffect>
{
	private AnimBool showTranslateFields;
	private AnimBool showStartPositionFields;   // only show the start position fields if there is a particle effect
	private AnimBool showEndPositionFields;     // only show the end position field if translation is enabled
	private AnimBool showParticleCameraFields;
	private AnimBool showStartCameraFields;
	private AnimBool showEndCameraFields;
	private AnimBool showChainedEffectFields;
	private AnimBool showOverrideScaleFields;
	private AnimBool showUIStartObjects;

	void OnEnable()
	{
		showTranslateFields = new AnimBool(true);
		showTranslateFields.valueChanged.AddListener(new UnityAction(base.Repaint));
		showOverrideScaleFields = new AnimBool(true);
		showOverrideScaleFields.valueChanged.AddListener(new UnityAction(base.Repaint));
		showUIStartObjects = new AnimBool(true);
		showUIStartObjects.valueChanged.AddListener(new UnityAction(base.Repaint));
		showStartPositionFields = new AnimBool(true);
		showStartPositionFields.valueChanged.AddListener(new UnityAction(base.Repaint));
		showEndPositionFields = new AnimBool(true);
		showEndPositionFields.valueChanged.AddListener(new UnityAction(base.Repaint));
		showParticleCameraFields = new AnimBool(true);
		showParticleCameraFields.valueChanged.AddListener(new UnityAction(base.Repaint));
		showStartCameraFields = new AnimBool(true);
		showStartCameraFields.valueChanged.AddListener(new UnityAction(base.Repaint));
		showEndCameraFields = new AnimBool(true);
		showEndCameraFields.valueChanged.AddListener(new UnityAction(base.Repaint));
		showChainedEffectFields = new AnimBool(true);
		showChainedEffectFields.valueChanged.AddListener(new UnityAction(base.Repaint));
	}

	protected override void drawGUIGuts()
	{
		// Draw the editor
		drawParticleEffectPrefabGUI();
		if (targetInstance.particleEffectPrefab != null)
		{
			drawParticleEffectGUI();
			drawScaleOverrideGUI();
			drawTranslateGUI();
			drawParticleStartPositionsGUI();
			drawParticleEndPositionsGUI();
			drawParticleInbetweenPositionsGUI();
			drawCameraSettingsGUI();
			drawAudioGUI();
			drawChainedParticleEffectGUI();
			drawEventGUI();
		}
	}

	private void drawEventGUI()
	{
		drawHeader("Events");
		drawHelp("We send out events when that can be used to react or update the interface");
		drawProperty("particleEffectStartedEvent");
		drawProperty("particleEffectCompleteEvent");
	}

	private void drawChainedParticleEffectGUI()
	{
		drawHeader("Chained Particle Effect");
		drawHelp(@"Trigger an another particle effect off this one. Good for meter bursts or arrive effects.
Use Final Position to Start : Use the final position of this particle for the start position of the chained particle effects.
Play Chained Effect After : Play chained effects after this particle has completed it animations.
Wait For Chained Effect Complete : Block complete event until chained effects are also completed.
");
		drawProperty("chainedParticleEffects");
		showParticleCameraFields.value = targetInstance.chainedParticleEffects != null && targetInstance.chainedParticleEffects.Count > 0;

		using (var group = new EditorGUILayout.FadeGroupScope(showParticleCameraFields.faded))
		{
			if (group.visible)
			{
				drawBool("useEndPositionForChainedEffectStartPosition", "Use Final Position to Start");
				drawBool("playChainedEffectsLast", "Play Chained Effect After");
				drawBool("waitForChainedEffectsToComplete", "Wait For Chained Effect Complete");
			}
		}
	}

	private void drawParticleEffectPrefabGUI()
	{
		drawHeader("Particle Settings");
		drawObject<GameObject>("particleEffectPrefab", "Particle Effect Prefab");

		if (targetInstance.particleEffectPrefab == null)
		{
			EditorGUILayout.HelpBox("You must assign the prefab that needs to be animated", MessageType.Warning);
		}
	}

	// Audio GUI
	private void drawAudioGUI()
	{
		enableHelp = true;
		drawHeader("Audio");
		drawHelp("Add sounds to play during the animation");
		drawProperty("particleEffectSounds");
	}

	//Camera GUI
	private void drawCameraSettingsGUI()
	{
		drawHeader("Cameras");
		drawHelp(@"Camera's are used to calculate particleEffect positions
- Particle Effect Camera renders the particleEffect
- Start Position Camera calculates position from start transform
- End Position Camera calculates position at end transform
- Select 'Use UI Camera' to use default NGUI camera");

		// Particle Effect Camera
		showParticleCameraFields.value = !serializedProperties["useUICameraForParticleEffect"].boolValue;
		using (var group = new EditorGUILayout.FadeGroupScope(showParticleCameraFields.faded))
		{
			if (group.visible)
			{
				drawObject<Camera>("particleEffectCamera", "Particle Effect Camera");
			}
		}

		if (serializedProperties["particleEffectCamera"].objectReferenceValue == null)
		{
			drawBool("useUICameraForParticleEffect", "Use UI Camera For Particle Effect");
		}

		// Start Position Camera
		showStartCameraFields.value = !serializedProperties["useUICameraForStartPosition"].boolValue;
		using (var group = new EditorGUILayout.FadeGroupScope(showStartCameraFields.faded))
		{
			if (group.visible)
			{
				drawObject<Camera>("startPositionCamera", "Start Position Camera");
			}
		}

		if (serializedProperties["startPositionCamera"].objectReferenceValue == null)
		{
			drawBool("useUICameraForStartPosition", "Use UI Camera As Start Position");
		}

		// End Position Camera
		if (serializedProperties["enableTranslation"].boolValue)
		{
			showEndCameraFields.value = !serializedProperties["useUICameraForEndPosition"].boolValue;
			using (var group = new EditorGUILayout.FadeGroupScope(showEndCameraFields.faded))
			{
				if (group.visible)
				{
					drawObject<Camera>("endPositionCamera", "End Position Camera");
				}
			}

			if (serializedProperties["endPositionCamera"].objectReferenceValue == null)
			{
				drawBool("useUICameraForEndPosition", "Use UI Camera For End Position");
			}

			drawBool("useStartTransformCameraForInbetween", "Use Start Camera For Inbetween Objects");
		}
	}

	// Start Position
	private void drawParticleStartPositionsGUI()
	{
		drawHeader("Start Position Settings");
		drawHelp(@"Particle Effect will be spawned at this position. The Start Position Camera should be the one that renders this GameObject");
		showStartPositionFields.value = !serializedProperties["useUIObjectAsStartPosition"].boolValue;
		using (var group = new EditorGUILayout.FadeGroupScope(showStartPositionFields.faded))
		{
			if (group.visible)
			{
				drawObject<Transform>("translateStartTransform", "Start Transform");
			}
		}

		if (serializedProperties["translateStartTransform"].objectReferenceValue == null)
		{
			drawBool("useUIObjectAsStartPosition", "Use UI Object as Start Position");

			if (serializedProperties["useUIObjectAsStartPosition"].boolValue)
			{
				serializedProperties["useUICameraForStartPosition"].boolValue = true;
				serializedProperties["uiStartPosition"].enumValueIndex = (int)(AnimatedParticleEffect.UIObjectPosition)EditorGUILayout.EnumPopup("UI Start Position", (AnimatedParticleEffect.UIObjectPosition) serializedProperties["uiStartPosition"].enumValueIndex);
			}
		}
	}

	// End Position
	private void drawParticleEndPositionsGUI()
	{
		using (var group = new EditorGUILayout.FadeGroupScope(showTranslateFields.faded))
		{
			if (group.visible)
			{
				drawHeader("End Position Settings");
				drawHelp(@"Particle Effect will be animated to this position. The End Position Camera should be the one that renders this GameObject");
				showEndPositionFields.value = !serializedProperties["useUIObjectAsEndPosition"].boolValue;
				using (var endGroup = new EditorGUILayout.FadeGroupScope(showEndPositionFields.faded))
				{
					if (endGroup.visible)
					{
						drawObject<Transform>("translateEndTransform", "End Transform");
					}
				}

				if (serializedProperties["translateEndTransform"].objectReferenceValue == null)
				{
					drawBool("useUIObjectAsEndPosition", "Use UI Object as End Position");
					if (serializedProperties["useUIObjectAsEndPosition"].boolValue)
					{
						serializedProperties["useUICameraForEndPosition"].boolValue = true;
						serializedProperties["uiEndPosition"].enumValueIndex = (int) (AnimatedParticleEffect.UIObjectPosition)EditorGUILayout.EnumPopup("UI End Position", (AnimatedParticleEffect.UIObjectPosition) serializedProperties["uiEndPosition"].enumValueIndex);
					}
				}
			}
		}
	}

	private void drawScaleOverrideGUI()
	{
		drawBool("overrideStartingScale", "Override Starting Scale");
		showOverrideScaleFields.value = targetInstance.overrideStartingScale;
		using (var group = new EditorGUILayout.FadeGroupScope(showOverrideScaleFields.faded))
		{
			if (group.visible)
			{
				drawVector3("newDefaultScale", "Starting Scale");
			}
		}
	}

	private void drawParticleEffectGUI()
	{
		drawHelp(@"Basic Particle Settings
Start Delay : Wait before creating and animating the particle
Time To Destroy : Delays after all the animations are complete
Complete Event Delay : Delays until we invoke the complete event
Layered Z Offset : Add a cumulative Z offset for every particle we spawn
Loop Particle Effect : Keep playing particle effect until stop is called
Wait For Animation List : Wait for the animations to finish before proceeding
Is Blocking : Blocks the caller from proceeding until complete, this
must be enabled for any blocking to work");

		drawFloat("particleEffectStartDelay", "Start Delay");
		drawFloat("particleEffectDestroyDelay", "Destroy Delay");
		drawFloat("particleEffectCompleteEventDelay", "Complete Event Delay");
		drawFloat("layeredZOffset", "Layered Z Offset");
		drawBool("loopParticleEffect", "Loop Particle Effect");
		drawBool("waitForAnimationListComplete", "Wait For Animation List");
		drawBool("isBlocking", "Is Blocking");
	}

	private void drawTranslateGUI()
	{
		drawHeader("Translation Settings");
		drawHelp(@"Translates a particleEffect from the Start Position to the End Position
Ease Type : Animation curve for the translation
Time : how quickly to do the translation
Delay : Delay the translation
Z Position : The Z axis of the end position - we add the Layered Z offset to this every particle
Wait For Translation Complete : Block until particle arrives at end point");

		drawBool("enableTranslation", "Translate Particle");
		showTranslateFields.value = targetInstance.enableTranslation;
		using (var group = new EditorGUILayout.FadeGroupScope(showTranslateFields.faded))
		{
			if (group.visible)
			{
				serializedProperties["translateEaseType"].enumValueIndex = (int)(iTween.EaseType)EditorGUILayout.EnumPopup("Ease Type", (iTween.EaseType) serializedProperties["translateEaseType"].enumValueIndex);
				drawFloat("translateTime", "Time");
				drawFloat("translateDelay", "Delay");
				drawFloat("translateZOffset", "Z Position");
				drawBool("waitForTranslationComplete", "Wait For Translation Complete");
			}
		}
	}
	
	private void drawParticleInbetweenPositionsGUI()
	{
		using (var group = new EditorGUILayout.FadeGroupScope(showTranslateFields.faded))
		{
			if (group.visible)
			{
				drawHeader("In Between Positions");
				drawHelp(@"Particle Effect will be animated to this position. The End Position Camera should be the one that renders this GameObject");
				using (var endGroup = new EditorGUILayout.FadeGroupScope(showEndPositionFields.faded))
				{
					if (endGroup.visible)
					{
						drawProperty("inbetweenTransforms");
					}
				}
			}
		}
	}
}

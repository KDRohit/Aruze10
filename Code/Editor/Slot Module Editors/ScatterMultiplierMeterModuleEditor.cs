using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScatterMultiplierMeterModule))]
public class ScatterMultiplierMeterModuleEditor : Editor
{
	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		DrawDefaultInspector();
		
		// only allow testing animation while game is running
		if (Application.isPlaying)
		{
			ScatterMultiplierMeterModule module = (ScatterMultiplierMeterModule) target;
			if (GUILayout.Button("Test Reparent Symbols Animation"))
			{
				RoutineRunner.instance.StartCoroutine(module.debugFreespinFinalSpinAnimation());
			}

			if (GUILayout.Button("Cleanup Test Reparent Symbol Animation GameObjects"))
			{
				module.cleanupDebugFreespinFinalSpinSymbols();
			}
		}

		// Save it out
		serializedObject.ApplyModifiedProperties();
	}
}
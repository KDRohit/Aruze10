using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;

/**
Helper to quickly navigate project scenes in the editor
*/
public static class LoadScene
{
	[MenuItem("Load Scene/(All options DO NOT save first.)")]
	public static void comments()
	{
		// Just an empty option.
	}

	[MenuItem("Load Scene/Scene: Startup HIR")]
	public static void loadSceneStartupHIR()
	{
		EditorApplication.isPlaying = false;
		EditorSceneManager.OpenScene("Assets/Data/HIR/Scenes/Startup.unity");
	}
	
	[MenuItem("Load Scene/Scene: Startup Logic")]
	public static void loadSceneStartupLogic()
	{
		EditorApplication.isPlaying = false;
		EditorSceneManager.OpenScene("Assets/Data/Common/Scenes/Startup Logic.unity");
	}

	[MenuItem("Load Scene/Art/Scene: Art Setup - HIR Basegame")]
	public static void loadSceneArtSetupHIRBasegame() 
	{
		EditorApplication.isPlaying = false;
		EditorSceneManager.OpenScene("Assets/Editor/Art Setup/Scenes/Art Setup - HIR Basegame.unity");
	}

	[MenuItem("Load Scene/Art/Scene: Art Setup - HIR Freespins")]
	public static void loadSceneArtSetupHIRFreespins() 
	{
		EditorApplication.isPlaying = false;
		EditorSceneManager.OpenScene("Assets/Editor/Art Setup/Scenes/Art Setup - HIR Freespins.unity");
	}

	[MenuItem("Load Scene/Art/Scene: Art Setup - HIR Feature")]
	public static void loadSceneArtSetupHIRFeature() 
	{
		EditorApplication.isPlaying = false;
		EditorSceneManager.OpenScene("Assets/Editor/Art Setup/Scenes/Art Setup - HIR Feature.unity");
	}

	[MenuItem("Load Scene/Art/Scene: Art Setup - Picking Game")]
	public static void loadSceneArtSetupPickingGame() 
	{
		EditorApplication.isPlaying = false;
		EditorSceneManager.OpenScene("Assets/Editor/Art Setup/Scenes/Art Setup - Picking Game.unity");
	}

	[MenuItem("Load Scene/Play HIR %#r")]
	public static void playHIR()
	{
		EditorApplication.isPlaying = false;
		EditorSceneManager.OpenScene("Assets/Data/HIR/Scenes/Startup.unity");
		EditorApplication.isPlaying = true;
	}
}

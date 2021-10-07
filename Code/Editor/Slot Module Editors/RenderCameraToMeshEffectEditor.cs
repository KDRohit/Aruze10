using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RenderCameraToMeshEffect))]
public class RenderCameraToMeshModuleEditor : Editor
{
	public override void OnInspectorGUI()
	{
		EditorGUILayout.HelpBox("Setup for Mechanical Reel Game: \n" +
		                        "1. Set Source Cameras to the cameras rendering things you want deformed (eg symbols layer). The first camera should have clear flag set to Color with alpha = 0. The rest should not clear or clear by depth.\n" +
		                        "2. Set Target Mesh to the deformed mesh to render to\n" +
		                        "3. Have target mesh be a child of the reel resizer so it scales proportionally to the rest of the game assets\n" +
		                        "4. Create a new camera for rendering the mesh, be sure it only views a layer not used by any other camera and that the target mesh is on that layer.\n" +
		                        "5. Adjust this new camera's depth to layer properly with other elements in the scene\n" +
		                        "6. Adjust render texture size parameters as needed to get desired resolution\n\n"+
		                        "If you see reel sizing issues: set ReelGameBackground's target camera to something other than the source cameras\n" +
		                        "If you see missing particle effects: check the shader on the pfx, it needs to be a shader that includes alpha information to display on a RenderTexture" +
		                        "If you see lighting issues or nothing rendering: check the material and shader on the target mesh"
			, MessageType.Info);
		DrawDefaultInspector();
	}
}

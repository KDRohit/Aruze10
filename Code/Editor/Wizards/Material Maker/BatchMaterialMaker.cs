using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Author: Hans Hameline                       
/// Date: Feb 24, 2014
/// 
/// This should be helpful in the making of the game materials
/// 
/// Example:
/// 1] Import the art's textures for Oz03, which should be located at "Assets/Models/Slots/Oz03/Textures/"
/// 2] Select the Textures folder
/// 3] Click the "Get Textures" button in the wizard
/// 4] Check name and type of the material
/// 5] Click the "Make Materials" buttons
/// 6] Done 
///  
/// </summary>
public class BatchMaterialMaker : EditorWindow 
{
    private Vector2 scrollPos = Vector2.zero;   

    private string[] materialOptions = { "Reel Symbols", "Gui BG", "Reel BG", "Banner" };
    private List<MaterialMakerOption> choices = new List<MaterialMakerOption>();        

    private Texture textureForPath;
    private string path;

    //For the help box
    private string helpString = "";
    private MessageType msgType = MessageType.None;
    private List<string> alphaTextures = new List<string>();
    private List<string> missingTextures = new List<string>();

    #region Show window
    [MenuItem("Zynga/Wizards/Game Material Maker")]
    public static void ShowWindow()
    {
        BatchMaterialMaker thisWindow = (BatchMaterialMaker)EditorWindow.GetWindow(typeof(BatchMaterialMaker));
        thisWindow.titleContent = new GUIContent("Material Maker");
    }
    #endregion Show window

    void OnGUI()
    {
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Select the game's \"Textures\" folder and press \"Get Textures\"", EditorStyles.boldLabel);
        EditorGUILayout.Separator();
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Get Textures", new GUILayoutOption[] { GUILayout.Width(250) }))
        {
            //Clear the current choices list to populate with the new options
            choices.Clear();

            //Insure there is a folder selected
            if (Selection.GetFiltered(typeof(Texture), SelectionMode.DeepAssets) != null)
            {
                //Set up the path for the materials
                //Note: This was kind of hacky but it was the easiest way to find the proper path to the textures folder as 
                //      AssetDatabase.GetAssetPath(<Folder>) returns a blank string
                textureForPath = Selection.GetFiltered(typeof(Texture), SelectionMode.DeepAssets)[0] as Texture;
                path = AssetDatabase.GetAssetPath(textureForPath);
                int index = path.IndexOf("Textures/");
                path = path.Remove(index);
                path = path + "Materials/";

                //Pull textures from the selected folder
                foreach (Texture t in Selection.GetFiltered(typeof(Texture), SelectionMode.DeepAssets))
                {
                    if (t != null)
                    {
                        choices.Add(new MaterialMakerOption(t.name, t));
                    }
                }                
                msgType = MessageType.None;
            }
            else
            {
                //Update the Help box
                helpString = "Make sure you have the textures folder selected.";
                msgType = MessageType.Error;
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginVertical();
        NGUIEditorTools.DrawSeparator();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, new GUILayoutOption[] { GUILayout.MinHeight(150), GUILayout.MaxHeight(550) });
        alphaTextures.Clear();
        missingTextures.Clear();
        //Handle the choices list
        for (int i = 0; i < choices.Count; i++)
        {
            EditorGUILayout.Separator();
            EditorGUILayout.BeginHorizontal(new GUILayoutOption[] { GUILayout.Width(750) });
            //The remove button
            if (GUILayout.Button("- ", new GUILayoutOption[] { GUILayout.MaxWidth(25), GUILayout.MaxHeight(20) }))
            {
                choices.RemoveAt(i);
                break;
            }
            EditorGUILayout.LabelField(choices[i].name);
            //The type of material that needs to be made
            choices[i].type = EditorGUILayout.Popup(choices[i].type, materialOptions, new GUILayoutOption[] { GUILayout.Width(350) });
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(new GUILayoutOption[] { GUILayout.Width(750) });
            Material mat;
            string shader;
            switch (choices[i].type)
            {
                case 0://Reel symbols
                    choices[i].wasChecked = EditorGUILayout.ToggleLeft("", choices[i].wasChecked, new GUILayoutOption[] { GUILayout.Width(20) });
                    GUI.enabled = !choices[i].wasChecked;
                    //This is where we try to guess the correct name for the material, if none can be guess we default to the texture name
                    choices[i].matName = EditorGUILayout.TextField("Material Name: ", choices[i].matName, new GUILayoutOption[] { GUILayout.Width(350) });
                    GUI.enabled = true;
                    EditorGUILayout.Space();
                    choices[i].texture = EditorGUILayout.ObjectField("Texture", choices[i].texture, (typeof(Texture)), false, new GUILayoutOption[] { GUILayout.Width(350) }) as Texture;
                    //create and set the material
                    shader = "Unlit/Special HSV";                    
                    mat = new Material(ShaderCache.find(shader));
                    mat.SetTexture("_MainTex", choices[i].texture);
                    choices[i].material = mat;
                    break;
                case 1://GUI BGs
                case 3://Banners
                    choices[i].wasChecked = EditorGUILayout.ToggleLeft("", choices[i].wasChecked, new GUILayoutOption[] { GUILayout.Width(20) });
                    GUI.enabled = !choices[i].wasChecked;
                    //This is where we try to guess the correct name for the material, if none can be guess we default to the texture name
                    choices[i].matName = EditorGUILayout.TextField("Material Name: ", choices[i].matName, new GUILayoutOption[] { GUILayout.Width(350) });
                    GUI.enabled = true;
                    EditorGUILayout.Space();
                    choices[i].texture = EditorGUILayout.ObjectField("Texture", choices[i].texture, (typeof(Texture)), false, new GUILayoutOption[] { GUILayout.Width(350) }) as Texture;
                    //create and set the material
                    shader = "Unlit/GUI Texture";                    
                    mat = new Material(ShaderCache.find(shader));
                    mat.SetTexture("_MainTex", choices[i].texture);
                    choices[i].material = mat;
                    break;
                case 2://Reel BGs
                    choices[i].wasChecked = EditorGUILayout.ToggleLeft("", choices[i].wasChecked, new GUILayoutOption[] { GUILayout.Width(20) });
                    GUI.enabled = !choices[i].wasChecked;
                    //This is where we try to guess the correct name for the material, if none can be guess we default to the texture name
                    choices[i].matName = EditorGUILayout.TextField("Material Name: ", choices[i].matName, new GUILayoutOption[] { GUILayout.Width(350) });
                    GUI.enabled = true;
                    EditorGUILayout.Space();
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginVertical();
                    choices[i].texture = EditorGUILayout.ObjectField("Texture", choices[i].texture, (typeof(Texture)), false, new GUILayoutOption[] { GUILayout.Width(350) }) as Texture;
                    //This type of material needs the frame alpha
                    choices[i].alphaTexture = EditorGUILayout.ObjectField("Frame Alpha Texture", choices[i].alphaTexture, (typeof(Texture)), false, new GUILayoutOption[] { GUILayout.Width(350) }) as Texture;
                    if (choices[i].alphaTexture != null)
                    {
                        //create and set the material
                        shader = "Unlit/GUI Texture Blended";                    
                        mat = new Material(ShaderCache.find(shader));
                        mat.SetTexture("_MainTex", choices[i].texture);
                        mat.SetTexture("_BlendTex", choices[i].alphaTexture);
                        choices[i].material = mat;
                    }
                    EditorGUILayout.EndVertical();
                    break;
                default:
                    Debug.Log("Material Maker - No type found!");
                    break;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();
            
            /*
             *   This will add a list of textures that contains the word 'alpha' since wil dont need to make materials for the frame alphas
             *   It will make suggestions to the user to remove this textures, didnt want to automatically remove them incase 'alpha' is used 
             *   in the name of another texture.
             * 
             *   Twe also want to make sure no one forgot to add the alpha for the reel backgrounds blank.
            */
            if ((choices[i].matName.ToUpper().Contains("ALPHA") || (choices[i].matName.ToUpper().Contains("MASK"))) && !choices[i].wasChecked)
            {
                alphaTextures.Add(choices[i].matName);
            }


            if (choices[i].type == 2 && !choices[i].material.HasProperty("_BlendTex") && !choices[i].wasChecked)
            {
                missingTextures.Add(choices[i].matName);
            }
        }

        EditorGUILayout.EndScrollView();
        NGUIEditorTools.DrawSeparator();
        path = EditorGUILayout.TextField("Path: ", path, new GUILayoutOption[] { GUILayout.Width(650) });
        
        //If things are missing textures and not "ok'd" disable create buttons
        GUI.enabled = (missingTextures.Count == 0);
        if (GUILayout.Button("Make Materials", new GUILayoutOption[] { GUILayout.Width(250) }))
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            foreach (MaterialMakerOption mmo in choices)
            {
                AssetDatabase.CreateAsset(mmo.material, path + mmo.matName + ".mat");
            }
        }
        GUI.enabled = true;

        //The help box messages
        if (missingTextures.Count > 0)
        {
            helpString = "Missing textures must be fixed before making the materials.";
            msgType = MessageType.Error;
            foreach (string s in missingTextures)
            {
                helpString = helpString + "\n" + s + " is set to Reel Background and is missing its alpha blend texture.";
            }
        }
        else
        {
            helpString = "Remember to double check the material names, set the Material Types, and check the path.";
            msgType = MessageType.Info;
            foreach (string s in alphaTextures)
            {
                helpString = helpString + "\n" + s + " may be a frame alpha texture consider removing it from the list.";
            }
        }
        EditorGUILayout.HelpBox(helpString, msgType);

        EditorGUILayout.EndVertical(); 
    }
}

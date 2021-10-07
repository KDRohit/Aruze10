using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/*
Setup a texture or textures (for example, setup the max size, the texture importer format, and the compression quality).
Currently works for inbox icons and lobby icons.
*/

public class TextureSetup : EditorWindow
{
	private Vector2 scrollPos = new Vector2(0.0f, 0.0f);
	
	public class TextureFileInfo
	{
		public string filePath; // eg "path/name.ext"
		public string fileName; // eg "name.ext"
		public string fileExt;  // eg ".ext"

		public TextureImporter textureImporter;
		
		public string type;
		public string subtype;
		public string typeLabel;

		public TextureImportInfo curIPhoneImportInfo;
		public TextureImportInfo curAndroidImportInfo;
		
		public TextureImportInfo newIPhoneImportInfo;
		public TextureImportInfo newAndroidImportInfo;	
	}

	public struct TextureImportInfo
	{
		public int maxSize;
		public TextureImporterFormat textureImporterFormat;
		public int compressionQuality;
	}
	
	List<TextureFileInfo> fileInfos = new List<TextureFileInfo>();

	//--------------------------------------------------------------------------------------------------
	// GUI
	//--------------------------------------------------------------------------------------------------
	
	[MenuItem("Zynga/Wizards/Texture Setup")]
	static public void OpenTextureSetup()
	{
		EditorWindow.GetWindow<TextureSetup>(false, "Texture Setup", true);
	}

	public void OnFocus()
	{
		OnSelectionChange();
	}
	
	public void OnSelectionChange()
	{
		fileInfos = new List<TextureFileInfo>();
		
		foreach (Object obj in Selection.objects)
		{
			TextureFileInfo fileInfo = getTextureFileInfo(obj);

			if (fileInfo != null)
			{
				fileInfos.Add(fileInfo);
			}
		}
	}
	
	public void OnGUI()
	{
		scrollPos = GUILayout.BeginScrollView(scrollPos);

		foreach (TextureFileInfo fileInfo in fileInfos)
		{
			updateTextureInfo(fileInfo);
			printTextureInfo(fileInfo);
		}
		
		GUILayout.EndScrollView();
		
		if (GUILayout.Button("Setup Textures",GUILayout.Height(40)))
		{
			foreach (TextureFileInfo fileInfo in fileInfos)
			{
				setTextureInfo(fileInfo);
			}
		}
	}
	
	public void OnInspectorUpdate()
	{
		Repaint();
	}
	
	//--------------------------------------------------------------------------------------------------
	// Get Texture File Info
	//--------------------------------------------------------------------------------------------------
	
	public TextureFileInfo getTextureFileInfo(Object obj)
	{
		TextureFileInfo fileInfo = new TextureFileInfo();
		
		fileInfo.filePath = AssetDatabase.GetAssetPath(obj);
		fileInfo.fileName = System.IO.Path.GetFileName(fileInfo.filePath);
		fileInfo.fileExt = System.IO.Path.GetExtension(fileInfo.filePath);

		if (!isTextureImporter(fileInfo))
		{
			return null;
		}
		
		if (!isTextureType(fileInfo))
		{
			return null;
		}
		
		fileInfo.typeLabel = fileInfo.type;
		
		if (fileInfo.subtype != "")
		{
			fileInfo.typeLabel += " " + fileInfo.subtype;
		}
		
		return fileInfo;
	}	
	
	public bool isTextureImporter(TextureFileInfo fileInfo)
	{
		fileInfo.textureImporter = AssetImporter.GetAtPath(fileInfo.filePath) as TextureImporter;
		
		if (fileInfo.textureImporter == null)
		{
			return false;
		}
		
		updateTextureInfo(fileInfo);
		
		fileInfo.newIPhoneImportInfo = fileInfo.curIPhoneImportInfo;
		fileInfo.newAndroidImportInfo = fileInfo.curAndroidImportInfo;
		
		return true;
	}
	
	//--------------------------------------------------------------------------------------------------
	// Texture Types
	//--------------------------------------------------------------------------------------------------

	public bool isTextureType(TextureFileInfo fileInfo)
	{
		if (isLobbyIcon(fileInfo))
		{
			return true;
		}
		
		if (isSummaryIcon(fileInfo))
		{
			return true;
		}
		
		if (isWings(fileInfo))
		{
			return true;
		}
		
		Debug.LogError(fileInfo.fileName + " is not a recognized texture type.");
		return false;
	}

	public bool isLobbyIcon(TextureFileInfo fileInfo)
	{
		if (fileInfo.filePath.Contains("/Bundles/Images/lobby_options/") &&
		    fileInfo.fileExt == ".png")
		{
			fileInfo.type = "Lobby Icon";
			
			fileInfo.newIPhoneImportInfo.maxSize = 256;
			fileInfo.newIPhoneImportInfo.textureImporterFormat = TextureImporterFormat.PVRTC_RGB4;
			fileInfo.newIPhoneImportInfo.compressionQuality = 100;
			
			fileInfo.newAndroidImportInfo.maxSize = 256;
			fileInfo.newAndroidImportInfo.textureImporterFormat = TextureImporterFormat.ETC_RGB4;
			fileInfo.newAndroidImportInfo.compressionQuality = 50;
			
			if (fileInfo.fileName.Contains("1X2"))
			{
				fileInfo.subtype = "1X2";
				
				fileInfo.newIPhoneImportInfo.maxSize = 512;
				fileInfo.newAndroidImportInfo.maxSize = 512;			
			}
			
			return true;
		}
		
		return false;
	}

	public bool isSummaryIcon(TextureFileInfo fileInfo)
	{
		if (fileInfo.filePath.Contains("Data/Games/") && fileInfo.filePath.Contains("/Images/") &&
			fileInfo.fileName.Contains("summary_icon") &&
		    fileInfo.fileExt == ".png")
		{
			fileInfo.type = "Summary Icon";
			
			fileInfo.newIPhoneImportInfo.maxSize = 256;
			fileInfo.newIPhoneImportInfo.textureImporterFormat = TextureImporterFormat.PVRTC_RGBA4;
			fileInfo.newIPhoneImportInfo.compressionQuality = 100;
			
			fileInfo.newAndroidImportInfo.maxSize = 256;
			fileInfo.newAndroidImportInfo.textureImporterFormat = TextureImporterFormat.ETC2_RGBA8;
			fileInfo.newAndroidImportInfo.compressionQuality = 50;
			
			return true;
		}
		
		return false;
	}

	public bool isWings(TextureFileInfo fileInfo)
	{
		if (fileInfo.filePath.Contains("Data/Games/") && fileInfo.filePath.Contains("/Images/") &&
		    fileInfo.fileName.Contains("wings") &&
		    fileInfo.fileExt == ".png")
		{
			fileInfo.type = "Wings";
			
			fileInfo.newIPhoneImportInfo.maxSize = 1024;
			fileInfo.newIPhoneImportInfo.textureImporterFormat = TextureImporterFormat.PVRTC_RGB4;
			fileInfo.newIPhoneImportInfo.compressionQuality = 100;
			
			fileInfo.newAndroidImportInfo.maxSize = 1024;
			fileInfo.newAndroidImportInfo.textureImporterFormat = TextureImporterFormat.ETC_RGB4;
			fileInfo.newAndroidImportInfo.compressionQuality = 50;
			
			return true;
		}
		
		return false;
	}
	
	//--------------------------------------------------------------------------------------------------
	// Update and Print Texture Info
	//--------------------------------------------------------------------------------------------------

	public void updateTextureInfo(TextureFileInfo fileInfo)
	{
		fileInfo.textureImporter.GetPlatformTextureSettings(
			"iPhone",
			out fileInfo.curIPhoneImportInfo.maxSize,
			out fileInfo.curIPhoneImportInfo.textureImporterFormat,
			out fileInfo.curIPhoneImportInfo.compressionQuality);
			
		
		fileInfo.textureImporter.GetPlatformTextureSettings(
			"Android",
			out fileInfo.curAndroidImportInfo.maxSize,
			out fileInfo.curAndroidImportInfo.textureImporterFormat,
			out fileInfo.curAndroidImportInfo.compressionQuality);
	}
	
	public void printTextureInfo(TextureFileInfo fileInfo)
	{
		GUILayout.Label(fileInfo.typeLabel);
		GUILayout.Label(fileInfo.fileName);
		GUILayout.Label(fileInfo.filePath);

		printImportInfo("iPhone", fileInfo.curIPhoneImportInfo);
		printImportInfo("Android", fileInfo.curAndroidImportInfo);
		
		GUILayout.Label("");
	}
	
	public void printImportInfo(string platform, TextureImportInfo importInfo)
	{
		GUILayout.Label(
			platform + " " +
			"MaxSize:" + importInfo.maxSize + " " +
			"TextureFormat:" + importInfo.textureImporterFormat + " " +
			"CompressQuality: " + importInfo.compressionQuality);
	}
	
	//--------------------------------------------------------------------------------------------------
	// Set Texture Info
	//--------------------------------------------------------------------------------------------------
	
	public void setTextureInfo(TextureFileInfo fileInfo)
	{
		bool shouldImportAsset = false;
		
		if (!fileInfo.curIPhoneImportInfo.Equals(fileInfo.newIPhoneImportInfo))
		{
			CommonEditor.setTextureImporterOverrides(
				fileInfo.textureImporter,
				"iPhone",
				fileInfo.newIPhoneImportInfo.maxSize,
				fileInfo.newIPhoneImportInfo.textureImporterFormat,
				fileInfo.newIPhoneImportInfo.compressionQuality,
				false);
			
			shouldImportAsset = true;
		}
		
		if (!fileInfo.curAndroidImportInfo.Equals(fileInfo.newAndroidImportInfo))
		{
			CommonEditor.setTextureImporterOverrides(
				fileInfo.textureImporter,
				"Android",
				fileInfo.newAndroidImportInfo.maxSize,
				fileInfo.newAndroidImportInfo.textureImporterFormat,
				fileInfo.newAndroidImportInfo.compressionQuality,
				false);
			
			shouldImportAsset = true;
		}

		if (shouldImportAsset)
		{
			AssetDatabase.ImportAsset(fileInfo.filePath, ImportAssetOptions.ForceUpdate);
		}
	}
}

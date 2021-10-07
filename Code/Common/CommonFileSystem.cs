using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Class for common file system stuff.  Like creating files or handling directories
 *
 * Creation Date: 11/26/2019
 * Original Author: Scott Lepthien
 */
public class CommonFileSystem
{
	// Create a directory if one doesn't exist yet for the passed filePath
	public static void createDirectoryIfNotExisting(string filePath)
	{
		if (string.IsNullOrEmpty(filePath))
		{
			Debug.LogWarning("CommonEditor.createDirectoryIfNotExisting() - filePath was NULL or empty string!");
			return;
		}
		
		// Create the directory if it does not exist
		string directory = System.IO.Path.GetDirectoryName(filePath);
		if (!System.IO.Directory.Exists(directory))
		{
			System.IO.Directory.CreateDirectory(directory);
		}		
	}
}

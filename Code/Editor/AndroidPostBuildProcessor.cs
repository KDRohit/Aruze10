using UnityEngine;
using System.Collections;
using System.IO;
using UnityEditor.Android;

public class AndroidPostBuildProcessor : IPostGenerateGradleAndroidProject
{
	public int callbackOrder
	{
		get
		{
			return 999;
		}
	}


	void IPostGenerateGradleAndroidProject.OnPostGenerateGradleAndroidProject(string path)
	{
		Debug.Log("Bulid path : " + path);
		string gradlePropertiesFile = path + "/gradle.properties";

		if (File.Exists(gradlePropertiesFile))
		{
			File.Delete(gradlePropertiesFile);
		}

		StreamWriter writer = File.CreateText(gradlePropertiesFile);
		writer.WriteLine("android.useAndroidX=true");
		writer.WriteLine("android.enableJetifier=true");
		writer.Flush();
		writer.Close();

	}
}
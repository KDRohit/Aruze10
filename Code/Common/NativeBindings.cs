using System;
using UnityEngine;
using System.Runtime.InteropServices;


public static class NativeBindings
{

#if UNITY_IPHONE
	[DllImport ("__Internal")]
	public static extern string GetSettingsURL();

	[DllImport ("__Internal")]
	public static extern void OpenSettings();

	[DllImport("__Internal")]
	public static extern void ShareContent(string subject, string body, string imagePath, string url);

#elif ZYNGA_KINDLE
	public static void ShareContent(string subject, string body, string imagePath, string url)
	{
		imagePath = sanitizeImagePath(imagePath);

		AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
		AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent");
		
		intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
		AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
		AndroidJavaObject uriObject = uriClass.CallStatic<AndroidJavaObject>("parse", "file://" + imagePath);
		intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), uriObject);
		intentObject.Call<AndroidJavaObject>("setType", "image/png");
		
		//subject used only for email, it seems
		intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_SUBJECT"), subject);
		
		//body of message or message
		intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), body + "\n" + url);
		
		AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity");
		
		AndroidJavaObject jChooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intentObject, "Send To");
		currentActivity.Call("startActivity", jChooser);
		
	}

	public static string GetSettingsURL()
	{
		return null;
	}
	
	public static void OpenSettings()
	{
		// noop
	}
	
#elif UNITY_ANDROID
	public static void ShareContent(string subject, string body, string imagePath, string url)
	{
		imagePath = sanitizeImagePath(imagePath);

		string mimeType = determineMimeType(subject, body, imagePath, url);
		if (string.IsNullOrEmpty(mimeType) && !string.IsNullOrEmpty(imagePath))
		{
			// If we have no mime type but are trying to share an file, log a warning and default to image/png.
			Debug.LogWarningFormat("NativeBindings.cs -- ShareContent -- no mimeType while trying to share imagePath: {0}. Setting mimeType to image/png", imagePath);
			mimeType = "image/png";
		}

		AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity");

		AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
		AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent");

		intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_SUBJECT"), subject);
		intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), body + "\n" + url);
		intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
		intentObject.Call<AndroidJavaObject>("setType", mimeType);


		
		if (!string.IsNullOrEmpty(imagePath))
		{
			// MCC -- Only call this if we have an image to share.

			string contentPath = "content://" + imagePath;
			// MCC Swapping to the API 24+ way of accessing files.

			AndroidJavaObject fileObj = new AndroidJavaObject("java.io.File", imagePath);
			AndroidJavaClass fileProvider = new AndroidJavaClass("android.support.v4.content.FileProvider");
			
			AndroidJavaObject unityContext = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
			
			string packageName = unityContext.Call<string>("getPackageName");
			
			string authority = packageName + ".fileprovider";
			if (fileObj == null)
			{
				Debug.LogErrorFormat("NativeBindings.cs -- ShareContent() -- file object was null for path: {0}, breaking out.", imagePath);
				return;
			}
			
			AndroidJavaObject uriObject = fileProvider.CallStatic<AndroidJavaObject>("getUriForFile", unityContext, authority, fileObj);

			// MCC Adding the read URI permission.
			int FLAG_GRANT_READ_URI_PERMISSION = intentObject.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION");
			int FLAG_ACTIVITY_NEW_TASK = intentObject.GetStatic<int>("FLAG_ACTIVITY_NEW_TASK");

			intentObject.Call<AndroidJavaObject>("addFlags", FLAG_ACTIVITY_NEW_TASK);
			intentObject.Call<AndroidJavaObject>("addFlags", FLAG_GRANT_READ_URI_PERMISSION);
			
			intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), uriObject);
		}

		if (!string.IsNullOrEmpty(url))
		{
			// if we have a url to add, lets do that here.
			AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
			AndroidJavaObject uriObject = uriClass.CallStatic<AndroidJavaObject>("parse", url);
			intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), uriObject);
		}

		AndroidJavaObject jChooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intentObject, "Send To");
		currentActivity.Call("startActivity", jChooser);
	}

	public static string GetSettingsURL()
	{
		return null;
	}
	
	public static void OpenSettings()
	{
		// noop
	}
#else
	public static string GetSettingsURL()
	{
		return null;
	}
	
	public static void OpenSettings()
	{
		// noop
	}

	public static void ShareContent(string subject, string body, string imagePath, string url)
	{
	}
#endif

	private static string sanitizeImagePath(string imagePath)
	{
		if (string.IsNullOrEmpty(imagePath))
		{
			return imagePath;
		}
		if (!imagePath.FastStartsWith(Application.persistentDataPath))
		{
			// Make sure that the path starts with the persistent data path.
			return System.IO.Path.Combine(Application.persistentDataPath, imagePath);
		}
		else
		{
			// if it is not null, and alreayd starts with the data path, return that.
			return imagePath;
		}
	}

	private static string determineMimeType(string subject, string body, string imagePath, string url)
	{
		if (!string.IsNullOrEmpty(imagePath))
		{
			// If we have an image, try to determine the type from the path.
			if (imagePath.FastEndsWith(".jpg") || imagePath.FastEndsWith(".jpeg"))
			{
				return "image/jpeg";
			}

			if (imagePath.FastEndsWith(".gif"))
			{
				return "image/gif";
			}

			if (imagePath.FastEndsWith(".png"))
			{
				return "image/png";
			}
			else
			{
				// PNG is the most common image type we use. If the type is not found above, default
				// to PNG, and log a warning with the path.
				Debug.LogWarningFormat("NativeBindings.cs -- determineMimeType -- could not determine image type from path: {0}, defaulting to image/png", imagePath);
				return "image/png";
			}
		}
		else
		{
			// We currently only try to share images or text.
			// If this changes we will need to udpate this to support more MIME types.
			return "text/plain";
		}
	}
}


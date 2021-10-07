using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;

public class ScreenshotDialog : DialogBase
{
	public MeshRenderer screenshotRenderer;

	private Texture2D shareTexture;
	private string uploadUrl;
	
	public override void init()
	{
	    shareTexture = (Texture2D)dialogArgs.getWithDefault(D.TEXTURE, null);
	    screenshotRenderer.material.mainTexture = shareTexture;
		uploadUrl = (string)dialogArgs.getWithDefault(D.URL, "");
	}

	public override void close()
	{
		// Cleanup here.
	}

    private void closeClicked()
	{
		Dialog.close();
	}

	private void shareClicked()
	{
		// Start upload process.
		RoutineRunner.instance.StartCoroutine(uploadScreenshot(uploadUrl, shareTexture));
		Dialog.close();
	}

    private static IEnumerator loadAndPostScreenshot(string filename, string uploadUrl)
	{
		string screenshotFilename = Application.persistentDataPath + filename;
		Texture2D screenshotTexture = null;

		bool didTakeScreenshot = false;
		Debug.LogFormat("PlayerLoveWeekHighlightsDialog.cs -- loadAndPostScreenshot -- loading screenshot from filepath: {0}", screenshotFilename);
#if UNITY_EDITOR
		screenshotFilename = filename;
#endif
		WWW fileLoader = new WWW("file://" + screenshotFilename);
		yield return fileLoader;
		if (fileLoader.error == null)
		{
			Debug.LogFormat("ScreenshotDialog.cs -- loadAndPostScreenshot -- successfully loaded image from {0}", screenshotFilename);
		    screenshotTexture = fileLoader.texture;
			if (screenshotTexture != null)
			{
			    didTakeScreenshot = true;
			}
		}
		else
		{
			Debug.LogErrorFormat("ScreenshotDialog.cs -- loadAndPostScreenshot -- failed to load image from {0}", screenshotFilename);
		}

		// If we got the screenshot texture from file, then show the dialog.
		if (didTakeScreenshot)
		{
			Dict args = Dict.create(D.TEXTURE, screenshotTexture, D.URL, uploadUrl);
			Scheduler.addDialog("screenshot_dialog", args);
		}
		else
		{
			Debug.LogErrorFormat("PlayerLoveWeek.cs -- loadAndPostScreenshot -- failed to upload texture");
		}
	}

    public static IEnumerator uploadScreenshot(string url, Texture2D texture)
	{
	    byte[] postData = texture.EncodeToJPG();
		Dictionary<string, string> headers = new Dictionary<string, string>();
		headers.Add("Content-type", "application/octet-stream");
		WWW upload = new WWW(url, postData, headers);
		yield return upload;
		if (upload.error == null)
		{
			Debug.LogFormat("PlayerLoveWeek.cs -- uploadScreenshot -- successfully uploaded screenshot to {0}", url);
		}
		else
		{
			// Then we error in the upload.
			Debug.LogErrorFormat("PlayerLoveWeek.cs -- uploadScreenshot -- failed to upload image to {0}", url);
		}
	}

	public static void showDialog(string filename, string uploadUrl)
	{
		RoutineRunner.instance.StartCoroutine(loadAndPostScreenshot(filename, uploadUrl));
	}
}
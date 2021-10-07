using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

/**
This is a purely static class of useful coroutine wrappers for 
*/
public static class CommonWebRequest
{
	// Container class for downloaded texture data.
	public class TextureData
	{
		public Texture2D texture = null;
		public string textureURL = null;
		public TextureData(string textureURL)
		{
			this.textureURL = textureURL;
		}
	}
	
	// Coroutine to efficiently retrieve a Texture2D from an image URL.
	// The resulting texture is stored in a caller-provided TextureData container class.
	public static IEnumerator requestTexture(TextureData textureData, int retries = 3)
	{
		if (textureData == null)
		{
			Debug.LogError("CommonWebRequest.requestTexture() : Missing TextureData.");
		}
		else if (string.IsNullOrEmpty(textureData.textureURL))
		{
			Debug.LogError("CommonWebRequest.requestTexture() : Missing texture URL.");
		}
		else if (textureData.texture != null)
		{
			Debug.LogErrorFormat("CommonWebRequest.requestTexture() : Texture is already assigned somehow, maybe a redundant call?");
		}
		else
		{	
			// TextureData appears to check out fine, try to get the texture.
			string lastError = null;
			long lastResponseCode = -1;
			for (int attempts = 0; attempts < retries; attempts++)
			{
				
				using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(textureData.textureURL, true))
				{
					yield return RoutineRunner.instance.StartCoroutine(makeRequest(www));
	 				lastResponseCode = www.responseCode;
	 				
					if (www.isNetworkError)
					{
						lastError = www.error;
					}
					else
					{
						try
						{
							textureData.texture = DownloadHandlerTexture.GetContent(www);
						}
						catch (System.Exception e)
						{
							Debug.LogErrorFormat("CommonWebRequest.requestTexture() : {0} : URL {1}", e.Message, textureData.textureURL);
						}
					}
				}
				
				if (textureData.texture != null)
				{
					// We got the data, stop retrying.
					break;
				}
			}
			
			// Check if texture is setup correctly.
			if (textureData.texture == null)
			{
				if (string.IsNullOrEmpty(lastError))
				{
					Debug.LogErrorFormat("CommonWebRequest.requestTexture() : Server error with response code {0}. URL: '{1}'.", lastResponseCode, textureData.textureURL);
				}
				else
				{
					Debug.LogErrorFormat("CommonWebRequest.requestTexture() : {0} URL: {1}", lastError, textureData.textureURL);
				}
			}
		}
	}
	
	
	
	// Container class for downloaded JSON data.
	public class JSONData
	{
		public JSON json = null;
		public string jsonURL = null;
		public JSONData(string jsonURL)
		{
			this.jsonURL = jsonURL;
		}
	}
	
	// Coroutine to efficiently retrieve JSON text data from a URL.
	// The resulting JSON object is stored in a caller-provided JSONData container class.
	public static IEnumerator requestJSON(JSONData jsonData, int retries = 3)
	{
		if (jsonData == null)
		{
			Debug.LogError("CommonWebRequest.requestJSON() : Missing JSONData.");
		}
		else if (string.IsNullOrEmpty(jsonData.jsonURL))
		{
			Debug.LogError("CommonWebRequest.requestJSON() : Missing JSON URL.");
		}
		else if (jsonData.json != null)
		{
			Debug.LogErrorFormat("CommonWebRequest.requestJSON() : JSON is already assigned somehow, maybe a redundant call?");
		}
		else
		{	
			// JSONData appears to check out fine, try to get the texture.
			string lastError = null;
			long lastResponseCode = -1;
			for (int attempts = 0; attempts < retries; attempts++)
			{
				
				using (UnityWebRequest www = UnityWebRequest.Get(jsonData.jsonURL))
				{
					yield return RoutineRunner.instance.StartCoroutine(makeRequest(www));
	 				lastResponseCode = www.responseCode;
	 				
					if (www.isNetworkError)
					{
						lastError = www.error;
					}
					else
					{
						jsonData.json = new JSON(www.downloadHandler.text);
					}
				}
				
				if (jsonData.json != null)
				{
					// We got the data, stop retrying.
					break;
				}
			}
			
			// Check if JSON is setup correctly.
			if (jsonData.json == null)
			{
				if (string.IsNullOrEmpty(lastError))
				{
					Debug.LogErrorFormat("CommonWebRequest.requestJSON() : Server error with response code {0}. URL: '{1}'.", lastResponseCode, jsonData.jsonURL);
				}
				else
				{
					Debug.LogErrorFormat("CommonWebRequest.requestJSON() : {0} URL: {1}", lastError, jsonData.jsonURL);
				}
			}
			else if (!jsonData.json.isValid)
			{
				jsonData.json = null;
				Debug.LogErrorFormat("CommonWebRequest.requestJSON() : JSON block is cannot be parsed. URL: {0}", jsonData.jsonURL);
			}
		}
	}
	
	
	
	// Container class for downloaded JSON data.
	public class TextData
	{
		public string text = null;
		public string textURL = null;
		public TextData(string textURL)
		{
			this.textURL = textURL;
		}
	}
	
	// Coroutine to efficiently retrieve JSON text data from a URL.
	// The resulting text string object is stored in a caller-provided TextData container class.
	public static IEnumerator requestText(TextData textData, int retries = 3)
	{
		if (textData == null)
		{
			Debug.LogError("CommonWebRequest.requestText() : Missing TextData.");
		}
		else if (string.IsNullOrEmpty(textData.textURL))
		{
			Debug.LogError("CommonWebRequest.requestText() : Missing Text URL.");
		}
		else if (textData.text != null)
		{
			Debug.LogErrorFormat("CommonWebRequest.requestText() : Text is already assigned somehow, maybe a redundant call?");
		}
		else
		{
			// TextData appears to check out fine, try to get the texture.
			string lastError = null;
			long lastResponseCode = -1;
			for (int attempts = 0; attempts < retries; attempts++)
			{
				using (UnityWebRequest www = UnityWebRequest.Get(textData.textURL))
				{
					yield return RoutineRunner.instance.StartCoroutine(makeRequest(www));
	 				lastResponseCode = www.responseCode;
	 				
					if (www.isNetworkError)
					{
						lastError = www.error;
					}
					else
					{
						textData.text = www.downloadHandler.text;
					}
				}
				
				if (textData.text != null)
				{
					// We got the data, stop retrying.
					break;
				}
			}
			
			// Check if text is null, indicating a failure to retrieve it.
			if (textData.text == null)
			{
				if (string.IsNullOrEmpty(lastError))
				{
					Debug.LogErrorFormat("CommonWebRequest.requestText() : Server error with response code {0}. URL: '{1}'.", lastResponseCode, textData.textURL);
				}
				else
				{
					Debug.LogErrorFormat("CommonWebRequest.requestText() : {0} URL: {1}", lastError, textData.textURL);
				}
			}
		}
	}
	
	// This exists to cover an annoying Unity bug where Send() completes but the isDone flag isn't set.
	// Also sidesteps the fact that TICoroutine doesn't know what to do with ASyncOperation yields.
	public static IEnumerator makeRequest(UnityWebRequest www)
	{
		www.SendWebRequest();
		while (!www.isDone)
		{
			yield return null;
		}
		yield return null;
	}
	
}


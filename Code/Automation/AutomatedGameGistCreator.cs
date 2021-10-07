using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

#if ZYNGA_TRAMP || !ZYNGA_PRODUCTION
// This class allows you to create a secret or public
// Author: Leo Schnee
// Date: 5/24/2017



public class AutomatedGameGistCreator
{

	public string description = "";
	public bool isPublic = false; // Defaults to false.
	public string gistURL = null; // The location the gist endedup
	public string gistID = null;
	public bool error = false;

	public bool createdGist
	{
		get
		{
			return !string.IsNullOrEmpty(gistURL) && !string.IsNullOrEmpty(gistID);
		}
	}

	private enum GistOperation
	{
		Create,
		Get,
		Edit
	}

	private JSON resultJSON;
	private GistFileList fileList = new GistFileList();

	private class GistFileList
	{
		private List<GistFile> fileList = new List<GistFile>();

		public void addFile(string title, string content)
		{
			fileList.Add(new GistFile(title, content));
		}

		public string toJSONString()
		{
			bool first = true;
			string jsonString = "\"files\": { ";
			foreach (GistFile gistFile in fileList)
			{
				if (first)
				{
					first = false;
				}
				else
				{
					jsonString += ", ";
				}
				jsonString += gistFile.toJSONString();
			}
			jsonString += "}";
			return jsonString;
		}

		private class GistFile
		{
			private Dictionary<string, string> fileInformation = new Dictionary<string, string>();
			private string title;
			private const string CONTENT = "content"; 
			public GistFile(string title, string content)
			{
				this.title = JSON.sanitizeString(title);
				fileInformation[CONTENT] = content;
			}


			// "<title>":{ "content:" "<content>" }
			public string toJSONString()
			{
				return JSON.createJsonString(title, fileInformation);
			}
		}

	}

	public void addFile(string fileName, string contents)
	{
		fileList.addFile(fileName, contents);
	}

	private string toJSONString()
	{
		System.Text.StringBuilder builder = new System.Text.StringBuilder();

		builder.AppendLine("{");
		JSON.buildJsonStringLine(builder, 0, "description", description, true);
		JSON.buildJsonStringLine(builder, 0, "public", isPublic, true);
		builder.AppendLine(fileList.toJSONString());
		builder.AppendLine("}");

		Debug.Log(builder.ToString());
		return builder.ToString();
	}

	public IEnumerator create()
	{
		if (!createdGist)
		{
			// We only want to send this if it doens't already exist.
			string requestData = toJSONString();
			yield return sendGistPost(GistOperation.Create, requestData);
			if (this.resultJSON != null && this.error == false)
			{
				Debug.LogFormat("Created gist id {0} at {1}", gistID, gistURL);
			}
			else
			{
				Debug.LogError("Error creating gist!");
			}
		}
		else
		{
			Debug.LogWarning("Trying to create gist but it already exists");
		}
	}

	public IEnumerator populateFromGistId(string gistIDToEdit)
	{
		if (!createdGist)
		{
			gistID = gistIDToEdit; // Set gist id to populate.
			yield return sendGistPost(GistOperation.Get, null);
			if (resultJSON != null && this.error == false)
			{
				// Unpack description and file list from gist.
				description = resultJSON.getString("description", "");
				JSON fileList = resultJSON.getJSON("files");
				List<string> fileNames = fileList.getKeyList();
				foreach(var fileName in fileNames)
				{
					JSON fileObject = fileList.getJSON(fileName);
					string content = fileObject.getString("content", "");
					addFile(fileName, content);
				}
				Debug.LogFormat("Got gist id {0} at {1}: description: {2} files: {3}", gistID, gistURL, description, fileList);
			}
			else
			{
				Debug.LogErrorFormat("Error populating gist {0}!", gistIDToEdit);
			}
		}
		else
		{
			Debug.LogWarning("Trying to populate gist but it already exists");
		}
	}

	public IEnumerator editGist()
	{
		if (!createdGist)
		{
			Debug.LogError("Gist doesn't exist to edit");
			yield break;
		}

		string requestData = toJSONString();
		yield return sendGistPost(GistOperation.Edit, requestData);
		if (resultJSON != null && this.error == false)
		{
			// Unpack description and file list from gist.
			description = resultJSON.getString("description", "");
			JSON fileList = resultJSON.getJSON("files");
			Debug.LogFormat("Done editing gist id {0} at {1}: description: {2} files: {3}", gistID, gistURL, description, fileList);
		}
		else
		{
			Debug.LogError("Error editing gist!");
		}
	}

	private string authenticate(string username, string password)
	{
		string auth = username + ":" + password;
		auth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(auth));
		auth = "Basic " + auth;
		return auth;
	}

	private IEnumerator sendGistPost(GistOperation op, string data)
	{
		string baseURL = "https://github-ca.corp.zynga.com/api/v3/gists";
		WWWForm form = new WWWForm();
		Dictionary<string,string> headers = form.headers;
		headers["Content-Type"] = "application/json";
		headers["AUTHORIZATION"] = authenticate(JIRAData.USER_NAME, JIRAData.PASSWORD);

		string requestURL = baseURL;
		UnityWebRequest www = null;

		switch (op)
		{
			case GistOperation.Create:
				requestURL = baseURL;
				www = UnityWebRequest.Post(requestURL, data);
				break;
			case GistOperation.Get:
				requestURL = string.Format("{0}/{1}", baseURL, gistID);
				www = UnityWebRequest.Get(requestURL);
				break;
			case GistOperation.Edit:
				requestURL = string.Format("{0}/{1}", baseURL, gistID);
				www = UnityWebRequest.Post(requestURL, data);
				www.method = "PATCH";
				break;
			default:
				Debug.LogError("Unknown GistOperation");
				this.error = true;
				this.resultJSON = null;
				yield break;
		}

		if (www == null)
		{
			Debug.LogError("Error initializing gist operation");
			this.error = true;
			this.resultJSON = null;
			yield break;
		}

		if (data != null)
		{
			byte[] bytes;
			UploadHandlerRaw uH;
			bytes= Encoding.UTF8.GetBytes(data);
			uH = new UploadHandlerRaw(bytes);
			uH.contentType= "application/json";
			www.uploadHandler = uH;
		}

		foreach (KeyValuePair<string,string> kvp in headers)
		{
			www.SetRequestHeader(kvp.Key, kvp.Value);
		}

		Debug.LogFormat("Sending gist API request {0} at {1}", op, requestURL);
		AsyncOperation operation = www.SendWebRequest();
		float oldProgress = -1.0f;
		while (!operation.isDone)
		{
			if (operation.progress > oldProgress)
			{
				oldProgress = operation.progress;
				Debug.LogFormat("Sending GIST request: {0}% done.", (operation.progress * 100.0f));
			}
			yield return null;
		}

		Debug.Log("Sending GIST request: done.");

		string result = "";

		if (www.isNetworkError)
		{
			Debug.LogErrorFormat("WWW Error sending gist request: {0}", www.error);
			result = www.error;
		}
		else
		{
			Debug.LogFormat("Gist API result: {0}", www.downloadHandler.text);
			result = www.downloadHandler.text;
		}

		resultJSON = new JSON(result);

		gistURL = resultJSON.getString("html_url", "");
		gistID = resultJSON.getString("id", "");
		if (!createdGist)
		{
			Debug.LogErrorFormat("Error making/editing gist: result: {0}", resultJSON);
			resultJSON = null; // Something went wrong and this didn't send.
			this.error = true;
		}
	}
}

#endif

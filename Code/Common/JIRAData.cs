using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

#if ZYNGA_TRAMP || !ZYNGA_PRODUCTION
// This class holds information that should be going into a JIRA ticket.
// Author: Willy Lee based on original by Leo Schnee
// Date: 6/20/2017

public enum JIRAPriority
{
	P0 = 0,
	P1 = 1,
	P2 = 2,
	P3 = 3,
	P4 = 4,
	TBD = 5
}

public class JIRAData
{
	public const string USER_NAME = "z_HIR-TRAMP";
	public const string PASSWORD = "G19z7PfhghTirn@";

	public JSON resultJSON;
	public UnityWebRequest jiraSubmitTicketWebRequest;
	public AsyncOperation jiraSubmitOperation;

	// JIRA FIELDS:
	private const string issuetypeFIELD = "issuetype";
	private const string issuetypeKEY = "name";
	public string issueType = "Task";

	private const string projectFIELD = "project";
	private const string projectKEY = "key";
	public string project = "HIR";

	private const string summaryFIELD = "summary";
	private const string summaryKEY = null; // No Key because this is formatted differently.
	public string summary = "This is a test of TRAMP JIRA creation."; // The title that we want to have for this specific bug.

	private const string priorityFIELD = "priority";
	private const string priorityKEY = "name"; // No idea what I can even send this...
	public JIRAPriority priority = JIRAPriority.TBD; // 0-5 P0, P1, P2, P3, P4, TBD

	private const string affectsVersionFIELD = "versions";
	private const string affectsVersionKEY = null;
	public string affectsVersion = ""; // This needs to come from the start up version of TRAMP?

	private const string fixVersionFIELD = "fixVersions";
	private const string fixVersionKEY = null; 
	public string fixVersion = ""; // This needs to come from the startup verison of TRAMP?

	private const string assigneeFIELD = "assignee";
	private const string assigneeKEY = "name"; // No idea what to send this.
	public string assignee = "z_HIR-TRAMP";

	private const string descriptionFIELD = "description";
	private const string descriptionKEY = null; // No Key because this is formatted differently.
	private string _description;
	public string description
	{
		get { return _description; }
		set
		{
			if (value.Length >= JIRA_TEXT_FIELD_CHARACTER_LIMIT)
			{
				value = value.Substring(0, JIRA_TEXT_FIELD_CHARACTER_LIMIT - 1);
				Debug.LogWarningFormat("Warning : JIRA description too long! Truncated to {0} characters", JIRA_TEXT_FIELD_CHARACTER_LIMIT);
			}
			_description = value;
		}
	}

	public string notes = "";

	private const string labelsFIELD = "labels";
	private const string labelsKEY = null;
	public string labels = "TRAMP"; // Put a label on all of these so we know they're all coming from TRAMP.

	private const string JIRA_JSON_FINAL_FORMAT = @"{{ ""fields"": {{ {0} }} }}";

	private const string JIRA_JSON_FIELD_FORMAT = @" ""{0}"": {{ ""{1}"": ""{2}"" }}, ";

	private const string JIRA_JSON_STRING_FORMAT = @"""{0}"": ""{1}"", ";
	
	private const string JIRA_JSON_ARRAY_FORMAT = @"""{0}"": [""{1}""], ";

	private const string JIRA_JSON_FIX_VERSION_FORMAT = @"""{0}"": [{{""name"":""{1}""}}], ";

	private const int JIRA_TEXT_FIELD_CHARACTER_LIMIT = 32766;

	private string createJSONStringOrField(string field, string key, string value)
	{
		if (string.IsNullOrEmpty(key))
		{
			if (isFieldArrayType(field))
			{
				return string.Format(JIRA_JSON_ARRAY_FORMAT,
				field,
				value);
			}
			else if (isFixVersionType(field))
			{
				return string.Format(JIRA_JSON_FIX_VERSION_FORMAT,
				field,
				value);
			}
			else
			{
				return string.Format(JIRA_JSON_STRING_FORMAT,
					field,
					value);
			}
			// We want to make a string JSON
		}
		else
		{
			// This is a field JSON
			return string.Format(JIRA_JSON_FIELD_FORMAT,
				field,
				key,
				value);
		}
	}

	private bool isFieldArrayType(string field)
	{
		return field == labelsFIELD;
	}

	private bool isFixVersionType(string field)
	{
		return field == affectsVersionFIELD || field == fixVersionFIELD;
	}

	public virtual string createJSONStringforJIRA()
	{

		string fields = "";

		fields += createJSONStringOrField(issuetypeFIELD, issuetypeKEY, JSON.sanitizeString(issueType));
		fields += createJSONStringOrField(projectFIELD, projectKEY, JSON.sanitizeString(project));
		fields += createJSONStringOrField(summaryFIELD, summaryKEY, JSON.sanitizeString(summary));
		if (issueType == "Task")
		{
			fields += createJSONStringOrField(priorityFIELD, priorityKEY, JSON.sanitizeString(priority.ToString()));
			fields += createJSONStringOrField(assigneeFIELD, assigneeKEY, JSON.sanitizeString(assignee));
		}
		fields += createJSONStringOrField(descriptionFIELD, descriptionKEY, JSON.sanitizeString(description));
		fields += createJSONStringOrField(labelsFIELD, labelsKEY, JSON.sanitizeString(labels));
		if (JSON.sanitizeString(affectsVersion) != "")
		{
			fields += createJSONStringOrField(affectsVersionFIELD, affectsVersionKEY, JSON.sanitizeString(affectsVersion));
		}
		if (JSON.sanitizeString(fixVersion) != "")
		{
			fields += createJSONStringOrField(fixVersionFIELD, fixVersionKEY, JSON.sanitizeString(fixVersion));
		}
		// Clean up the JSON we just made since it can't end in a ,
		fields = fields.Substring(0, fields.LastIndexOf(','));
		fields = fields.Replace("\n", @"\n");
		string JSONString = string.Format(JIRA_JSON_FINAL_FORMAT, fields);
		return JSONString;
	}
	
	public void sendRequestNoCoroutine()
	{
		string requestData = createJSONStringforJIRA();
		string URL = "https://jira.corp.zynga.com/rest/api/2/issue/";
		WWWForm form = new WWWForm();
		Dictionary<string,string> headers = form.headers;
		string auth = "Basic " + System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(USER_NAME + ":" + PASSWORD));
		headers["Authorization"] = auth;
		headers["Content-Type"] = "application/json";

		byte[] myData = System.Text.Encoding.UTF8.GetBytes(requestData);
		jiraSubmitTicketWebRequest = UnityWebRequest.Post(URL, requestData);
		foreach (KeyValuePair<string,string> kvp in headers)
		{
			jiraSubmitTicketWebRequest.SetRequestHeader(kvp.Key, kvp.Value);
		}

		byte[] bytes= Encoding.UTF8.GetBytes(requestData);
		UploadHandlerRaw uH = new UploadHandlerRaw(bytes);
		uH.contentType= "application/json";
		jiraSubmitTicketWebRequest.uploadHandler = uH;

		Debug.Log("Sending JIRA API request");
		AsyncOperation operation = jiraSubmitTicketWebRequest.SendWebRequest();
	}

	public IEnumerator sendRequest()
	{
		string requestData = createJSONStringforJIRA();
		string URL = "https://jira.corp.zynga.com/rest/api/2/issue/";
		WWWForm form = new WWWForm();
		Dictionary<string,string> headers = form.headers;
		string auth = "Basic " + System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(USER_NAME + ":" + PASSWORD));
		headers["Authorization"] = auth;
		headers["Content-Type"] = "application/json";

		byte[] myData = System.Text.Encoding.UTF8.GetBytes(requestData);
		UnityWebRequest www = UnityWebRequest.Post(URL, requestData);
		foreach (KeyValuePair<string,string> kvp in headers)
		{
			www.SetRequestHeader(kvp.Key, kvp.Value);
		}

		byte[] bytes= Encoding.UTF8.GetBytes(requestData);
		UploadHandlerRaw uH = new UploadHandlerRaw(bytes);
		uH.contentType= "application/json";
		www.uploadHandler = uH;

		Debug.Log("Sending JIRA API request");
		AsyncOperation operation = www.SendWebRequest();
		float oldProgress = -1.0f;
		while (!operation.isDone)
		{
			if (operation.progress > oldProgress)
			{
				oldProgress = operation.progress;
				Debug.LogFormat("Sending JIRA request: {0}% done.", (operation.progress * 100.0f));
			}
			yield return null;
		}
		Debug.Log("Sending JIRA request: done.");

		string result = "";

		if (www.isNetworkError)
		{
			Debug.Log(www.error);
			result = www.error;
		}
		else
		{
			Debug.Log(www.downloadHandler.text);
			result = www.downloadHandler.text;
		}

		resultJSON = new JSON(result);
	}

	public JIRAData()
	{
	}

	// TODO: Make this a common function and use it throughout the code.
	protected string logTypeToString(LogType logType)
	{
		string logTypeString = "<Unknown>";
		switch (logType)
		{
			case LogType.Log:
				logTypeString = "<LOG>";
				break;
	
			case LogType.Warning:
				logTypeString = "<WARNING>";
				break;

			case LogType.Error:
				logTypeString = "<ERROR>";
				break;
			case LogType.Exception:
				logTypeString = "<EXCEPTION>";
				break;
			case LogType.Assert:
				logTypeString = "<Assert>";
				break;
		}
		return logTypeString;
	}
}

#endif

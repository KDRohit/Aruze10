using UnityEngine;
using System.Collections;
using System.Text;

public class AutomatedCompanionVisualBug 
{

	public string message;
	public System.DateTime timestamp;

	int logNum = 0;

	public AutomatedCompanionVisualBug(string message, int logNum) 
	{
		this.message = message;
		this.timestamp = System.DateTime.Now;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AutomatedCompanionVisualBug"/> class using JSON data.
	/// </summary>
	/// <param name="json">Json data to parse and load into this class.</param>
	public AutomatedCompanionVisualBug(JSON json)
	{
		this.message = json.getString(AutomationJSONKeys.LOG_MESSAGE_KEY, "No Details Provided", "No Details Provided");
		this.timestamp = System.DateTime.Parse(json.getString(AutomationJSONKeys.TIMESTAMP_KEY, System.DateTime.MinValue.ToString()));
		this.logNum = json.getInt(AutomationJSONKeys.LOGNUM_KEY, 0);

	}

	/// <summary>
	/// Returns data from this class as a JSON string, to be saved into a file.
	/// </summary>
	/// <returns>The JSON data as a string.</returns>
	public string toJSON()
	{
		StringBuilder build = new StringBuilder();

		// Use this log's long (includes milliseconds) timestamp as a key for the JSON.
		build.AppendFormat("\"{0}\":{{", timestamp.ToString("MM/dd/yyyy hh:mm:ss.fff tt"));

		// Encode the visual bug message to JSON
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.LOG_MESSAGE_KEY, message));

		// Encode the timestamp to JSON
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.TIMESTAMP_KEY, timestamp.ToString()));

		// Note: No comma at end, this is the last element of the visual bug JSON.
		build.AppendFormat("{0}", JSON.createJsonString(AutomationJSONKeys.LOGNUM_KEY, logNum));


		return build.ToString();

	}
}

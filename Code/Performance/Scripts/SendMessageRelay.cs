using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * This class is responsible for handling messages sent from the facebook canvas jslib
 */
public class SendMessageRelay : MonoBehaviour
{
	public static bool hasFocus { get; private set; }

	private static List<SendMessageObject> sendMessageQueue = new List<SendMessageObject>();

	void Awake()
	{
		DontDestroyOnLoad(this);
	}

	private void popMessages()
	{
		StartCoroutine(popMessageQueue());
	}

	private IEnumerator popMessageQueue()
	{
		// give messages a frame to make sure everything is initialized properly
		yield return null;

		List<SendMessageObject> completed = new List<SendMessageObject>();

		for (int i = 0; i < sendMessageQueue.Count; ++i)
		{
			SendMessageObject obj = sendMessageQueue[i];
			string objName = sendMessageQueue[i].objectName;

			GameObject messageObj = GameObject.Find(objName);

			if (messageObj != null)
			{
				Debug.LogFormat("Sending Stored Message, {0}, {1}, {2}", objName, obj.method, obj.param);
				messageObj.SendMessage(obj.method, obj.param);
				completed.Add(obj);
			}
			else
			{
				Debug.LogError("Failed to find object for send message: " + obj);
			}
		}

		for (int k = 0; k < completed.Count; ++k)
		{
			sendMessageQueue.Remove(completed[k]);
		}

		if (sendMessageQueue.Count > 0)
		{
			popMessages();
		}
	}

	public void onSendMessage(string strParams)
	{
		string[] options = strParams.Split('|');
		if (options.Length < 3)
		{
			Debug.LogError("Failed to SendMessage with appropriate parameters for JSBridge");
			return;
		}

		string obj = options[0];
		string method = options[1];
		string p = options[2];

		sendMessageQueue.Add(new SendMessageObject(obj, method, p));

		popMessages();

		Debug.LogFormat("Application SendMessage received from JS, {0}, {1}, {2}", obj, method, p);
	}

	private class SendMessageObject
	{
		public string objectName = "";
		public string method = "";
		public string param = "";

		public SendMessageObject(string objectName, string method, string param)
		{
			this.objectName = objectName;
			this.method = method;
			this.param = param;
		}
	}
}

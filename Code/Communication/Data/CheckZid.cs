using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using Zynga.Zdk;

public class CheckZid : IResetGame
{
	public const string RESPONSE_KEY = "CheckZid";

	public static float lastPlayerDataRequestTime;

	private static string checkZidUrl
	{
		get
		{
			if (Application.isEditor)
			{
				return Glb.zidCheckUrl;
			}
			else
			{
				if (_checkZidUrl == null)
				{
					_checkZidUrl = Glb.zidCheckUrl;
				}

				return _checkZidUrl;
			}
		}
	}
	private static string _checkZidUrl = null;

	public static IEnumerator checkZid(string zid)
	{
		// Setup login form
		Dictionary<string, string> elements = new Dictionary<string,string>();

		elements.Add("zid",	zid);
		elements.Add ("sn_id", (int)ZdkManager.Instance.Zsession.Snid+"");
		elements.Add ("client_id", (int)ZyngaConstants.ClientId+"");

		lastPlayerDataRequestTime = Time.realtimeSinceStartup;
		yield return RoutineRunner.instance.StartCoroutine(Server.attemptRequest(checkZidUrl, elements, "error_failed_to_login", RESPONSE_KEY, false));
		//yield return RoutineRunner.instance.StartCoroutine(Data.attemptServerRequest(checkZidUrl, elements, "error_failed_to_login", RESPONSE_KEY, false));
		JSON jsonData = Server.getResponseData(RESPONSE_KEY, false);

		if (jsonData == null)
		{
			Debug.LogError("We attempted to hit the following checkzid URL, and we could not get login data from it!");
			Loading.hide(Loading.LoadingTransactionResult.FAIL);
			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, Localize.text("error"),
					D.MESSAGE, Localize.text("actions_error_message"),
					D.REASON, "check-zid-failed",
					D.CALLBACK, new DialogBase.AnswerDelegate( (args) => { Glb.resetGame("checkZid() failed to connect"); })
				),
				SchedulerPriority.PriorityType.BLOCKING
			);
			yield break;
		}

		bool status = jsonData.getBool("status", true);
		string message = jsonData.getString("message", "nope");
		Debug.Log("Checkzid returns: " + status + " --- " + message + " FOR zid: " + zid);
		//Data.setLoginData(jsonData);
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		lastPlayerDataRequestTime = 0;
	}
}


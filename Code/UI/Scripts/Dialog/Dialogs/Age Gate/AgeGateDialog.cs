using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;

/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/

public class AgeGateDialog : DialogBase
{
	public static AgeGateDialog instance = null;
	public AgeBox ageSelection = null;
	
	public UIDragPanelContents ageWindowFrame;
	public UIDraggablePanel ageWindowPane;
	public UIGrid ageGrid;
	public GameObject agePrefab;
	public UIButton okButton;
	
	public const int MIN_AGE = 13;
	public const int MAX_AGE = 80;
	
	private AgeBox ageBox18 = null;
	
	private const string IMAGE_PATH = "misc_dialogs/velvet_rope_background.jpg"; // HIR Only

	// When AgeYoungDialog closing, it call the callback method first than init method. So, Singleton needs more fast initialization.
	public void Awake()
	{
		instance = this;
	}

	public override void init()
	{
		UIEventListener.Get(ageWindowFrame.gameObject).onDrag += onDrag;
		ageWindowPane.onScrollingFinished += OnScrollingFinished;
		
		// Partly because of the way windows bounce at the top and bottom of a scrolling window,
		// windows will not "naturally" stop scrolling at the youngest or oldest ages.
		// Hack around it by adding fake youngest and oldest objects at the top and bottom.
		
		GameObject youngestObject = NGUITools.AddChild(ageGrid.gameObject, agePrefab);
		AgeBox youngestBox = youngestObject.GetComponent<AgeBox>();
		youngestBox.init(0);
		
		for (int age = MIN_AGE; age <= MAX_AGE; age++ )
		{
			GameObject ageObject = NGUITools.AddChild(ageGrid.gameObject, agePrefab);
			
			if (ageObject != null)
			{
				AgeBox ageBox = ageObject.GetComponent<AgeBox>();
				if (ageBox != null)
				{
					ageBox.init(age);
				}
				
				UIButtonMessage ageMessage = ageObject.GetComponent<UIButtonMessage>();
				if (ageMessage != null)
				{
					ageMessage.target = this.gameObject;
				}
				
				if (age == 18)
				{
					ageBox18 = ageBox;
				}
				
				UIEventListener.Get(ageObject).onDrag += onDrag;
			}
		}

		GameObject oldestObject = NGUITools.AddChild(ageGrid.gameObject, agePrefab);
		AgeBox oldestBox = oldestObject.GetComponent<AgeBox>();
		oldestBox.init(0);
		
		okButton.isEnabled = false;
		StatsManager.Instance.LogCount("dialog", "age_gate", "view");
	}
	
	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();

		if (ageBox18 != null)
		{
			ageBox18.center();
		}			
	}

#if ZYNGA_TRAMP || UNITY_EDITOR
	public override IEnumerator automate()
	{
		ageSelection.age = 21;
		clickOk();
		yield return null;
	}
#endif
	
	private void onDrag(GameObject go, Vector2 delta)
	{
		unselectAge();
	}
	
	private void OnScrollingFinished()
	{
		if (instance.ageSelection == null)
		{
			GameObject closestObject = ageBox18.gameObject;
			float closestDistance = closestObject.transform.position.y - ageWindowFrame.transform.position.y;
			closestDistance = System.Math.Abs(closestDistance);
			
			int numAges = ageGrid.transform.childCount;
			for (int iChild = 0; iChild<numAges; iChild++)
			{
				GameObject ageObject = ageGrid.transform.GetChild(iChild).gameObject;
				AgeBox ageBox = ageObject.GetComponent<AgeBox>();
				
				if (ageBox.age == 0)
				{
					continue;
				}
				
				float distance = ageObject.transform.position.y - ageWindowFrame.transform.position.y;
				distance = System.Math.Abs(distance);
				
				if (distance < closestDistance)
				{
					closestObject = ageObject;
					closestDistance = distance;
				}
			}
			
			clickAge(closestObject);
		}
	}
	
	private static void unselectAge()
	{
		if (instance.ageSelection != null && instance.okButton != null)
		{
			instance.ageSelection.select(false);
			instance.ageSelection = null;
			instance.okButton.isEnabled = false;
		}
	}
	
	private void clickAge(GameObject ageObject)
	{
		if (ageSelection != null)
		{
			ageSelection.select(false);
		}
		
		ageSelection = ageObject.GetComponent<AgeBox>();
		
		if (ageSelection != null)
		{
			ageSelection.select(true);
			okButton.isEnabled = true;
		}
	}
	
	private void clickOk()
	{
		if (ageSelection == null)
		{
			return;
		}
		
		int age = ageSelection.age;

		StatsManager.Instance.LogCount("dialog", "age_gate", "click", age.ToString());
		StatsManager.Instance.LogMileStone("android_age_entry", age); // Android-only right now.
		StatsManager.Instance.LogCount("dialog", "age_gate", "click", "okay");
		
		if (age >= 21)
		{
			Dialog.close();
			CustomPlayerData.setValue(CustomPlayerData.SHOW_AGE_GATE, 0);
			MOTDFramework.showGlobalMOTD(MOTDFramework.SURFACE_POINT.APP_ENTRY);
		}
		else
		{
			AgeYoungDialog.showDialog(new DialogBase.AnswerDelegate(sorryCallback));
				
			StatsManager.Instance.LogCount("dialog", "age_gate_under21", "view");
		}
	}
	
	private static void sorryCallback(Dict answerArgs)
	{
		if ((answerArgs[D.ANSWER] as string) == "overAge")
		{
			unselectAge();
			StatsManager.Instance.LogCount("dialog", "age_gate_under21", "click", "over_21");
		}
		else if ((answerArgs[D.ANSWER] as string) == "underAge")
		{
			StatsManager.Instance.LogCount("dialog", "age_gate_under21", "click", "under_21");
			
			GenericDialog.showDialog(
				Dict.create(
					D.TITLE , Localize.toUpper(Localize.text("age_sorry_title")),
					D.MESSAGE , Localize.text("age_sorry_message"),
					D.SHOW_CLOSE_BUTTON , false,
					D.OPTION1 , Localize.toUpper(Localize.text("quit")),
					D.REASON, "age-gate-underage",
					D.CALLBACK , new DialogBase.AnswerDelegate(failCallback),
					D.STACK, true
				),
				SchedulerPriority.PriorityType.BLOCKING
			);

			StatsManager.Instance.LogCount("dialog", "age_gate_block", "view");
		}
	}
	
	private static void failCallback(Dict answerArgs)
	{
		StatsManager.Instance.LogCount("dialog", "age_gate_block", "click", "quit");
		
#if UNITY_ANDROID 
		//Simulate a pause to update the badge counter correctly upon exiting the app in android
		NotificationManager.Instance.pauseHandler(true);
#endif
		
#if UNITY_EDITOR
		// In the editor, do the closest thing we can to quitting, which is pausing the game.
		Debug.Break();
#else
		Common.QuitApp();
#endif
	}

	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
		instance = null;
	}

	public static bool showDialog()
	{
		Dialog.instance.showDialogAfterDownloadingTextures("age_gate", IMAGE_PATH);
		return true;
	}
}

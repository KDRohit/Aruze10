using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

public class TicketTumblerStatusButton : MonoBehaviour
{
	public TextMeshPro 	numTickets;
	public TextMeshPro 	ticketLabel;
	public TextMeshPro 	roundTimeLeftLabel;

	public ImageButtonHandler button;
	public AnimationListController.AnimationInformationList getTicketAnimations;
	public AnimationListController.AnimationInformationList betIncreaseAnimations;
	public AnimationListController.AnimationInformationList getHeatedAnimations;
	public AnimationListController.AnimationInformationList spinAnimations;
	public AnimationListController.AnimationInformationList gettingCloseAnimations;
	public AnimationListController.AnimationInformationList noTicketAnimations;

	private List<TICoroutine> runningCoroutines = new List<TICoroutine>();		
	public List<GameObject> ticketStack = new List<GameObject>();

	public float TICKET_WAIT_TIME = 0.8f;

	void Awake()
	{
		button.registerEventDelegate(showIntroDialog);
		SpinPanel.instance.activateFeatureButton(button.imageButton);
		if (TicketTumblerFeature.instance.eventData == null)
		{
			RoutineRunner.instance.StartCoroutine(waitForEventData());
			return;
		}
		else
		{
			init();
		}
	}

	private void init()
	{
		Audio.play("FeatureAdvance01TicketTumbler", 0.0f);   // play at zero volume so it doesn't cause a hiccup first time it is played for real.

		setTicketLabels();
		if (roundTimeLeftLabel != null)
		{
			TicketTumblerFeature.instance.roundEventTimer.registerLabel(roundTimeLeftLabel);
		}
	}

	public void onRedAlert()
	{
		if (runningCoroutines.Count == 0 && gameObject.activeSelf)	// don't do if it is already playing other stuff
		{
			StartCoroutine(AnimationListController.playListOfAnimationInformation(getHeatedAnimations));
		}
	}

	public void onGotTicket(bool doAnimation = true)
	{
		if (gameObject.activeSelf && doAnimation)
		{
			StartCoroutine(gotTicketAnimation());
		}

		if (!doAnimation)
		{
			setTicketLabels();		
		}
	}

	private IEnumerator gotTicketAnimation()
	{
		StartCoroutine(AnimationListController.playListOfAnimationInformation(getTicketAnimations));
		yield return new WaitForSeconds(TICKET_WAIT_TIME);
		setTicketLabels();		
	}		

	public void onWagerChange()
	{
		if (runningCoroutines.Count == 0 && gameObject.activeSelf)	// don't do if it is already playing
		{
			StartCoroutine(AnimationListController.playListOfAnimationInformation(betIncreaseAnimations, runningCoroutines));
			StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutines));
		}
	}

	public void setTicketLabels()
	{
		SafeSet.labelText(numTickets, CommonText.formatNumber(TicketTumblerFeature.instance.ticketCount));
		if (TicketTumblerFeature.instance.ticketCount == 1)
		{
			SafeSet.labelText(ticketLabel, Localize.text("ticket"));
		}
		else
		{
			SafeSet.labelText(ticketLabel, Localize.text("tickets"));
		}

		TicketTumblerFeature.instance.stackTickets(ticketStack, TicketTumblerFeature.instance.ticketCount);
	}


	public void onEventProgress(bool doAnimation = true)
	{
		if (gameObject.activeSelf && doAnimation)
		{
			StartCoroutine(AnimationListController.playListOfAnimationInformation(gettingCloseAnimations));
		}	
	}

	public void onSpin(bool doAnimation = true)
	{
		if (gameObject.activeSelf && doAnimation)
		{
			StartCoroutine(AnimationListController.playListOfAnimationInformation(spinAnimations));
		}	
	}

	public void deactivate()
	{
		gameObject.SetActive(false);
	}	

	// Sometimes the data comes in late. So wait for it.
	private IEnumerator waitForEventData()
	{
		while (TicketTumblerFeature.instance.eventData == null)
		{
			yield return null;
		}

		init();
	}

	// click handler
	private void showIntroDialog(Dict args = null)
	{
		if (!Scheduler.hasTaskWith("ticket_tumbler"))
		{
			StatsManager.Instance.LogCount(counterName:"dialog", kingdom:"lottery_day_motd", klass:"in_game", genus:"view");

			TicketTumblerDialog.showDialog("", null, true);
		}
	}
}
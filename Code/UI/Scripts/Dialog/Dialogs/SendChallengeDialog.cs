using UnityEngine;
using System.Collections;
using Com.Scheduler;

/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/

public class SendChallengeDialog : DialogBase
{
	public UILabel scoreLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent scoreLabelWrapperComponent;

	public LabelWrapper scoreLabelWrapper
	{
		get
		{
			if (_scoreLabelWrapper == null)
			{
				if (scoreLabelWrapperComponent != null)
				{
					_scoreLabelWrapper = scoreLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_scoreLabelWrapper = new LabelWrapper(scoreLabel);
				}
			}
			return _scoreLabelWrapper;
		}
	}
	private LabelWrapper _scoreLabelWrapper = null;
	
	public UILabel nameLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent nameLabelWrapperComponent;

	public LabelWrapper nameLabelWrapper
	{
		get
		{
			if (_nameLabelWrapper == null)
			{
				if (nameLabelWrapperComponent != null)
				{
					_nameLabelWrapper = nameLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_nameLabelWrapper = new LabelWrapper(nameLabel);
				}
			}
			return _nameLabelWrapper;
		}
	}
	private LabelWrapper _nameLabelWrapper = null;
	
	public UILabel prizeLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent prizeLabelWrapperComponent;

	public LabelWrapper prizeLabelWrapper
	{
		get
		{
			if (_prizeLabelWrapper == null)
			{
				if (prizeLabelWrapperComponent != null)
				{
					_prizeLabelWrapper = prizeLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_prizeLabelWrapper = new LabelWrapper(prizeLabel);
				}
			}
			return _prizeLabelWrapper;
		}
	}
	private LabelWrapper _prizeLabelWrapper = null;
	
	public Renderer playerPicRenderer;
		
	/// Initialization
	public override void init()
	{
		long score = (long)dialogArgs.getWithDefault(D.SCORE, 0);
		scoreLabelWrapper.text = CommonText.formatNumber(score);
		prizeLabelWrapper.text = CreditsEconomy.convertCredits(Glb.CHALLENGE_BONUS_CREDITS);
		nameLabelWrapper.text = Localize.toUpper(SlotsPlayer.instance.socialMember.firstName);
		StartCoroutine(SlotsPlayer.instance.socialMember.setPicOnRenderer(playerPicRenderer));

	}
	
	// void Update()
	// {
	// 	// Touching anywhere on this dialog will close it.
	// 	if (TouchInput.didTap)
	// 	{
	// 		Dialog.close();
	// 	}
	// }
		
	public void challengeClicked()
	{
		dialogArgs.merge(D.ANSWER, "yes");
		Dialog.close();
	}
	
	public void closeClicked()
	{
		dialogArgs.merge(D.ANSWER, "no");
		Dialog.close();
	}
			
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}
	
	public static void showDialog(Dict args)
	{
		Scheduler.addDialog(
			"send_challenge",
			args,
			SchedulerPriority.PriorityType.IMMEDIATE
		);
	}
}


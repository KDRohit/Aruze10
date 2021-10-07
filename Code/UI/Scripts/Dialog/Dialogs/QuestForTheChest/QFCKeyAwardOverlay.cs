using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace QuestForTheChest
{
	public class QFCKeyAwardOverlay : MonoBehaviour
	{
		[SerializeField] private TextMeshPro amountLabel;
		[SerializeField] private ButtonHandler button;
		private ClickHandler.onClickDelegate onClickHandler;
		private string eventId;
		
		private const string QFC_FINAL_KEY_COLLECT_FINAL_SOUND = "QfcFinalKeyCollectFinal";

		public void init(string id, long coinAmount, ClickHandler.onClickDelegate onClick)
		{
			amountLabel.text = CreditsEconomy.convertCredits(coinAmount);
			onClickHandler = onClick;
			eventId = id;
			gameObject.SetActive(true);
			button.registerEventDelegate(okClicked);
		}
		
		private void okClicked(Dict args)
		{
			Audio.play(QFC_FINAL_KEY_COLLECT_FINAL_SOUND + ExperimentWrapper.QuestForTheChest.theme);
			button.unregisterEventDelegate(okClicked);
			if (onClickHandler != null)
			{
				onClickHandler.Invoke(Dict.create(D.EVENT_ID, eventId));
			}

			gameObject.SetActive(false);
		}

	}
}

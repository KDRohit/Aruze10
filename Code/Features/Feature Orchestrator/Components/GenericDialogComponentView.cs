using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FeatureOrchestrator
{
	public class GenericDialogComponentView : DialogBase
	{
		[SerializeField] protected LabelWrapperComponent titleLabel;
		[SerializeField] protected LabelWrapperComponent messageLabel;
		[SerializeField] private AudioListController.AudioInformationList dialogInitAudio;
		[SerializeField] private AudioListController.AudioInformationList dialogCloseAudio;

		public ButtonHandler[] buttons;
		
		private ShowDialogComponent parentComponent;
		
		public override void init()
		{
			if (dialogArgs == null)
			{
				return;
			}

			if (titleLabel != null)
			{
				titleLabel.text = Localize.text(dialogArgs.getWithDefault(D.TITLE, "") as string);	
			}
			if (messageLabel != null)
			{
				messageLabel.text = Localize.text(dialogArgs.getWithDefault(D.MESSAGE, "") as string);	
			}
			
			parentComponent = dialogArgs.getWithDefault(D.DATA, null) as ShowDialogComponent;
			
			StartCoroutine(AudioListController.playListOfAudioInformation(dialogInitAudio));
		}
		
		/// Called by Dialog.close() - do not call directly.	
		public override void close()
		{
			// Do special cleanup.
			StartCoroutine(AudioListController.playListOfAudioInformation(dialogCloseAudio));
		}

		public void clickClose(Dict args = null)
		{
			Dialog.close();
		}
		
	}
}

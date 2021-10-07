using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Com.Scheduler;

public class PowerupInfoDialog : DialogBase
{
	[SerializeField] private AlbumDialogCardView content;

	private PowerupBase powerup;

	private const string CARD_PATH = "Features/Collections/Prefabs/Card Assets/Card Frames/Card Frame 6 PowerUps";

	public override void init()
	{
		powerup = dialogArgs.getWithDefault(D.DATA, null) as PowerupBase;

		if (powerup != null)
		{
			content = GetComponent<AlbumDialogCardView>();

			if (PowerupsManager.hasActivePowerupByName(powerup.name))
			{
				Audio.play("PowerUpOpenSingle");
			}
			else
			{
				Audio.play("PowerUpOpenSingleInactive");
			}

			string keyName = "";
			foreach (KeyValuePair<string, string> entry in PowerupBase.collectablesPowerupsMap)
			{
				if (entry.Value == powerup.name)
				{
					keyName = entry.Key;
					break;
				}
			}

			if (content != null && !string.IsNullOrEmpty(keyName))
			{
				CollectableCardData data = Collectables.Instance.findCard(keyName);

				if (data != null)
				{
					
					List<CollectableCardData> cards = Collectables.Instance.getCardsFromSet(data.setKey, true);
					int index = cards.IndexOf(data);

					Dict args = Dict.create(D.DATA, cards, D.KEY, index);
					displayCard(args);
				}
			}
			else
			{
				Dialog.close(this);
			}

		}
		else
		{
			Dialog.close(this);
		}
	}

	private void displayCard(Dict data)
	{
		Texture2D texture = downloadedTextures[0];

		int index = (int)data.getWithDefault(D.KEY, 0);
		List<CollectableCardData> cards = data.getWithDefault(D.DATA, null) as List<CollectableCardData>;

		Dictionary<string, Texture2D> loadedTextures = new Dictionary<string, Texture2D> { {texture.name, texture } };

		content.init(cards[index], index, cards, null, null, null, loadedTextures);

		content.togglePageController(false);
	}

	public override void close()
	{

	}

	public static void showDialog(PowerupBase powerup)
	{
		Dict args = Dict.create(D.DATA, powerup);
		Dialog.instance.showDialogAfterDownloadingTextures("powerups_info_dialog", CARD_PATH, args, true, SchedulerPriority.PriorityType.IMMEDIATE, true);
	}
}
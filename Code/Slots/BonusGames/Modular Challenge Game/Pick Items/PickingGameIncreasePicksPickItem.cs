using UnityEngine;

/**
 * Class specific to "extra pick" reveals on pick items
 */
public class PickingGameIncreasePicksPickItem : PickingGameBasePickItemAccessor
{
	public LabelWrapperComponent increaseLabel;
	public LabelWrapperComponent grayIncreaseLabel;

	[SerializeField] private string labelFormatString = "+{0} Picks";

	// Sets extra picks awarded labels
	public void setPicksAwarded(int quantity)
	{
		if (increaseLabel != null)
		{
			increaseLabel.text = formatPicksLabel(quantity);
		}

		if (grayIncreaseLabel != null)
		{
			grayIncreaseLabel.text = formatPicksLabel(quantity);
		}
	}

	// return a formatted string for the label
	public string formatPicksLabel(int quantity)
	{
		return string.Format(labelFormatString, quantity);
	}
}

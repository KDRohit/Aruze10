
/*
 * Variant of LabelWrapper that updates synchronized labels when text gets updated. Replaces old system in MultiLabelWrapperComponent
 * of directly updating list of LabelWrapperComponents so that cases where the LabelWrapper is accessed directly still work
 * as expected (eg in SlotUtils.rollup() we use labelWrapper instead of the component)
 * Author: Caroline
 */

using System.Collections.Generic;

public class MultiLabelWrapper : LabelWrapper
{
	private List<LabelWrapper> syncLabels = new List<LabelWrapper>();

	public bool isInitialized { get; protected set; }

	public MultiLabelWrapper()
	{
		
	}
	
	public MultiLabelWrapper(LabelWrapper baseLabelWrapper, List<LabelWrapper> labels)
	{
		ngui = baseLabelWrapper.ngui;
		tmPro = baseLabelWrapper.tmPro;
		syncLabels = labels;
		isInitialized = true;
	}

	public void setLabelWrappers(LabelWrapper baseLabelWrapper, List<LabelWrapper> labels)
	{
		ngui = baseLabelWrapper.ngui;
		tmPro = baseLabelWrapper.tmPro;
		syncLabels = labels;
		isInitialized = true;
	}
	
	public override string text
	{
		get { return base.text; }
		set
		{
			base.text = value;
			
			foreach (LabelWrapper syncLabel in syncLabels)
			{
				syncLabel.text = value;
			}
		}
	}
}
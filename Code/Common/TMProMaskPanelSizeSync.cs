using UnityEngine;

[ExecuteInEditMode]
public class TMProMaskPanelSizeSync : MonoBehaviour
{
	//For constraining a quad with a TMPro Mask material to the size and position of a UI panel with clipping (to easier match sprite clip areas up with a text clip areas)

	[Tooltip("The UIPanel you wish to sync with")]
	[SerializeField] private UIPanel panel;
	[Tooltip("The TMPro Mask object to sync with the UIPanel")]
	[SerializeField] private Transform maskObject;

	private Vector4 panelCenterSize;
	private Vector3 maskPosition;
	private Vector3 maskScale;

	private void OnEnable()
	{
		panel.onChange += syncTMProMaskWithPanel;
	}

	private void OnDisable()
	{
		panel.onChange -= syncTMProMaskWithPanel;
	}

#if UNITY_EDITOR
	private void Update()
	{
		//Without this if UNITY_EDITOR then the TMP Mask object won't resize in real-time in editor
		if (!Application.isPlaying)
		{
			syncTMProMaskWithPanel();
		}
	}
#endif

	private void syncTMProMaskWithPanel()
	{
		if (panel == null || maskObject == null)
		{
			return;
		}

		if (panel.clipping == UIDrawCall.Clipping.None)
		{
			return;
		}

		panelCenterSize = panel.clipRange; 
		/* clipRange.x = CenterX
		 * clipRange.y = CenterY
		 * clipRange.z = SizeX
		 * clipRange.w = SizeY
		 */

		maskPosition = new Vector3(panel.clipRange.x, panel.clipRange.y, maskObject.localPosition.z);
		maskScale = new Vector3(panel.clipRange.z, panel.clipRange.w, maskObject.localScale.z);

		maskObject.localPosition = maskPosition;
		maskObject.localScale = maskScale;
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using Com.States;
using TMPro;
using UnityEngine;

public class PowerupTint : MonoBehaviour
{
	[SerializeField] private UIWidget[] tintWidgets;
	[SerializeField] private TextMeshPro[] labels;
	[SerializeField, PowerupCustomAttribute] public string applicablePowerup;

	private PowerupBase powerup;
	private bool forceActive = false;
	private float tintAmount = 1f;

	public bool isCollectedCardElement;

	void Awake()
	{
		setup();
	}

	private void setup()
	{
		if (!string.IsNullOrEmpty(applicablePowerup))
		{
			powerup = PowerupsManager.getActivePowerup(applicablePowerup);
		}

		if (powerup != null)
		{
			setTintColor((powerup.runningTimer != null && powerup.runningTimer.timeRemaining > 0) || forceActive);
			powerup.runningTimer.registerFunction(onTimerExpired);
		}
		else
		{
			setTintColor(forceActive || isCollectedCardElement);
		}
	}

	public void setPowerup(PowerupBase powerup, bool forceActive = false)
	{
		this.powerup = powerup;
		this.forceActive = forceActive;
		setup();
	}

	void OnDestroy()
	{
		if (powerup != null && powerup.runningTimer != null)
		{
			powerup.runningTimer.removeFunction(onTimerExpired);
		}
	}

	private void onPowerupEnabled()
	{
		setTintColor(true);
	}

	private void onTimerExpired(Dict args, GameTimerRange sender)
	{
		setTintColor(false);
	}

	public void setTintColor(bool active)
	{
		if (!active)
		{
			resetTints();
			updateTints();
		}
		else if (active || isCollectedCardElement)
		{
			resetTints();
			updateTints(2.0f);
		}
	}

	private void resetTints()
	{
		updateTints(1 / tintAmount);
		tintAmount = 0f;
	}

	public void updateTints(float amount = 0.5f)
	{
		tintAmount += amount;

		if (tintWidgets != null)
		{
			for (int i = 0; i < tintWidgets.Length; i++)
			{
				tintWidgets[i].color = new Color(tintWidgets[i].color.r * amount, tintWidgets[i].color.g * amount,
					tintWidgets[i].color.b * amount, 1.0f);
			}
		}

		if (labels != null)
		{
			for (int i = 0; i < labels.Length; i++)
			{
				labels[i].color = new Color(labels[i].color.r * amount, labels[i].color.g * amount,
					labels[i].color.b * amount, labels[i].color.a);
			}
		}
	}

	public void updateLayer(int layer)
	{
		CommonGameObject.setLayerRecursively(gameObject, layer);
	}
}
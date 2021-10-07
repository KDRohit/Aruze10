using UnityEngine;
using System.Collections;

namespace Com.Scheduler
{
	public class DialogTask : SchedulerTask
	{
		// =============================
		// INTERNAL
		// =============================
		internal string dialogKey;

		// =============================
		// PROTECTED
		// =============================
		protected DialogType dialogType;
		protected bool isLobbyOnly = false;
		
		// =============================
		// PRIVATE
		// =============================
		private bool isExecuting = false;

		public DialogTask(string dialogKey, Dict args = null) : base(args)
		{
			this.dialogKey = dialogKey;
			this.args = args != null ? args : Dict.create();
			this.dialogType = DialogType.find(dialogKey);

			priority.addToRating(SchedulerPriority.PriorityType.SINGLETON);

			MOTDFramework.notifyOnShow(args);
			setThemePath();
			sanitize();
		}

		/// <inheritdoc/>
		public override void execute()
		{
			if (!isExecuting)
			{
				isExecuting = true;
				if (priority.isType(SchedulerPriority.PriorityType.SINGLETON))
				{
					Scheduler.removeDuplicatesOf(this, dialogKey);
				}

				if (dialogType == null)
				{
					Debug.LogErrorFormat("DialogTask: Failed to find dialog type for {0}", dialogKey);
				}

				if (dialogType.isBundled)
				{
					Userflows.flowStart(string.Format("opening_dialog_{0} ", dialogKey));
					//This will implicitly load the prefab's bundle along with ALL of its contents since isSkippingMapping
					//is false by default and isSkippingBundleMap is usually not set in 'Dialog Types.txt'
					AssetBundleManager.load(this, dialogType.dialogPrefabPath, onBundleDownloaded, onBundleFailed, args, isSkippingMapping:dialogType.isSkippingBundleMap, fileExtension:".prefab");
				}
				else if (dialogType.prefab != null)
				{
					Userflows.flowStart(string.Format("opening_dialog_{0} ", dialogKey));
					onBundleDownloaded(dialogType.dialogPrefabPath, dialogType.prefab);
				}
				else
				{
					onBundleFailed(dialogType.dialogPrefabPath);
				}
			}
		}

		private void onBundleDownloaded(string assetPath, Object obj, Dict data = null)
		{
			Debug.LogFormat("DialogTask.onBundleDownloaded() succeeded in downloading asset: {0}", assetPath);
			
			base.execute();
			logUserFlowSpinState();

			Dialog.instance.StartCoroutine
			(
				Dialog.instance.create
				(
					dialogType,
					this,
					args,
					args.getWithDefault(D.CALLBACK, null) as DialogBase.AnswerDelegate,
					args.getWithDefault(D.CLOSE, null) as DialogBase.CloseDelegate, 
					(bool)args.getWithDefault(D.SHROUD, true), 
					(bool) args.getWithDefault(D.SHOW_PET, false)
				)
			);

			if (Loading.isLoading && priority.rating >= (int)SchedulerPriority.PriorityType.IMMEDIATE)
			{
				Loading.hide(Loading.LoadingTransactionResult.NONE);
			}

			MOTDFramework.alertToDialogShowCall(args);
		}

		private void onBundleFailed(string assetPath, Dict data = null)
		{
			Debug.LogErrorFormat("DialogTask.onBundleFailed() failed to download prefab at path: {0}", assetPath);
			
			base.execute();
			Userflows.flowEnd(string.Format("opening_dialog_{0} ", dialogKey), false, "Couldn't load dialog");

			Scheduler.removeTask(this);

			MOTDFramework.alertToDialogShowCall(args);

			Dialog.instance.resetShroud();
		}

		/// <inheritdoc/>
		public override bool contains<T>(T value)
		{
			return Equals(value, dialogKey);
		}

		/*=========================================================================================
		ANCILLARY
		=========================================================================================*/
		/// <summary>
		/// Userflow logs based on the users spin state
		/// </summary>
		private void logUserFlowSpinState()
		{
			if (args != null && args.ContainsValue(D.IS_WAITING))
			{
				if (!(bool)args[D.IS_WAITING] && Glb.spinTransactionInProgress)
				{
					Userflows.flowEnd(string.Format("opening_dialog_{0} ", dialogKey), false, "Dialog opened late");

					Debug.LogErrorFormat("Dialog {0} tried to show while spinning after being called to show before we spun! Check userflow opening_dialog_ for this dialog", dialogKey);
				}
			}
			else
			{
				Userflows.flowEnd(string.Format("opening_dialog_{0} ",dialogKey), true, "Dialog opened successfully and on time");
			}
		}

		/// <summary>
		/// Some dialogs have special themes the prefab paths need to be set to
		/// </summary>
		private void setThemePath()
		{
			if (args != null && args.containsKey(D.THEME) && !string.IsNullOrEmpty((string)args[D.THEME]))
			{
				dialogType.setThemePath((string)args[D.THEME]);
			}
		}

		/// <summary>
		/// This is a validation method to determine whether or not the dialog task added is actually
		/// able to be shown. For example, a purchase dialog cannot show if purchases are disabled.
		/// We leave this to the DialogTask due to the fact that the generic dialog may be part of this
		/// task if that's the case.
		///
		/// Other cases for santization is due to the conversion of Dict args. Things like "should hide loading"
		/// has now been assigned a priority. As well as D.STACK usage
		/// </summary>
		protected virtual void sanitize()
		{
			if (dialogType.isPurchaseDialog)
			{
				if (!Glb.ALLOW_CREDITS_PURCHASE)
				{
					Debug.LogWarningFormat("DialogTask: {0} failed to show due to scat disabled purchases", dialogKey);

					dialogKey = "generic";
					dialogType = DialogType.find("generic");
					args = Dict.create
					(
						D.TITLE,	Localize.text("disallow_credits_dialog_title"),
						D.MESSAGE,	Localize.text("disallow_credits_dialog_message")
					);

					priority.addToRating(SchedulerPriority.PriorityType.IMMEDIATE);
				}
				else if (!Packages.PaymentsManagerEnabled())
				{
					Debug.LogWarningFormat("DialogTask: {0} failed to show due payments being disabled", dialogKey);

					dialogKey = "generic";
					dialogType = DialogType.find("generic");
					args = Dict.create
					(
						D.TITLE,	Localize.text("error"),
						D.MESSAGE,	Localize.text("products_failed")
					);
					StatsManager.Instance.LogCount("debug", "purchasing", "products_failed");

					priority.addToRating(SchedulerPriority.PriorityType.IMMEDIATE);
				}
			}

			if (args != null)
			{
				if ((bool)args.getWithDefault(D.STACK, false))
				{
					priority.addToRating(SchedulerPriority.PriorityType.IMMEDIATE);
				}

				// Allow the optional function arguments to come in as part of the args.
				if ((SchedulerPriority.PriorityType)args.getWithDefault(D.PRIORITY, SchedulerPriority.PriorityType.LOW) == SchedulerPriority.PriorityType.IMMEDIATE)
				{
					priority.addToRating(SchedulerPriority.PriorityType.IMMEDIATE);
				}

				isLobbyOnly = (bool)args.getWithDefault(D.IS_LOBBY_ONLY_DIALOG, false);
				isLobbyOnly = isLobbyOnly || !string.IsNullOrEmpty((string)args.getWithDefault(D.MOTD_KEY, ""));
			}
		}

		public override string ToString()
		{
			int rating = priority != null ? priority.rating : (int)SchedulerPriority.PriorityType.LOW;
			return string.Format("DialogTask: {0} | priority {1}", dialogKey, rating.ToString());
		}

		/*=========================================================================================
		GETTERS/SETTERS
		=========================================================================================*/
		/// <inheritdoc/>
		public override bool canExecute
		{
			get
			{
				if (isLobbyOnly && !GameState.isMainLobby)
				{
					return false;
				}

				// check if dialogs can even be displayed
				if (base.canExecute)
				{
					// if this dialog has an immediate or higher priority, we are going to show it
					if (priority.rating >= (int)SchedulerPriority.PriorityType.IMMEDIATE)
					{
						return true;
					}

					// otherwise, as long as payments aren't happening, and we aren't transitioning, show it
					return !Packages.PaymentsPurchaseInProgress() && !MainLobby.isTransitioning;
				}

				return false;
			}
		}
	}
}
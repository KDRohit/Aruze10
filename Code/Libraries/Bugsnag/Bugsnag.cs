using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Wrapper for Bugsnag classes: Bugsnag version 4 places functionality inside BugsnagUnity namespace; wrap so that
///  existing game code can continue to use old-style calls.  </summary>
public class Bugsnag
{
	public static void LeaveBreadcrumb(string message)
	{
		if (BugsnagUnity.Bugsnag.Client != null)
		{
			if (message.Length > 30)
			{
				// Workaround for undocumented 30 char limit on breadcrumb messages.  Bugsnag says they are working on
				// increasing that limit but no timeline as yet for when that will be released.
				BugsnagUnity.Bugsnag.Breadcrumbs.Leave("Breadcrumb", BugsnagUnity.Payload.BreadcrumbType.Manual, new Dictionary<string, string>{{"message", message}});
			}
			else
			{
				BugsnagUnity.Bugsnag.LeaveBreadcrumb(message);
			}
		}
	}

	public static string ReleaseStage
	{
		get {
			return BugsnagUnity.Bugsnag.Configuration.ReleaseStage;
		}
		set {
			BugsnagUnity.Bugsnag.Configuration.ReleaseStage = value;
		}
	}

	public static string Context
	{
		get {
			return BugsnagUnity.Bugsnag.Configuration.Context;
		}
		set {
			BugsnagUnity.Bugsnag.Configuration.Context = value;
		}
	}

	public static LogType NotifyLevel
	{
		get {
			return BugsnagUnity.Bugsnag.Configuration.NotifyLevel;
		}
		set {
			BugsnagUnity.Bugsnag.Configuration.NotifyLevel = value;
		}
	}

	public static void SetUser(string id, string email, string name)
	{
		BugsnagUnity.Bugsnag.SetUser(id, email, name);
	}

	public static void StartSession()
	{
		BugsnagUnity.Bugsnag.StartSession();
	}

	public static void Notify(System.Exception exception)
	{
		BugsnagUnity.Bugsnag.Notify(exception);
	}

	public static void AddToTab(string tabName, Dictionary<string, object> metadata)
	{
		BugsnagHIR.AddToTab(tabName, metadata);
	}

	public static void AddToTab(string tabName, string itemName, object itemValue)
	{
		BugsnagHIR.AddToTab(tabName, new Dictionary<string, object> {{itemName, itemValue}});
	}
}

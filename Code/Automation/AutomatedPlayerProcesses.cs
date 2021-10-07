using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class makes it much easier to run any processes, such as git commands, that 
// are otherwise rather difficult from inside Unity. Also contains several helpful
// methods for creating and logging those processes.
public class AutomatedPlayerProcesses {

	// Debug strings.
	public const string PROCESS_DEBUG_COLOR = "blue";
	public const string PROCESS_LOG_MESSAGE = "<color={0}>[PROCESS] Log: {1}</color>";
	public const string PROCESS_ERROR_MESSAGE = "<color={0}>[PROCESS] Error: {1}</color>";
	public const string BRANCH_LOG_MESSAGE = "<color={0}>[PROCESS] Getting git branch</color>";

	// Destination of the tools directory, relative to asset data path (Usually assets folder).
	public const string TOOLS_PATH_FROM_DATAPATH = "/../../tools/";

	// Path to the tools directory in this Unity project. If the location ever changes, this will need to be updated.
	private static string toolsDirectoryPath
	{
		get
		{
			// Starts at the Assets folder, then goes up to find tools.
			return Application.dataPath + TOOLS_PATH_FROM_DATAPATH;
		}
	}

	// Strings for process names.
	public const string GIT_PROCESS_STRING = "git";
	public const string BASH_PROCESS_STRING = "/bin/bash";

	// Strings for process arguments.
	public const string GET_BRANCH_ARGUMENTS = "symbolic-ref --short HEAD";
	public const string PULL_ARGUMENTS = "pull";
	public const string CHECKOUT_ARGUMENTS = "checkout {0}";
	public const string STASH_CHECKOUT_ARGUMENTS = "stashcheckout-run-TRAMP-HIR.sh --ProcessID {0} --BranchName {1} --ToolsDirectory {2}";


	// Returns the name of the currently active git branch.
	public static string getBranchName()
	{

		Debug.LogFormat(BRANCH_LOG_MESSAGE, PROCESS_DEBUG_COLOR);

		// Get the branch name from git command.
		string scriptArguments = GET_BRANCH_ARGUMENTS;
		var process = setupNewProcess(GIT_PROCESS_STRING, scriptArguments, "");
		process.Start();

		using (System.IO.StreamReader reader = process.StandardOutput)
		{
			string branchName = reader.ReadLine();
			if (string.IsNullOrEmpty(branchName))
			{
				return "[Unkown-Branch]";
			}
			return branchName;
		}
	}

	// Pulls the latest changes from git.
	public static bool pullLatestFromGit()
	{
		var process = setupNewProcess(GIT_PROCESS_STRING, PULL_ARGUMENTS, "");
		process.Start();

		if (errorDuringProcess(process))
		{
			logStandardError(process);
			return false;
		}

		return true;
	}

	// Pulls the current branch and restarts Unity. 
	public static bool pullAndRestartUnity()
	{
		return checkoutGitBranch(getBranchName());
	}

	// Checks out the specifed git branch. Returns true if successful, false otherwise.
	public static bool checkoutGitBranch(string branch)
	{

		var pullProcess = setupNewProcess(GIT_PROCESS_STRING, PULL_ARGUMENTS, "");

		if (!string.IsNullOrEmpty(branch))
		{
			// Checkout the specified branch.
			string scriptArguments = string.Format(CHECKOUT_ARGUMENTS, branch);
			var checkoutProcess = setupNewProcess(GIT_PROCESS_STRING, scriptArguments, "");

			// Store a list of processes to run in order.
			List<System.Diagnostics.Process> processesToRun = new List<System.Diagnostics.Process>();
			processesToRun.Add(pullProcess);      // First, pull to get any new branches.
			processesToRun.Add(checkoutProcess);  // Next, checkout the desired branch.
			processesToRun.Add(pullProcess);      // Finally, pull the latest from the checked out branch.

			runMultipleProcessesSequentially(processesToRun, true, false);
		}
		else
		{
			Debug.LogError("Can't check out an empty branch!");
			return false;
		}

		return true;
	}

	// Pulls, stashes any current changes, checks out the specified branch, and restarts Unity.
	public static bool checkoutAndRestartUnity(string branchName)
	{

		int pid = System.Diagnostics.Process.GetCurrentProcess().Id;
		string scriptArguments = string.Format(toolsDirectoryPath + STASH_CHECKOUT_ARGUMENTS, pid, branchName, toolsDirectoryPath);

		var process = setupNewProcess(BASH_PROCESS_STRING, scriptArguments, "");
		process.Start();

		if (errorDuringProcess(process))
		{
			logStandardError(process);
			return false;
		}

		return true;
	}

	// Creates a new process with the specified arguments.
	private static System.Diagnostics.Process setupNewProcess(string processFileName, string arguments, string workingDirectory = "", bool redirectStdo = true, bool redirectStderr = true, bool useShell = false, bool createWindow = false)
	{

		// Create the process and its start info.
		System.Diagnostics.Process process = new System.Diagnostics.Process();
		System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(processFileName);

		// Start info options, can be passed in.
		startInfo.RedirectStandardOutput = redirectStdo;
		startInfo.RedirectStandardError = redirectStderr;
		startInfo.UseShellExecute = useShell;
		startInfo.CreateNoWindow = !createWindow;
		startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;

		// Directory to run the process.
		startInfo.WorkingDirectory = workingDirectory;

		// Arguments for the command.
		startInfo.Arguments = arguments;

		process.StartInfo = startInfo;
		return process;
	}

	// Iterate over a list of processes and start them, each one waiting until the previous one finished.
	private static void runMultipleProcessesSequentially(List<System.Diagnostics.Process> processes, bool logErrors, bool logOutput)
	{

		foreach (var process in processes)
		{
			process.Start();

			// Logs error or output if desired.
			if (logErrors)
			{
				logStandardError(process);
			}
			if (logOutput)
			{
				logStandardOutput(process);
			}

			// Wait until the process is done.
			process.WaitForExit();
		}
	}

	// Checks if there was any error output during the process.
	private static bool errorDuringProcess(System.Diagnostics.Process process)
	{
		if (process.StartInfo.RedirectStandardError)
		{
			System.IO.StreamReader errorReader = process.StandardError;
			
			if (errorReader.Peek() != -1)
			{
				return true;
			}
		}
	
		return false;
	}

	// Logs any standard output from the process.
	private static string logStandardOutput(System.Diagnostics.Process process, string logPrefix = "")
	{
		string output = "";

		if (process.StartInfo.RedirectStandardOutput)
		{

			System.IO.StreamReader outputReader = process.StandardOutput;

			if (outputReader.Peek() != -1)
			{
				output = string.Format(PROCESS_LOG_MESSAGE, PROCESS_DEBUG_COLOR, logPrefix + outputReader.ReadToEnd());
				Debug.Log(output);
			}
		}

		return output;
	}

	// Logs any error output from the process.
	private static string logStandardError(System.Diagnostics.Process process, string logPrefix = "")
	{

		string output = "";

		if (process.StartInfo.RedirectStandardError)
		{
			System.IO.StreamReader errorReader = process.StandardError;

			if (errorDuringProcess(process))
			{
				output = string.Format(PROCESS_ERROR_MESSAGE, PROCESS_DEBUG_COLOR, logPrefix + errorReader.ReadToEnd());
				Debug.LogError(output);
			}
		}
		
		return output;
	}
}

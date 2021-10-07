using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This is the sort of outcome used for crossword style games (e.g. zynga04).
public class CrosswordOutcome : GenericBonusGameOutcome<CrosswordPick>
{
	public string paytableName;
	public string board;
	public Dictionary<string, long> words = new Dictionary<string, long>();
	
	public long getWordScore(string theWord)
	{
		foreach (KeyValuePair<string, long> pair in words)
		{
			if (pair.Key == theWord)
			{
				return pair.Value;
			}
		}
		
		Debug.LogError("No score found for word.");
		
		return 0;
	}
	
	public long getTotalCredits()
	{
		long total = 0;
		
		foreach (KeyValuePair<string, long> pair in words)
		{
			total += pair.Value;
		}
		
		if (total == 0)
		{
			Debug.LogError("No score total.");
		}
		
		return total;
	}
	
	public CrosswordOutcome(SlotOutcome baseOutcome) : base(baseOutcome.getBonusGame())
	{	
		paytableName = baseOutcome.getBonusGamePayTableName();
		board = baseOutcome.getBoard();
		words = baseOutcome.getWords();
	
		entries = new List<CrosswordPick>();
		reveals = new List<CrosswordPick>();
		
		string[] picks = baseOutcome.getPicks();
		
		foreach (string curPick in picks)
		{
			CrosswordPick pick = new CrosswordPick(curPick[0]);
			entries.Add(pick);
			reveals.Add(pick);
		}
	}
	
	public override string ToString()
	{
		string output = "\n";
		
		output += "paytableName: " + paytableName + "\n";
		output += "board: " + board + "\n";
		output += "words: [";
		
		int count = 0;
		
		foreach (KeyValuePair<string, long> curWord in words)
		{
			output += "(" + curWord.Key + ", " + curWord.Value + ")";
			
			if (count < words.Count-1)
			{
				output += ", ";
			}
			
			count++;
		}
		
		output += "]\n";
		
		return output;
	}
}

public class CrosswordPick : CorePickData
{
	public char letter = '\0';
	
	public CrosswordPick(char letter)
	{
		this.letter = letter;
	}
	
	/// Output for debugging purposes.
	public override string ToString()
	{
		return "letter: " + letter;
	}
}

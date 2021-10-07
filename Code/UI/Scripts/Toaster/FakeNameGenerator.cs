using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FakeNameGenerator
{
	public static List<string> firstNames = new List<string>()
	{
		{"Tricia"}, 
		{"Sylvia"}, 
		{"Ward"}, 
		{"Joane"}, 
		{"Pamela"}, 
		{"Mackenzie"}, 
		{"Laurie"}, 
		{"Iesha"}, 
		{"Ramon"}, 
		{"Irwin"}, 
		{"Annelle"}, 
		{"Hester"}, 
		{"Terresa"}, 
		{"Ervin"}, 
		{"Charla"}, 
		{"Gertrude"}, 
		{"Hettie"}, 
		{"Shondra"}, 
		{"Shanti"}, 
		{"Denny"}, 
		{"Marshall"}, 
		{"Martin"}, 
		{"Lakeshia"}, 
		{"Brandi"}, 
		{"Ozell"}, 
		{"Brain"}, 
		{"Brendon"}, 
		{"Ok"}, 
		{"Rosanne"}, 
		{"Chet"}, 
		{"Remona"}, 
		{"Deandrea"}, 
		{"Blanche"}, 
		{"Lynell"}, 
		{"Lorette"}, 
		{"Arletta"}, 
		{"Celina"}, 
		{"Tameika"}, 
		{"Williams"}, 
		{"Loida"}, 
		{"Ronny"}, 
		{"Marcelene"}, 
		{"Arie"}, 
		{"Juan"}, 
		{"Deonna"}, 
		{"Indira"}, 
		{"Clemente"}, 
		{"Jerold"}, 
		{"Jamey"},
		{"Oscar"}
	};

	public static List<string> alphabet = new List<string>()
	{
		{ "a" },
		{ "b" },
		{ "c" },
		{ "d" },
		{ "e" },
		{ "f" },
		{ "g" },
		{ "h" },
		{ "i" },
		{ "j" },
		{ "k" },
		{ "l" },
		{ "m" },
		{ "n" },
		{ "o" },
		{ "p" },
		{ "q" },
		{ "r" },
		{ "s" },
		{ "t" },
		{ "u" },
		{ "v" },
		{ "w" },
		{ "x" },
		{ "y" },
		{ "z" }
	};
	
	public static string getFakeFirstName()
	{
		return firstNames[Random.Range(0, firstNames.Count)];	
	}

	public static string getFakeNameLastLetter()
	{
		return getFakeFirstName() + " " + alphabet[Random.Range(0, alphabet.Count)].ToUpper();
	}
}
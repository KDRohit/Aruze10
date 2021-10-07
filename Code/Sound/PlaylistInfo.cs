using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
PlaylistInfo stores the data concerning playlists, including playback usage flags.
*/
public class PlaylistInfo : IResetGame
{
	public string keyName { get; private set; }
	public bool randomStartTrack { get; private set; }
	public bool shuffleTracks { get; private set; }
	
	// Store the track keys as all uppercase since clip names ignore casing
	public List<string> tracks = null;
	private List<string> randomPool = null;
	private int nextTrackIndex;
	
	// Should a collection cycle through its tracks over and over again, or should they play each track once then stop?
	// Hopefully we'll get this from SCAT eventually.
	public bool shouldCycleTracks = true;
	
	// Store playlist keys as all uppercase since clip names ignore casing.
	// When find() is called for a playlist, the passed key is converted to uppercase to find a playlist.
	private static Dictionary<string, PlaylistInfo> all = new Dictionary<string, PlaylistInfo>();

	// A method that call's populate all and then returns the all dictionary called and cleans up what it used.
	public static Dictionary<string, PlaylistInfo>  testPopulateAll(JSON[] audioData, JSON[] audioListData)
	{
		// Make sure these are all the way reset.
		resetStaticClassData();
		AudioInfo.resetStaticClassData();

		AudioInfo.populateAll(audioData);
		populateAll(audioListData);
		Dictionary<string, PlaylistInfo> result = new Dictionary<string, PlaylistInfo>(all);
		// Put it back into a clean state.
		resetStaticClassData();
		AudioInfo.resetStaticClassData();
		return result;
	}
	
	// Populate all the audio list data using provided server data
	public static void populateAll(JSON[] audioListData)
	{
		foreach (JSON listEntry in audioListData)
		{
			string keyName = listEntry.getString("key_name", "");
			bool randomStartTrack = listEntry.getBool("random_start_track", true);
			bool shuffleTracks = listEntry.getBool("shuffle", true);
			
			List<string> tracks = new List<string>();
			foreach (string trackKey in listEntry.getStringArray("tracks"))
			{
				tracks.Add(trackKey); // uppercase the key since sound clip names won't care about casing.
			}
			
			if (tracks.Count != 0)
			{
				new PlaylistInfo(keyName, randomStartTrack, shuffleTracks, tracks);
			}
		}
	}
	
	public PlaylistInfo(
		string keyName,
		bool randomStartTrack,
		bool shuffleTracks,
		List<string> tracks)
	{
		this.keyName = keyName;
		this.randomStartTrack = randomStartTrack;
		this.shuffleTracks = shuffleTracks;
		this.tracks = tracks;
	
		reset();

		string keyNameUpper = keyName.ToUpper(); // uppercase the key for the dictionary since sound clip names won't care about casing.
		if (all.ContainsKey(keyNameUpper))
		{
			Debug.LogWarning("Duplicate PlaylistInfo key: " + keyNameUpper);
		}
		else
		{
			all.Add(keyNameUpper, this);
		}
	}

	// Peek at and return what the next track will be, without moving to that track
	public string peekNextTrack()
	{
		string nextTrack = null;
		
		if (shuffleTracks)
		{
			if (nextTrackIndex < randomPool.Count)
			{
				nextTrack = randomPool[nextTrackIndex];
			}
			else
			{
				Debug.LogWarning("PlayLlistInfo.peekNextTrack() - nextTrackIndex was outside the bounds of the randomPool list!");
			}
		}
		else
		{
			if (nextTrackIndex < tracks.Count)
			{
				nextTrack = tracks[nextTrackIndex];
			}
			else
			{
				Debug.LogWarning("PlaylistInfo.peekNextTrack() - nextTrackIndex was outside the bounds of the tracks list!");
			}
		}

		return nextTrack;
	}
	
	// Get the next track to play
	public string getNextTrack()
	{
		string nextTrack = null;
		
		if (shuffleTracks)
		{
			if (nextTrackIndex < randomPool.Count)
			{
				nextTrack = randomPool[nextTrackIndex];
			}
			else
			{
				Debug.LogWarning("PlayLlistInfo.getNextTrack() - nextTrackIndex was outside the bounds of the randomPool list!");
			}
		}
		else
		{
			if (nextTrackIndex < tracks.Count)
			{
				nextTrack = tracks[nextTrackIndex];
			}
			else
			{
				Debug.LogWarning("PlaylistInfo.getNextTrack() - nextTrackIndex was outside the bounds of the tracks list!");
			}
		}

		nextTrackIndex++;
		if (nextTrackIndex >= tracks.Count && shouldCycleTracks)
		{
			nextTrackIndex = 0;

			if (shuffleTracks)
			{
				// need to reshuffle the tracks since we've played through all of them
				randomPool = getRandomizedListCopy(isPreventingPlayingTrackTwice: true, lastTrackPlayed: nextTrack);
			}
		}
		
		return nextTrack;
	}

	// skips a track in the playlist, maybe you need to control which of them gets played for some reason
	public void skipTrack()
	{
		if (shuffleTracks)
		{
			nextTrackIndex = (nextTrackIndex + 1) % randomPool.Count;
		}
		else
		{
			nextTrackIndex = (nextTrackIndex + 1) % tracks.Count;
		}
	}
	
	// Resets a playlist to the beginning (whatever that means for any particular playlist)
	public void reset()
	{
		// only allow randomStartTrack if we aren't shuffling, otherwise it doesn't really do anything
		if (randomStartTrack && !shuffleTracks)
		{
			nextTrackIndex = Random.Range(0, tracks.Count);
		}
		else
		{
			nextTrackIndex = 0;
		}
		
		if (shuffleTracks)
		{
			randomPool = getRandomizedListCopy(isPreventingPlayingTrackTwice: false);
		}
	}
	
	// Gets a copy of the list of tracks, which is good for custom playlist usage.
	public List<string> getListCopy()
	{
		List<string> listCopy = new List<string>();
		foreach (string audioInfo in tracks)
		{
			listCopy.Add(audioInfo);
		}
		return listCopy;
	}

	// Gets a randomized list, for collections that are shuffled
	public List<string> getRandomizedListCopy(bool isPreventingPlayingTrackTwice, string lastTrackPlayed = null)
	{
		List<string> listCopy = getListCopy();
		CommonDataStructures.shuffleList<string>(listCopy);

		if (isPreventingPlayingTrackTwice)
		{
			// check if the first track in the newly shuffled list is the same as
			// the one we are about to return, and if so, randomize that track
			// somewhere else so we don't play the same track twice
			if (lastTrackPlayed != null && tracks.Count > 1 && listCopy[0] == lastTrackPlayed)
			{
				int newLocationForFirstTrack = Random.Range(1, tracks.Count);
				string firstTrack = listCopy[0];
				listCopy[0] = listCopy[newLocationForFirstTrack];
				listCopy[newLocationForFirstTrack] = firstTrack;
			}
		}

		return listCopy;
	}
	
	// Find a given PlaylistInfo using its key
	// Convert the key to uppercase to find the playlist in the dictionary.
	public static PlaylistInfo find(string key)
	{
		if(key != null && all.ContainsKey(key.ToUpper()))
		{
			return all[key.ToUpper()];
		}
		return null;
	}
	
	// Implements IResetGame
	public static void resetStaticClassData()
	{
		all = new Dictionary<string, PlaylistInfo>();
	}
}

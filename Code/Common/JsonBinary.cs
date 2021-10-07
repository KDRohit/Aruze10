using System.Collections.Generic;
using System.IO;
using System.Text;
using Google.Apis.Json;

/*

The Json "binary" format exists for high performance local caching of JSON data - in a binary format, small and fast to create/parse.

The utility functions here can convert a json structure to and from a byte array.

Since we're parsing externally provided data, lots can go wrong, so user code should catch exceptions and handle as a failure.

This binary format has a few helpful improvements over plain json text:
= De-duplication of all key & value strings (minimizing string parsing, duplicates are referenced by index)
= Strings are converted via .Net's byte-based UTF8 encoder, no extra escape character parsing needed.
= Use of a value-string cache, enabling most duplicate strings to use byte sized indices  (HIR has over 100,000 value strings)
= Key-string indices are efficiently encoded to use byte-size indexing (HIR doesn't need key cache, has ~800 unique keys)
= Objects & arrays include count sizes, so they are pre-sized at loadtime (preventing continual re-allocations caused by growth)

Background: Previously, HIR spent > 4.5 seconds on an iPad2 simply parsing/converting > 4 MB of text into json structures; 
most of this time reading (duplicate) strings, searching for escape characters, and re-allocating objects/arrays as they grew.

*/

public class JsonBinaryWriter
{	
	const int CACHE_SIZE = 256;					// cache size based on what a byte can index
	List<string> valueStringCache;				// the cache of valueStrings (the last 256 strings we've parsed or referenced)
	int cacheIndex;								// round-robin index to update the valueStringCache (from 0 - 255)
	
	Dictionary<string, int> keyTable;			// dictionary of keyStrings to index #
	Dictionary<string, ValueEntry> valueTable;	// dictionary of valueStrings to index # and (optional) cache index #
	MemoryStream outstream;						// the byte stream we are writing to (todo: presize?)
	Encoding stringEncoder;						// text encoding to use for strings (UTF8 / Unicode)
	
	class ValueEntry
	{
		public int indexInStringTable;			// keystring index
		public int indexIfCached;				// (optional) cache index, else -1 if not in cache
	}

	public byte[] jsonToBinary(object obj, JSON.Format format, string extraInfoString)
	{
		outstream = new MemoryStream();
		keyTable = new Dictionary<string, int>(); 
		valueTable = new Dictionary<string, ValueEntry>();
		valueStringCache = new List<string>(CACHE_SIZE);
		cacheIndex = 0;		
		
		// reserve space
		for(int i=0; i < CACHE_SIZE; i++) valueStringCache.Add(null);
		
		// setup string encoder
		if (format == JSON.Format.UTF8)
		{
			stringEncoder = new UTF8Encoding(false, true);
		}
		else
		{
			stringEncoder = new UnicodeEncoding(false, true);
		}
		
		// write to binary stream		
		writeHeaderPlaceholder();
		jsonToBinary_recurse(obj);
		emitString(extraInfoString);
		updateHeader(format);
		
		// done, return the byte[]
		return outstream.ToArray();
	}

	
	void jsonToBinary_recurse(object obj)
	{
		if (obj == null)
		{
			// This is a null object; emit "null" token
			outstream.WriteByte( (byte) TokenType.Null );
		}
		else if (obj is string)
		{
			// this is a 'value' string; emit an index if in MRU cache or previously used, else emit entire string
			var str = obj as string;
			
			ValueEntry valueEntry;
			if( valueTable.TryGetValue(str, out valueEntry) ) // re-using an existing string
			{
				if (valueEntry.indexIfCached >= 0)
				{
					// Currently in MRU cache, so emit a byteIndex (and no MRU update)
					outstream.WriteByte( (byte)TokenType.ValueStringByteIndex );
					outstream.WriteByte( (byte)valueEntry.indexIfCached);
				}
				else
				{
					// Not in MRU cache, but in stringTable, so emit short or int index
					var index = valueEntry.indexInStringTable;
					if (index <= 0xffff)
					{
						outstream.WriteByte( (byte) TokenType.ValueStringShortIndex );
						emitShort(index);
					}
					else
					{
						outstream.WriteByte( (byte) TokenType.ValueStringIntIndex );
						emitInt(index);
					}
					
					// update cache & references to it
					updateValueStringCache(valueEntry, str);
				}
			}
			else
			{
				// emit string token + string
				emitString (str);
				
				// track it in the valueTable
				valueEntry = new ValueEntry();
				valueEntry.indexInStringTable = valueTable.Count + CACHE_SIZE;
				valueEntry.indexIfCached = -1;
				valueTable[str] = valueEntry;
				
				// update cache & references to it
				updateValueStringCache(valueEntry, str);
			}
		}
		else if (obj is JsonDictionary)
		{
			// This is a json object; emit object token, property count, and all object key-value pairs
			var dict = obj as JsonDictionary;
			outstream.WriteByte( (byte)TokenType.Obj );
			emitVli(dict.Count, outstream);
			
			foreach (var pair in dict)
			{
				// Emit 'key' as a string (the first time), else an index to a previous string
				// Tokens exist for the first 768 and last 256 elements, for efficient encoding
				// Else encodes 2 or 4 byte indices. (HIR GlobalData has < 1000 unique keya)
				var key = pair.Key;
				var value = pair.Value;
				
				int index;
				if( keyTable.TryGetValue(key, out index) )
				{
					// Key exists, emit the smallest token & index possible
					if (index < 256)
					{
						outstream.WriteByte( (byte)TokenType.KeyStringByteIndex );
						outstream.WriteByte( (byte)index);
					}
					
					else if (index < 512)
					{
						outstream.WriteByte( (byte)TokenType.KeyStringByteIndexMinus256 );
						outstream.WriteByte( (byte)(index-256));
					}
					else if (index < 768)
					{
						outstream.WriteByte( (byte)TokenType.KeyStringByteIndexMinus512 );
						outstream.WriteByte( (byte)(index-512));
					}
					
					else if (keyTable.Count - index - 1 <= 255)
					{
						outstream.WriteByte( (byte)TokenType.KeyStringByteIndexFromEnd );
						outstream.WriteByte( (byte)(keyTable.Count - index - 1));
					}
					else if (index <= 0xffff)
					{
						outstream.WriteByte( (byte)TokenType.KeyStringShortIndex );
						emitShort(index);
					}
					else
					{
						outstream.WriteByte( (byte)TokenType.KeyStringIntIndex );
						emitInt(index);
					}
				}
				else
				{
					// emit string token + string, and add it to the keyTable for possible re-use
					emitString(key);
					keyTable[key] = keyTable.Count;
				}
				
				// and recurse to encode each KVP value
				jsonToBinary_recurse(value); //RECURSE
			}
		} 
		else if (obj is List<object>)
		{
			// This is a json array; emit array token, size, and then each element item
			var list = obj as List<object>;
			outstream.WriteByte( (byte)TokenType.Array );
			emitVli(list.Count, outstream);
			
			foreach (var value in list)
			{
				jsonToBinary_recurse(value); //RECURSE
			}
		} 
		else if (obj is bool)
		{
			// This is a bool; emit "true bool" or "false bool" token
			outstream.WriteByte( (byte) ((bool)obj ? TokenType.True : TokenType.False) );
		} 
		else
		{
			throw new System.Exception("jsonToBinary_recurse() - unexpected obj type: " + ((obj!=null) ? obj.GetType().ToString() : "<null>") );
		}
	}
	
	//.......................................................................................................
	// The "value cache" puts recently accessed value-strings in the first 256 entries of the string table,
	// allowing for small (1 byte) indices, instead of 2 or 4 bytes per index. (HIR has over 100,000 value refs).
	//
	// For rapid performance, we write to a 256-element cache in a round-robin manner, instead of trying to
	// purge the oldest entry. This saves us having to continually search or delete/shift elements in an array,
	// (this imperfect MRU is fast to encode & only costs us a couple KB).
	
	void updateValueStringCache(ValueEntry valueEntry, string str)
	{
		// If we're overwriting a cache entry, remove existing dictionary reference to it					
		if ( valueStringCache[cacheIndex] != null)
		{
			ValueEntry prevEntry = valueTable[ valueStringCache[cacheIndex] ];
			prevEntry.indexIfCached = -1;
		}
		
		// add to the valueStringCache array and remember the index
		valueStringCache[cacheIndex] = str;
		valueEntry.indexIfCached = cacheIndex;
		cacheIndex = (cacheIndex+1) % CACHE_SIZE;
	}	
	
	//.......................................................................................................
	// Helper functions that emit shorts, ints, strings, etc. to our current output memorystream
	
	void emitShort(int value)
	{
		outstream.WriteByte( (byte)(value >> 8) );
		outstream.WriteByte( (byte)value );
	}
	
	void emitInt(int value)
	{
		outstream.WriteByte( (byte)(value >> 24) );
		outstream.WriteByte( (byte)(value >> 16) );
		outstream.WriteByte( (byte)(value >> 8) );
		outstream.WriteByte( (byte)value );
	}
	
	void emitString(string str)
	{
		// emit string token, string length, and string bytes			
		outstream.WriteByte( (byte)TokenType.String );
		byte[] bytes = stringEncoder.GetBytes(str); //allocates mem :(
		emitVli(bytes.Length, outstream);
		outstream.Write(bytes, 0, bytes.Length);
	}	
	
	// Variable-length encoding of integers; favoring small (byte-size) values
	// Of all the string lengths in globaldata, 358032 are < 255, 9 are larger
	// So, encode a value as:
	//  if <=     253: encode as 1 byte value
	//  if <= 16 bits: encode '254' token followed by 2 byte value   
	//  else         : encode '255' token followed by 4 byte value
	// (the vli reader function must match the writer function)
		
	void emitVli(int intValue, MemoryStream outstream)
	{
		uint value = (uint)intValue; 
		
		if (value <= 253)
		{
			outstream.WriteByte( (byte)value );
		}
		else if (value <= 0x0000ffff)
		{
			outstream.WriteByte( 254 );
			outstream.WriteByte( (byte)(value >> 8) );
			outstream.WriteByte( (byte)value );
		}
		else
		{
			outstream.WriteByte( 255 );
			outstream.WriteByte( (byte)(value >> 24) );
			outstream.WriteByte( (byte)(value >> 16) );
			outstream.WriteByte( (byte)(value >> 8) );
			outstream.WriteByte( (byte)value );
		}
	}
	
	// Header consists of:
	// int : JSON.Format enum (0: UTF8, 1: Unicode)
	// int : Number of key strings
	// int : Number of value strings
	
	void writeHeaderPlaceholder()
	{
		emitInt(0);
		emitInt(0);
		emitInt(0);
	}
	
	void updateHeader(JSON.Format format)
	{
		outstream.Position = 0;
		emitInt( (int)format );
		emitInt( keyTable.Count );
		emitInt( valueTable.Count );
	} 
	
}	
	
//=======================================================================================================	

public class JsonBinaryReader
{
	const int CACHE_SIZE = 256;		// cache size based on what a byte can index
	int cacheIndex;					// round-robin index to update the valueString cache (from 0 - 255)
	
	List<string> keyStrings;		// all keyStrings parsed so far (duplicate references index into this)		
	List<string> valueStrings;		// all ValueStrings parsed so far (first 256 reserved for MRU cache)
	MemoryStream instream;			// the byte stream we are reading from
	byte[] rawBytes;				// the raw byte array we a reading from
	Encoding stringDecoder;			// text encoding to use for strings (UTF8 / Unicode)
	
	public JsonDictionary binaryToJson(byte[] bytes)
	{
		rawBytes = bytes;
		instream = new MemoryStream(bytes);

		JSON.Format format;
		int numKeys, numValues;
		readHeader(out format, out numKeys, out numValues);
		keyStrings = new List<string>(numKeys);
		valueStrings = new List<string>(numValues);
		cacheIndex = 0;
		
		// reserve cache space at beginning of valueString array
		for(int i=0; i < CACHE_SIZE; i++) valueStrings.Add(null);
		
		// setup string decoder
		if (format == JSON.Format.UTF8)
		{
			stringDecoder = new UTF8Encoding(false, true);
		}
		else
		{
			stringDecoder = new UnicodeEncoding(false, true);
		}
		
		// do the work
		return binaryToJson_recurse() as JsonDictionary;
	}

		
	object binaryToJson_recurse()
	{
		var token = (TokenType) instream.ReadByte();
		switch(token)
		{
			case TokenType.True:		
				return true;
				
			case TokenType.False:
				return false;
				
			case TokenType.Null:
				return null;
				
			case TokenType.String:
				string str = readString();
				valueStrings.Add(str);
				valueStrings[cacheIndex++ % CACHE_SIZE] = str; //update cache
				return str;
				
			case TokenType.ValueStringByteIndex:
				int index = instream.ReadByte(); 
				// no MRU update when reading from cache; fastest
				return valueStrings[index];			
				
			case TokenType.ValueStringShortIndex:
				index = readShort();
				valueStrings[cacheIndex++ % CACHE_SIZE] = valueStrings[index]; //update cache
				return valueStrings[index];
				
			case TokenType.ValueStringIntIndex:
				index = readInt();				
				valueStrings[cacheIndex++ % CACHE_SIZE] = valueStrings[index]; //update cache
				return valueStrings[index];

			case TokenType.Array:
				var arraySize = readVli();
				var array = new List<object>(arraySize);
				
				for(int i=0; i < arraySize; i++)
				{
					object val = binaryToJson_recurse(); //RECURSE
					array.Add( val );
				}
				return array;
												
			case TokenType.Obj:
				// create json dictionary/object...
				var dictSize = readVli();
				var dict = new JsonDictionary(dictSize);
								
				for(int i=0; i < dictSize; i++)
				{
					// parse dictionary key (either a string or an string index)
					string key = "";
					token = (TokenType) instream.ReadByte();
					
					switch(token)
					{
						case TokenType.String:
							key = readString ();
							keyStrings.Add(key);
							break;
							
						case TokenType.KeyStringByteIndex:
							key = keyStrings[instream.ReadByte()];
							break;
							
						case TokenType.KeyStringByteIndexMinus256:
							key = keyStrings[instream.ReadByte() + 256];
							break;
							
						case TokenType.KeyStringByteIndexMinus512:
							key = keyStrings[instream.ReadByte() + 512];
							break;
							
						case TokenType.KeyStringByteIndexFromEnd:
							key = keyStrings[keyStrings.Count - instream.ReadByte() - 1];
							break;
							
						case TokenType.KeyStringShortIndex:
							key = keyStrings[readShort()];
							break;
							
						case TokenType.KeyStringIntIndex:
							key = keyStrings[readInt()];
							break;
							
						default:	
							throw new System.Exception("binaryToJson_recurse() - unexpected keytoken: " + token);
					}
					
					object val = binaryToJson_recurse(); //RECURSE
					dict.Add( key, val );
				}								
				return dict;
		}
		
		// shouldn't get here...
		throw new System.Exception("binaryToJson_recurse() - unexpected token: " + token);
	}
	
	
	//.......................................................................................................
	// Helper functions that read shorts, ints, strings, etc. from our current input memorystream

	int readShort()
	{
		return (instream.ReadByte() << 8) | instream.ReadByte(); 
	}
	
	int readInt()
	{
		return  (instream.ReadByte() << 24) +
				(instream.ReadByte() << 16) +
				(instream.ReadByte() << 8) +
				(instream.ReadByte());	
	}
	
	string readString()
	{
		int stringSize = readVli();
		var str = stringDecoder.GetString(rawBytes, (int)instream.Position, stringSize);
		instream.Position += stringSize;
		return str;
	}
	
	// Variable-length encoding of integers; favoring small (byte-size) values
	// Of all the string lengths in globaldata, 358032 are < 255, 9 are larger
	// So, encode a value as:
	//  if <=     253: encode as 1 byte value
	//  if <= 16 bits: encode '254' token followed by 2 byte value   
	//  else         : encode '255' token followed by 4 byte value
	// (the vli reader function must match the writer function)
	
	int readVli()
	{
		var val = instream.ReadByte();
		if (val <= 253)
		{
			return val;
		}
		else if (val == 254)
		{
			return (instream.ReadByte() << 8) | instream.ReadByte(); 
		}
		else
		{
			return  (instream.ReadByte() << 24) +
					(instream.ReadByte() << 16) +
					(instream.ReadByte() << 8) +
					(instream.ReadByte());					
		}
	}

	// Header consists of:
	// int : JSON.Format enum (0: UTF8, 1: Unicode)
	// int : Number of key strings
	// int : Number of value strings

	void readHeader(out JSON.Format format, out int numKeys, out int numValues)
	{
		format = (JSON.Format)readInt();
		numKeys = readInt();
		numValues = readInt();
	}
	
}

//=======================================================================================================	
// ** WARNING ** DO NOT change the existing enum values, as they are persisted in datafiles.
// I've even left some gaps if you really want to add some tokens between existing groups.

enum TokenType : byte
{
	Unused                      = 0, //DO NOT CHANGE EXISTING VALUES Todd
	String                      = 1,
	True                        = 2,
	False                       = 3,
	Obj                         = 4,
	Array                       = 5,
	Null                        = 6,
	
	KeyStringByteIndex          = 10,
	KeyStringByteIndexMinus256  = 11,
	KeyStringByteIndexMinus512  = 12,
	KeyStringByteIndexFromEnd   = 13,
	KeyStringShortIndex         = 14,
	KeyStringIntIndex           = 15,
	
	ValueStringByteIndex        = 20,
	ValueStringShortIndex       = 21,
	ValueStringIntIndex         = 22,
}


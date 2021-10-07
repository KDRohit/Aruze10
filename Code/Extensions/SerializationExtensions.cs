using System.Runtime.Serialization;

public static class SerializationInfoExtensions
{
	public static bool TryGetValue<T>(this SerializationInfo serializationInfo, string name, out T value)
	{
		try
		{
			value = (T)serializationInfo.GetValue(name, typeof(T));
			return true;
		}
		catch (SerializationException)
		{
			value = default(T);
			return false;
		}
	}
}

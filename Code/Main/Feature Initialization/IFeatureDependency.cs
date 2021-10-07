namespace Com.Initialization
{
	public interface IFeatureDependency
	{
		bool isInitialized { get; }
		bool canInitialize { get; }
		bool isSkipped      { get; }
		void init();
	}
}
namespace RealtimeCSG.Foundation
{
    internal static class Versioning
    {
	#if TEST_ENABLED
	    public const string PluginVersion = "TEST";
        public const string PrevPluginVersion = "1_559";
	#else
        public const string PluginVersion = "1_559";
	#endif
    }
}
#if DEBUG

[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(Foster.Framework.HotReloadHandler))]

namespace Foster.Framework;

internal static class HotReloadHandler
{
	public static void UpdateApplication(Type[]? _)
		=> App.OnHotReload?.Invoke();
}

#endif

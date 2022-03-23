using RealtimeCSG.Components;

namespace RealtimeCSG
{
	internal partial class InternalCSGModelManager
	{ 
		internal static void ClearCaches()
		{
			foreach (var brush in Brushes)
				brush.ClearCache();
			
			foreach (var operation in Operations)
				operation.ClearCache();
			
			foreach (var model in Models)
				model.ClearCache();
		}
	}
}
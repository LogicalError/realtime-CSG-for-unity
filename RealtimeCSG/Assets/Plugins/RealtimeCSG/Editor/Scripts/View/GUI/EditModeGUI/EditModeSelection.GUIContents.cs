using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	internal sealed partial class EditModeSelectionGUI
	{
		static GUIContent	ContentTitleLabel					= new GUIContent("Realtime CSG");
		static GUIContent	HandleAsOneLabel					= new GUIContent("Handle as one object", "Select this operation when a child is selected");
		static GUIContent	PrefabLabelContent					= new GUIContent("Drag & drop behaviour");
		static GUIContent	RaySnappingLabelContent				= new GUIContent("Ray snapping behaviour");

		static GUIContent	PrefabInstantiateBehaviourContent	= new GUIContent("Instantiation");
		static GUIContent	DestinationAlignmentContent			= new GUIContent("Placement");
		static GUIContent	SourceAlignmentContent				= new GUIContent("Front");
//		static GUIContent	ContentLayerContent					= new GUIContent("Contents");
		static GUIContent	SurfacesContent						= new GUIContent("Surfaces");
		
		
		static ToolTip		RaySnappingBehaviourTooltip			= new ToolTip("Ray snapping behaviour", "Here you can set how your object will align to the surface underneath it when dragging it while ray-snapping (holding Shift)");
		
		static ToolTip		PrefabInstantiateBehaviourTooltip	= new ToolTip("Instantiation", "Here you can set if your prefab is copied into the scene without any link to the original prefab, or if it'll be an instance of your prefab (default unity behaviour).");
		static ToolTip		DestinationAlignmentTooltip			= new ToolTip("Placement", "Here you can set how your object will be rotated if it's dragged over a brush surface.\n\n"+
																							   "Align to Surface:\nRotate it so that it's aligned with the surface you drag over.\n\n"+
																							   "Always face up:\nAlign it with the surface but always make it face 'up'.\n\n" +
																							   "None:\ndo not rotate it along the surface.");
		static ToolTip		SourceAlignmentTooltip				= new ToolTip("Front", "Define here what direction the original object was created and what is considered it's 'front', this helps align it properly during placement.");
//		static ToolTip		ContentLayerTooltip					= new ToolTip("Content (experimental)", "What type of brush this is and how it interacts with other brushes.\n\n"+
//																			  "This is an experimental feature and will be improved over time.\n\n" +
//																			  "Currently Water brushes need to be defined before Glass brushes, and both need to be defined before Solid brushes for this to work.");

//		static GUIContent[]	ContentLayerTexts =
//		{
//			new GUIContent("Solid"),
//			new GUIContent("Glass"),
//			new GUIContent("Water")
//		};

		static GUIContent	VersionLabel						= new GUIContent(RealtimeCSG.Foundation.CSGManager.VersionName);
		static GUIContent DisabledLabelContent					= new GUIContent("Realtime CSG is disabled, press control-F3 to enable");
		static GUIContent EnableRealtimeCSGContent              = new GUIContent("Enable Realtime-CSG");
	}
}

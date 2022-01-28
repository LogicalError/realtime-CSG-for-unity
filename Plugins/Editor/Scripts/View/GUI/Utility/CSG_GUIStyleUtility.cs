using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG
{

	internal sealed class CSG_Skin
	{
		public GUIContent[] hierarchyOperations	= new GUIContent[CSG_GUIStyleUtility.operationTypeCount];
		public GUIContent hierarchyPassThrough;
		
		public GUIContent[] operationNames		= new GUIContent[CSG_GUIStyleUtility.operationTypeCount];
		public GUIContent[] operationNamesOn	= new GUIContent[CSG_GUIStyleUtility.operationTypeCount];
		
		public GUIContent[] shapeModeNames		= new GUIContent[CSG_GUIStyleUtility.shapeModeCount];
		public GUIContent[] shapeModeNamesOn	= new GUIContent[CSG_GUIStyleUtility.shapeModeCount];

		public GUIContent passThrough;
		public GUIContent passThroughOn;
		
		public GUIContent wireframe;
		public GUIContent wireframeOn;
		
		public GUIContent[] clipNames			= new GUIContent[CSG_GUIStyleUtility.clipTypeCount];
		public GUIContent[] clipNamesOn			= new GUIContent[CSG_GUIStyleUtility.clipTypeCount];

		public GUIContent rebuildIcon;

		public GUIContent gridIcon;
		public GUIContent gridIconOn;

		public GUIContent gridSnapIcon;
		public GUIContent gridSnapIconOn;
        public GUIContent relSnapIcon;
        public GUIContent relSnapIconOn;
        public GUIContent noSnapIcon;
        public GUIContent noSnapIconOn;
        public GUIStyle   messageStyle;
		public GUIStyle   messageWarningStyle;

		public Color lockedBackgroundColor;
		public GUIStyle	xToolbarButton;
		public GUIStyle	yToolbarButton;
		public GUIStyle	zToolbarButton;

		public GUIStyle redToolbarDropDown;

		public GUIStyle menuItem;
	};

	internal static class CSG_GUIStyleUtility
	{
		internal const int operationTypeCount = 3;
		internal const int shapeModeCount = ((int)ShapeMode.Last) + 1;
		internal const int clipTypeCount = 3;


		static bool stylesInitialized = false;

		public static string[] brushEditModeNames;
		public static GUIContent[] brushEditModeContent;
		public static ToolTip[] brushEditModeTooltips;
		public static ToolEditMode[] brushEditModeValues;

		public const float BottomToolBarHeight = 17;
		public static GUIStyle BottomToolBarStyle;

		internal const float kSingleLineHeight = 16;
		public static GUIStyle emptyMaterialStyle = null;
		public static GUIStyle unselectedIconLabelStyle = null;
		public static GUIStyle selectedIconLabelStyle = null;

		public static GUIStyle selectionRectStyle;
		public static GUIStyle redTextArea;
		public static GUIStyle redTextLabel;
		public static GUIStyle redButton;
		public static GUIStyle wrapLabel;

		public static GUIStyle versionLabelStyle;

		public static GUIStyle toolTipTitleStyle;
		public static GUIStyle toolTipContentsStyle;
		public static GUIStyle toolTipKeycodesStyle;

		public static GUIStyle sceneTextLabel;
		
		public static GUIStyle rightAlignedLabel;

		public static GUIStyle unpaddedWindow;

		public static GUILayoutOption[] ContentEmpty = new GUILayoutOption[0];


		public static ToolTip PopOutTooltip = new ToolTip("Pop out tool window",
														  "Click this to turn this into a floating tool window.\n" +
														  "Close that tool window to get the in scene tool window back", new KeyEvent(KeyCode.F2, EventModifiers.Control));

		const string csgAdditionTooltip = "Addition";
		const string csgSubtractionTooltip = "Subtraction";
		const string csgIntersectionTooltip = "Intersection";

		static readonly GUIContent[] proInActiveOperationTypes = new GUIContent[]
			{
				IconContent("icon_pro_csg_addition_small"    ),
				IconContent("icon_pro_csg_subtraction_small" ),
				IconContent("icon_pro_csg_intersection_small")
			};

		static readonly GUIContent[] proActiveOperationTypes = new GUIContent[]
			{
				IconContent("icon_pro_csg_addition_small_on"    ),
				IconContent("icon_pro_csg_subtraction_small_on" ),
				IconContent("icon_pro_csg_intersection_small_on")
			};

		static readonly GUIContent[] personalInActiveOperationTypes = new GUIContent[]
			{
				IconContent("icon_pers_csg_addition_small"    ),
				IconContent("icon_pers_csg_subtraction_small" ),
				IconContent("icon_pers_csg_intersection_small")
			};

		static readonly GUIContent[] personalActiveOperationTypes = new GUIContent[]
			{
				IconContent("icon_pers_csg_addition_small_on"    ),
				IconContent("icon_pers_csg_subtraction_small_on" ),
				IconContent("icon_pers_csg_intersection_small_on")
			};

		private static readonly string[] operationText = new string[]
			{
				" Addition",
				" Subtraction",
				" Intersection"
			};

		public static ToolTip[] operationTooltip = new ToolTip[]
			{
				new ToolTip("Additive CSG Operation", "Set the selection to be additive", Keys.MakeSelectedAdditiveKey),
				new ToolTip("Subtractive CSG operation", "Set the selection to be subtractive", Keys.MakeSelectedSubtractiveKey),
				new ToolTip("Intersecting CSG operation", "Set the selection to be an intersection operation", Keys.MakeSelectedIntersectingKey)
			};


		const string csgPassthroughTooltip = "PassThrough|No CSG operation";

		static readonly GUIContent proPassThrough = IconContent("icon_pro_pass_through");
		static readonly GUIContent proPassThroughOn = IconContent("icon_pro_pass_through_on");

		static readonly GUIContent personalPassThrough = IconContent("icon_pers_pass_through");
		static readonly GUIContent personalPassThroughOn = IconContent("icon_pers_pass_through_on");

		private static readonly string passThroughText = " Pass through";
		public static readonly ToolTip passThroughTooltip = new ToolTip("Perform no CSG operation", "No operation is performed. Child nodes act as if there is no operation above it. This is useful to group different kinds of nodes with.", Keys.MakeSelectedPassThroughKey);



		static readonly GUIContent proWireframe = IconContent("icon_pro_wireframe");
		static readonly GUIContent proWireframeOn = IconContent("icon_pro_wireframe_on");

		static readonly GUIContent personalWireframe = IconContent("icon_pers_wireframe");
		static readonly GUIContent personalWireframeOn = IconContent("icon_pers_wireframe_on");

		private static readonly string wireframeTooltip = "Show/Hide brush wireframe";



		static readonly GUIContent[] proInActiveShapeModes = new GUIContent[shapeModeCount]
			{
				GUIContent.none,	//IconContent("icon_pro_free_draw"),
				GUIContent.none,	//IconContent("icon_pro_cylinder")
				GUIContent.none,	//IconContent("icon_pro_box")
                GUIContent.none,
				//GUIContent.none,
				GUIContent.none
			};

		static readonly GUIContent[] proActiveShapeModes = new GUIContent[shapeModeCount]
			{
				GUIContent.none,	//IconContent("icon_pro_free_draw_on"),
				GUIContent.none,	//IconContent("icon_pro_cylinder_on"),
				GUIContent.none,	//IconContent("icon_pro_box_on")
                GUIContent.none,
				//GUIContent.none,
				GUIContent.none
			};

		static readonly GUIContent[] personalInActiveShapeModes = new GUIContent[shapeModeCount]
			{
				GUIContent.none,	//IconContent("icon_pers_free_draw"),
				GUIContent.none,	//IconContent("icon_pers_cylinder")
				GUIContent.none, 	//IconContent("icon_pers_box")
                GUIContent.none,
				//GUIContent.none,
				GUIContent.none
			};

		static readonly GUIContent[] personalActiveShapeModes = new GUIContent[shapeModeCount]
			{
				GUIContent.none,	//IconContent("icon_pers_free_draw_on"),
				GUIContent.none,	//IconContent("icon_pers_cylinder_on")
				GUIContent.none,	//IconContent("icon_pers_box_on")
                GUIContent.none,
				//GUIContent.none,
				GUIContent.none
			};

		private static readonly string[] shapeModeText = new string[shapeModeCount]
			{
				"Free-draw",
				"Box",
				"Sphere",
				"Cylinder",
				//"Spiral Stairs",
				"Linear Stairs"
			};

		public static readonly ToolTip[] shapeModeTooltips = new ToolTip[shapeModeCount]
			{
				new ToolTip("Free-draw brush", "Use this to draw a 2D shape and extrude it,\noptionally with curves by double clicking on edges.", Keys.FreeBuilderMode),
				new ToolTip("Create Box brush", "Use this to create boxes", Keys.BoxBuilderMode),
				new ToolTip("Create (Hemi)Sphere brush", "Use this to create (hemi)spheres", Keys.SphereBuilderMode),
				new ToolTip("Create Cylinder brush", "Use this to create cylinders", Keys.CylinderBuilderMode),
				//new ToolTip("Create Spiral Stairs", "Use this to create spiral stairs", Keys.SpiralStairsBuilderMode),
				new ToolTip("Create Linear Stairs", "Use this to create linear stairs", Keys.LinearStairsBuilderMode)
			};



		static readonly GUIContent[] proInActiveClipTypes = new GUIContent[]
			{
				IconContent("icon_pro_remove_front"     ),
				IconContent("icon_pro_remove_behind"    ),
				IconContent("icon_pro_split"            )
			};

		static readonly GUIContent[] proActiveClipTypes = new GUIContent[]
			{
				IconContent("icon_pro_remove_front_on"  ),
				IconContent("icon_pro_remove_behind_on" ),
				IconContent("icon_pro_split_on"         )
			};

		static readonly GUIContent[] personalInActiveClipTypes = new GUIContent[]
			{
				IconContent("icon_pers_remove_front"    ),
				IconContent("icon_pers_remove_behind"   ),
				IconContent("icon_pers_split"           )
			};

		static readonly GUIContent[] personalActiveClipTypes = new GUIContent[]
			{
				IconContent("icon_pers_remove_front_on" ),
				IconContent("icon_pers_remove_behind_on"),
				IconContent("icon_pers_split_on"        )
			};

		private static readonly string[] clipText = new string[]
			{
				" Remove in front",
				" Remove behind",
				" Split"
			};

		public static readonly ToolTip[] clipTooltips = new ToolTip[]
			{
				new ToolTip("Remove in front", "Remove the area in front of the created clipping plane from the selected brushes"),
				new ToolTip("Remove behind", "Remove the area behind the created clipping plane from the selected brushes"),
				new ToolTip("Split", "Split the selected brushes with the created splitting plane")
			};

		//const string rebuildTooltip		= "Rebuild all CSG geometry";
		//const string gridTooltip		= "Turn grid on/off | Shift-G";
		//const string gridOnTooltip		= "Turn grid on/off | Shift-G";
		//const string snappingTooltip	= "Turn automatic snap to grid on/off | Shift-T";
		//const string snappingOnTooltip	= "Turn automatic snap to grid on/off | Shift-T";

		static GUIContent proRebuildIcon        = IconContent("icon_pro_rebuild");
		static GUIContent proGridIcon           = IconContent("icon_pro_grid");
		static GUIContent proGridIconOn         = IconContent("icon_pro_grid_on");
		static GUIContent proGridSnapIcon       = IconContent("icon_pro_gridsnap");
		static GUIContent proGridSnapIconOn     = IconContent("icon_pro_gridsnap_on");
        static GUIContent proRelSnapIcon        = IconContent("icon_pro_relsnap");
        static GUIContent proRelSnapIconOn      = IconContent("icon_pro_relsnap_on");
        static GUIContent proNoSnapIcon         = IconContent("icon_pro_nosnap");
        static GUIContent proNoSnapIconOn       = IconContent("icon_pro_nosnap_on");

        static GUIContent personalRebuildIcon    = IconContent("icon_pers_rebuild");
		static GUIContent personalGridIcon       = IconContent("icon_pers_grid");
		static GUIContent personalGridIconOn     = IconContent("icon_pers_grid_on");
		static GUIContent personalGridSnapIcon   = IconContent("icon_pers_gridsnap");
		static GUIContent personalGridSnapIconOn = IconContent("icon_pers_gridsnap_on");
        static GUIContent personalRelSnapIcon    = IconContent("icon_pers_relsnap");
        static GUIContent personalRelSnapIconOn  = IconContent("icon_pers_relsnap_on");
        static GUIContent personalNoSnapIcon     = IconContent("icon_pers_nosnap");
        static GUIContent personalNoSnapIconOn   = IconContent("icon_pers_nosnap_on");


        static CSG_Skin Pro = new CSG_Skin();
		static CSG_Skin Personal = new CSG_Skin();

		public static CSG_Skin Skin
		{
			get
			{
				return (EditorGUIUtility.isProSkin) ? CSG_GUIStyleUtility.Pro : CSG_GUIStyleUtility.Personal;
			}
		}

		public static void InitStyles()
		{
			if (stylesInitialized)
				return;

			var oldSkin = GUI.skin;
			stylesInitialized = true;
			SetDefaultGUISkin();

			var whiteTexture = MaterialUtility.CreateSolidColorTexture(8, 8, Color.white);

			sceneTextLabel = new GUIStyle(GUI.skin.textArea);
			sceneTextLabel.richText = true;
			sceneTextLabel.onActive.background =
			sceneTextLabel.onFocused.background =
			sceneTextLabel.onHover.background =
			sceneTextLabel.onNormal.background = whiteTexture;
			sceneTextLabel.onActive.textColor =
			sceneTextLabel.onFocused.textColor =
			sceneTextLabel.onHover.textColor =
			sceneTextLabel.onNormal.textColor = Color.black;


			var toolTipStyle = new GUIStyle(GUI.skin.textArea);
			toolTipStyle.richText = true;
			toolTipStyle.wordWrap = true;
			toolTipStyle.stretchHeight = true;
			toolTipStyle.padding.left += 4;
			toolTipStyle.padding.right += 4;
			toolTipStyle.clipping = TextClipping.Overflow;

			toolTipTitleStyle = new GUIStyle(toolTipStyle);
			toolTipTitleStyle.padding.top += 4;
			toolTipTitleStyle.padding.bottom += 2;
			toolTipContentsStyle = new GUIStyle(toolTipStyle);
			toolTipKeycodesStyle = new GUIStyle(toolTipStyle);
			toolTipKeycodesStyle.padding.top += 2;


			rightAlignedLabel = new GUIStyle();
			rightAlignedLabel.alignment = TextAnchor.MiddleRight;

			unpaddedWindow = new GUIStyle(GUI.skin.window);
			unpaddedWindow.padding.left = 2;
			unpaddedWindow.padding.bottom = 2;


			emptyMaterialStyle = new GUIStyle(GUIStyle.none);
			emptyMaterialStyle.normal.background = MaterialUtility.CreateSolidColorTexture(2, 2, Color.black);


			selectionRectStyle = GetStyle("selectionRect");


			var redToolbarDropDown = GetStyle("toolbarDropDown");

			Pro.redToolbarDropDown = new GUIStyle(redToolbarDropDown);
			//Pro.redToolbarDropDown.normal.background = MaterialUtility.CreateSolidColorTexture(2, 2, Color.red);
			Pro.redToolbarDropDown.normal.textColor = Color.Lerp(Color.red, redToolbarDropDown.normal.textColor, 0.5f);
			Pro.redToolbarDropDown.onNormal.textColor = Color.Lerp(Color.red, redToolbarDropDown.onNormal.textColor, 0.125f);
			Personal.redToolbarDropDown = new GUIStyle(redToolbarDropDown);
			//Personal.redToolbarDropDown.normal.background = MaterialUtility.CreateSolidColorTexture(2, 2, Color.red);
			Personal.redToolbarDropDown.normal.textColor = Color.Lerp(Color.red, redToolbarDropDown.normal.textColor, 0.5f);
			Personal.redToolbarDropDown.onNormal.textColor = Color.Lerp(Color.red, redToolbarDropDown.onNormal.textColor, 0.125f);


			Pro.menuItem = GetStyle("MenuItem");
			Personal.menuItem = GetStyle("MenuItem");

			Pro.lockedBackgroundColor = Color.Lerp(Color.white, Color.red, 0.5f);

			Pro.xToolbarButton = new GUIStyle(EditorStyles.toolbarButton);
			Pro.xToolbarButton.normal.textColor = Color.Lerp(Handles.xAxisColor, Color.gray, 0.75f);
			Pro.xToolbarButton.onNormal.textColor = Color.Lerp(Handles.xAxisColor, Color.white, 0.125f);

			Pro.yToolbarButton = new GUIStyle(EditorStyles.toolbarButton);
			Pro.yToolbarButton.normal.textColor = Color.Lerp(Handles.yAxisColor, Color.gray, 0.75f);
			Pro.yToolbarButton.onNormal.textColor = Color.Lerp(Handles.yAxisColor, Color.white, 0.125f);

			Pro.zToolbarButton = new GUIStyle(EditorStyles.toolbarButton);
			Pro.zToolbarButton.normal.textColor = Color.Lerp(Handles.zAxisColor, Color.gray, 0.75f);
			Pro.zToolbarButton.onNormal.textColor = Color.Lerp(Handles.zAxisColor, Color.white, 0.125f);

			Personal.lockedBackgroundColor = Color.Lerp(Color.black, Color.red, 0.5f);

			Personal.xToolbarButton = new GUIStyle(EditorStyles.toolbarButton);
			Personal.xToolbarButton.normal.textColor = Color.Lerp(Handles.xAxisColor, Color.white, 0.75f);
			Personal.xToolbarButton.onNormal.textColor = Color.Lerp(Handles.xAxisColor, Color.black, 0.25f);

			Personal.yToolbarButton = new GUIStyle(EditorStyles.toolbarButton);
			Personal.yToolbarButton.normal.textColor = Color.Lerp(Handles.yAxisColor, Color.white, 0.75f);
			Personal.yToolbarButton.onNormal.textColor = Color.Lerp(Handles.yAxisColor, Color.black, 0.25f);

			Personal.zToolbarButton = new GUIStyle(EditorStyles.toolbarButton);
			Personal.zToolbarButton.normal.textColor = Color.Lerp(Handles.zAxisColor, Color.white, 0.75f);
			Personal.zToolbarButton.onNormal.textColor = Color.Lerp(Handles.zAxisColor, Color.black, 0.25f);

			redTextArea = new GUIStyle(GUI.skin.textArea);
			redTextArea.normal.textColor = Color.red;

			redTextLabel = new GUIStyle(GUI.skin.label);
			redTextLabel.normal.textColor = Color.red;
			redTextLabel.richText = true;
			redTextLabel.wordWrap = true;

			redButton = new GUIStyle(GUI.skin.button);
			redButton.normal.textColor = Color.red;

			wrapLabel = new GUIStyle(GUI.skin.label);
			wrapLabel.wordWrap = true;



			versionLabelStyle = new GUIStyle(EditorStyles.label);
			versionLabelStyle.alignment = TextAnchor.MiddleRight;
			//versionLabelStyle.fontSize = versionLabelStyle.font.fontSize - 1; 
			var original_color = versionLabelStyle.normal.textColor; 
			original_color.a = 0.4f;
			versionLabelStyle.normal.textColor = original_color;

			BottomToolBarStyle = new GUIStyle(EditorStyles.toolbar);
			//BottomToolBarStyle.fixedHeight = BottomToolBarHeight;


			brushEditModeContent = new GUIContent[]
			{
				new GUIContent("Place"),
				new GUIContent("Generate"),
				new GUIContent("Edit"),
				new GUIContent("Clip"),
				new GUIContent("Surfaces")
			};

			brushEditModeTooltips = new ToolTip[]
			{
				new ToolTip("Place mode",       "In this mode you can place, rotate and scale brushes", Keys.SwitchToObjectEditMode),
				new ToolTip("Generate mode",    "In this mode you can create brushes using several generators", Keys.SwitchToGenerateEditMode),
				new ToolTip("Edit mode",        "In this mode you can edit the shapes of brushes", Keys.SwitchToMeshEditMode),
				new ToolTip("Clip mode",        "In this mode you can split or clip brushes", Keys.SwitchToClipEditMode),
				new ToolTip("Surfaces mode",    "In this mode you can modify the texturing and everything else related to brush surfaces", Keys.SwitchToSurfaceEditMode)
			};

			var enum_type = typeof(ToolEditMode);
			brushEditModeNames = (from name in System.Enum.GetNames(enum_type) select ObjectNames.NicifyVariableName(name)).ToArray();
			brushEditModeValues = System.Enum.GetValues(enum_type).Cast<ToolEditMode>().ToArray();
			for (int i = 0; i < brushEditModeNames.Length; i++)
			{
				if (brushEditModeContent[i].text != brushEditModeNames[i])
					Debug.LogError("Fail!");
			}

			var pro_skin = CSG_GUIStyleUtility.Pro;
			var personal_skin = CSG_GUIStyleUtility.Personal;

			for (int i = 0; i < clipTypeCount; i++)
			{
				pro_skin.clipNames[i] = new GUIContent(proInActiveClipTypes[i]);
				pro_skin.clipNamesOn[i] = new GUIContent(proActiveClipTypes[i]);

				pro_skin.clipNames[i].text = clipText[i];
				pro_skin.clipNamesOn[i].text = clipText[i];

				personal_skin.clipNames[i] = new GUIContent(personalActiveClipTypes[i]);
				personal_skin.clipNamesOn[i] = new GUIContent(personalInActiveClipTypes[i]);

				personal_skin.clipNames[i].text = clipText[i];
				personal_skin.clipNamesOn[i].text = clipText[i];
			}

			pro_skin.passThrough = new GUIContent(proPassThrough);
			pro_skin.passThroughOn = new GUIContent(proPassThroughOn);
			pro_skin.hierarchyPassThrough = new GUIContent(proPassThroughOn);
			pro_skin.passThrough.text = passThroughText;
			pro_skin.passThroughOn.text = passThroughText;


			personal_skin.passThrough = new GUIContent(personalPassThrough);
			personal_skin.passThroughOn = new GUIContent(personalPassThroughOn);
			personal_skin.hierarchyPassThrough = new GUIContent(personalPassThroughOn);
			personal_skin.passThrough.text = passThroughText;
			personal_skin.passThroughOn.text = passThroughText;


			pro_skin.wireframe = new GUIContent(proWireframe);
			pro_skin.wireframeOn = new GUIContent(proWireframeOn);
			pro_skin.wireframe.tooltip = wireframeTooltip;
			pro_skin.wireframeOn.tooltip = wireframeTooltip;

			personal_skin.wireframe = new GUIContent(personalWireframe);
			personal_skin.wireframeOn = new GUIContent(personalWireframeOn);
			personal_skin.wireframe.tooltip = wireframeTooltip;
			personal_skin.wireframeOn.tooltip = wireframeTooltip;



			for (int i = 0; i < shapeModeCount; i++)
			{
				pro_skin.shapeModeNames[i] = new GUIContent(proInActiveShapeModes[i]);
				pro_skin.shapeModeNamesOn[i] = new GUIContent(proActiveShapeModes[i]);

				personal_skin.shapeModeNames[i] = new GUIContent(personalActiveShapeModes[i]);
				personal_skin.shapeModeNamesOn[i] = new GUIContent(personalInActiveShapeModes[i]);

				pro_skin.shapeModeNames[i].text = shapeModeText[i];
				pro_skin.shapeModeNamesOn[i].text = shapeModeText[i];

				personal_skin.shapeModeNames[i].text = shapeModeText[i];
				personal_skin.shapeModeNamesOn[i].text = shapeModeText[i];
			}


			for (int i = 0; i < operationTypeCount; i++)
			{
				pro_skin.operationNames[i] = new GUIContent(proInActiveOperationTypes[i]);
				pro_skin.operationNamesOn[i] = new GUIContent(proActiveOperationTypes[i]);
				pro_skin.hierarchyOperations[i] = new GUIContent(proActiveOperationTypes[i]);

				personal_skin.operationNames[i] = new GUIContent(personalActiveOperationTypes[i]);
				personal_skin.operationNamesOn[i] = new GUIContent(personalInActiveOperationTypes[i]);
				personal_skin.hierarchyOperations[i] = new GUIContent(personalInActiveOperationTypes[i]);

				pro_skin.operationNames[i].text = operationText[i];
				pro_skin.operationNamesOn[i].text = operationText[i];

				personal_skin.operationNames[i].text = operationText[i];
				personal_skin.operationNamesOn[i].text = operationText[i];
			}

			pro_skin.rebuildIcon    = proRebuildIcon;
			pro_skin.gridIcon       = proGridIcon;
			pro_skin.gridIconOn     = proGridIconOn;
			pro_skin.gridSnapIcon   = proGridSnapIcon;
			pro_skin.gridSnapIconOn = proGridSnapIconOn;
			pro_skin.relSnapIcon    = proRelSnapIcon;
			pro_skin.relSnapIconOn  = proRelSnapIconOn;
            pro_skin.noSnapIcon     = proNoSnapIcon;
            pro_skin.noSnapIconOn   = proNoSnapIconOn;

            //pro_skin.rebuildIcon.tooltip	= rebuildTooltip;
            //pro_skin.gridIcon.tooltip		= gridTooltip;
            //pro_skin.gridIconOn.tooltip		= gridOnTooltip;
            //pro_skin.snappingIcon.tooltip	= snappingTooltip;
            //pro_skin.snappingIconOn.tooltip	= snappingOnTooltip;

            personal_skin.rebuildIcon    = personalRebuildIcon;
			personal_skin.gridIcon       = personalGridIcon;
			personal_skin.gridIconOn     = personalGridIconOn;
			personal_skin.gridSnapIcon   = personalGridSnapIcon;
			personal_skin.gridSnapIconOn = personalGridSnapIconOn;
            personal_skin.relSnapIcon    = personalRelSnapIcon;
            personal_skin.relSnapIconOn  = personalRelSnapIconOn;
            personal_skin.noSnapIcon     = personalNoSnapIcon;
            personal_skin.noSnapIconOn   = personalNoSnapIconOn;

            //personal_skin.rebuildIcon.tooltip		= rebuildTooltip;
            //personal_skin.gridIcon.tooltip			= gridTooltip;
            //personal_skin.gridIconOn.tooltip		= gridOnTooltip;
            //personal_skin.snappingIcon.tooltip		= snappingTooltip;
            //personal_skin.snappingIconOn.tooltip	= snappingOnTooltip;

            var skin2 = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
			personal_skin.messageStyle = new GUIStyle(skin2.textArea);
			personal_skin.messageWarningStyle = new GUIStyle(personal_skin.messageStyle);

			pro_skin.messageStyle = new GUIStyle(skin2.textArea);
			pro_skin.messageWarningStyle = new GUIStyle(pro_skin.messageStyle);


			unselectedIconLabelStyle = new GUIStyle(GUI.skin.label);
			unselectedIconLabelStyle.richText = true;
			var color = unselectedIconLabelStyle.normal.textColor;
			color.r *= 232.0f / 255.0f;
			color.g *= 232.0f / 255.0f;
			color.b *= 232.0f / 255.0f;
			color.a = 153.0f / 255.0f;
			unselectedIconLabelStyle.normal.textColor = color;

			selectedIconLabelStyle = new GUIStyle(GUI.skin.label);
			selectedIconLabelStyle.richText = true;

			GUI.skin = oldSkin;
		}

		public static void ResetGUIState()
		{
			GUI.skin = null;
			Color white = Color.white;
			GUI.contentColor = white;
			GUI.backgroundColor = white;
			GUI.color = Color.white;
			GUI.enabled = true;
			GUI.changed = false;
			EditorGUI.indentLevel = 0;
			//EditorGUI.ClearStacks();
			EditorGUIUtility.fieldWidth = 0f;
			EditorGUIUtility.labelWidth = 0f;
			//EditorGUIUtility.SetBoldDefaultFont(false);
			//EditorGUIUtility.UnlockContextWidth();
			EditorGUIUtility.hierarchyMode = false;
			EditorGUIUtility.wideMode = false;
			//ScriptAttributeUtility.propertyHandlerCache = null;
			SetDefaultGUISkin();
		}

		public static void SetDefaultGUISkin()
		{
			if (EditorGUIUtility.isProSkin)
				GUI.skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
			else
				GUI.skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
		}


		static GUIContent IconContent(string name)
		{
			var path = "Assets/Plugins/RealtimeCSG/Editor/Resources/Icons/" + name + ".png";
			var image = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Debug.Assert(image, "Could not find image at " + path);
			return new GUIContent(image);
		}

		public static GUIStyle GetStyle(string styleName)
		{
			GUIStyle s = GUI.skin.FindStyle(styleName);
			if (s == null)
			{
				var oldSkin = GUI.skin;
				SetDefaultGUISkin();
				s = GUI.skin.FindStyle(styleName);
				GUI.skin = oldSkin;
			}
			return s;
		}


	}
}

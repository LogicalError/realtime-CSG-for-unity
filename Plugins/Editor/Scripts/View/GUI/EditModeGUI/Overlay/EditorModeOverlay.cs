#if UNITY_2021_3_OR_NEWER
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace RealtimeCSG
{
    [Overlay(typeof(SceneView), displayName: "Realtime CSG", id : _id, defaultDisplay: true
#if !UNITY_2022_3_OR_NEWER
        ,ussName: "RealtimeCSG"
#else
        ,defaultLayout = Layout.VerticalToolbar
#endif
    )]

    internal class EditorModeOverlay: ToolbarOverlay
    {
        public const string iconPath = "Packages/com.prenominal.realtimecsg/Plugins/Editor/Resources/GUI/";
        public const string _id = "RealtimeCSG";

        public EditorModeOverlay()
        : base(

            CSGActivateToggleButton._id,
            PlaceEditorModeButton._id,
            GenerateEditorModeButton._id,
            EditEditorModeButton._id,
            ClipEditorModeButton._id,
            SurfaceEditorModeButton._id
        )
        {
        }

    }

    [EditorToolbarElement(_id, typeof(SceneView))]
    internal class CSGActivateToggleButton : EditorToolbarToggle
    {
        public const string _id = EditorModeOverlay._id + "/CSGActivateToggle";
        public CSGActivateToggleButton()
        {
            tooltip = "Toggle CSG Realtime";
            
            onIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(EditorModeOverlay.iconPath + "CSG_Icon.png");
            offIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(EditorModeOverlay.iconPath + "CSG_Icon_off.png");
            
            this.RegisterValueChangedCallback(x => OnClicked());
            CSGSettings.OnRealtimeCSGEnabledChanged += OnRealtimeCSGEnabledChanged;
            
            value = CSGSettings.EnableRealtimeCSG;
        }

        private void OnClicked()
        {
            RealtimeCSG.CSGSettings.SetRealtimeCSGEnabled(value);
            OnRealtimeCSGEnabledChanged(value);
        }

        void OnRealtimeCSGEnabledChanged(bool isEnabled)
        {
            value = isEnabled;
            foreach (EditorModeButton button in parent.Query<EditorModeButton>().ToList())
            {
                button.SetEnabled(isEnabled);
            }
        }
    }

    internal class EditorModeButton : EditorToolbarToggle
    {
        ToolEditMode mode;
        public EditorModeButton(string iconName, ToolEditMode _mode)
        {
            mode = _mode;

            CSG_GUIStyleUtility.InitializeEditModeTexts();
            ToolTip tt = CSG_GUIStyleUtility.brushEditModeTooltips[(int)mode];
            tooltip = $"{tt.TitleString()}\n{tt.ContentsString()}\n{tt.KeyString()}";

            icon = AssetDatabase.LoadAssetAtPath<Texture2D>(EditorModeOverlay.iconPath + iconName);

            this.RegisterValueChangedCallback(x => OnClicked());

            value = EditModeManager.EditMode == mode;

            EditModeManager.OnEditModeChanged += OnEditModeChanged;

            SetEnabled(CSGSettings.EnableRealtimeCSG);
        }

        void OnEditModeChanged(ToolEditMode _mode) => SetValueWithoutNotify(_mode == mode);

        private void OnClicked()
        {
            //do nothing if clicking on an already clicked toggle button, and reactivate it
            if (!value)
                value = true;
            if (value)
                RealtimeCSG.CSGSettings.SetRealtimeCSGEnabled(true);
            EditModeManager.EditMode = mode;
        }
    }

#region Buttons class for each Mode
    [EditorToolbarElement(_id, typeof(SceneView))]
    internal class PlaceEditorModeButton : EditorModeButton
    {
        public const string _id = EditorModeOverlay._id + "/Place";
        public PlaceEditorModeButton() : base ("Place.png", ToolEditMode.Place){}
    }


    [EditorToolbarElement(_id, typeof(SceneView))]
    internal class GenerateEditorModeButton : EditorModeButton
    {
        public const string _id = EditorModeOverlay._id + "/Generate";
        public GenerateEditorModeButton() : base("Generate.png", ToolEditMode.Generate) { }
    }

    [EditorToolbarElement(_id, typeof(SceneView))]
    internal class EditEditorModeButton : EditorModeButton
    {
        public const string _id = EditorModeOverlay._id + "/Edit";
        public EditEditorModeButton() : base("Edit.png", ToolEditMode.Edit) { }
    }

    [EditorToolbarElement(_id, typeof(SceneView))]
    internal class ClipEditorModeButton : EditorModeButton
    {
        public const string _id = EditorModeOverlay._id + "/Clip";
        public ClipEditorModeButton() : base("Clip.png", ToolEditMode.Clip) { }
    }

    [EditorToolbarElement(_id, typeof(SceneView))]
    internal class SurfaceEditorModeButton : EditorModeButton
    {
        public const string _id = EditorModeOverlay._id + "/Surfaces";
        public SurfaceEditorModeButton() : base("Surface.png", ToolEditMode.Surfaces) { }
    }

#endregion


}
#endif
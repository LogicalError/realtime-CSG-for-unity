using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RealtimeCSG.Foundation;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
#if TEST_ENABLED
	sealed class CSGHierarchyView : EditorWindow
	{
		CSGHierarchyView()
		{
			windows.Add(this);
		}

		void Awake()
		{
			Selection.selectionChanged -= OnSelectionChanged;
			Selection.selectionChanged += OnSelectionChanged;
		}

		void OnSelectionChanged()
		{
			this.Repaint();
		}

		void OnDestroy()
		{
			windows.Remove(this);
		}

		Dictionary<int, bool> openNodes = new Dictionary<int, bool>();
		static List<CSGHierarchyView> windows = new List<CSGHierarchyView>();

		public static void RepaintAll()
		{
			foreach (var window in windows)
			{
				if (window)
					window.Repaint();
			}
		}

		[MenuItem("Window/CSG Hierarchy")]
		static void Create()
		{
			window = (CSGHierarchyView)EditorWindow.GetWindow(typeof(CSGHierarchyView), false, "CSG Hierarchy");
			window.autoRepaintOnSceneChange = true;
		}

		static CSGHierarchyView window;

		class Styles
		{
			public GUIStyle emptyItem;
			public GUIStyle emptySelected;
			public GUIStyle foldOut;
			public GUIStyle foldOutSelected;
		};

		static Styles styles;

		static void UpdateStyles()
		{
			styles = new Styles();
			styles.emptyItem = new GUIStyle(EditorStyles.foldout);

			styles.emptyItem.active.background = null;
			styles.emptyItem.hover.background = null;
			styles.emptyItem.normal.background = null;
			styles.emptyItem.focused.background = null;

			styles.emptyItem.onActive.background = null;
			styles.emptyItem.onHover.background = null;
			styles.emptyItem.onNormal.background = null;
			styles.emptyItem.onFocused.background = null;

			styles.emptySelected = new GUIStyle(styles.emptyItem);
			styles.emptySelected.normal = styles.emptySelected.active;
			styles.emptySelected.onNormal = styles.emptySelected.onActive;

			styles.foldOut = new GUIStyle(EditorStyles.foldout);
			styles.foldOut.active = styles.foldOut.normal;
			styles.foldOut.onActive = styles.foldOut.onNormal;

			styles.foldOutSelected = new GUIStyle(EditorStyles.foldout);
			styles.foldOutSelected.normal = styles.foldOutSelected.active;
			styles.foldOutSelected.onNormal = styles.foldOutSelected.onActive;

			styles.emptyItem.active = styles.emptyItem.normal;
			styles.emptyItem.onActive = styles.emptyItem.onNormal;
		}

		const int kScrollWidth = 20;
		const int kItemIndent = 20;
		const int kIconWidth = 20;
		const int kPadding = 2;
		static Vector2 m_ScrollPos;

		sealed class StackItem
		{
			public StackItem(CSGTreeNode[] _children, float _xpos = 0) { children = _children; index = 0; count = children.Length; xpos = _xpos; }
			public int index;
			public int count;
			public float xpos;
			public CSGTreeNode[] children;
		}
		static List<StackItem>  itemStack = new List<StackItem>();

		static int GetVisibleItems(Dictionary<int, CSGTreeNode[]> sceneHierarchies, ref Dictionary<int, bool> openNodes)
		{
			if (sceneHierarchies == null || sceneHierarchies.Count == 0)
				return 0;

			int totalCount = 0;
			foreach (var item in sceneHierarchies)
			{
				totalCount += 1; // scene foldout itself
				itemStack.Clear();
				totalCount += GetVisibleItems(item.Value, ref openNodes);
			}
			return totalCount;
		}

		// TODO: cache item heights
		static int GetVisibleItems(CSGTreeNode[] hierarchyItems, ref Dictionary<int, bool> openNodes)
		{
			if (hierarchyItems == null)
				return 0;

			int totalCount = hierarchyItems.Length;
			itemStack.Add(new StackItem(hierarchyItems));

			ContinueOnNextStackItem:
			if (itemStack.Count == 0)
				return totalCount;

			var currentStackItem = itemStack[itemStack.Count - 1];
			var children = currentStackItem.children;

			while (currentStackItem.index < currentStackItem.count)
			{
				int i = currentStackItem.index;
				currentStackItem.index++;

				var nodeID = children[i].NodeID;
				bool isOpen;
				if (!openNodes.TryGetValue(nodeID, out isOpen))
				{
					isOpen = true;
					openNodes[nodeID] = true;
				}
				if (isOpen)
				{
					var childCount = children[i].Count;
					if (childCount > 0)
					{
						totalCount += childCount;
						itemStack.Add(new StackItem(children[i].ChildrenToArray()));
						goto ContinueOnNextStackItem;
					}
				}
			}
			itemStack.RemoveAt(itemStack.Count - 1);
			goto ContinueOnNextStackItem;
		}

		static void AddFoldOuts(ref Rect itemRect, ref Rect visibleArea, CSGTreeNode[] hierarchyItems, HashSet<int> selectedInstanceIDs, ref Dictionary<int, bool> openNodes)
		{
			if (hierarchyItems == null || hierarchyItems.Length == 0)
				return;

			var defaultColor = GUI.color;
			AddFoldOuts(ref itemRect, ref visibleArea, hierarchyItems, selectedInstanceIDs, defaultColor, ref openNodes);
			GUI.color = defaultColor;
		}

		static string NameForCoreNode(CSGTreeNode coreNode)
		{
			var userID = coreNode.UserID;
			var nodeID = coreNode.NodeID;
			var obj = (userID != 0) ? EditorUtility.InstanceIDToObject(userID) : null;
			if (obj == null)
				return string.Format("<unknown> [{0}:{1}]", (nodeID-1), userID);
			return obj.name + string.Format(" [{0}:{1}]", (nodeID-1), userID);
		}

		static void AddFoldOuts(ref Rect itemRect, ref Rect visibleArea, CSGTreeNode[] hierarchyItems, HashSet<int> selectedInstanceIDs, Color defaultColor, ref Dictionary<int, bool> openNodes)
		{
			if (hierarchyItems == null)
				return;
			itemStack.Add(new StackItem(hierarchyItems, itemRect.x));

			ContinueOnNextStackItem:
			if (itemStack.Count == 0)
				return;

			float kItemHeight = EditorGUIUtility.singleLineHeight;

			var currentStackItem = itemStack[itemStack.Count - 1];
			var children = currentStackItem.children;
			itemRect.x = currentStackItem.xpos;
			while (currentStackItem.index < currentStackItem.count)
			{
				int i = currentStackItem.index;
				currentStackItem.index++;
				if (itemRect.y > visibleArea.yMax)
					return;

				var nodeID		= children[i].NodeID;
				var userID		= children[i].UserID;
				var childCount	= children[i].Count;
				if (itemRect.y > visibleArea.yMin)
				{
					var name		= NameForCoreNode(children[i]);
					var selected = selectedInstanceIDs.Contains(userID);
					var style		= (childCount > 0) ? 
										(selected ? styles.foldOutSelected : styles.foldOut) : 
										(selected ? styles.emptySelected : styles.emptyItem);

					EditorGUI.BeginChangeCheck();

					bool isOpen;
					if (!openNodes.TryGetValue(nodeID, out isOpen))
						openNodes[nodeID] = false;

					openNodes[nodeID] = EditorGUI.Foldout(itemRect, isOpen, name, true, style);
					if (EditorGUI.EndChangeCheck())
					{
						var obj = EditorUtility.InstanceIDToObject(userID);
						if (!(obj is GameObject))
						{
							var mono = (obj as MonoBehaviour);
							if (mono)
								userID = mono.gameObject.GetInstanceID();
						}
						Selection.instanceIDs = new[] { userID };
					}
				}
				itemRect.y += kItemHeight;

				if (openNodes[nodeID])
				{
					if (childCount > 0)
					{
						itemStack.Add(new StackItem(children[i].ChildrenToArray(), itemRect.x + kItemIndent));
						goto ContinueOnNextStackItem;
					}
				}
			}
			itemStack.RemoveAt(itemStack.Count - 1);
			goto ContinueOnNextStackItem;
		}


		void OnGUI()
		{
			if (styles == null)
				UpdateStyles();

			var selectedInstanceIDs = new HashSet<int>();
			foreach(var instanceID in Selection.instanceIDs)
			{
				var obj = EditorUtility.InstanceIDToObject(instanceID);
				var go = obj as GameObject;
				if (go != null)
				{
					foreach(var no in go.GetComponents<CSGNode>())
					{
						var instanceID_ = no.GetInstanceID();
						selectedInstanceIDs.Add(instanceID_);
					}
				}
			}


			float kItemHeight = EditorGUIUtility.singleLineHeight;
			
			var allNodes = CSGManager.AllTreeNodes;
			var allRootNodeList = new List<CSGTreeNode>();
			for (int i = 0; i < allNodes.Length;i++)
			{
				if (allNodes[i].Type != CSGNodeType.Tree && 
					(allNodes[i].Tree .Valid || allNodes[i].Parent.Valid))
					continue;
				
				allRootNodeList.Add(allNodes[i]);
			}

			var allRootNodes = allRootNodeList.ToArray();

			var totalCount = GetVisibleItems(allRootNodes, ref openNodes);

			var itemArea = position;
			itemArea.x = 0;
			itemArea.y = 0;
			itemArea.height -= 200;

			var totalRect = position;
			totalRect.x = 0;
			totalRect.y = 0;
			totalRect.width = position.width - kScrollWidth;
			totalRect.height = (totalCount * kItemHeight) + (2 * kPadding);

			var itemRect = position;
			itemRect.x = 0;
			itemRect.y = kPadding;
			itemRect.height = kItemHeight;

			m_ScrollPos = GUI.BeginScrollView(itemArea, m_ScrollPos, totalRect);
			{
				Rect visibleArea = itemArea;
				visibleArea.x += m_ScrollPos.x;
				visibleArea.y += m_ScrollPos.y;
				
				AddFoldOuts(ref itemRect, ref visibleArea, allRootNodes, selectedInstanceIDs, ref openNodes);
			}
			GUI.EndScrollView();
			if (selectedInstanceIDs.Count == 1)
			{
				var obj = EditorUtility.InstanceIDToObject(selectedInstanceIDs.First()) as CSGNode;
				if (obj)
				{ 
					var brush		= obj as CSGBrush;
					var operation	= obj as CSGOperation;
					var model		= obj as CSGModel;
					int nodeID = CSGNode.InvalidNodeID;
					if (brush) nodeID = brush.brushNodeID;
					if (operation) nodeID = operation.operationNodeID;
					if (model) nodeID = model.modelNodeID;

					if (nodeID != CSGNode.InvalidNodeID)
					{
						var labelArea = itemArea;
						labelArea.x = 0;
						labelArea.y = labelArea.height;
						labelArea.height = kItemHeight;
						CSGTreeNode node = new CSGTreeNode { nodeID = nodeID };
						GUI.Label(labelArea, "NodeID: " + nodeID); labelArea.y += kItemHeight;
						GUI.Label(labelArea, "UserID: " + node.UserID); labelArea.y += kItemHeight;
						GUI.Label(labelArea, "Valid: " + node.Valid); labelArea.y += kItemHeight;
						GUI.Label(labelArea, "NodeType: " + node.Type); labelArea.y += kItemHeight;
						GUI.Label(labelArea, "ChildCount: " + node.Count); labelArea.y += kItemHeight;
						var nodeType = node.Type;
						if (nodeType != CSGNodeType.Tree)
						{ 
							GUI.Label(labelArea, "Parent: " + node.Parent.NodeID + " valid " + node.Parent.Valid); labelArea.y += kItemHeight;
							GUI.Label(labelArea, "Model: " + node.Tree.NodeID + " valid " + node.Tree.Valid); labelArea.y += kItemHeight;

							if (nodeType == CSGNodeType.Brush)
							{
								CSGOperationType op = ((CSGTreeBrush)node).Operation;
								GUI.Label(labelArea, "Operation: " + op.ToString()); labelArea.y += kItemHeight;
							} else 
							if (nodeType == CSGNodeType.Branch)
							{
								CSGOperationType op = ((CSGTreeBranch)node).Operation;
								GUI.Label(labelArea, "Operation: " + op.ToString()); labelArea.y += kItemHeight;
							}
						}
					}
				}
			}
		}
	}
#endif
}


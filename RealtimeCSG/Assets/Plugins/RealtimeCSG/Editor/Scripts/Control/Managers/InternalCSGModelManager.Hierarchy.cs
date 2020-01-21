using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using InternalRealtimeCSG;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	internal partial class InternalCSGModelManager
	{
		private static bool    _isHierarchyModified    = true;
		
		public static void UpdateHierarchy()
		{
			_isHierarchyModified = true;
		}


		#region GetParentData
		public static ParentNodeData GetParentData(ChildNodeData childNode)
		{
			var parent = childNode.Parent;
			if (System.Object.Equals(parent, null) || !parent || !parent.transform)
			{
				var model = childNode.Model;
				if (System.Object.Equals(model, null) || !model || model.modelNodeID == CSGNode.InvalidNodeID || !model.transform)
					return null;
				return model.parentData;
			}
			if (parent.operationNodeID == CSGNode.InvalidNodeID)
				return null;
			
			return parent.ParentData;
		}
		#endregion

		#region UpdateChildList
		struct TreePosition
		{
			public TreePosition(HierarchyItem _item) { item = _item; index = 0; }
			public HierarchyItem item;
			public int index;
		}

		static Int32[] UpdateChildList(HierarchyItem top)
		{
			var ids = new List<int>();
			{
				var parents = new List<TreePosition>
				{
					new TreePosition(top)
				};
				while (parents.Count > 0)
				{
					var parent		= parents[parents.Count - 1];
					parents.RemoveAt(parents.Count - 1);
					var children	= parent.item.ChildNodes;
					for (var i = parent.index; i < children.Length; i++)
					{
						var node = children[i];
						var nodeID = node.NodeID;
						if (nodeID == CSGNode.InvalidNodeID)
						{
							var operation = node.Transform ? node.Transform.GetComponent<CSGOperation>() : null;
							if (operation)
							{
								if (operation.operationNodeID != CSGNode.InvalidNodeID)
								{
									nodeID = node.NodeID = operation.operationNodeID;
								}
							}
							if (nodeID == CSGNode.InvalidNodeID)
							{
								if (node.ChildNodes.Length > 0)
								{
									var next_index = i + 1;
									if (next_index < children.Length)
									{
										parent.index = next_index;
										parents.Add(parent);
									}
									parents.Add(new TreePosition(node));
									break;
								}
								continue;
							}
						}
						ids.Add(nodeID);
						if (node.PrevSiblingIndex != node.SiblingIndex)
						{
							External.SetDirty(nodeID);
							node.PrevSiblingIndex = node.SiblingIndex;
						}
					}
				}
			}
			return ids.ToArray();
		}
		#endregion
		
		#region CheckTransformChanged

		internal const int BrushCheckChunk = 3000;
		internal static int BrushCheckPos = 0;

		public static void CheckTransformChanged(bool checkAllBrushes = false)
		{
			if (External == null)
			{
				return;
			}
			
			for (int i = 0; i < Operations.Count; i++)
			{
				var operation = Operations[i];
				if (!Operations[i]) continue;

				if ((int)operation.PrevOperation == (int)operation.OperationType)
					continue;
				
				operation.PrevOperation = operation.OperationType;
				External.SetOperationOperationType(operation.operationNodeID,
													operation.OperationType);
			}

			for (int i = 0; i < Models.Length; i++)
			{
				var model = Models[i];
				if (!model || !model.gameObject.activeInHierarchy) continue;

				if (!model.cachedTransform) model.cachedTransform = model.transform;
			}
			
			for (int brushIndex = 0; brushIndex < Brushes.Count; brushIndex++)
			{
				var brush		= Brushes[brushIndex];
				if (System.Object.ReferenceEquals(brush, null) || !brush)
					continue;

				var brushNodeID = brush.brushNodeID;
				// make sure it's registered, otherwise ignore it
				if (brushNodeID == CSGNode.InvalidNodeID)
					continue;
				
				var brushTransform				= brush.hierarchyItem.Transform;
				var currentLocalToWorldMatrix	= brushTransform.localToWorldMatrix;					
				var prevTransformMatrix			= brush.compareTransformation.localToWorldMatrix;
				if (prevTransformMatrix.m00 != currentLocalToWorldMatrix.m00 ||
					prevTransformMatrix.m01 != currentLocalToWorldMatrix.m01 ||
					prevTransformMatrix.m02 != currentLocalToWorldMatrix.m02 ||
					prevTransformMatrix.m03 != currentLocalToWorldMatrix.m03 ||

					prevTransformMatrix.m10 != currentLocalToWorldMatrix.m10 ||
					prevTransformMatrix.m11 != currentLocalToWorldMatrix.m11 ||
					prevTransformMatrix.m12 != currentLocalToWorldMatrix.m12 ||
					prevTransformMatrix.m13 != currentLocalToWorldMatrix.m13 ||

					prevTransformMatrix.m20 != currentLocalToWorldMatrix.m20 ||
					prevTransformMatrix.m21 != currentLocalToWorldMatrix.m21 ||
					prevTransformMatrix.m22 != currentLocalToWorldMatrix.m22 ||
					prevTransformMatrix.m23 != currentLocalToWorldMatrix.m23)
				{
					var modelTransform = brush.ChildData.Model.transform;
					brush.compareTransformation.localToWorldMatrix = currentLocalToWorldMatrix;
					brush.compareTransformation.brushToModelSpaceMatrix = modelTransform.worldToLocalMatrix * 
						brush.compareTransformation.localToWorldMatrix;
					
					var localToModelMatrix = brush.compareTransformation.brushToModelSpaceMatrix;
					External.SetBrushToModelSpace(brushNodeID, localToModelMatrix);

					if (brush.ControlMesh != null)
						brush.ControlMesh.Generation = brush.controlMeshGeneration + 1;
				}
				
				if (brush.OperationType != brush.prevOperation)
				{
					brush.prevOperation = brush.OperationType;

					External.SetBrushOperationType(brushNodeID,
												   brush.OperationType);
				}

				if (brush.ControlMesh == null)
				{
					brush.ControlMesh = ControlMeshUtility.EnsureValidControlMesh(brush);
					if (brush.ControlMesh == null)
						continue;
					
					brush.controlMeshGeneration = brush.ControlMesh.Generation;
					ControlMeshUtility.RebuildShape(brush);
				} else
				if (brush.controlMeshGeneration != brush.ControlMesh.Generation)
				{
					brush.controlMeshGeneration = brush.ControlMesh.Generation;
					ControlMeshUtility.RebuildShape(brush);
				}
			}
		}
		#endregion

		
		// for a given transform, try to to find the first transform parent that is a csg-node
		#region FindParentTransform
		public static Transform FindParentTransform(Transform childTransform)
		{
			var iterator	= childTransform.parent;
			if (!iterator)
				return null;
			
			var currentNodeObj = iterator.GetComponent(TypeConstants.CSGNodeType);
			if (currentNodeObj)
			{
				var brush = currentNodeObj as CSGBrush;
				if (brush)
				{
					return ((!brush.ChildData.Parent) ? ((!brush.ChildData.Model) ? null : brush.ChildData.Model.transform) : brush.ChildData.Parent.transform);
				}
			}
			while (iterator)
			{
				currentNodeObj = iterator.GetComponent(TypeConstants.CSGNodeType);
				if (currentNodeObj)
					return iterator;

				iterator = iterator.parent;
			}
			return null;
		}
		#endregion

		// for a given transform, try to to find the model transform
		#region FindParentTransform
		public static Transform FindModelTransform(Transform childTransform)
		{
			var currentNodeObj = childTransform.GetComponent(TypeConstants.CSGNodeType);
			if (currentNodeObj)
			{
				var model = currentNodeObj as CSGModel;
				if (model)
					return null;

				var brush = currentNodeObj as CSGBrush;
				if (brush)
					return (brush.ChildData.Model == null) ? null : brush.ChildData.Model.transform;
				
				var operation = currentNodeObj as CSGOperation;
				if (operation && !operation.PassThrough)
					return (operation.ChildData.Model == null) ? null : operation.ChildData.Model.transform;
			}
			
			var iterator = childTransform.parent;
			while (iterator)
			{
				var currentModelObj = iterator.GetComponent(TypeConstants.CSGModelType);
				if (currentModelObj)
					return currentModelObj.transform; 

				iterator = iterator.parent;
			}
			return null;
		}
		#endregion

		#region FindParentNode
		static void FindParentNode(Transform opTransform, out CSGNode parentNode, out Transform parentTransform)
		{
			var iterator = opTransform.parent;
			while (iterator)
			{
				var currentNodeObj = iterator.GetComponent(TypeConstants.CSGNodeType);
				if (currentNodeObj)
				{
					var operationNode = currentNodeObj as CSGOperation;
					if (!operationNode ||
						!operationNode.PassThrough)
					{
						parentNode = currentNodeObj as CSGNode;
						parentTransform = iterator;
						return;
					}
				}

				iterator = iterator.parent;
			}
			parentTransform = null;
			parentNode = null;
		}
		#endregion

		#region FindParentOperation
		static void FindParentOperation(Transform opTransform, out CSGOperation parentOp)
		{
			if (opTransform)
			{
				var iterator = opTransform.parent;
				while (iterator)
				{
					var currentNodeObj = iterator.GetComponent(TypeConstants.CSGOperationType);
					if (currentNodeObj)
					{
						var currentParent = currentNodeObj as CSGOperation;
						if (!currentParent.PassThrough)
						{
							parentOp = currentParent;
							return;
						}
					}

					iterator = iterator.parent;
				}
			}
			parentOp = null;
		}
		#endregion

		#region FindParentModel
		static void FindParentModel(Transform opTransform, out CSGModel parentModel)
		{
			if (!opTransform)
			{
				parentModel = null;
				return;
			}

			var iterator = opTransform.parent;
			while (iterator)
			{
				var currentNodeObj = iterator.GetComponent(TypeConstants.CSGModelType);
				if (currentNodeObj)
				{
					parentModel = currentNodeObj as CSGModel;
					return;
				}

				iterator = iterator.parent;
			}
			parentModel = GetDefaultCSGModelForObject(opTransform);
		}
		#endregion

		#region FindParentOperationAndModel
		internal static void FindParentOperationAndModel(Transform opTransform, out CSGOperation parentOp, out CSGModel parentModel)
		{
			parentOp = null;
			parentModel = null;
			if (!opTransform)
				return;
			var iterator = opTransform.parent;
			while (iterator)
			{
				var currentNodeObj = iterator.GetComponent(TypeConstants.CSGNodeType);
				if (currentNodeObj)
				{
					parentModel = currentNodeObj as CSGModel;
					if (parentModel)
					{
						parentOp = null;
						return;
					}

					var tempParentOp = currentNodeObj as CSGOperation;
					if (tempParentOp)
					{
						if (tempParentOp.operationNodeID != CSGNode.InvalidNodeID && !tempParentOp.PassThrough)
						{
							parentOp = tempParentOp;
							break;
						}
					}
				}

				iterator = iterator.parent;
			}
			
			while (iterator)
			{
				var currentNodeObj = iterator.GetComponent(TypeConstants.CSGModelType);
				if (currentNodeObj)
				{
					parentModel = currentNodeObj as CSGModel;
					return;
				}

				iterator = iterator.parent;
			}
			parentModel = GetDefaultCSGModelForObject(opTransform);
		}
		#endregion
		


		#region InitializeHierarchy
		static void InitializeHierarchy(ChildNodeData childData, Transform childTransform)
		{
			Transform parentTransform;
			CSGNode parentNode;
			FindParentNode(childTransform, out parentNode, out parentTransform);
			if (!parentNode)
			{
				parentTransform = childTransform.root;
				childData.Model = GetDefaultCSGModelForObject(childTransform);
				childData.Parent = null;
			} else
			{
				// maybe our parent is a model?
				var model = parentNode as CSGModel;
				if (model)
				{
					childData.Model = model;
				} else
				{
					// is our parent an operation?
					var operation = parentNode as CSGOperation;
					if (operation &&
						!operation.PassThrough)
					{
						childData.Parent = operation;
					}

					// see if our parent has already found a model
					if (childData.Parent)
					{
						if (childData.Parent.ChildData != null)
							childData.Model = childData.Parent.ChildData.Model;
					}
				}

				// haven't found a model?
				if (!childData.Model)
				{
					// if not, try higher up in the hierarchy ..
					FindParentModel(parentTransform, out childData.Model);
				}
			}
		}

		static void InitializeHierarchy(CSGBrush brush, Transform brushTransform)
		{
			var currentModel	 = brush.ChildData.Model;
			var modelTransform	 = (!currentModel) ? null : currentModel.transform;
			brush.compareTransformation.EnsureInitialized(brushTransform, modelTransform);			
		}
		#endregion

		#region UpdateChildrenParent
		static void UpdateChildrenParent(CSGOperation parent, Transform container, bool forceSet = false)
		{
			if (External == null)
			{
				return;
			}
			for (int i = 0; i < container.childCount; i++)
			{
				var child = container.GetChild(i);
				var nodeObj = child.GetComponent(TypeConstants.CSGNodeType);
				if (nodeObj)
				{
					var op = nodeObj as CSGOperation;
					if (op &&
						!op.PassThrough)
					{
						// make sure the node has already been initialized, otherwise
						// assume it'll still get initialized at some point, in which
						// case we shouldn't update it's hierarchy here
						if (!op.IsRegistered)
							continue;

						if (op.ChildData != null)
						{
							if ((forceSet || op.ChildData.Parent != parent) &&
								op.ChildData.Model)     // assume we're still initializing
							{
								SetCSGOperationHierarchy(op, parent, op.ChildData.Model);
							}
						}
						continue;
					}

					var brush = nodeObj as CSGBrush;
					if (brush)
					{
						// make sure the node has already been initialized, otherwise
						// assume it'll still get initialized at some point, in which
						// case we shouldn't update it's hierarchy here
						if (!brush.IsRegistered)
							continue;

						if (brush.ChildData != null)
						{
							if ((forceSet || brush.ChildData.Parent != parent) &&
								brush.ChildData.Model)	// assume we're still initializing
							{
								SetCSGBrushHierarchy(brush, parent, brush.ChildData.Model);
							}
						}
						continue;
					}
				}
				UpdateChildrenParent(parent, child);
			}
		}
		#endregion

		#region UpdateChildrenModel
		static void UpdateChildrenModel(CSGModel model, Transform container)
		{
			if (External == null)
			{
				return;
			}
			if (model == null)
				return;

			for (int i = 0; i < container.childCount; i++)
			{
				var child = container.GetChild(i);
				var nodeObj = child.GetComponent(TypeConstants.CSGNodeType);
				if (nodeObj)
				{
					var op = nodeObj as CSGOperation;
					if (op && 
						!op.PassThrough)
					{
						// make sure the node has already been initialized, otherwise
						// assume it'll still get initialized at some point, in which
						// case we shouldn't update it's hierarchy here
						if (!op.IsRegistered)
							continue;
						
						if (op.ChildData.Model != model)
						{
							if (model) // assume we're still initializing
							{
								SetCSGOperationHierarchy(op, op.ChildData.Parent, model);
							}
						} else
						{
							// assume that if this operation already has the 
							// correct model, then it's children will have the same model
							break;
						}
					}
					

					var brush = nodeObj as CSGBrush;
					if (brush)
					{
						// make sure the node has already been initialized, otherwise
						// assume it'll still get initialized at some point, in which
						// case we shouldn't update it's hierarchy here
						if (!brush.IsRegistered)
							continue;

						if (brush.ChildData.Model != model)
						{
							if (model) // assume we're still initializing
							{
								SetCSGBrushHierarchy(brush, brush.ChildData.Parent, model);
								InternalCSGModelManager.CheckSurfaceModifications(brush);
							}
						} else
						{
							// assume that if this brush already has the 
							// correct model, then it's children will have the same model
							break;
						}
					}
				}
				UpdateChildrenModel(model, child);
			}
		}
		#endregion

		#region OnOperationTransformChanged
		public static void OnOperationTransformChanged(CSGOperation op)
		{
			// unfortunately this event is sent before it's destroyed, so we need to defer it.
			OperationTransformChanged.Add(op);
		}
		#endregion

		#region OnBrushTransformChanged
		public static void OnBrushTransformChanged(CSGBrush brush)
		{
			// unfortunately this event is sent before it's destroyed, so we need to defer it.
			BrushTransformChanged.Add(brush);
		}
		#endregion
		
		#region SetNodeParent
		static void SetNodeParent(ChildNodeData childData, HierarchyItem hierarchyItem, CSGOperation parentOp, CSGModel parentModel)
		{
			var oldParentData = childData.OwnerParentData;

			childData.Parent = parentOp;
			childData.Model  = parentModel;			
			var newParentData = GetParentData(childData); 
			if (oldParentData != newParentData)
			{
				if (oldParentData != null) oldParentData.RemoveNode(hierarchyItem);
				if (newParentData != null) newParentData.AddNode(hierarchyItem);
				childData.OwnerParentData = newParentData;
				childData.ModelTransform = (!childData.Model) ? null : childData.Model.transform;
			}
		}
		#endregion
		
		#region SetCSGOperationHierarchy
		static void SetCSGOperationHierarchy(CSGOperation op, CSGOperation parentOp, CSGModel parentModel)
		{
			SetNodeParent(op.ChildData, op.ParentData, parentOp, parentModel);
/*			
			if (!operationCache.ChildData.Model)
				return;
			
			External.SetOperationHierarchy(op.operationNodeID,
										   operationCache.ChildData.modelNodeID,
										   operationCache.ChildData.parentNodeID);*/
		}
		#endregion

		#region CheckOperationHierarchy
		static void CheckOperationHierarchy(CSGOperation op)
		{
			if (External == null)
			{
				return;
			}

			if (!op || !op.gameObject.activeInHierarchy)
			{
				return;
			}

			// make sure the node has already been initialized, 
			// otherwise ignore it
			if (!op.IsRegistered)
			{
				return;
			}

			// NOTE: returns default model when it can't find parent model
			CSGModel parentModel;
			CSGOperation parentOp;
			FindParentOperationAndModel(op.transform, out parentOp, out parentModel);

			if (op.ChildData.Parent == parentOp &&
				op.ChildData.Model == parentModel)
				return;
			
			SetCSGOperationHierarchy(op, parentOp, parentModel);
		}
		#endregion

		#region SetCSGBrushHierarchy
		static void SetCSGBrushHierarchy(CSGBrush brush, CSGOperation parentOp, CSGModel parentModel)
		{
			SetNodeParent(brush.ChildData, brush.hierarchyItem, parentOp, parentModel);
/*
			if (!brushCache.childData.Model)
				return;

			External.SetBrushHierarchy(brush.brushNodeID,
									   brushCache.childData.modelNodeID,
									   brushCache.childData.parentNodeID);*/
		}
		#endregion

		#region CheckSiblingPosition
		static void CheckSiblingPosition(CSGBrush brush)
		{
			if (!brush || !brush.gameObject.activeInHierarchy)
				return;

			// NOTE: returns default model when it can't find parent model
			CSGModel parentModel;
			CSGOperation parentOp;
			FindParentOperationAndModel(brush.transform, out parentOp, out parentModel);
			if (!parentOp)
				return;

			if (brush.ChildData.Parent != parentOp || 
				brush.ChildData.Model != parentModel)
				return;
			
			ParentNodeDataExtensions.UpdateNodePosition(brush.hierarchyItem, parentOp.ParentData);
		}
		#endregion

		#region CheckBrushHierarchy
		static void CheckBrushHierarchy(CSGBrush brush)
		{
			if (External == null)
			{
				return;
			}

			if (!brush || !brush.gameObject.activeInHierarchy)
			{
				if (!brush && brush.IsRegistered)
					OnDestroyed(brush);
				return;
			}

			// make sure the node has already been initialized, 
			// otherwise ignore it
			if (!brush.IsRegistered)
			{
				return;
			}

			if (RemovedBrushes.Contains(brush.brushNodeID))
			{
				return;
			}

			// NOTE: returns default model when it can't find parent model
			CSGModel parentModel;
			CSGOperation parentOp;
			FindParentOperationAndModel(brush.transform, out parentOp, out parentModel);

			if (brush.ChildData.Parent == parentOp &&
				brush.ChildData.Model  == parentModel)
			{
				if (parentOp)
				{ 
					ParentNodeDataExtensions.UpdateNodePosition(brush.hierarchyItem, parentOp.ParentData);
					return;
				}
			}
			
			SetCSGBrushHierarchy(brush, parentOp, parentModel);
		}
		#endregion


		internal static double hierarchyValidateTime = 0.0;
		internal static double updateHierarchyTime = 0.0;

		public static void OnHierarchyModified()
		{
			if (External == null)
			{
				return;
			}

			HierarchyItem.CurrentLoopCount = (HierarchyItem.CurrentLoopCount + 1);

			UpdateRegistration();

			var startTime = EditorApplication.timeSinceStartup;
			if (OperationTransformChanged.Count > 0)
			{
				foreach (var item in OperationTransformChanged)
				{
					if (item) CheckOperationHierarchy(item);
				}
				OperationTransformChanged.Clear();
			}


			if (BrushTransformChanged.Count > 0)
			{
				foreach (var item in BrushTransformChanged)
				{
					if (!item)
						continue;

					CheckBrushHierarchy(item);
					ValidateBrush(item); // to detect material changes when moving between models
				}
				BrushTransformChanged.Clear();
			}
			hierarchyValidateTime = EditorApplication.timeSinceStartup - startTime;


			// remove all nodes that have been scheduled for removal
			if (RemovedBrushes   .Count > 0)
			{
				External.DestroyNodes(RemovedBrushes   .ToArray()); RemovedBrushes   .Clear();
			}
			if (RemovedOperations.Count > 0)
			{
				External.DestroyNodes(RemovedOperations.ToArray()); RemovedOperations.Clear();
			}
			if (RemovedModels    .Count > 0)
			{
				External.DestroyNodes(RemovedModels    .ToArray()); RemovedModels    .Clear();
			}
			

			for (var i = Brushes.Count - 1; i >= 0; i--)
			{
				var item = Brushes[i];
				if (item && item.brushNodeID != CSGNode.InvalidNodeID)
					continue;

				UnregisterBrush(item);
			}
			
			for (var i = Operations.Count - 1; i >= 0; i--)
			{
				var item = Operations[i];
				if (!item || item.operationNodeID == CSGNode.InvalidNodeID)
				{
					UnregisterOperation(item);
					continue;
				}
				
				if (!item.ParentData.Transform)
					item.ParentData.Init(item, item.operationNodeID);
			}

			for (var i = Models.Length - 1; i >= 0; i--)
			{
				var item = Models[i];
				if (!item || item.modelNodeID == CSGNode.InvalidNodeID)
				{
					UnregisterModel(item);
					continue;
				}
				
				if (!item.parentData.Transform)
					item.parentData.Init(item, item.modelNodeID);
			}

			startTime = EditorApplication.timeSinceStartup;
			for (var i = Operations.Count - 1; i >= 0; i--)
			{
				var item = Operations[i];
				if (!item || item.operationNodeID == CSGNode.InvalidNodeID)
					continue;
					
				var parentData = item.ChildData.OwnerParentData;
				if (parentData == null)
					continue;

				ParentNodeDataExtensions.UpdateNodePosition(item.ParentData, parentData);
			}

			for (var i = Brushes.Count - 1; i >= 0; i--)
			{
				var item = Brushes[i];
				if (!item || item.brushNodeID == CSGNode.InvalidNodeID)
					continue;
				
				var parentData	= item.ChildData.OwnerParentData;
				if (parentData == null)
					continue;

				ParentNodeDataExtensions.UpdateNodePosition(item.hierarchyItem, parentData);
			}

			if (External.SetChildNodes != null)
			{
				for (var i = 0; i < Operations.Count; i++)
				{
					var item = Operations[i];
					if (!item || item.operationNodeID == CSGNode.InvalidNodeID)
						continue;
					
					if (!item.ParentData.ChildrenModified)
						continue;
					
					var childList = UpdateChildList(item.ParentData);

					External.SetChildNodes(item.operationNodeID, childList.Length, childList);
					item.ParentData.ChildrenModified = false;
				}

				for (var i = 0; i < Models.Length; i++)
				{
					var item = Models[i];
					if (!item || item.modelNodeID == CSGNode.InvalidNodeID)
						continue;
					
					if (!item.parentData.ChildrenModified)
						continue;
					
					var childList = UpdateChildList(item.parentData);
					
					External.SetChildNodes(item.modelNodeID, childList.Length, childList);
					item.parentData.ChildrenModified = false;
				}
			}

			updateHierarchyTime = EditorApplication.timeSinceStartup - startTime;
		}
	}
}
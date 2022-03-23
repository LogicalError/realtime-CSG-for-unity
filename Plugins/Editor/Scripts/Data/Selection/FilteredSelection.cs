using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	[Serializable]
	internal sealed class FilteredSelection// : ScriptableObject
	{
		[SerializeField] public Transform[]		OtherTargets			= new Transform[0];
		[SerializeField] public CSGNode[]		NodeTargets				= new CSGNode[0];
		[SerializeField] public CSGBrush[]		BrushTargets			= new CSGBrush[0];
		[SerializeField] public CSGOperation[]	OperationTargets		= new CSGOperation[0];
		[SerializeField] public CSGModel[]		ModelTargets			= new CSGModel[0];

		[SerializeField] public CSGNode[]		RemovedNodes			= new CSGNode[0];
		[SerializeField] public Transform[]		RemovedOthers			= new Transform[0];

		[SerializeField] public CSGNode[]		AddedNodes				= new CSGNode[0];
		[SerializeField] public Transform[]		AddedOthers				= new Transform[0];

		public HashSet<CSGBrush> GetAllContainedBrushes()
		{
			var brushes = new HashSet<CSGBrush>();
			if (this.BrushTargets != null &&
				this.BrushTargets.Length > 0)
			{
				var brush_targets = this.BrushTargets;
				for (int i = 0; i < brush_targets.Length; i++)
				{
					var brush = brush_targets[i];
					if ((brush.Flags & BrushFlags.InfiniteBrush) == BrushFlags.InfiniteBrush)
						continue;
					brush.EnsureInitialized();
					if (brush.ControlMesh == null ||
						brush.Shape == null)
						continue;
					brushes.Add(brush);
				}
			}
			if (this.OperationTargets != null &&
				this.OperationTargets.Length > 0)
			{
				var operation_targets = this.OperationTargets;
				for (int i = 0; i < operation_targets.Length; i++)
				{
					var operation = operation_targets[i];
					var operation_brushes = operation.GetComponentsInChildren<CSGBrush>();
					for (int j = 0; j < operation_brushes.Length; j++)
					{
						var brush = operation_brushes[j];
						if ((brush.Flags & BrushFlags.InfiniteBrush) == BrushFlags.InfiniteBrush)
							continue;
						brush.EnsureInitialized();
						if (brush.ControlMesh == null ||
							brush.Shape == null)
							continue;
						brushes.Add(brush);
					}
				}
			}
			if (this.ModelTargets != null &&
				this.ModelTargets.Length > 0)
			{
				var model_targets = this.ModelTargets;
				for (int i = 0; i < model_targets.Length; i++)
				{
					var model = model_targets[i];
					var model_brushes = model.GetComponentsInChildren<CSGBrush>();
					for (int j = 0; j < model_brushes.Length; j++)
					{
						var brush = model_brushes[j];
						if ((brush.Flags & BrushFlags.InfiniteBrush) == BrushFlags.InfiniteBrush)
							continue;
						brush.EnsureInitialized();
						if (brush.ControlMesh == null ||
							brush.Shape == null)
							continue;
						brushes.Add(brush);
					}
				}
			}
			return brushes;
		} 

		public List<Transform> GetAllTopTransforms()
		{
			var transforms = new List<Transform>();
			if (this.BrushTargets != null &&
				this.BrushTargets.Length > 0)
			{
				foreach (var brush in this.BrushTargets)
				{
					transforms.Add(brush.transform);
				}
			}
			if (this.OperationTargets != null &&
				this.OperationTargets.Length > 0)
			{
				foreach (var operation in this.OperationTargets)
				{
					transforms.Add(operation.transform);
				}
			}
			if (this.ModelTargets != null &&
				this.ModelTargets.Length > 0)
			{
				foreach (var model in this.ModelTargets)
				{
					transforms.Add(model.transform);
				}
			}
			
			if (this.OtherTargets != null && 
				this.OtherTargets.Length > 0)
				transforms.AddRange(this.OtherTargets);

			for (int i = transforms.Count - 1; i >= 0; i--)
			{
				bool found = false;
				var iterator = transforms[i].parent;
				while (iterator != null)
				{
					if (transforms.Contains(iterator))
					{
						found = true;
						break;
					}
					iterator = iterator.parent;
				}
				if (found)
					transforms.RemoveAt(i);
			}
			return transforms;
		}
		
		bool Validate()
		{
			bool modified = false;
			if (this.OtherTargets != null &&
				this.OtherTargets.Length > 0) 
			{
				for (int i = this.OtherTargets.Length - 1; i >= 0; i--)
				{
					if (!this.OtherTargets[i])
					{
						ArrayUtility.RemoveAt(ref this.OtherTargets, i);
						modified = true;
					}
				}
				if (this.OtherTargets.Length == 0)
					this.OtherTargets = new Transform[0];
			}
			if (this.NodeTargets != null &&
				this.NodeTargets.Length > 0) 
			{
				for (int i = this.NodeTargets.Length - 1; i >= 0; i--)
				{
					if (!this.NodeTargets[i])
					{
						ArrayUtility.RemoveAt(ref this.NodeTargets, i);
						modified = true;
					}
				}
				if (this.NodeTargets.Length == 0)
				{
					this.NodeTargets = new CSGNode[0];
					this.ModelTargets = new CSGModel[0];
					this.BrushTargets = new CSGBrush[0];
					this.OperationTargets = new CSGOperation[0];
				} else
				{
					if (this.BrushTargets != null &&
						this.BrushTargets.Length > 0)
					{
						for (int i = this.BrushTargets.Length - 1; i >= 0; i--)
						{
							if (!this.BrushTargets[i])
							{
								ArrayUtility.RemoveAt(ref this.BrushTargets, i);
								modified = true;
							}
						}
						if (this.BrushTargets.Length == 0)
							this.BrushTargets = new CSGBrush[0];
					}
					if (this.OperationTargets != null &&
						this.OperationTargets.Length > 0)
					{
						for (int i = this.OperationTargets.Length - 1; i >= 0; i--)
						{
							if (!this.OperationTargets[i])
							{
								ArrayUtility.RemoveAt(ref this.OperationTargets, i);
								modified = true;
							}
						}
						if (this.OperationTargets.Length == 0)
							this.OperationTargets = new CSGOperation[0];
					}
					if (this.ModelTargets != null &&
						this.ModelTargets.Length > 0)
					{
						for (int i = this.ModelTargets.Length - 1; i >= 0; i--)
						{
							if (!this.ModelTargets[i])
							{
								ArrayUtility.RemoveAt(ref this.ModelTargets, i);
								modified = true;
							}
						}
						if (this.ModelTargets.Length == 0)
							this.ModelTargets = new CSGModel[0];
					}
				}
			}
			return modified;
		}
		
		public bool UpdateSelection(HashSet<CSGNode> newTargetNodes, HashSet<Transform> newTargetOthers)
		{
			if (newTargetNodes  == null)
				newTargetNodes  = new HashSet<CSGNode>();
			if (newTargetOthers  == null)
				newTargetOthers  = new HashSet<Transform>();

			this.RemovedNodes	= null;
			this.RemovedOthers	= null;

			this.AddedNodes		= null;
			this.AddedOthers	= null;

			var foundRemovedNodes	= new List<CSGNode>();
			if (this.NodeTargets != null &&
				this.NodeTargets.Length > 0)
			{
				if (newTargetNodes.Count == 0)
				{
					foundRemovedNodes.AddRange(this.NodeTargets);
				} else
				{
					for (int i = 0; i < this.NodeTargets.Length; i++)
					{
						if (!this.NodeTargets[i] ||
							!newTargetNodes.Contains(this.NodeTargets[i]))
						{
							foundRemovedNodes.Add(this.NodeTargets[i]);
						}
					}
				}
			}
			
			var foundRemovedOthers	= new List<Transform>();
			if (this.OtherTargets != null &&
				this.OtherTargets.Length > 0)
			{
				if (newTargetOthers.Count == 0)
				{
					foundRemovedOthers.AddRange(this.OtherTargets);
				} else
				{
					for (int i = 0; i < this.OtherTargets.Length; i++)
					{
						if (!this.OtherTargets[i] ||
							!newTargetOthers.Contains(this.OtherTargets[i]))
						{
							foundRemovedOthers.Add(this.OtherTargets[i]);
						}
					}
				}
			}

			var originalTargetNodeCount		= (this.NodeTargets  == null) ? 0 : this.NodeTargets.Length;
			var originalTargetOtherCount	= (this.OtherTargets == null) ? 0 : this.OtherTargets.Length;

			// If our counts are the same and nothing is removed (and nothing could've been added), nothing has changed.
			if (newTargetNodes .Count == originalTargetNodeCount &&
				newTargetOthers.Count == originalTargetOtherCount &&
				foundRemovedNodes.Count == 0 &&
				foundRemovedOthers.Count == 0)
			{
				return false;
			}

			Validate();

			foreach(var node in foundRemovedNodes)
			{
				ArrayUtility.Remove(ref this.NodeTargets, node);

				var brush = node as CSGBrush;
				if (brush != null)
				{
					ArrayUtility.Remove(ref this.BrushTargets, brush);
					continue;
				}
				var operation = node as CSGOperation;
				if (node is CSGOperation)
				{
					ArrayUtility.Remove(ref this.OperationTargets, operation);
					continue;
				}
				var model = node as CSGModel;
				if (node is CSGModel)
				{					
					ArrayUtility.Remove(ref this.ModelTargets, model);
					continue;
				}
			}

			foreach (var other in foundRemovedOthers)
			{
				ArrayUtility.Remove(ref this.OtherTargets, other);
			}
			
			var foundAddedNodes	= new List<CSGNode>();
			foreach (var node in newTargetNodes)
			{
				if (this.NodeTargets != null &&
					ArrayUtility.Contains(this.NodeTargets, node))
					continue;

				if (this.NodeTargets == null)
				{
					this.NodeTargets = new CSGNode[] { node };
				} else
				{
					ArrayUtility.Add(ref this.NodeTargets, node);
				}

				foundAddedNodes.Add(node);

				if (CSGPrefabUtility.IsPrefabAsset(node.gameObject))
					continue;

				var brush = node as CSGBrush;
				if (brush != null)
				{					
					if (this.BrushTargets == null)
						this.BrushTargets = new CSGBrush[] { brush };
					else
						ArrayUtility.Add(ref this.BrushTargets, brush);
					continue;
				}
				var operation = node as CSGOperation;
				if (node is CSGOperation)
				{
					if (this.OperationTargets == null)
						this.OperationTargets = new CSGOperation[] { operation };
					else
						ArrayUtility.Add(ref this.OperationTargets, operation);
					continue;
				}
				var model = node as CSGModel;
				if (node is CSGModel)
				{					
					if (this.ModelTargets == null)
						this.ModelTargets = new CSGModel[] { model };
					else
						ArrayUtility.Add(ref this.ModelTargets, model);
					continue;
				}
			}
			
			var foundAddedOthers	= new List<Transform>();
			foreach (var other in newTargetOthers)
			{
				if (this.OtherTargets != null &&
					ArrayUtility.Contains(this.OtherTargets, other))
					continue;
								
				if (this.OtherTargets == null)
					this.OtherTargets = new Transform[] { other };
				else
					ArrayUtility.Add(ref this.OtherTargets, other);
				foundAddedOthers.Add(other);
			}

			for (int i = foundAddedNodes.Count - 1; i >= 0; i--)
			{
				var node = foundAddedNodes[i];
				var brush = node as CSGBrush;
				if (brush != null)
				{
					if (brush.ChildData == null ||
						brush.ChildData.Model == null)
						continue;

					var childModel = brush.ChildData.Model;
                    if (ModelTraits.IsModelEditable(childModel))
						SelectionUtility.LastUsedModel = childModel;
					break;
				}
				var operation = node as CSGOperation;
				if (operation != null)
				{
					if (operation.ChildData == null ||
						operation.ChildData.Model == null)
						continue;

					SelectionUtility.LastUsedModel = operation.ChildData.Model;
					break;
				}
				var model = node as CSGModel;
                if (ModelTraits.IsModelEditable(model))
				{
					SelectionUtility.LastUsedModel = model;
					break;
				}
			}

			this.RemovedNodes	= foundRemovedNodes.ToArray();
			this.RemovedOthers	= foundRemovedOthers.ToArray();

			this.AddedNodes		= foundAddedNodes.ToArray();
			this.AddedOthers	= foundAddedOthers.ToArray();

			return (foundAddedNodes.Count > 0 || foundRemovedNodes.Count > 0 || foundAddedOthers.Count > 0 || foundRemovedOthers.Count > 0);
		}
	}
}

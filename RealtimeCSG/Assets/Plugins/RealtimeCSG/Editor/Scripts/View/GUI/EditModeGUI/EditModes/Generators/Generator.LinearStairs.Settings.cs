using System;
using UnityEditor;
using UnityEngine;
using RealtimeCSG.Legacy;

namespace RealtimeCSG
{
	[Serializable]
	public enum StairsBottom
	{
		Filled,
		Steps,
		//Sloped,
	}

	[Serializable]
    internal sealed class GeneratorLinearStairsSettings : IShapeSettings
	{
		public const float	kMinStepDepth	= 0.1f;
		public const float	kMinStepHeight	= 0.1f;
		public const int	kMaxSteps		= 300;

		public AABB         bounds				= new AABB();
		public Vector3[]	vertices			= new Vector3[0];
		public int[]		vertexIDs			= new int[0];
        public bool[]		onGeometryVertices	= new bool[0];

		[SerializeField] float stepDepth;
		public float		StepDepth
		{
			get { return stepDepth; }
			private set { if (stepDepth == value) return; stepDepth = value; RealtimeCSG.CSGSettings.LinearStairsStepLength = value; RealtimeCSG.CSGSettings.Save(); }
		}
		
		[SerializeField] float stepHeight;
		public float		StepHeight
		{
			get { return stepHeight; }
			private set { if (stepHeight == value) return; stepHeight = value; RealtimeCSG.CSGSettings.LinearStairsStepHeight = value; RealtimeCSG.CSGSettings.Save(); }
		}
		
		[SerializeField] float stairsWidth;
		public float		StairsWidth
		{
			get { return stairsWidth; }
			private set { if (stairsWidth == value) return; stairsWidth = value; RealtimeCSG.CSGSettings.LinearStairsStepWidth = value; RealtimeCSG.CSGSettings.Save(); }
		}
		
		[SerializeField] int totalSteps;
		public int			TotalSteps
		{
			get { return Mathf.Clamp(totalSteps, 1, kMaxSteps); }
			private set { if (totalSteps == value) return; totalSteps = Mathf.Clamp(value, 1, kMaxSteps); RealtimeCSG.CSGSettings.LinearStairsTotalSteps = value; RealtimeCSG.CSGSettings.Save(); }
		}
		
		[SerializeField] float stairsDepth;
		public float		StairsDepth
		{
			get { return stairsDepth; }
			private set { if (stairsDepth == value) return; stairsDepth = value; RealtimeCSG.CSGSettings.LinearStairsLength = value; RealtimeCSG.CSGSettings.Save(); }
		}
		
		[SerializeField] float stairsHeight;
		public float		StairsHeight
		{
			get { return stairsHeight; }
			private set { if (stairsHeight == value) return; stairsHeight = value; RealtimeCSG.CSGSettings.LinearStairsHeight = value; RealtimeCSG.CSGSettings.Save(); }
		}
		
		[SerializeField] float extraDepth;
		public float		ExtraDepth
		{
			get { return extraDepth; }
			private set { if (extraDepth == value) return; extraDepth = value; RealtimeCSG.CSGSettings.LinearStairsLengthOffset = value; RealtimeCSG.CSGSettings.Save(); }
		}
		
		[SerializeField] float extraHeight;
		public float		ExtraHeight
		{
			get { return extraHeight; }
			private set { if (extraHeight == value) return; extraHeight = value; RealtimeCSG.CSGSettings.LinearStairsHeightOffset = value; RealtimeCSG.CSGSettings.Save(); }
		}
		
		[SerializeField] StairsBottom stairsBottom;
		public StairsBottom	StairsBottom
		{
			get { return stairsBottom; }
			set { if (stairsBottom == value) return; stairsBottom = value; RealtimeCSG.CSGSettings.LinearStairsBottom = value; RealtimeCSG.CSGSettings.Save(); }
		}
		

		public void CalcTotalSteps()
		{
			totalSteps	= Mathf.Max(1, Mathf.FloorToInt((stairsHeight - extraHeight) / stepHeight));
		}

		public void SetStairsWidth(float newWidth)
		{
			var newBounds = bounds;
			if (newWidth >= 0)
				newBounds.MaxX = newBounds.MinX + newWidth;
			else
				newBounds.MinX = newBounds.MaxX - newWidth;
			SetBounds(newBounds);
		}

		public void SetStairsHeight(float newHeight)
		{
			var newBounds = bounds;
			if (newHeight >= 0)
				newBounds.MaxY = newBounds.MinY + newHeight;
			else
				newBounds.MinY = newBounds.MaxY - newHeight;
			SetBounds(newBounds);
		}

		public void SetStairsDepth(float newDepth)
		{
			var newBounds = bounds;
			if (newDepth >= 0)
				newBounds.MaxZ = newBounds.MinZ + newDepth;
			else
				newBounds.MinZ = newBounds.MaxZ - newDepth;
			SetBounds(newBounds);
		}

		public void SetBounds(AABB newBounds, bool autoAdjust = true)
		{
			var boundsSize	= newBounds.Size;
			stairsWidth		= GeometryUtility.CleanLength(boundsSize.x);
			stairsHeight	= GeometryUtility.CleanLength(boundsSize.y);
			stairsDepth		= GeometryUtility.CleanLength(boundsSize.z);

			if (float.IsInfinity(stairsWidth	) || float.IsNaN(stairsWidth )) stairsWidth  = 1.0f;
			if (float.IsInfinity(stairsDepth	) || float.IsNaN(stairsDepth )) stairsDepth  = stepDepth;
			if (float.IsInfinity(stairsHeight   ) || float.IsNaN(stairsHeight)) stairsHeight = stepHeight;
			stairsWidth		= Mathf.Max(0, stairsWidth);
			stairsDepth		= Mathf.Max(0, stairsDepth);
			stairsHeight	= Mathf.Max(0, stairsHeight);

			if (autoAdjust)
			{ 
				if (!bounds.IsEmpty())
				{ 
					float offsetY = (newBounds.MaxY - bounds.MaxY) + (bounds.MinY - newBounds.MinY);					
					float offsetZ = (newBounds.MaxZ - bounds.MaxZ) + (bounds.MinZ - newBounds.MinZ);

					if (offsetY != 0) // scaling in height direction
					{
						if (offsetY > 0) // growing
						{
							if (extraDepth > 0)
							{
								extraDepth = stairsDepth - ((Mathf.Max(0, stairsHeight - extraHeight) / stepHeight) * stepDepth);
							} else
							{
								extraDepth  -= offsetY; 
								extraHeight += offsetY; 
							}
						} else // shrinking
						{
							extraHeight += offsetY; 
							extraDepth = stairsDepth - ((Mathf.Max(0, stairsHeight - extraHeight) / stepHeight) * stepDepth);
						}
					}
					
					if (offsetZ != 0) // scaling in depth direction
					{
						if (offsetZ > 0) // growing
						{
							if (extraHeight > 0)
							{
								extraHeight = stairsHeight - ((Mathf.Max(0, stairsDepth - extraDepth) / stepDepth) * stepHeight);
							} else
							{
								extraDepth += offsetZ; if (extraDepth < 0) extraDepth = 0;
								extraHeight -= offsetZ; 
							}
						} else // shrinking
						{
							if (extraDepth > 0) extraDepth = Mathf.Max(0, extraDepth + offsetZ);
							extraHeight = stairsHeight - ((Mathf.Max(0, stairsDepth - extraDepth) / stepDepth) * stepHeight);
						}
						extraDepth = stairsDepth - ((Mathf.Max(0, stairsHeight - extraHeight) / stepHeight) * stepDepth);
					}
				
					if (extraDepth  < 0) extraDepth  = 0;
					if (extraHeight < 0) extraHeight = 0;
				} else
				{
					extraDepth  = Mathf.Max(0, stairsDepth - (totalSteps * stepDepth));
					extraHeight = 0;
				}
				CalcTotalSteps();

				if ((totalSteps * stepDepth) > stairsDepth)
				{
					var newTotalSteps = Mathf.Max(1, Mathf.FloorToInt((stairsDepth - extraDepth) / stepDepth));
					extraHeight += (totalSteps - newTotalSteps) * stepHeight;
					totalSteps = newTotalSteps;
				}

			}

			bounds		= newBounds;
		}

		public void SetExtraHeight(float newHeightOffset)
		{
			extraHeight = Mathf.Min(stairsHeight, Mathf.Max(0, newHeightOffset));
			var totalStepHeight = (stairsHeight - extraHeight);
			var totalStepLength = (stairsDepth - extraDepth);
			var totalHeightSteps = Mathf.Max(1, totalStepHeight / stepHeight);
			stepDepth = Mathf.Max(kMinStepDepth, totalStepLength / totalHeightSteps);
			CalcTotalSteps();
		}
		
		public void SetExtraDepth(float newLengthOffset)
		{
			CalcTotalSteps();
			var minTotalStepLength	= totalSteps * kMinStepDepth;
			extraDepth = Mathf.Min(stairsDepth - minTotalStepLength, Mathf.Max(0, newLengthOffset));
			var totalStepHeight = (stairsHeight - extraHeight);
			var totalStepLength = (stairsDepth - extraDepth);
			var totalHeightSteps = Mathf.Max(1, totalStepHeight / stepHeight);
			stepDepth = Mathf.Max(kMinStepDepth, totalStepLength / totalHeightSteps);
		}
				
		public void SetStepDepth(float newStepDepth)
		{
			extraDepth = stairsDepth - (totalSteps * stepDepth);
			stepDepth = Mathf.Max(kMinStepDepth, newStepDepth);
			var totalDepth = (stepDepth * totalSteps) + extraDepth;
			var newBounds = bounds;
			if (totalDepth >= 0)	newBounds.MaxZ = newBounds.MinZ + totalDepth;
			else					newBounds.MinZ = newBounds.MaxZ - totalDepth;
			SetBounds(newBounds, autoAdjust: false);
			CalcTotalSteps();
		}
				
		public void SetStepHeight(float newStepHeight)
		{
			stepHeight = Mathf.Max(kMinStepHeight, newStepHeight);
			CalcTotalSteps();
		}
				
		public void SetTotalSteps(int newTotalSteps)
		{
			extraDepth = stairsDepth - ((Mathf.Max(0, stairsHeight - extraHeight) / stepHeight) * stepDepth);
			var totalDepth	= (stepDepth  * newTotalSteps) + extraDepth;
			var totalHeight = (stepHeight * newTotalSteps) + extraHeight;

			totalSteps = newTotalSteps;
			
			var newBounds = bounds;
			if (totalHeight >= 0)	newBounds.MaxY = newBounds.MinY + totalHeight;
			else					newBounds.MinY = newBounds.MaxY - totalHeight;
			if (totalDepth >= 0)	newBounds.MaxZ = newBounds.MinZ + totalDepth;
			else					newBounds.MinZ = newBounds.MaxZ - totalDepth;
			SetBounds(newBounds, autoAdjust: false);
		}


			

		public void AddPoint(Vector3 position)
        {
            ArrayUtility.Add(ref vertices, position);
		}

		public void SetPoint(int index, Vector3 position)
        {
			if (vertices.Length < index)
				return;

			if (vertices.Length == index)
			{
				ArrayUtility.Add(ref vertices, position);
				return;
			}
			vertices[index] = position;
		}


		public void CalculatePlane(ref CSGPlane plane)
		{
		}
		
		public void Reset()
		{
			stepDepth		= RealtimeCSG.CSGSettings.LinearStairsStepLength; 
			stepHeight		= RealtimeCSG.CSGSettings.LinearStairsStepHeight;
			stairsWidth		= RealtimeCSG.CSGSettings.LinearStairsStepWidth;
			totalSteps		= Mathf.Max(1, RealtimeCSG.CSGSettings.LinearStairsTotalSteps);
			stairsDepth		= RealtimeCSG.CSGSettings.LinearStairsLength;
			stairsHeight	= RealtimeCSG.CSGSettings.LinearStairsHeight;
			extraDepth		= RealtimeCSG.CSGSettings.LinearStairsLengthOffset;
			extraHeight		= RealtimeCSG.CSGSettings.LinearStairsHeightOffset;
			stairsBottom	= RealtimeCSG.CSGSettings.LinearStairsBottom;

			vertices	= new Vector3[0];
			extraDepth  = 0;
			extraHeight = 0;
			bounds.Reset();
		}

		public Vector3[] GetVertices(CSGPlane buildPlane, Vector3 worldPosition, Vector3 gridTangent, Vector3 gridBinormal, out bool isValid)
		{
			isValid = true;
			return vertices;
		}
		
		public void ProjectShapeOnBuildPlane(CSGPlane plane)
		{
			for (int i = 0; i < vertices.Length; i++)
				vertices[i] = plane.Project(vertices[i]);
        }

        public void MoveShape(Vector3 offset)
        {
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = vertices[i] + offset;
        }

        public Vector3 GetCenter(CSGPlane plane)
		{
			return vertices[0];
		}

		public RealtimeCSG.AABB CalculateBounds(Quaternion rotation, Vector3 gridTangent, Vector3 gridBinormal)
		{
			var bounds = new RealtimeCSG.AABB();
			bounds.Reset();

			for (int i = 0; i < vertices.Length; i++)
				bounds.Extend(rotation * vertices[i]);
			
			return bounds;
		}
	}
}

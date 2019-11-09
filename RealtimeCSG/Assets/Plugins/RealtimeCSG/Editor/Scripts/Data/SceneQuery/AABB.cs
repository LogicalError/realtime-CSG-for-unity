#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Runtime.InteropServices;

namespace RealtimeCSG
{
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	internal struct AABB
	{
		public float MinX;
		public float MaxX;

		public float MinY;
		public float MaxY;

		public float MinZ;
		public float MaxZ;

	    public static readonly AABB Empty = new AABB
        {
            MinX = float.PositiveInfinity,
            MinY = float.PositiveInfinity,
			MinZ = float.PositiveInfinity,
						
			MaxX = float.NegativeInfinity,
			MaxY = float.NegativeInfinity,
			MaxZ = float.NegativeInfinity
        };

		public Vector3 Center
		{
			get
			{
				return new Vector3((MinX + MaxX) * 0.5f, (MinY + MaxY) * 0.5f, (MinZ + MaxZ) * 0.5f);
			}
		}

		public Vector3 Size
		{
			get
			{
				return new Vector3(MaxX - MinX, MaxY - MinY, MaxZ - MinZ);
			}
		}

		public Vector3 Min
		{
			get
			{
				return new Vector3(MinX, MinY, MinZ);
			}
			set
			{
				MinX = value.x;
				MinY = value.y;
				MinZ = value.z;
			}
		}

		public Vector3 Max
		{
			get
			{
				return new Vector3(MaxX, MaxY, MaxZ);
			}
			set
			{
				MaxX = value.x;
				MaxY = value.y;
				MaxZ = value.z;
			}
		}

		public bool Valid
		{
			get
			{
				return !(float.IsNaN(MinX) || float.IsNaN(MaxX) || float.IsInfinity(MinX) || float.IsInfinity(MaxX) ||
						 float.IsNaN(MinY) || float.IsNaN(MaxY) || float.IsInfinity(MinY) || float.IsInfinity(MaxY) ||
						 float.IsNaN(MinZ) || float.IsNaN(MaxZ) || float.IsInfinity(MinZ) || float.IsInfinity(MaxZ));
			}
		}

		public void Reset()
		{
		    this = Empty;
		}

		public void Extend(Vector3 point)
		{
			MinX = Mathf.Min(MinX, point.x);
			MinY = Mathf.Min(MinY, point.y);
			MinZ = Mathf.Min(MinZ, point.z);
						
			MaxX = Mathf.Max(MaxX, point.x);
			MaxY = Mathf.Max(MaxY, point.y);
			MaxZ = Mathf.Max(MaxZ, point.z);
		}

		public void Translate(Vector3 point)
		{
			MinX += point.x;
			MinY += point.y;
			MinZ += point.z;

			MaxX += point.x;
			MaxY += point.y;
			MaxZ += point.z;
		}

		public void Extrude(Vector3 size)
		{
			var oldMinX = MinX;
			var oldMinY = MinY;
			var oldMinZ = MinZ;
			
			var oldMaxX = MaxX;
			var oldMaxY = MaxY;
			var oldMaxZ = MaxZ;

			MinX = Mathf.Min(MinX, oldMinX + size.x);
			MinY = Mathf.Min(MinY, oldMinY + size.y);
			MinZ = Mathf.Min(MinZ, oldMinZ + size.z);
			
			MinX = Mathf.Min(MinX, oldMaxX + size.x);
			MinY = Mathf.Min(MinY, oldMaxY + size.y);
			MinZ = Mathf.Min(MinZ, oldMaxZ + size.z);
						
			MaxX = Mathf.Max(MaxX, oldMinX + size.x);
			MaxY = Mathf.Max(MaxY, oldMinY + size.y);
			MaxZ = Mathf.Max(MaxZ, oldMinZ + size.z);
						
			MaxX = Mathf.Max(MaxX, oldMaxX + size.x);
			MaxY = Mathf.Max(MaxY, oldMaxY + size.y);
			MaxZ = Mathf.Max(MaxZ, oldMaxZ + size.z);
		}

		public void Add(AABB other)
		{
			Extend(new Vector3(other.MinX, other.MinY, other.MinZ));
			Extend(new Vector3(other.MaxX, other.MaxY, other.MaxZ));
		}
		public void Add(AABB other, Vector3 offset)
		{
			Extend(new Vector3(other.MinX + offset.x, other.MinY + offset.y, other.MinZ + offset.z));
			Extend(new Vector3(other.MaxX + offset.x, other.MaxY + offset.y, other.MaxZ + offset.z));
		}

		public bool IsEmpty()
		{
			return	float.IsInfinity(MinX) ||
					float.IsInfinity(MinY) ||
					float.IsInfinity(MinZ) ||
					float.IsInfinity(MaxX) ||
					float.IsInfinity(MaxY) ||
					float.IsInfinity(MaxZ);
		}

		public Vector3[] GetCorners()
		{
			if (MinX == MaxX)
			{
				if (MinY == MaxY)
				{
					if (MinZ == MaxZ)
					{
						return new Vector3[] { new Vector3(MinX, MinY, MinZ) };
					} else
					{
						return new Vector3[]
						{
							new Vector3(MinX, MinY, MinZ),
							new Vector3(MinX, MinY, MaxZ)
						};
					}
				} else
				{
					if (MinZ == MaxZ)
					{
						return new Vector3[]
						{
							new Vector3(MinX, MinY, MinZ),
							new Vector3(MinX, MaxY, MinZ),

							new Vector3(MinX, MinY, MaxZ),
							new Vector3(MinX, MaxY, MaxZ)
						};
					} else
					{
						return new Vector3[]
						{
							new Vector3(MinX, MinY, MinZ),
							new Vector3(MinX, MaxY, MinZ)
						};
					}
				}
			} else
			{
				if (MinY == MaxY)
				{
					if (MinZ == MaxZ)
					{
						return new Vector3[]
						{
							new Vector3(MinX, MaxY, MinZ),
							new Vector3(MaxX, MaxY, MinZ)
						};
					} else
					{
						return new Vector3[]
						{
							new Vector3(MinX, MaxY, MinZ),
							new Vector3(MaxX, MaxY, MinZ),

							new Vector3(MinX, MaxY, MaxZ),
							new Vector3(MaxX, MaxY, MaxZ)
						};
					}
				} else
				{
					return new Vector3[]
					{
						new Vector3(MinX, MinY, MinZ),
						new Vector3(MinX, MaxY, MinZ),
						new Vector3(MaxX, MaxY, MinZ),
						new Vector3(MaxX, MinY, MinZ),

						new Vector3(MinX, MinY, MaxZ),
						new Vector3(MinX, MaxY, MaxZ),
						new Vector3(MaxX, MaxY, MaxZ),
						new Vector3(MaxX, MinY, MaxZ)
					};
				}
			}

		}

		public Vector3[] GetAllCorners()
		{
			return new Vector3[]
			{
				new Vector3(MinX, MinY, MinZ),
				new Vector3(MinX, MaxY, MinZ),
				new Vector3(MaxX, MaxY, MinZ),
				new Vector3(MaxX, MinY, MinZ),

				new Vector3(MinX, MinY, MaxZ),
				new Vector3(MinX, MaxY, MaxZ),
				new Vector3(MaxX, MaxY, MaxZ),
				new Vector3(MaxX, MinY, MaxZ)
			};
		}

		public override string ToString()
		{
			return new Vector3(MinX, MinY, MinZ).ToString() + " " + new Vector3(MaxX, MaxY, MaxZ).ToString();
 		}
	}
}
#endif

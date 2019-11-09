using System;
using UnityEngine;
using System.Runtime.InteropServices;

namespace RealtimeCSG.Legacy
{
	/// <summary>Defines how a surface is orientated in space, and an index to a <see cref="RealtimeCSG.Legacy.TexGen"/>.</summary>
	/// <remarks><note>This code is legacy and will be removed eventually.</note></remarks>
	/// <seealso cref="RealtimeCSG.Legacy.Shape"/>
	/// <seealso cref="RealtimeCSG.Legacy.ControlMesh"/>
	/// <seealso cref="RealtimeCSG.Legacy.Polygon"/>
	/// <seealso cref="RealtimeCSG.Legacy.TexGen"/>
	/// <seealso cref="RealtimeCSG.Legacy.TexGenFlags"/>
	[Serializable, StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct Surface
	{
		/// <value>The geometric Plane this surface lies on</value>
		public CSGPlane		Plane;

		/// <value>The tangent vector for this surface</value>
		public Vector3		Tangent;
		
		/// <value>The binormal vector for this surface</value>
		public Vector3		BiNormal;
		
		/// <value>Index to a <see cref="RealtimeCSG.Legacy.TexGen"/> that is in the same <see cref="RealtimeCSG.Legacy.Shape"/> as this <see cref="RealtimeCSG.Legacy.Surface"/></value>
		public Int32		TexGenIndex;
		
		/// <summary>Generates a matrix to transform from local brush space to plane space (z is up)</summary>
		/// <returns>Transformation matrix from local brush to plane space</returns>
		public Matrix4x4 GenerateLocalBrushSpaceToPlaneSpaceMatrix()
		{
			var normal		 = Plane.normal;
			var tangent		 = Tangent;
			var biNormal	 = BiNormal;
			var pointOnPlane = Plane.pointOnPlane;
			
			return new Matrix4x4()
			{ 
				m00 = tangent.x,  m01 = tangent.y,  m02 = tangent.z,  m03 = Vector3.Dot(tangent,  pointOnPlane),
				m10 = biNormal.x, m11 = biNormal.y, m12 = biNormal.z, m13 = Vector3.Dot(biNormal, pointOnPlane),
				m20 = normal.x,   m21 = normal.y,   m22 = normal.z,   m23 = Vector3.Dot(normal,   pointOnPlane),
				m30 = 0.0f, m31 = 0.0f, m32 = 0.0f, m33 = 1.0f
			};
		}

		public override string ToString() { return string.Format("Plane: {0} Tangent: {1} BiNormal: {2} TexGenIndex: {3}", Plane, Tangent, BiNormal, TexGenIndex); }
	}
}

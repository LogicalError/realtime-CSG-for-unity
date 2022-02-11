using UnityEngine;

namespace InternalRealtimeCSG
{
	public static class MathConstants
	{
		public const float	SnapDistanceSqr			= 0.0001f;
		public const float	AngleEpsilon			= 0.05f;
		public const float	DistanceEpsilon			= 0.00015f;
		public const float	EqualityEpsilon			= 0.0001f;
		public const float  EqualityEpsilonSqr		= (EqualityEpsilon   * EqualityEpsilon  );// + (0*0) + (0*0)
		public const float	ConvexTestEpsilon		= 0.000000001f; 
		public const float  ConvexTestEpsilonSqr3   = (ConvexTestEpsilon * ConvexTestEpsilon);// + (0*0) + (0*0)
		public const float	AlignmentTestEpsilon	= 0.000001f; 
		public const float	AABBTestEpsilon			= 0.000002f;
		public const float	NormalEpsilon			= 0.0001f;
		public const float	MinimumScale			= 0.00001f;
		public const float	GUIAlignmentTestEpsilon	= 0.01f; 
		
		public const float	ZeroAreaTest			= 0.00000001f;
		
		public const float	ConsideredZero			= 0.001f;
		public const float	MinimumHeight			= 0.001f;
		public const float  MinimumMouseMovement	= 2.0f;

		public static readonly Vector3 zeroVector3				= Vector3.zero;
		public static readonly Vector3 oneVector3				= Vector3.one;
		public static readonly Vector3 unitXVector3				= new Vector3(1,0,0);
		public static readonly Vector3 unitYVector3				= new Vector3(0,1,0);
		public static readonly Vector3 unitZVector3				= new Vector3(0,0,1);

		public static readonly Vector3 upVector3				= Vector3.up;
		public static readonly Vector3 forwardVector3			= Vector3.forward;
		public static readonly Vector3 rightVector3				= Vector3.right;
		public static readonly Vector3 leftVector3				= Vector3.left;
		public static readonly Vector3 downVector3				= Vector3.down;
		public static readonly Vector3 backVector3				= Vector3.back;
		public static readonly Vector3 NaNVector3				= new Vector3(float.NaN, float.NaN, float.NaN);
		public static readonly Vector3 PositiveInfinityVector3	= new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
		public static readonly Vector3 NegativeInfinityVector3	= new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

		public static readonly Vector2 zeroVector2		= Vector2.zero;
		public static readonly Vector2 oneVector2		= Vector2.one;
		public static readonly Vector2 upVector2		= Vector2.up;
		
		public static readonly Ray emptyRay				= new Ray();

		public static readonly Matrix4x4 identityMatrix			= Matrix4x4.identity;
		public static readonly Quaternion identityQuaternion	= Quaternion.identity;
		public static readonly Quaternion YQuaternion			= Quaternion.AngleAxis(90, Vector3.left);
		public static readonly Quaternion ZQuaternion			= Quaternion.AngleAxis(90, Vector3.forward);
		public static readonly Quaternion XQuaternion			= Quaternion.AngleAxis(90, Vector3.up);

	}
}

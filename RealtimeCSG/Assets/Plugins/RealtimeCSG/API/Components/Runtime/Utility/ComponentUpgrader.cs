using System;
using UnityEngine;
using InternalRealtimeCSG;
using UnityEngine.Rendering;
using RealtimeCSG.Legacy;

namespace RealtimeCSG.Components
{
#if UNITY_EDITOR
	internal static class ComponentUpgrader
	{
		public static void UpgradeWhenNecessary(CSGModel model)
		{
			if (model.Version >= CSGModel.CurrentVersion)
				return;

			if (model.Version < 1.0f)
				model.Version = 1.0f;

			if (model.Version == 1.0f)
			{
//if !PACKAGE_GENERATOR_ACTIVE
				// Unity defaults are horrible
				//UnityEditor.UnwrapParam uvGenerationSettings;
				//UnityEditor.UnwrapParam.SetDefaults(out uvGenerationSettings);
				model.angleError	= 1;//uvGenerationSettings.angleError;
				model.areaError		= 1;//uvGenerationSettings.areaError;
				model.hardAngle		= 60;//uvGenerationSettings.hardAngle;
				model.packMargin	= 20;//uvGenerationSettings.packMargin;
//#endif
				model.Version = 1.1f;
			}

			model.angleError = Mathf.Clamp(model.angleError, CSGModel.MinAngleError, CSGModel.MaxAngleError);
			model.areaError = Mathf.Clamp(model.areaError, CSGModel.MinAreaError, CSGModel.MaxAreaError);

			model.Version = CSGModel.CurrentVersion;
		}

		public static void UpgradeWhenNecessary(CSGBrush brush)
		{
			if (brush.Version >= CSGBrush.CurrentVersion)
				return;

			if (brush.Version < 1.0f)
				brush.Version = 1.0f;

			if (brush.Version == 1.0f)
			{
#pragma warning disable 618 // Type is now obsolete
				if (brush.Shape.Materials != null && brush.Shape.Materials.Length > 0)
				{
					// update textures
					if (brush.Shape.TexGens != null)
					{
						for (int i = 0; i < brush.Shape.TexGens.Length; i++)
						{ 
							brush.Shape.TexGens[i].RenderMaterial = null;
						}
						
#pragma warning disable 618 // Type is now obsolete
						for (int i = 0; i < Mathf.Min(brush.Shape.Materials.Length, brush.Shape.TexGens.Length); i++) 
						{
#pragma warning disable 618 // Type is now obsolete
							brush.Shape.TexGens[i].RenderMaterial = brush.Shape.Materials[i];
						}

						for (int i = 0; i < brush.Shape.TexGenFlags.Length; i++)
						{
							var oldFlags			= (int)brush.Shape.TexGenFlags[i];
							var isWorldSpaceTexture	= (oldFlags & 1) == 1;

							var isNotVisible		= (oldFlags & 2) == 2;
							var isNoCollision		= isNotVisible;
							var isNotCastingShadows	= ((oldFlags & 4) == 0) && !isNotVisible;

							TexGenFlags newFlags = (TexGenFlags)0;
							if (isNotVisible)		 newFlags |= TexGenFlags.NoRender;
							if (isNoCollision)		 newFlags |= TexGenFlags.NoCollision;
							if (isNotCastingShadows) newFlags |= TexGenFlags.NoCastShadows;
							if (isWorldSpaceTexture) newFlags |= TexGenFlags.WorldSpaceTexture;
						} 
					}
				}

            }
            if (brush.Version == 2.0f)
            {
                if (brush.CompareTag("EditorOnly"))
                    brush.tag = "Untagged";
            }

            brush.Version = CSGBrush.CurrentVersion;
		}

		public static void UpgradeWhenNecessary(CSGOperation operation)
		{
			if (operation.Version >= CSGOperation.CurrentVersion)
				return;

			if (operation.Version < 1.0f)
				operation.Version = 1.0f;

            if (operation.Version == 1.0f)
            {
                if (operation.CompareTag("EditorOnly"))
                    operation.tag = "Untagged";
            }

			operation.Version = CSGOperation.CurrentVersion;
		}
	}
#endif
}

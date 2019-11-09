using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
    internal static class GUIConstants
    {
        public const float  handleScale             = 1.0f;
        public const float  backHandleScale         = 0.8f;
        public const float  hoverHandleScale        = handleScale * 1.2f;
        public const float  lineScale               = 1.5f;
        public const float  thickLineScale          = 1.5f;
        public const float  thinLineScale           = 1.0f;

        public const float  oldLineScale            = 1.5f / 32.0f;
        public const float  oldThickLineScale       = 0.03f / 2.0f;
        public const float  oldThinLineScale        = 0.02f / 2.0f;

        public const float  minRotateRadius         = 0.25f * 25.0f;
        public const float  backfaceTransparency    = 0.60f;
	}
}

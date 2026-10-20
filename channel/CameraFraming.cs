using System;

namespace RoRCheatPanel.Channel
{
    internal static class CameraFraming
    {
        public const float MinDistance = 0f;
        public const float MaxDistance = 30f;

        public static float DistanceForBody(float scale)
        {
            if (float.IsNaN(scale))
                scale = 1f;
            if (scale < 0.2f)
                scale = 0.2f;
            if (scale > 8f)
                scale = 8f;
            if (scale >= 0.92f && scale <= 1.05f)
                return 0f;
            if (scale < 0.92f)
                return Clamp(1.4f + (1f - scale) * 1.6f, 1.4f, 4f);
            return Clamp((scale - 1f) * 3.4f, 0f, MaxDistance);
        }

        public static float ClampDistance(float distance)
        {
            if (float.IsNaN(distance))
                return 0f;
            return Clamp(distance, MinDistance, MaxDistance);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}

using System;

namespace FoodUnlock
{
    internal static class FoodBookScale
    {
        internal const float Width = 800f;
        internal const float Height = 760f;
        internal const float DefaultMultiplier = 1f;
        internal const float MaximumMultiplier = 2f;
        private const float TotalMargin = 32f;

        internal static float Fit(float requestedMultiplier, float parentWidth, float parentHeight)
        {
            if (float.IsNaN(requestedMultiplier)) requestedMultiplier = DefaultMultiplier;
            var preferred = Math.Max(DefaultMultiplier, Math.Min(MaximumMultiplier, requestedMultiplier));
            // Parent and panel dimensions are both in canvas units.
            var widthLimit = (parentWidth - TotalMargin) / Width;
            var heightLimit = (parentHeight - TotalMargin) / Height;
            return Math.Max(0f, Math.Min(preferred, Math.Min(widthLimit, heightLimit)));
        }
    }
}

using System;

namespace NeeView
{
    internal static class SlideShowAutoScrollDuration
    {
        public static TimeSpan Calculate(double distance, double speed, TimeSpan fallbackDuration)
        {
            if (distance <= 0.0)
            {
                return fallbackDuration;
            }

            if (speed <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(speed));
            }

            return TimeSpan.FromSeconds(distance / speed);
        }
    }
}

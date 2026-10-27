using NeeLaboratory;
using System;
using System.Windows.Input;

namespace NeeView
{
    /// <summary>
    /// ホイール回転をノッチ回数に変更
    /// </summary>
    public class MouseWheelDelta
    {
        private int _delta;
        private int _timestamp;
        private int _count;

        private readonly int _resetTime = 1000;
        private readonly int _notch = 120;

        public void Reset()
        {
            _delta = 0;
        }

        public int NotchCount(MouseWheelEventArgs e, MouseWheelDeltaOption options = MouseWheelDeltaOption.None)
        {
            return NotchCount(e.Delta, e.Timestamp, options);
        }

        public int NotchCount(int delta, int timestamp, MouseWheelDeltaOption options = MouseWheelDeltaOption.None)
        {
            if (timestamp - _timestamp > _resetTime)
            {
                ////System.Diagnostics.Debug.WriteLine($"WheelCount: reset.");
                _delta = 0;
            }

            _delta += delta;
            int count = _delta / _notch;
            _delta -= count * _notch;

            if ((options & MouseWheelDeltaOption.LimitOnce) == MouseWheelDeltaOption.LimitOnce)
            {
                if (_timestamp == timestamp && _count != 0)
                {
                    return 0;
                }

                count = MathUtility.Clamp(count, -1, 1);
            }

            _count = count;
            _timestamp = timestamp;

            ////System.Diagnostics.Debug.WriteLine($"WheelCount: {count}, {_delta}");
            return count;
        }

        /// <summary>Converts a wheel delta to one signed page step.</summary>
        public static int PageCount(int delta) => Math.Sign(delta);
    }

    [Flags]
    public enum MouseWheelDeltaOption
    {
        None = 0,
        LimitOnce = (1 << 0),
    }

}

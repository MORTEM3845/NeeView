using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using NeeView.Windows;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace NeeView
{
    /// <summary>
    /// Positions detail tooltips beside the cursor and keeps them inside the active monitor's work area.
    /// </summary>
    public static class DetailToolTipPlacement
    {
        private const double CursorGap = 12.0;
        private const string ContentTemplateKey = "DetailToolTipContentTemplate";
        private static bool _classHandlerRegistered;

        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DetailToolTipPlacement),
            new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

        /// <summary>
        /// Calculates a top-left point that prefers the cursor's right side, falls back to its left,
        /// and clamps both axes to the monitor work area.
        /// Coordinates and sizes use the same units (the caller uses device pixels).
        /// </summary>
        public static Rect CalculateBounds(Point cursor, Size tooltipSize, Rect workArea, double gap = CursorGap)
        {
            if (workArea.Width <= 0 || workArea.Height <= 0)
            {
                return new Rect(workArea.TopLeft, new Size());
            }

            var width = Math.Min(Math.Max(0, tooltipSize.Width), workArea.Width);
            var height = Math.Min(Math.Max(0, tooltipSize.Height), workArea.Height);

            var rightX = cursor.X + gap;
            var leftX = cursor.X - gap - width;
            var x = rightX + width <= workArea.Right
                ? rightX
                : leftX >= workArea.Left
                    ? leftX
                    : rightX;

            x = Math.Clamp(x, workArea.Left, workArea.Right - width);
            var y = Math.Clamp(cursor.Y, workArea.Top, workArea.Bottom - height);
            return new Rect(x, y, width, height);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is true && !_classHandlerRegistered)
            {
                EventManager.RegisterClassHandler(typeof(ToolTip), ToolTip.OpenedEvent, new RoutedEventHandler(ToolTip_Opened));
                _classHandlerRegistered = true;
            }
        }

        private static void ToolTip_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is not ToolTip toolTip || toolTip.PlacementTarget is not DependencyObject target || !GetIsEnabled(target))
            {
                return;
            }

            var cursor = CursorInfo.GetNativeCursorPos();
            var hMonitor = PInvoke.MonitorFromPoint(cursor, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
            if (hMonitor == IntPtr.Zero)
            {
                return;
            }

            var monitorInfo = new MONITORINFO { cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>() };
            if (!PInvoke.GetMonitorInfo((HMONITOR)hMonitor, ref monitorInfo))
            {
                return;
            }

            var work = monitorInfo.rcWork;
            var workArea = new Rect(work.left, work.top, work.right - work.left, work.bottom - work.top);
            var dpi = CursorInfo.GetDpiScaleFactor(toolTip);

            // Bound long names to the work area and wrap them before calculating the final offset.
            toolTip.MaxWidth = workArea.Width / dpi.X;
            toolTip.MaxHeight = workArea.Height / dpi.Y;
            if (Application.Current?.TryFindResource(ContentTemplateKey) is DataTemplate contentTemplate)
            {
                toolTip.ContentTemplate = contentTemplate;
            }
            toolTip.UpdateLayout();

            var cursorPoint = new Point(cursor.X, cursor.Y);
            var tooltipSize = new Size(toolTip.ActualWidth * dpi.X, toolTip.ActualHeight * dpi.Y);
            var bounds = CalculateBounds(cursorPoint, tooltipSize, workArea);
            toolTip.HorizontalOffset = (bounds.X - cursorPoint.X) / dpi.X;
            toolTip.VerticalOffset = (bounds.Y - cursorPoint.Y) / dpi.Y;
        }
    }
}

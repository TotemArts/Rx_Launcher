using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;


namespace LauncherTwo
{
    public partial class RxWindow : ModernWindow
    {
        public static readonly RoutedCommand ShowSystemMenuUnderMouseCommand = new RoutedCommand();
        public static readonly RoutedCommand ShowSystemMenuUnderIconCommand = new RoutedCommand();
        public static readonly RoutedCommand DragMoveCommand = new RoutedCommand();
        public static readonly RoutedCommand ToggleMaximizedCommand = new RoutedCommand();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        public RxWindow()
        {
            CommandBindings.Add(new CommandBinding(ShowSystemMenuUnderMouseCommand, (element, args) =>
            {
                var point = Mouse.GetPosition(this);
                SystemCommands.ShowSystemMenu(this, PointToScreen(point));
            }));
            CommandBindings.Add(new CommandBinding(ShowSystemMenuUnderIconCommand, (element, args) =>
            {
                var border = SystemParameters.WindowNonClientFrameThickness;
                var offset = new Point(border.Left, border.Top);
                SystemCommands.ShowSystemMenu(this, PointToScreen(offset));
            }));
            CommandBindings.Add(new CommandBinding(DragMoveCommand, (element, args) =>
            {
                var wmSyscommand = 0x0112;
                var wmLbuttonup = 0x0202;
                var scMousemove = 0xf012;
                var handle = new WindowInteropHelper(this).Handle;
                SendMessage(handle, wmSyscommand, (IntPtr)scMousemove, IntPtr.Zero);
                SendMessage(handle, wmLbuttonup, IntPtr.Zero, IntPtr.Zero);
            }));
            CommandBindings.Add(new CommandBinding(ToggleMaximizedCommand, (element, args) =>
            {
                WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
            }));
        }
    }
}
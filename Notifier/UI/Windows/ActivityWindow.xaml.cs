﻿using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WinForms = System.Windows.Forms;
using Wokhan.WindowsFirewallNotifier.Common.Properties;
using Messages = Wokhan.WindowsFirewallNotifier.Common.Properties.Resources;
using System.Diagnostics;

namespace Wokhan.WindowsFirewallNotifier.Notifier.UI.Windows
{

    /*
     Activity window with lights for allow/block activities.
    */
    public partial class ActivityWindow : Window

    {
        private readonly NotificationWindow notifierWindow;

        private bool HasPositionChanged = false;  // used to remember the position when moved by a user
        private bool DisableClick = false;

        public enum WindowAlignmentEnum
        {
            Horizontal, Vertical
        }
        readonly WindowAlignmentEnum WindowAlignment = WindowAlignmentEnum.Horizontal;

        private double ExpectedTop
        {
            get { return HasPositionChanged ? this.Top : SystemParameters.WorkArea.Height - this.ActualHeight; }
        }

        private double ExpectedLeft
        {
            get { return HasPositionChanged ? this.Left : SystemParameters.WorkArea.Width - this.ActualWidth; }
        }

        private double ExpectedWidth
        {
            get { return SystemParameters.WorkArea.Width - this.ExpectedLeft; }
        }

        public static ActivityWindow Init(NotificationWindow window)
        {
            ActivityWindow factory = new ActivityWindow(window);
            return factory;
        }

        private ActivityWindow() { }
        private ActivityWindow(NotificationWindow window)
        {
            notifierWindow = window;

            InitializeComponent();
            if (WindowAlignment.Equals(WindowAlignmentEnum.Horizontal))
            {
                // switch orientation
                double origWidth = this.Width;
                this.Width = this.Height;
                this.Height = origWidth;
                ControlsContainer.Orientation = Orientation.Horizontal;
                ControlsContainer.Height = this.Height;
                ControlsContainer.Width = this.Width;
            }

            ShowInTaskbar = false;  // hide the icon in the taskbar

            ClickableIcon.ContextMenu = InitMenu();
            ClickableIcon.ToolTip = Messages.NotifierTrayIcon_NotifierStaysHiddenWhenMinimizedClickToOpen;
        }

        private ContextMenu InitMenu()
        {
            void MenuShow_Click(object Sender, RoutedEventArgs e)
            {
                notifierWindow.RestoreWindowState();
            }
            void MenuClose_Click(object Sender, EventArgs e)
            {
                notifierWindow.Close();
                Close();
            }
            void MenuConsole_Click(object Sender, EventArgs e)
            {
                Process.Start(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WFN.exe"));
            }
            void addMenuItem(ContextMenu cm, string caption, RoutedEventHandler eh)
            {
                MenuItem mi = new MenuItem
                {
                    Header = caption
                };
                mi.Click += eh;
                cm.Items.Add(mi);
            }
            ContextMenu contextMenu = new ContextMenu();
            addMenuItem(contextMenu, Messages.ActivityWindow_ShowNotifier, MenuShow_Click);
            addMenuItem(contextMenu, Messages.ActivityWindow_OpenConsole, MenuConsole_Click);
            addMenuItem(contextMenu, Messages.ActivityWindow_DiscardAndClose, MenuClose_Click);

            return contextMenu;
        }

        public new void Show()
        {
            base.Show();

            // initial position needs to be calculated after Show()
            Top = ExpectedTop;
            Left = ExpectedLeft;

            Topmost = true;
        }

        public new void Hide()
        {
            base.Hide();
        }

        private void ToggleGreen()
        {
            GreenLight.Background = (Brushes.DarkGreen.Equals(GreenLight.Background)) ? Brushes.LightGreen : Brushes.DarkGreen;
        }
        private void ToggleRed()
        {
            RedLight.Background = (Brushes.DarkRed.Equals(RedLight.Background)) ? Brushes.Red : Brushes.DarkRed;
        }

        private async void ToggleLightsTask(Border control, int waitMillis)
        {
            Brush origBrush = control.Background;
            void action()
            {
                if (GreenLight.Equals(control))
                {
                    ToggleGreen();
                } 
                else if (RedLight.Equals(control))
                {
                    ToggleRed();
                }
            };

            for (int i = 0; i < 2; i++)
            {
                Dispatcher.Invoke(action);
                await Task.Delay(waitMillis).ConfigureAwait(false);
            }
        }

        public enum ActivityEnum
        {
            Allowed, Blocked
        }
        public void ShowActivity(ActivityEnum activity)
        {
            if (ActivityEnum.Allowed.Equals(activity))
            {
                ToggleLightsTask(GreenLight, 200);
            }
            else
            {
                ToggleLightsTask(RedLight, 200);
            }
        }

        private Point MouseDownPos;
        private void StackPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Converts a relative position to screen coordinates
            MouseDownPos = PointToScreen(e.GetPosition(this));
            DisableClick = false;
        }

        private void Button_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!DisableClick)
            {
                ShowActivity(ActivityEnum.Allowed);
                if (WindowState.Minimized == notifierWindow.WindowState)
                {
                    notifierWindow.RestoreWindowState();
                } else
                {
                    notifierWindow.HideWindowState();
                }
            }
        }

        private void StackPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point p = PointToScreen(e.GetPosition(this));
                double deltaX = p.X - MouseDownPos.X;
                double deltaY = p.Y - MouseDownPos.Y;
                this.Top = Top + deltaY;
                this.Left = Left + deltaX;
                MouseDownPos = p;
                HasPositionChanged = true;
                DisableClick = true;
            }
        }
    }
}
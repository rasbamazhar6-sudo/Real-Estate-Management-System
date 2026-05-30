using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

namespace Project
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Set window size after window is loaded
            this.Loaded += MainWindow_Loaded;
            
            // Handle window state changes
            this.StateChanged += MainWindow_StateChanged;
            
            // Handle display settings changes (e.g., monitor changes, resolution changes)
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            
            // Clean up event handler when window closes
            this.Closed += MainWindow_Closed;
        }

        /// <summary>
        /// Sets the window size when it's first loaded
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeWindowSize();
        }

        /// <summary>
        /// Handles window state changes (Normal, Maximized, Minimized)
        /// </summary>
        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            // When window is restored from maximized, reset to work area size
            if (this.WindowState == WindowState.Normal)
            {
                InitializeWindowSize();
            }
        }

        /// <summary>
        /// Initializes the window size to use 100% of the screen excluding the taskbar
        /// </summary>
        private void InitializeWindowSize()
        {
            // Only set size if window is in Normal state (not maximized)
            if (this.WindowState == WindowState.Normal)
            {
                // Get the work area (screen minus taskbar)
                var workArea = SystemParameters.WorkArea;
                
                // Set window size to fill the work area
                this.Width = workArea.Width;
                this.Height = workArea.Height;
                
                // Position window at the top-left of the work area
                this.Left = workArea.Left;
                this.Top = workArea.Top;
            }
        }

        /// <summary>
        /// Handles display settings changes (resolution changes, monitor changes, etc.)
        /// </summary>
        private void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
        {
            // Use Dispatcher to ensure UI updates happen on the UI thread
            this.Dispatcher.Invoke(() =>
            {
                // Only resize if window is in Normal state
                if (this.WindowState == WindowState.Normal)
                {
                    InitializeWindowSize();
                }
            });
        }

        /// <summary>
        /// Clean up event handler when window closes
        /// </summary>
        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
        }
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                BtnMaximize_Click(sender, e);
                return;
            }

            DragMove();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                btnMaximize.Content = "□";
                btnMaximize.ToolTip = "Maximize";
                // Window size will be set automatically by StateChanged event
            }
            else
            {
                WindowState = WindowState.Maximized;
                btnMaximize.Content = "❐";
                btnMaximize.ToolTip = "Restore Down";
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (MainFrame.Content is UIElement page)
            {
                page.Opacity = 0;

                DoubleAnimation fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(250)
                };

                page.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            }
        }
        public void NavigateWithFade(Page newPage)
        {
            if (MainFrame.Content is UIElement currentPage)
            {
                DoubleAnimation fadeOut = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(200)
                };

                fadeOut.Completed += (s, e) =>
                {
                    MainFrame.Navigate(newPage);
                };

                currentPage.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }
            else
            {
                MainFrame.Navigate(newPage);
            }
        }
        private void ResizeLeft(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ResizeWindow(1);
        }

        private void ResizeRight(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ResizeWindow(2);
        }

        private void ResizeTop(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ResizeWindow(3);
        }

        private void ResizeBottom(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ResizeWindow(6);
        }

        private void ResizeWindow(int direction)
        {
            SendMessage(new WindowInteropHelper(this).Handle, 0x112, (IntPtr)(61440 + direction), IntPtr.Zero);
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media.Animation;

namespace Project.Pages
{
    public partial class WelcomePage : Page
    {
        private readonly DispatcherTimer _timer;

        public WelcomePage()
        {
            InitializeComponent();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };

            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _timer.Stop();

            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400)
            };

            fadeOut.Completed += (s, _) =>
            {
                ((MainWindow)Application.Current.MainWindow).NavigateWithFade(new LoginPage());
            };

            this.BeginAnimation(Page.OpacityProperty, fadeOut);
        }
    }
}
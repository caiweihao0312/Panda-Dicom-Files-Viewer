using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace DemoAnimationApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // VisualStateManager handlers
        private void State1_Click(object sender, RoutedEventArgs e)
            => VisualStateManager.GoToElementState(VsmBox, "State1", true);

        private void State2_Click(object sender, RoutedEventArgs e)
            => VisualStateManager.GoToElementState(VsmBox, "State2", true);

        // Code-behind animation handler
        private void AnimateButton_Click(object sender, RoutedEventArgs e)
        {
            var animation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.2,
                Duration = TimeSpan.FromSeconds(0.5),
                AutoReverse = true
            };
            CodeBehindRect.BeginAnimation(OpacityProperty, animation);
        }
    }
}
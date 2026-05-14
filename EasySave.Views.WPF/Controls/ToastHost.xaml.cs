using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MahApps.Metro.IconPacks;

namespace EasySave.Views.WPF.Controls
{
    /// <summary>
    /// Lightweight in-app toast notification host. Sits on top of the main window
    /// and slides notifications in from the right, auto-dismissing after a delay.
    /// Use ToastHost.Show(window, title, message, ToastKind).
    /// </summary>
    public partial class ToastHost : UserControl
    {
        public enum ToastKind { Success, Error, Warning, Info }

        public ToastHost() => InitializeComponent();

        /// <summary>Pushes a new toast onto the host (auto-dismisses after 4s).</summary>
        public void Push(string title, string message, ToastKind kind)
        {
            var toast = BuildToast(title, message, kind);
            ToastsList.Items.Insert(0, toast);

            // Slide-in + fade-in animation
            var slide = new ThicknessAnimation
            {
                From = new Thickness(80, 0, 0, 0),
                To   = new Thickness(0,  0, 0, 0),
                Duration = TimeSpan.FromMilliseconds(260),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var fadeIn = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(220)
            };
            toast.BeginAnimation(MarginProperty,  slide);
            toast.BeginAnimation(OpacityProperty, fadeIn);

            // Auto-dismiss after 4 seconds
            var timer = new System.Windows.Threading.DispatcherTimer
                       { Interval = TimeSpan.FromSeconds(4) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                DismissToast(toast);
            };
            timer.Start();
        }

        private void DismissToast(Border toast)
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1, To = 0,
                Duration = TimeSpan.FromMilliseconds(220)
            };
            fadeOut.Completed += (_, _) =>
            {
                if (ToastsList.Items.Contains(toast))
                    ToastsList.Items.Remove(toast);
            };
            toast.BeginAnimation(OpacityProperty, fadeOut);
        }

        /// <summary>Builds the toast Border + icon + texts + close button.</summary>
        private Border BuildToast(string title, string message, ToastKind kind)
        {
            // Colour + icon per kind
            (SolidColorBrush bg, SolidColorBrush accent, PackIconMaterialKind icon) = kind switch
            {
                ToastKind.Success => (B("#1B5E20"), B("#43A047"), PackIconMaterialKind.CheckCircle),
                ToastKind.Error   => (B("#B71C1C"), B("#E53935"), PackIconMaterialKind.AlertCircle),
                ToastKind.Warning => (B("#E65100"), B("#FF9800"), PackIconMaterialKind.Alert),
                _                 => (B("#0D47A1"), B("#2196F3"), PackIconMaterialKind.Information)
            };

            var iconCtrl = new PackIconMaterial
            {
                Kind = icon,
                Width = 22, Height = 22,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };

            var titleBlock = new TextBlock
            {
                Text = title,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13
            };
            var messageBlock = new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0),
                MaxWidth = 280
            };

            var textsPanel = new StackPanel { Orientation = Orientation.Vertical };
            textsPanel.Children.Add(titleBlock);
            textsPanel.Children.Add(messageBlock);

            var closeBtn = new Button
            {
                Content = new PackIconMaterial
                {
                    Kind  = PackIconMaterialKind.Close,
                    Width = 14, Height = 14,
                    Foreground = Brushes.White
                },
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Width = 26, Height = 26,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Top
            };

            var content = new DockPanel { LastChildFill = true, Width = 340 };
            DockPanel.SetDock(iconCtrl, Dock.Left);
            DockPanel.SetDock(closeBtn, Dock.Right);
            content.Children.Add(iconCtrl);
            content.Children.Add(closeBtn);
            content.Children.Add(textsPanel);

            var inner = new Border
            {
                Background = bg,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 12, 10, 12),
                Child = content,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 14, ShadowDepth = 2, Opacity = 0.35,
                    Color = Colors.Black
                }
            };

            // Left accent stripe
            var stripe = new Border
            {
                Background = accent,
                Width = 4,
                CornerRadius = new CornerRadius(8, 0, 0, 8)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(stripe, 0);
            Grid.SetColumn(inner,  1);
            grid.Children.Add(stripe);
            grid.Children.Add(inner);

            var outer = new Border
            {
                Margin = new Thickness(0, 0, 0, 10),
                CornerRadius = new CornerRadius(8),
                ClipToBounds = true,
                Child = grid,
                Opacity = 0
            };

            closeBtn.Click += (_, _) => DismissToast(outer);

            return outer;
        }

        private static SolidColorBrush B(string hex) =>
            (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;

        // ── Static helper for any window ──────────────────────────────────────

        /// <summary>Convenience: finds the ToastHost named "Toasts" in the given window and pushes a toast on it.</summary>
        public static void Show(Window window, string title, string message, ToastKind kind)
        {
            if (window.FindName("Toasts") is ToastHost host)
                host.Push(title, message, kind);
        }
    }
}

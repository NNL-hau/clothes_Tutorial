using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SlidingLoginDemo
{
    public static class WatermarkService
    {
        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.RegisterAttached(
                "Watermark",
                typeof(string),
                typeof(WatermarkService),
                new PropertyMetadata(string.Empty, OnWatermarkChanged));

        public static void SetWatermark(DependencyObject element, string value)
            => element.SetValue(WatermarkProperty, value);

        public static string GetWatermark(DependencyObject element)
            => (string)element.GetValue(WatermarkProperty);

        private static readonly DependencyProperty AdornerProperty =
            DependencyProperty.RegisterAttached(
                "Adorner",
                typeof(WatermarkAdorner),
                typeof(WatermarkService),
                new PropertyMetadata(null));

        private static void SetAdorner(DependencyObject element, WatermarkAdorner value)
            => element.SetValue(AdornerProperty, value);

        private static WatermarkAdorner? GetAdorner(DependencyObject element)
            => (WatermarkAdorner?)element.GetValue(AdornerProperty);

        private static void OnWatermarkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox tb)
            {
                tb.Loaded -= TextBox_Loaded;
                tb.Loaded += TextBox_Loaded;

                tb.TextChanged -= TextBox_Changed;
                tb.TextChanged += TextBox_Changed;
            }
            else if (d is PasswordBox pb)
            {
                pb.Loaded -= PasswordBox_Loaded;
                pb.Loaded += PasswordBox_Loaded;

                pb.PasswordChanged -= PasswordBox_Changed;
                pb.PasswordChanged += PasswordBox_Changed;
            }
        }

        private static void TextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb) Update(tb);
        }

        private static void PasswordBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb) Update(pb);
        }

        private static void TextBox_Changed(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb) Update(tb);
        }

        private static void PasswordBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb) Update(pb);
        }

        private static void Update(TextBox tb)
        {
            EnsureAdorner(tb, string.IsNullOrEmpty(tb.Text), GetWatermark(tb));
        }

        private static void Update(PasswordBox pb)
        {
            EnsureAdorner(pb, string.IsNullOrEmpty(pb.Password), GetWatermark(pb));
        }

        private static void EnsureAdorner(Control control, bool show, string text)
        {
            var layer = AdornerLayer.GetAdornerLayer(control);
            if (layer == null) return;

            var adorner = GetAdorner(control);

            if (show)
            {
                if (adorner == null)
                {
                    adorner = new WatermarkAdorner(control, text);
                    SetAdorner(control, adorner);
                    layer.Add(adorner);
                }
                else
                {
                    adorner.Text = text;
                    adorner.InvalidateVisual();
                }
            }
            else
            {
                if (adorner != null)
                {
                    layer.Remove(adorner);
                    SetAdorner(control, null!);
                }
            }
        }

        private class WatermarkAdorner : Adorner
        {
            public string Text { get; set; }

            public WatermarkAdorner(UIElement adornedElement, string text) : base(adornedElement)
            {
                IsHitTestVisible = false;
                Text = text;
            }

            protected override void OnRender(DrawingContext dc)
            {
                if (AdornedElement is not Control c) return;
                if (!c.IsVisible || c.Opacity == 0) return;

                var typeface = new Typeface(c.FontFamily, c.FontStyle, c.FontWeight, c.FontStretch);

                var ft = new FormattedText(
                    Text ?? "",
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    c.FontSize <= 0 ? 12 : c.FontSize,
                    new SolidColorBrush(Color.FromRgb(150, 165, 165)),
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                double x = 14;
                double y = (c.ActualHeight - ft.Height) / 2.0;
                dc.DrawText(ft, new Point(x, y));
            }
        }
        public static void HideWatermarks(DependencyObject root)
        {
            if (root == null) return;

            TryRemoveAdorner(root);

            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                HideWatermarks(VisualTreeHelper.GetChild(root, i));
            }
        }

        public static void RefreshWatermarks(DependencyObject root)
        {
            if (root == null) return;

            TryRefresh(root);

            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                RefreshWatermarks(VisualTreeHelper.GetChild(root, i));
            }
        }

        private static void TryRemoveAdorner(DependencyObject d)
        {
            if (d is Control c)
            {
                var layer = AdornerLayer.GetAdornerLayer(c);
                if (layer == null) return;

                var adorner = GetAdorner(c);
                if (adorner != null)
                {
                    layer.Remove(adorner);
                    SetAdorner(c, null!);
                }
            }
        }

        private static void TryRefresh(DependencyObject d)
        {
            if (d is TextBox tb)
            {
                bool show = string.IsNullOrEmpty(tb.Text);
                EnsureAdorner(tb, show, GetWatermark(tb));
            }
            else if (d is PasswordBox pb)
            {
                bool show = string.IsNullOrEmpty(pb.Password);
                EnsureAdorner(pb, show, GetWatermark(pb));
            }
        }


    }

}

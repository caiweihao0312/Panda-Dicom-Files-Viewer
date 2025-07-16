using MedicalImagingSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MedicalImagingSystem.Views
{
    /// <summary>
    /// DicomImageView.xaml 的交互逻辑
    /// </summary>
    public partial class DicomImageView : UserControl
    {
        private Point _origin;
        private Point _start;
        private bool _isDragging = false;
        private DicomImageViewModel dicomImageViewModel => DataContext as DicomImageViewModel;

        public DicomImageView()
        {
            InitializeComponent();
        }

        public DicomImageView(DicomImageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        // 鼠标滚轮缩放
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (PART_Image.Source == null) return;

            if (dicomImageViewModel != null && !dicomImageViewModel.isOpenZoom)
            {
                return;
            }

            var position = e.GetPosition(PART_Image);
            var transformGroup = (TransformGroup)PART_Image.RenderTransform;
            var scaleTransform = (ScaleTransform)transformGroup.Children[0];
            var translateTransform = (TranslateTransform)transformGroup.Children[1];

            double zoom = e.Delta > 0 ? 1.2 : 1 / 1.2;
            double newScale = Math.Clamp(scaleTransform.ScaleX * zoom, 0.2, 10);

            // 以鼠标为中心缩放
            var relativeX = position.X / PART_Image.ActualWidth;
            var relativeY = position.Y / PART_Image.ActualHeight;
            var absX = (position.X - translateTransform.X) / scaleTransform.ScaleX;
            var absY = (position.Y - translateTransform.Y) / scaleTransform.ScaleY;

            scaleTransform.ScaleX = scaleTransform.ScaleY = newScale;

            // 缩放后调整平移，保证以鼠标为中心
            translateTransform.X = position.X - absX * newScale;
            translateTransform.Y = position.Y - absY * newScale;

            // 限制图像不超出边界
            //LimitImagePosition(scaleTransform, translateTransform);

            e.Handled = true;
        }

        // 鼠标左键按下开始拖动
        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (PART_Image.Source == null) return;
            if (dicomImageViewModel != null && !dicomImageViewModel.isOpenZoom)
            {
                return;
            }
            _isDragging = true;
            _start = e.GetPosition(this);
            var transformGroup = (TransformGroup)PART_Image.RenderTransform;
            _origin = new Point(
                ((TranslateTransform)transformGroup.Children[1]).X,
                ((TranslateTransform)transformGroup.Children[1]).Y);
            PART_Image.CaptureMouse();
        }

        // 鼠标移动拖动
        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;
            if (dicomImageViewModel != null && !dicomImageViewModel.isOpenZoom)
            {
                return;
            }
            var transformGroup = (TransformGroup)PART_Image.RenderTransform;
            var scaleTransform = (ScaleTransform)transformGroup.Children[0];
            var translateTransform = (TranslateTransform)transformGroup.Children[1];

            Vector v = e.GetPosition(this) - _start;
            translateTransform.X = _origin.X + v.X;
            translateTransform.Y = _origin.Y + v.Y;

            // 限制图像不超出边界
            LimitImagePosition(scaleTransform, translateTransform);
        }

        // 鼠标左键释放结束拖动
        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dicomImageViewModel != null && !dicomImageViewModel.isOpenZoom)
            {
                return;
            }
            _isDragging = false;
            PART_Image.ReleaseMouseCapture();
        }

        // 限制图像不超出显示区域
        private void LimitImagePosition(ScaleTransform scale, TranslateTransform translate)
        {
            if (PART_Image.Source == null) return;

            double imgWidth = PART_Image.Source.Width * scale.ScaleX;
            double imgHeight = PART_Image.Source.Height * scale.ScaleY;
            double viewWidth = PART_ScrollViewer.ViewportWidth;
            double viewHeight = PART_ScrollViewer.ViewportHeight;

            // 只允许图像边缘不超出可视区域
            double minX = Math.Min(0, viewWidth - imgWidth);
            double minY = Math.Min(0, viewHeight - imgHeight);
            double maxX = Math.Max(0, viewWidth - imgWidth);
            double maxY = Math.Max(0, viewHeight - imgHeight);

            translate.X = Math.Clamp(translate.X, minX, maxX);
            translate.Y = Math.Clamp(translate.Y, minY, maxY);
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MedicalImagingSystem.Services;
using MedicalImagingSystem.Helper;
using MedicalImagingSystem.Messenge;
using MedicalImagingSystem.Model;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MedicalImagingSystem.ViewModels
{
    /// <summary>
    /// DicomImageViewModel 负责 DICOM 医学影像的显示、交互、测量等功能的视图模型。
    /// </summary>
    public partial class DicomImageViewModel : ObservableObject
    {
        /// <summary>
        /// DICOM 服务接口，用于加载和处理 DICOM 图像。
        /// </summary>
        private readonly IDicomService _dicomService;

        // 文件名
        [ObservableProperty]
        private string fileName;

        // 当前显示的 DICOM 图像
        [ObservableProperty]
        public BitmapSource currentImage;

        /// <summary>
        /// 当前图像发生变化时，通知 isImageLoaded 属性变化。
        /// </summary>
        partial void OnCurrentImageChanged(BitmapSource value)
        {
            OnPropertyChanged(nameof(isImageLoaded));
        }

        // 是否已加载图像
        [ObservableProperty]
        public bool isImageLoaded = false;

        // 患者信息
        [ObservableProperty]
        private string patientName;
        [ObservableProperty]
        private string patientId;
        [ObservableProperty]
        private string birthDate;

        // 检查信息
        [ObservableProperty]
        private string studyId;
        [ObservableProperty]
        private string studyDate;

        // 设备信息
        [ObservableProperty]
        private string modality;
        [ObservableProperty]
        private string manufacturer;

        // 图像参数
        [ObservableProperty]
        private int imageRows;
        [ObservableProperty]
        private int imageColumns;

        // 鼠标坐标信息
        [ObservableProperty]
        private int mouseX;
        [ObservableProperty]
        private int mouseY;

        // 控制信息面板是否展开
        [ObservableProperty]
        private bool isExpanded;

        // 图片缩略图集合
        [ObservableProperty]
        public ObservableCollection<ImageItem> imageThumbnails = new ObservableCollection<ImageItem>();

        /// <summary>
        /// 当前 DICOM 标签信息对象，包含像素数据、窗宽窗位等。
        /// </summary>
        public DicomTagInfoDTO m_DicomTagInfoDTO;

        // 当前序列的所有帧（多帧图像）
        [ObservableProperty]
        private ObservableCollection<BitmapSource> imageSeries = new ObservableCollection<BitmapSource>();

        // 当前帧索引
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentFrameText))]
        private int currentFrameIndex;

        // 窗宽
        [ObservableProperty]
        private double windowWidth;
        partial void OnWindowWidthChanged(double value)
        {
            CurrentImage = UpdateImageDisplay();
        }

        // 窗位
        [ObservableProperty]
        private double windowCenter;
        partial void OnWindowCenterChanged(double value)
        {
            CurrentImage = UpdateImageDisplay();
        }

        // 旋转角度（度）
        [ObservableProperty]
        private double rotationAngle = 0;

        /// <summary>
        /// 旋转命令，每次旋转 90 度。
        /// </summary>
        [RelayCommand]
        private void Rotate()
        {
            RotationAngle = (RotationAngle + 90) % 360;
        }

        // 水平镜像标志
        [ObservableProperty]
        private bool isFlippedX = false;

        // 垂直镜像标志
        [ObservableProperty]
        private bool isFlippedY = false;

        /// <summary>
        /// 水平镜像命令。
        /// </summary>
        [RelayCommand]
        private void HorizontalMirror()
        {
            IsFlippedX = !IsFlippedX;
        }

        /// <summary>
        /// 垂直镜像命令。
        /// </summary>
        [RelayCommand]
        private void VerticallyMirror()
        {
            IsFlippedY = !IsFlippedY;
        }

        /// <summary>
        /// 当前帧文本（如 1/10），用于 UI 显示。
        /// </summary>
        public string CurrentFrameText => $"{CurrentFrameIndex + 1}/{ImageSeries?.Count ?? 0}";

        // 拖动调整窗宽窗位相关变量
        private double dragStartX;
        private double dragStartY;
        private double initialWindowWidth;
        private double initialWindowCenter;
        private bool isDragging = false;
        public bool isOpenAdjustWindowing = false; // 是否开启窗宽窗位调整
        public bool isOpenZoom = false; // 是否开启缩放

        /// <summary>
        /// 构造函数，支持文件或目录方式加载 DICOM 图像。
        /// </summary>
        /// <param name="dicomService">DICOM 服务实例</param>
        /// <param name="filePath">DICOM 文件路径</param>
        /// <param name="directoryPath">DICOM 序列目录</param>
        public DicomImageViewModel(IDicomService dicomService, string filePath = null, string directoryPath = null)
        {
            _dicomService = dicomService;
            if (!string.IsNullOrEmpty(filePath))
            {
                // 加载单个 DICOM 文件
                FileName = System.IO.Path.GetFileName(filePath);
                DicomTagInfoDTO dicomTagInfoDTO = m_DicomTagInfoDTO = _dicomService.LoadDicomImage(filePath);
                if (dicomTagInfoDTO != null)
                {
                    // 填充患者、检查、设备、图像参数等信息
                    PatientName = dicomTagInfoDTO?.PatientName;
                    PatientId = dicomTagInfoDTO.PatientId;
                    BirthDate = dicomTagInfoDTO.BirthDate.ToString("yyyy.MM.dd");
                    StudyId = dicomTagInfoDTO.StudyId;
                    StudyDate = dicomTagInfoDTO.StudyDate;
                    Modality = dicomTagInfoDTO.Modality;
                    Manufacturer = dicomTagInfoDTO.Manufacturer;
                    ImageRows = dicomTagInfoDTO.ImageRows;
                    ImageColumns = dicomTagInfoDTO.ImageColumns;
                    // 添加缩略图
                    ImageThumbnails.Add(new ImageItem(this) { Thumbnail = dicomTagInfoDTO?.bitmapSource });
                    // 设置当前显示图像
                    CurrentImage = dicomTagInfoDTO?.bitmapSource;
                    WindowWidth = dicomTagInfoDTO.WindowWidth;
                    WindowCenter = dicomTagInfoDTO.WindowCenter;
                }
                isExpanded = true;
                isImageLoaded = true;
                ImageSeries.Add(CurrentImage);
            }
            else if (!string.IsNullOrEmpty(directoryPath))
            {
                // 加载 DICOM 序列（多帧）
                isImageLoaded = true;
                ImageSeries = _dicomService.LoadDicomSeries(directoryPath);
                CurrentImage = ImageSeries.Count > 0 ? ImageSeries[0] : null;
            }
            CurrentFrameIndex = 0;
        }

        /// <summary>
        /// 应用窗宽窗位（暂未启用，留作扩展）
        /// </summary>
        [RelayCommand]
        private void ApplyWindowing()
        {
            //if (ImageSeries.Count > 0)
            //{
            //    CurrentImage = _dicomService.ApplyWindowing(ImageSeries[CurrentFrameIndex], WindowWidth, WindowCenter);
            //}
        }

        /// <summary>
        /// 切换到下一帧图像。
        /// </summary>
        [RelayCommand]
        private void NextFrame()
        {
            if (ImageSeries.Count > 0 && CurrentFrameIndex < ImageSeries.Count - 1)
            {
                CurrentFrameIndex++;
                CurrentImage = ImageSeries[CurrentFrameIndex];
            }
        }

        /// <summary>
        /// 切换到上一帧图像。
        /// </summary>
        [RelayCommand]
        private void PreviousFrame()
        {
            if (ImageSeries.Count > 0 && CurrentFrameIndex > 0)
            {
                CurrentFrameIndex--;
                CurrentImage = ImageSeries[CurrentFrameIndex];
            }
        }

        /// <summary>
        /// 鼠标左键按下事件处理，支持测量工具和窗宽窗位调整。
        /// </summary>
        [RelayCommand]
        public void MouseLeftButtonDown(object e)
        {
            // 测量工具模式
            if (IsMeasureToolEnabled)
            {
                if (!IsMeasureToolEnabled || MeasureMode == MeasureMode.None) return;
                if (e is MouseButtonEventArgs args)
                {
                    // 获取 Image 和 Canvas
                    var image = args.Source as Image;
                    var canvas = FindCanvasFromImage(image);
                    if (image == null || canvas == null) return;

                    // 获取鼠标在 Image 上的坐标
                    var imagePos = args.GetPosition(image);
                    // 转换为 Canvas 坐标
                    var pos = image.TranslatePoint(imagePos, canvas);
                    if (MeasureMode == MeasureMode.Length)
                    {
                        PreviewStartPoint = pos;
                        PreviewCurrentPoint = pos;
                    }
                    else if (MeasureMode == MeasureMode.Angle)
                    {
                        if (PreviewStartPoint == null)
                            PreviewStartPoint = pos;
                        else if (PreviewVertexPoint == null)
                            PreviewVertexPoint = pos;
                        PreviewCurrentPoint = pos;
                    }
                    OnPropertyChanged(nameof(PreviewStartPoint));
                    OnPropertyChanged(nameof(PreviewVertexPoint));
                    OnPropertyChanged(nameof(PreviewCurrentPoint));
                    OnPropertyChanged(nameof(PreviewLength));
                    OnPropertyChanged(nameof(PreviewAngle));
                }
            }
            else
            {
                // 窗宽窗位调整模式
                if (!isOpenAdjustWindowing)
                {
                    isDragging = false;
                    return;
                }
                if (e is System.Windows.Input.MouseButtonEventArgs args)
                {
                    isDragging = true;
                    var pos = args.GetPosition(args.Source as Image);
                    dragStartX = pos.X;
                    dragStartY = pos.Y;
                    initialWindowWidth = WindowWidth;
                    initialWindowCenter = WindowCenter;
                }
            }
        }

        /// <summary>
        /// 辅助方法：从 Image 查找同级 Canvas（用于测量绘制）。
        /// </summary>
        private Canvas FindCanvasFromImage(Image image)
        {
            var parent = VisualTreeHelper.GetParent(image);
            if (parent is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is Canvas canvas)
                        return canvas;
                }
            }
            return null;
        }

        /// <summary>
        /// 鼠标左键抬起事件处理，支持测量工具和窗宽窗位调整。
        /// </summary>
        [RelayCommand]
        public void MouseLeftButtonUp(object e)
        {
            // 测量工具模式
            if (IsMeasureToolEnabled)
            {
                if (!IsMeasureToolEnabled || MeasureMode == MeasureMode.None) return;
                if (e is MouseButtonEventArgs args)
                {
                    var image = args.Source as Image;
                    var canvas = FindCanvasFromImage(image);
                    if (image == null || canvas == null) return;

                    var imagePos = args.GetPosition(image);
                    var pos = image.TranslatePoint(imagePos, canvas);
                    if (MeasureMode == MeasureMode.Length && PreviewStartPoint != null)
                    {
                        // 添加测量线
                        var line = new MeasureLine
                        {
                            Start = PreviewStartPoint.Value,
                            End = pos,
                            Length = Math.Sqrt(Math.Pow(pos.X - PreviewStartPoint.Value.X, 2) + Math.Pow(pos.Y - PreviewStartPoint.Value.Y, 2))
                        };
                        MeasureLines.Add(line);
                        PreviewStartPoint = null;
                        PreviewCurrentPoint = null;
                    }
                    else if (MeasureMode == MeasureMode.Angle)
                    {
                        if (PreviewStartPoint != null && PreviewVertexPoint != null)
                        {
                            // 添加测量角
                            var angle = new MeasureAngle
                            {
                                Point1 = PreviewStartPoint.Value,
                                Vertex = PreviewVertexPoint.Value,
                                Point2 = pos,
                                Angle = CalculateAngle(PreviewStartPoint.Value, PreviewVertexPoint.Value, pos)
                            };
                            MeasureAngles.Add(angle);
                            PreviewStartPoint = null;
                            PreviewVertexPoint = null;
                            PreviewCurrentPoint = null;
                        }
                        else if (PreviewStartPoint != null)
                        {
                            PreviewVertexPoint = pos;
                        }
                    }
                    OnPropertyChanged(nameof(PreviewStartPoint));
                    OnPropertyChanged(nameof(PreviewVertexPoint));
                    OnPropertyChanged(nameof(PreviewCurrentPoint));
                    OnPropertyChanged(nameof(PreviewLength));
                    OnPropertyChanged(nameof(PreviewAngle));
                }
            }
            else
            {
                // 窗宽窗位调整模式
                if (!isOpenAdjustWindowing) return;
                isDragging = false;
            }
        }

        /// <summary>
        /// 鼠标移动事件处理，支持测量工具和窗宽窗位调整。
        /// </summary>
        [RelayCommand]
        public void MouseMove(object e)
        {
            if (IsMeasureToolEnabled)
            {
                if (!IsMeasureToolEnabled || MeasureMode == MeasureMode.None) return;
                if (e is MouseEventArgs args)
                {
                    var image = args.Source as Image;
                    var canvas = FindCanvasFromImage(image);
                    if (image == null || canvas == null) return;

                    var imagePos = args.GetPosition(image);
                    var pos = image.TranslatePoint(imagePos, canvas);
                    MouseX = (int)pos.X;
                    MouseY = (int)pos.Y;
                    PreviewCurrentPoint = pos;
                    OnPropertyChanged(nameof(PreviewCurrentPoint)); 
                    OnPropertyChanged(nameof(PreviewLength));
                    OnPropertyChanged(nameof(PreviewAngle));
                }
            }
            else
            {
                if (!isOpenAdjustWindowing)
                {
                    isDragging = false;
                    return;
                }
                if (isDragging && e is System.Windows.Input.MouseEventArgs args)
                {
                    var pos = args.GetPosition(args.Source as Image);
                    MouseX = (int)pos.X;
                    MouseY = (int)pos.Y;
                    double deltaX = pos.X - dragStartX;
                    double deltaY = pos.Y - dragStartY;

                    // 根据鼠标移动调整窗宽窗位
                    WindowWidth = Convert.ToInt32(Math.Max(1, initialWindowWidth + deltaX * 0.6));
                    WindowCenter = Convert.ToInt32(initialWindowCenter + deltaY * 0.6);

                    // 刷新图像显示
                    CurrentImage = UpdateImageDisplay();
                }
            }
        }

        /// <summary>
        /// 根据当前窗宽窗位等参数，刷新并返回新的 BitmapSource。
        /// </summary>
        public BitmapSource UpdateImageDisplay()
        {
            if (!isOpenAdjustWindowing)
                return CurrentImage;
            if (m_DicomTagInfoDTO?.PixelData != null)
            {
                // 通过 DICOM 服务转换像素数据为 BitmapSource
                return _dicomService.ConvertPixelDataToBitmapSource(
                    m_DicomTagInfoDTO.PixelData,
                    m_DicomTagInfoDTO.ImageColumns,
                    m_DicomTagInfoDTO.ImageRows,
                    WindowWidth,
                    WindowCenter,
                    m_DicomTagInfoDTO.PixelFormat, // 传递像素格式
                    true);
            }
            else
            {
                return CurrentImage;
            }
        }

        /// <summary>
        /// 重置图像显示为缩略图（原始状态）。
        /// </summary>
        public BitmapSource ResetImageDisplay()
        {
            if (ImageThumbnails.Count > 0 && CurrentFrameIndex < ImageThumbnails.Count)
            {
                return ImageThumbnails[CurrentFrameIndex].Thumbnail;
            }
            else
            {
                return CurrentImage;
            }
        }

        #region 测量工具

        // 测量工具相关属性和方法


        /// <summary>
        /// 预览测量起点
        /// </summary>
        public Point? PreviewStartPoint { get; set; }
        /// <summary>
        /// 预览测量终点
        /// </summary>
        public Point? PreviewEndPoint { get; set; }
        /// <summary>
        /// 预览角度测量顶点
        /// </summary>
        public Point? PreviewVertexPoint { get; set; }
        /// <summary>
        /// 预览当前点
        /// </summary>
        public Point? PreviewCurrentPoint { get; set; }

        /// <summary>
        /// 实时预览测量长度
        /// </summary>
        public double? PreviewLength
        {
            get
            {
                if (PreviewStartPoint.HasValue && PreviewCurrentPoint.HasValue)
                {
                    var dx = PreviewCurrentPoint.Value.X - PreviewStartPoint.Value.X;
                    var dy = PreviewCurrentPoint.Value.Y - PreviewStartPoint.Value.Y;
                    return Math.Sqrt(dx * dx + dy * dy);
                }
                return null;
            }
        }

        /// <summary>
        /// 实时预览测量角度
        /// </summary>
        public double? PreviewAngle
        {
            get
            {
                if (PreviewStartPoint.HasValue && PreviewVertexPoint.HasValue && PreviewCurrentPoint.HasValue)
                {
                    var a = PreviewStartPoint.Value;
                    var b = PreviewVertexPoint.Value;
                    var c = PreviewCurrentPoint.Value;
                    var ab = new Vector(a.X - b.X, a.Y - b.Y);
                    var cb = new Vector(c.X - b.X, c.Y - b.Y);
                    var angle = Vector.AngleBetween(ab, cb);
                    return Math.Abs(angle);
                }
                return null;
            }
        }
        // 当前测量模式（无、长度、角度）
        [ObservableProperty]
        private MeasureMode measureMode = MeasureMode.None;

        // 是否启用测量工具
        [ObservableProperty]
        private bool isMeasureToolEnabled = false;

        // 当前所有测量线集合
        [ObservableProperty]
        private ObservableCollection<MeasureLine> measureLines = new();

        // 当前所有测量角集合
        [ObservableProperty]
        private ObservableCollection<MeasureAngle> measureAngles = new();

        // 鼠标操作临时变量
        private Point? tempStartPoint;
        private Point? tempVertexPoint;

        /// <summary>
        /// 切换测量工具启用状态。
        /// </summary>
        [RelayCommand]
        private void ToggleMeasurementTool()
        {
            IsMeasureToolEnabled = !IsMeasureToolEnabled;
            if (!IsMeasureToolEnabled)
            {
                MeasureMode = MeasureMode.None;
            }
        }

        /// <summary>
        /// 启用长度测量模式。
        /// </summary>
        [RelayCommand]
        private void MeasureLength()
        {
            if (IsMeasureToolEnabled)
                MeasureMode = MeasureMode.Length;
        }

        /// <summary>
        /// 启用角度测量模式。
        /// </summary>
        [RelayCommand]
        private void MeasureAngle()
        {
            if (IsMeasureToolEnabled)
                MeasureMode = MeasureMode.Angle;
        }

        /// <summary>
        /// 清除所有测量数据。
        /// </summary>
        [RelayCommand]
        private void ClearMeasurementsData()
        {
            MeasureLines.Clear();
            MeasureAngles.Clear();
            PreviewStartPoint = null;
            PreviewVertexPoint = null;
            PreviewCurrentPoint = null;
            OnPropertyChanged(nameof(PreviewStartPoint));
            OnPropertyChanged(nameof(PreviewVertexPoint));
            OnPropertyChanged(nameof(PreviewCurrentPoint));
        }

        //// 鼠标事件处理（可与窗宽窗位等模式互斥）
        //[RelayCommand]
        //public void ImageMouseDown(object e) { ... }
        //[RelayCommand]
        //public void ImageMouseUp(object e) { ... }

        /// <summary>
        /// 计算三点夹角（用于角度测量）。
        /// </summary>
        private double CalculateAngle(Point p1, Point vertex, Point p2)
        {
            var v1 = new Vector(p1.X - vertex.X, p1.Y - vertex.Y);
            var v2 = new Vector(p2.X - vertex.X, p2.Y - vertex.Y);
            double angle = Vector.AngleBetween(v1, v2);
            return Math.Abs(angle);
        }

        #endregion
    }

    /// <summary>
    /// ImageItem 表示一个缩略图项，支持点击切换主图像。
    /// </summary>
    public partial class ImageItem : ObservableObject
    {
        // 缩略图
        [ObservableProperty]
        private BitmapSource thumbnail;

        // 显示图像命令
        public IRelayCommand ShowImageCommand { get; }

        private readonly DicomImageViewModel _parent;

        /// <summary>
        /// 构造函数，绑定父视图模型。
        /// </summary>
        public ImageItem(DicomImageViewModel parent)
        {
            _parent = parent;
            ShowImageCommand = new RelayCommand(() =>
            {
                parent.CurrentImage = Thumbnail;
                // 发送切换消息，通知其他组件
                CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Send(
                    new SwitchDicomImageViewModelMessage(_parent));
            });
        }
    }
}
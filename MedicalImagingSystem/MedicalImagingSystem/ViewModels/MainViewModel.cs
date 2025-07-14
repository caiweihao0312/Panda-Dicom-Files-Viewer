using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Dicom;
using MedicalImagingSystem.Services;
using MedicalImagingSystem.Messenge;
using MedicalImagingSystem.Model;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace MedicalImagingSystem.ViewModels
{
    /// <summary>
    /// MainViewModel 负责主界面的数据绑定、命令处理和 DICOM 影像的管理。
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        /// <summary>
        /// DICOM 服务接口，用于加载和处理 DICOM 图像。
        /// </summary>
        private readonly IDicomService _dicomService;

        /// <summary>
        /// DICOM 序列集合，包含所有已加载的 DICOM 影像，用于界面绑定。
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<DicomImageViewModel> dicomSeries = new ObservableCollection<DicomImageViewModel>();

        /// <summary>
        /// 当前选中的 DICOM 影像视图模型。
        /// </summary>
        [ObservableProperty]
        private DicomImageViewModel selectedDicomImageViewModel;

        /// <summary>
        /// 状态栏文本，显示当前操作状态。
        /// </summary>
        [ObservableProperty]
        private string statusText = "就绪";

        /// <summary>
        /// 构造函数，注入 DICOM 服务，并注册消息监听影像切换。
        /// </summary>
        /// <param name="dicomService">DICOM 服务实例</param>
        public MainViewModel(IDicomService dicomService)
        {
            _dicomService = dicomService;
            CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Register<SwitchDicomImageViewModelMessage>(
            this, (r, m) =>
            {
                SelectedDicomImageViewModel = m.Target;
            });
        }

        /// <summary>
        /// 打开单个 DICOM 文件，并添加到序列集合。
        /// </summary>
        [RelayCommand]
        private void OpenDicomFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "DICOM 文件 (*.dcm)|*.dcm" };
            if (dialog.ShowDialog() == true)
            {
                var vm = new DicomImageViewModel(_dicomService, dialog.FileName);
                if (!DicomSeries.Contains(vm))
                {
                    DicomSeries.Add(vm);
                }
                SelectedDicomImageViewModel = vm;
                StatusText = $"已加载文件: {dialog.FileName}";
            }
        }

        /// <summary>
        /// 打开 DICOM 序列（文件夹），批量加载所有 DICOM 文件。
        /// </summary>
        [RelayCommand]
        private void OpenDicomSeries()
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                DicomSeries.Clear();
                foreach (var file in System.IO.Directory.GetFiles(dialog.SelectedPath, "*.dcm"))
                {
                    DicomSeries.Add(new DicomImageViewModel(_dicomService, file));
                }
                SelectedDicomImageViewModel = DicomSeries.Count > 0 ? DicomSeries[0] : null;
                StatusText = $"已加载序列: {dialog.SelectedPath}";
            }
        }

        /// <summary>
        /// 导出当前影像为 PNG 格式图片。
        /// </summary>
        [RelayCommand]
        private void ExportPngImage()
        {
            if (SelectedDicomImageViewModel?.CurrentImage == null)
            {
                StatusText = "没有可导出的影像";
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "PNG 图像 (*.png)|*.png",
                FileName = SelectedDicomImageViewModel.FileName + ".png"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (var fileStream = new FileStream(dialog.FileName, FileMode.Create))
                    {
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(SelectedDicomImageViewModel.CurrentImage));
                        encoder.Save(fileStream);
                    }
                    StatusText = $"PNG图像已导出: {dialog.FileName}";
                }
                catch (Exception ex)
                {
                    StatusText = $"导出PNG失败: {ex.Message}";
                }
            }
        }

        /// <summary>
        /// 导出当前影像为 DICOM 文件。
        /// </summary>
        [RelayCommand]
        private void ExportDicomImage()
        {
            if (SelectedDicomImageViewModel?.m_DicomTagInfoDTO == null)
            {
                StatusText = "没有可导出的DICOM影像";
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "DICOM 文件 (*.dcm)|*.dcm",
                FileName = SelectedDicomImageViewModel.FileName
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // 假设m_DicomTagInfoDTO中有原始DicomDataset或可重建
                    // 这里以最简单的方式保存原始像素数据和元数据
                    var dto = SelectedDicomImageViewModel.m_DicomTagInfoDTO;
                    var dataset = new DicomDataset();
                    // 填充必要的DICOM标签
                    dataset.Add(DicomTag.PatientName, dto.PatientName ?? "");
                    dataset.Add(DicomTag.PatientID, dto.PatientId ?? "");
                    dataset.Add(DicomTag.Rows, dto.ImageRows);
                    dataset.Add(DicomTag.Columns, dto.ImageColumns);
                    dataset.Add(DicomTag.PixelData, dto.PixelData);
                    dataset.Add(DicomTag.WindowCenter, dto.WindowCenter);
                    dataset.Add(DicomTag.WindowWidth, dto.WindowWidth);
                    // ...可根据需要补充其它标签

                    var dicomFile = new DicomFile(dataset);
                    dicomFile.Save(dialog.FileName);

                    StatusText = $"DICOM文件已导出: {dialog.FileName}";
                }
                catch (Exception ex)
                {
                    StatusText = $"导出DICOM失败: {ex.Message}";
                }
            }
        }

        /// <summary>
        /// 清空 DICOM 序列集合及当前选中影像。
        /// </summary>
        [RelayCommand]
        private void ClearDicomSeries()
        {
            if (DicomSeries != null && DicomSeries.Count > 0)
            {
                DicomSeries.Clear();
                SelectedDicomImageViewModel = null;
                StatusText = $"文件列表已清空";
            }
            else
            {
                StatusText = $"文件列表无数据";
            }
        }

        /// <summary>
        /// 开启或关闭窗宽/窗位调整功能。
        /// </summary>
        [RelayCommand]
        private void AdjustWindowing()
        {
            if (SelectedDicomImageViewModel != null)
            {
                SelectedDicomImageViewModel.isOpenZoom = false;
                SelectedDicomImageViewModel.isOpenAdjustWindowing = !SelectedDicomImageViewModel.isOpenAdjustWindowing;
                string statusText = SelectedDicomImageViewModel.isOpenAdjustWindowing ? "窗宽/窗位调整功能已开启" : "窗宽/窗位调整功能已关闭";
                StatusText = statusText;
            }
            else
            {
                StatusText = "请先选择一个影像进行窗宽/窗位调整";
            }
        }

        /// <summary>
        /// 开启或关闭影像缩放功能，关闭时恢复原始显示。
        /// </summary>
        [RelayCommand]
        private void Zoom()
        {
            if (SelectedDicomImageViewModel != null)
            {
                SelectedDicomImageViewModel.isOpenAdjustWindowing = false;
                SelectedDicomImageViewModel.isOpenZoom = !SelectedDicomImageViewModel.isOpenZoom;
                string statusText = SelectedDicomImageViewModel.isOpenZoom ? "缩放功能已开启" : "缩放功能已关闭";
                StatusText = statusText;
                // 如果关闭了缩放功能，则恢复原有的图像大小和位置显示
                if (!SelectedDicomImageViewModel.isOpenZoom)
                {
                    // 恢复原始图像显示
                    // 重新加载当前帧的原始图像
                    if (SelectedDicomImageViewModel.ImageSeries != null && SelectedDicomImageViewModel.ImageSeries.Count > 0)
                    {
                        SelectedDicomImageViewModel.CurrentImage = SelectedDicomImageViewModel.ResetImageDisplay();
                    }
                }
            }
            else
            {
                StatusText = "请先选择一个影像进行缩放";
            }
        }

        /// <summary>
        /// 旋转当前影像，每次旋转 90 度。
        /// </summary>
        [RelayCommand]
        private void Rotate()
        {
            if (SelectedDicomImageViewModel != null)
            {
                SelectedDicomImageViewModel.isOpenAdjustWindowing = false;
                SelectedDicomImageViewModel.isOpenZoom = false;
                SelectedDicomImageViewModel.RotateCommand.Execute(null);
                StatusText = $"影像已旋转，当前角度：{SelectedDicomImageViewModel.RotationAngle}°";
            }
            else
            {
                StatusText = "请先选择一个影像进行旋转";
            }
        }

        /// <summary>
        /// 图像水平镜像处理
        /// </summary>
        [RelayCommand]
        private void HorizontalMirror()
        {
            if (SelectedDicomImageViewModel != null)
            {
                SelectedDicomImageViewModel.HorizontalMirrorCommand.Execute(null);
                StatusText = "已进行水平镜像";
            }
            else
            {
                StatusText = "请先选择一个影像进行水平镜像";
            }
        }

        /// <summary>
        /// 图像垂直镜像处理
        /// </summary>
        [RelayCommand]
        private void VerticallyMirror()
        {
            if (SelectedDicomImageViewModel != null)
            {
                SelectedDicomImageViewModel.VerticallyMirrorCommand.Execute(null);
                StatusText = "已进行垂直镜像";
            }
            else
            {
                StatusText = "请先选择一个影像进行垂直镜像";
            }
        }

        /// <summary>
        /// 启用长度测量模式。
        /// </summary>
        [RelayCommand]
        private void MeasureLength()
        {
            SelectedDicomImageViewModel?.MeasureLengthCommand.Execute(null);
            StatusText = "长度测量模式";
        }

        /// <summary>
        /// 启用角度测量模式。
        /// </summary>
        [RelayCommand]
        private void MeasureAngle()
        {
            SelectedDicomImageViewModel?.MeasureAngleCommand.Execute(null);
            StatusText = "角度测量模式";
        }

        /// <summary>
        /// 切换测量工具启用状态。
        /// </summary>
        [RelayCommand]
        private void ToggleMeasurementTool()
        {
            SelectedDicomImageViewModel?.ToggleMeasurementToolCommand.Execute(null);
            StatusText = SelectedDicomImageViewModel?.IsMeasureToolEnabled == true ? "测量工具已开启" : "测量工具已关闭";
        }

        /// <summary>
        /// 清除所有测量数据。
        /// </summary>
        [RelayCommand]
        public void ClearMeasurements()
        {
            SelectedDicomImageViewModel?.ClearMeasurementsDataCommand.Execute(null);
            StatusText = "已清除测量数据";
        }
    }

}
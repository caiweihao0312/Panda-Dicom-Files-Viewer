using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CbctHostApp.Models;
using CbctHostApp.Services;

namespace CbctHostApp.ViewModels
{
    /// <summary>
    /// 主视图模型类，负责管理应用程序的主要逻辑和数据绑定。
    /// </summary>
    public class MainViewModel : ObservableObject
    {
        private IModbusService _modbus; // Modbus 服务，用于与硬件设备通信
        private readonly IReconstructionService _reconstructor; // 重建服务，用于处理图像重建
                                                                //private readonly IOpenIgtLinkService _igtService; // （注释掉）用于与外部设备通信的服务

        private CancellationTokenSource _cts; // 用于取消异步任务的令牌源
        private BitmapImage _previewImage; // 当前预览图像

        /// <summary>
        /// 扫描参数，包含 FOV、帧率和曝光时间等设置。
        /// </summary>
        public ScanParameters ScanParams { get; } = new ScanParameters();

        /// <summary>
        /// 开始扫描命令。
        /// </summary>
        public IRelayCommand StartScanCommand { get; }
        /// <summary>
        /// 停止扫描命令。
        /// </summary>
        public IRelayCommand StopScanCommand { get; }

        /// <summary>
        /// 当前预览图像，用于绑定到 UI。
        /// </summary>
        public BitmapImage PreviewImage
        {
            get => _previewImage;
            set => SetProperty(ref _previewImage, value);
        }

        /// <summary>
        /// 构造函数，初始化 Modbus 服务、重建服务和命令。
        /// </summary>
        public MainViewModel()
        {
            _reconstructor = new ReconstructionService(); // 初始化重建服务
            //_igtService = new OpenIgtLinkService("127.0.0.1", 18944); // 初始化外部通信服务（已注释）

            // 初始化命令
            StartScanCommand = new RelayCommand(async () => await StartScanAsync());
            StopScanCommand = new RelayCommand(StopScan);
        }

        /// <summary>
        /// 异步方法：开始扫描。
        /// </summary>
        private async Task StartScanAsync()
        {
            _cts = new CancellationTokenSource(); // 创建新的取消令牌源

            _modbus = new ModbusService("COM3", 19200); // 初始化 Modbus 服务
            // 1. 下发扫描参数到硬件设备
            await _modbus.ConfigureScanAsync(ScanParams);

            // 2. 启动图像重建循环
            _ = Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested) // 循环直到取消
                {
                    var bmp = await _reconstructor.GetNextSliceAsync(_cts.Token); // 获取下一张图像切片
                    PreviewImage = bmp; // 更新预览图像
                    //await _igtService.SendImageAsync(bmp); // 发送图像到外部设备（已注释）
                    await Task.Delay(TimeSpan.FromMilliseconds(1000.0 / ScanParams.FrameRate), _cts.Token); // 按帧率延迟
                }
            }, _cts.Token);
        }

        /// <summary>
        /// 停止扫描。
        /// </summary>
        private void StopScan()
        {
            _cts?.Cancel(); // 取消异步任务
        }
    }
}
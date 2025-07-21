using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DemoHardwareIntegrationApp.Services
{
    /// <summary>
    /// 串口读写服务，基于 BackgroundService 实现事件驱动。
    /// 负责打开串口、监听数据并通过 Channel 推送数据。
    /// </summary>
    public class SerialPortService : BackgroundService
    {
        private readonly ILogger<SerialPortService> _logger; // 日志记录器
        private readonly SerialPort _serialPort; // 串口对象

        // 全局通道，用于将串口数据推送给其他服务
        public static readonly Channel<string> SerialToProcessingChannel = Channel.CreateUnbounded<string>();

        /// <summary>
        /// 构造函数，初始化串口参数并注册数据接收事件。
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public SerialPortService(ILogger<SerialPortService> logger)
        {
            _logger = logger;
            _serialPort = new SerialPort("COM1", 115200, Parity.None, 8, StopBits.One)
            {
                NewLine = "\r\n",      // 设置换行符
                ReadTimeout = 500      // 读取超时时间
            };
            _serialPort.DataReceived += OnDataReceived; // 注册数据接收事件
        }

        /// <summary>
        /// 服务启动时执行，打开串口并记录日志。
        /// </summary>
        /// <param name="stoppingToken">取消令牌</param>
        /// <returns>任务对象</returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_serialPort.IsOpen)
            {
                _serialPort.Open();
                _logger.LogInformation("Serial port opened on {PortName}", _serialPort.PortName);
            }
            // 不在此方法中循环读取，依赖 DataReceived 事件
            return Task.CompletedTask;
        }

        /// <summary>
        /// 服务停止时执行，注销事件并关闭串口。
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>任务对象</returns>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _serialPort.DataReceived -= OnDataReceived;
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
                _logger.LogInformation("Serial port closed.");
            }
            await base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// 串口数据接收事件处理方法，读取数据并推送到通道。
        /// </summary>
        /// <param name="sender">事件源</param>
        /// <param name="e">事件参数</param>
        private async void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                var line = _serialPort.ReadLine(); // 读取一行数据
                _logger.LogInformation("Received from serial: {Data}", line);
                await SerialToProcessingChannel.Writer.WriteAsync(line); // 推送到通道
            }
            catch (TimeoutException)
            {
                // 超时忽略
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading serial data");
            }
        }
    }
}
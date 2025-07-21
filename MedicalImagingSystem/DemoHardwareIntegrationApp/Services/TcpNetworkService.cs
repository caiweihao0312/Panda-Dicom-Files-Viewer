using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DemoHardwareIntegrationApp.Services
{
    /// <summary>
    /// TCP 客户端服务，基于 BackgroundService 实现异步收发。
    /// 负责连接远程服务器、转发串口数据并处理网络通信。
    /// </summary>
    public class TcpNetworkService : BackgroundService
    {
        private readonly ILogger<TcpNetworkService> _logger; // 日志记录器
        private TcpClient _client; // TCP 客户端对象
        private NetworkStream _stream; // 网络流对象
        private readonly Channel<string> _sendChannel = Channel.CreateUnbounded<string>(); // 发送消息通道

        /// <summary>
        /// 构造函数，注入日志记录器。
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public TcpNetworkService(ILogger<TcpNetworkService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 服务启动时执行，连接 TCP 服务器并启动收发循环。
        /// </summary>
        /// <param name="stoppingToken">取消令牌</param>
        /// <returns>任务对象</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _client = new TcpClient();
            await _client.ConnectAsync("192.168.1.100", 9000, stoppingToken); // 连接远程服务器
            _logger.LogInformation("Connected to TCP server at 192.168.1.100:9000");

            _stream = _client.GetStream();

            // 启动网络接收和发送循环
            var receiveTask = ReceiveLoop(stoppingToken);
            var sendTask = SendLoop(stoppingToken);

            // 订阅串口数据，将其转发到网络
            _ = Task.Run(async () =>
            {
                await foreach (var msg in SerialPortService.SerialToProcessingChannel.Reader.ReadAllAsync(stoppingToken))
                {
                    var forward = $"FromSerial:{msg}"; // 串口数据加前缀后转发
                    await _sendChannel.Writer.WriteAsync(forward, stoppingToken);
                }
            }, stoppingToken);

            await Task.WhenAll(receiveTask, sendTask); // 等待收发任务完成
        }

        /// <summary>
        /// 网络接收循环，异步读取服务器数据并记录日志。
        /// </summary>
        /// <param name="ct">取消令牌</param>
        private async Task ReceiveLoop(CancellationToken ct)
        {
            var buffer = new byte[1024];
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, ct);
                    if (bytesRead == 0) break; // 对方关闭
                    var msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    _logger.LogInformation("Received from network: {Msg}", msg);

                    // 如果需要，也可以将网络数据写回串口
                    // SerialPortService.SerialPort.WriteLine(msg);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ReceiveLoop");
            }
        }

        /// <summary>
        /// 网络发送循环，异步发送通道中的消息到服务器。
        /// </summary>
        /// <param name="ct">取消令牌</param>
        private async Task SendLoop(CancellationToken ct)
        {
            try
            {
                await foreach (var msg in _sendChannel.Reader.ReadAllAsync(ct))
                {
                    var data = Encoding.UTF8.GetBytes(msg + "\n");
                    await _stream.WriteAsync(data, ct);
                    _logger.LogInformation("Sent to network: {Msg}", msg);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendLoop");
            }
        }

        /// <summary>
        /// 服务停止时执行，释放网络资源并记录日志。
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>任务对象</returns>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _stream?.Dispose();
            _client?.Close();
            _logger.LogInformation("TCP connection closed.");
            await base.StopAsync(cancellationToken);
        }
    }
}
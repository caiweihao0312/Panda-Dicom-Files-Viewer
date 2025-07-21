using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// 创建并配置主机，注册后台服务和日志
await Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // 注册串口服务为后台托管服务
        services.AddHostedService<DemoHardwareIntegrationApp.Services.SerialPortService>();
        // 注册网络服务为后台托管服务
        services.AddHostedService<DemoHardwareIntegrationApp.Services.TcpNetworkService>();
    })
    .ConfigureLogging(logging =>
    {
        // 清除默认日志提供程序
        logging.ClearProviders();
        // 添加控制台日志输出
        logging.AddConsole();
    })
    .Build()
    .RunAsync(); // 异步运行主机
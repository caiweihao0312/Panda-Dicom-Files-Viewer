using MedicalConsoleApp.Services;
using Serilog;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace MedicalConsoleApp
{
    class Program
    {
        static int Main(string[] args)
        {
            // 日志初始化
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logs/console.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();


            // 提供用户操作选项
            Console.WriteLine("请选择通信协议：");
            Console.WriteLine(" 1. dicom 通信协议");
            Console.WriteLine(" 2. modbus 通信协议");
            Console.Write("输入编号：");
            var key = Console.ReadLine();
            var protocolOption = new Option<string>("--protocol", () => "dicom", "通信协议 (dicom|modbus)");
            var serverOption = new Option<string>("--server", () => "127.0.0.1", description: "设备 IP 地址 或串口号");
            var portOption = new Option<int>("--port", () => 104, "端口号 或 波特率");
            var rootCommand = new RootCommand("Medical Imaging Console App")
                        {
                            protocolOption,
                            serverOption,
                            portOption
                        };
            switch (key)
            {
                case "1":
                    {
                        // 定义dicom协议命令行选项
                        protocolOption = new Option<string>("--protocol", () => "dicom", "通信协议 (dicom|modbus)");
                        serverOption = new Option<string>("--server", () => "127.0.0.1", description: "设备 IP 地址 或串口号");
                        portOption = new Option<int>("--port", () => 11112, "端口号 或 波特率");
                        rootCommand = new RootCommand("Medical Imaging Console App")
                        {
                            protocolOption,
                            serverOption,
                            portOption
                        };
                    }
                    break;
                case "2":
                    {
                        // 定义modbus协议命令行选项
                        protocolOption = new Option<string>("--protocol", () => "modbus", "通信协议 (dicom|modbus)");
                        serverOption = new Option<string>("--server", () => "COM1", description: "设备 IP 地址 或串口号");
                        portOption = new Option<int>("--port", () => 9600, "端口号 或 波特率");
                        rootCommand = new RootCommand("Medical Imaging Console App")
                        {
                            protocolOption,
                            serverOption,
                            portOption
                        };

                        // 列出可用串口
                        ModbusService.ListAvailablePorts();
                    }
                    break;
                default:
                    Console.WriteLine("未知操作。默认dicom 通信协议");
                    break;
            }

            // 设置命令行处理器
            rootCommand.SetHandler((protocol, server, port) =>
            {
                switch (protocol.ToLower())
                {
                    case "dicom":
                        new Services.MyDicomService(server, port).Run();
                        break;
                    case "modbus":
                        new Services.ModbusService(server, port).Run();
                        break;
                    default:
                        Console.WriteLine("Unsupported protocol. Use 'dicom' or 'modbus'.");
                        break;
                }
            }, protocolOption, serverOption, portOption);

            return rootCommand.InvokeAsync(args).Result;
        }
    }
}
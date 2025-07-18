using System;
using System.Threading.Tasks;
using NModbus;
using NModbus.Serial;
using CbctHostApp.Models;
using System.IO.Ports;

namespace CbctHostApp.Services
{
    /// <summary>
    /// ModbusService 类用于通过串口与 Modbus 设备进行通信，实现参数下发和扫描控制。
    /// </summary>
    public class ModbusService : IModbusService, IDisposable
    {
        private readonly IModbusMaster _master; // Modbus 主站对象，用于发送 Modbus 命令

        /// <summary>
        /// 构造函数，初始化串口并创建 Modbus 主站。
        /// </summary>
        /// <param name="portName">串口端口名，例如 "COM3"</param>
        /// <param name="baudRate">串口波特率，例如 115200</param>
        public ModbusService(string portName, int baudRate)
        {
            // 创建串口对象并打开串口
            var serial = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
            serial.Open();
            // 创建 Modbus 工厂并生成主站对象
            var factory = new ModbusFactory();
            _master = factory.CreateRtuMaster(serial);
        }

        /// <summary>
        /// 异步方法：向设备下发扫描参数，并启动扫描命令。
        /// </summary>
        /// <param name="param">扫描参数对象，包括 FOV、帧率、曝光时间</param>
        /// <returns>异步任务</returns>
        public Task ConfigureScanAsync(ScanParameters param)
        {
            // 寄存器映射：40001:FOV, 40002:FrameRate, 40003:ExposureTime
            _master.WriteSingleRegister(1, 0, (ushort)param.Fov);
            _master.WriteSingleRegister(1, 1, (ushort)param.FrameRate);
            _master.WriteSingleRegister(1, 2, (ushort)param.ExposureTime);
            // 启动扫描命令寄存器
            _master.WriteSingleCoil(1, 10, true);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 释放资源，关闭 Modbus 连接。
        /// </summary>
        public void Dispose()
        {
            (_master.Transport as IDisposable)?.Dispose();
        }
    }
}
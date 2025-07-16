using NModbus;
using System.IO.Ports;

namespace MedicalConsoleApp.Services
{
    /**
     *   1. MODBUS 协议简介
         - MODBUS 是一种应用层协议，广泛用于工业自动化领域，用于主站（Master）和从站（Slave）之间的数据通信。
         - RTU（Remote Terminal Unit）模式 是 MODBUS 协议的一种传输模式，使用二进制格式传输数据，具有高效、紧凑的特点。
         
         2. MODBUS RTU 的通信流程
         - 主站（Master）：发起通信请求（如读取寄存器、写入寄存器）。
         - 从站（Slave）：接收请求并返回响应（如返回寄存器值或确认写入操作）。
         - 通信通过串口（如 RS-232 或 RS-485）进行。

         3. 程序中的含义
         在代码中，`启动 MODBUS RTU` 表示：
            1. 初始化串口通信：
               - 打开指定的串口（如 `COM1`）。
               - 设置通信参数（如波特率、数据位、校验位等）。
            2. 创建 MODBUS 主站：
               - 使用 `NModbus` 库创建一个 RTU 主站（`IModbusSerialMaster`）。
               - 主站将通过串口与从站通信。
            
            3. 准备进行数据交互：
               - 主站可以发送请求（如读取或写入寄存器）。
               - 从站会根据请求返回数据或执行操作。
         
         4. 代码中的实现
         以下代码片段展示了如何启动 MODBUS RTU：
         
         5. 实际意义
         - 启动 MODBUS RTU 是程序准备与从站设备进行通信的第一步。
         - 它确保串口已打开，通信参数已正确设置，MODBUS 主站已初始化。
         - 后续可以通过主站发送请求（如读取或写入寄存器）与从站交互。
         
         6. 应用场景
         - 工业自动化：如 PLC、传感器、执行器等设备的监控和控制。
         - 能源管理：如电表、温度控制器的数据采集。
         - 远程监控：通过串口与远程设备通信，获取状态或发送指令。
         
     * **/
    public class ModbusService
    {
        private readonly string _portName;  // 串口名称（如 COM1）
        private readonly int _baudRate;     // 波特率（如 9600）

        // 构造函数，初始化串口名称和波特率
        public ModbusService(string portName, int baudRate)
        {
            _portName = portName;
            _baudRate = baudRate;
        }
        /// <summary>
        /// 检测并列出可用的串口
        /// </summary>
        public static void ListAvailablePorts()
        {
            Console.WriteLine("可用的串口列表：");
            var ports = SerialPort.GetPortNames(); // 获取系统中所有可用的串口名称
            if (ports.Length == 0)
            {
                Console.WriteLine("没有检测到可用的串口。");
            }
            else
            {
                foreach (var port in ports)
                {
                    Console.WriteLine($"- {port}"); // 输出每个可用的串口名称
                }
            }
        }

        /// <summary>
        /// 启动 MODBUS RTU 通信，执行读写操作
        /// 使用 Modbus 模拟器（如 Modbus Slave）测试写入和验证功能
        /// 下载地址：https://www.modbustools.com/modbus_slave.html
        /// 或使用 com0com 虚拟串口驱动程序创建虚拟串口对
        /// 下载地址：https://sourceforge.net/projects/com0com/
        /// 注意：在实际应用中，请确保串口号和波特率与设备配置一致
        /// 
        /// </summary>
        public void Run()
        {
            Console.WriteLine($"启动 MODBUS RTU，通过串口 {_portName} @ {_baudRate}bps");
            try
            {
                // 初始化串口通信
                using var serial = new SerialPort(_portName, _baudRate)
                {
                    ReadTimeout = 5000,  // 设置读取超时时间为 5000 毫秒
                    WriteTimeout = 5000 // 设置写入超时时间为 5000 毫秒
                };

                serial.Open(); // 打开串口
                var factory = new ModbusFactory(); // 创建 Modbus 工厂
                var master = factory.CreateRtuMaster((NModbus.IO.IStreamResource)serial); // 创建 RTU 主站

                // 示例：读取寄存器
                ushort startAddress = 0;    // 起始寄存器地址
                ushort numInputs = 5;       // 读取的寄存器数量
                try
                {
                    // 读取保持寄存器的值
                    ushort[] registers = master.ReadHoldingRegisters(1, startAddress, numInputs);
                    Console.WriteLine($"读取寄存器[{startAddress}~{startAddress + numInputs - 1}]：{string.Join(",", registers)}");
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("错误：读取操作超时，请检查设备连接或重试。");
                }

                // 示例：写入单个寄存器并验证
                try
                {
                    // 实现写入单个寄存器操作
                    ushort writeAddress = 10; // 要写入的寄存器地址
                    ushort valueToWrite = 1234; // 要写入的值
                    int maxRetries = 3; // 最大重试次数
                    int attempt = 0;    // 当前尝试次数
                    bool success = false;// 写入是否成功

                    while (attempt < maxRetries && !success)
                    {
                        try
                        {
                            attempt++;
                            master.WriteSingleRegister(1, writeAddress, valueToWrite);
                            Console.WriteLine($"成功写入寄存器[{writeAddress}]，值：{valueToWrite}");

                            // 验证写入是否成功
                            ushort[] readBackValue = master.ReadHoldingRegisters(1, writeAddress, 1);
                            if (readBackValue[0] == valueToWrite)
                            {
                                Console.WriteLine($"验证成功：寄存器[{writeAddress}]的值为 {readBackValue[0]}");
                                success = true;
                            }
                            else
                            {
                                Console.WriteLine($"验证失败：寄存器[{writeAddress}]的值为 {readBackValue[0]}，预期值为 {valueToWrite}");
                            }
                        }
                        catch (TimeoutException)
                        {
                            Console.WriteLine($"错误：写入操作超时（第 {attempt} 次尝试），请检查设备连接或重试。");
                        }
                        catch (IOException ex)
                        {
                            Console.WriteLine($"错误：写入操作失败（第 {attempt} 次尝试），原因：{ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"未知错误：{ex.Message}");
                            break; // 遇到未知错误时停止重试
                        }
                    }
                    if (!success)
                    {
                        Console.WriteLine($"写入操作失败：已尝试 {maxRetries} 次，仍未成功。");
                        // TODO: 记录日志或通知用户
                    }
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("错误：写入操作超时，请检查设备连接或重试。");
                }
                // 示例：写入多个寄存器并验证
                try
                {
                    // 实现写入多个寄存器操作（如写多个寄存器或线圈）
                    ushort writeAddress = 10; // 要写入的寄存器地址
                    ushort[] valuesToWrite = { 1234, 5678, 9101 };// 要写入的值
                    master.WriteMultipleRegisters(1, writeAddress, valuesToWrite);
                    Console.WriteLine($"成功写入寄存器[{writeAddress}]，值：{valuesToWrite}");

                    // 批量验证写入是否成功
                    ushort[] readBackValues = master.ReadHoldingRegisters(1, writeAddress, (ushort)valuesToWrite.Length);
                    bool allMatch = true;

                    for (int i = 0; i < valuesToWrite.Length; i++)
                    {
                        if (readBackValues[i] != valuesToWrite[i])
                        {
                            Console.WriteLine($"验证失败：寄存器[{writeAddress + i}]的值为 {readBackValues[i]}，预期值为 {valuesToWrite[i]}");
                            allMatch = false;
                        }
                    }

                    if (allMatch)
                    {
                        Console.WriteLine($"批量验证成功：寄存器[{writeAddress}~{writeAddress + valuesToWrite.Length - 1}]的值为 {string.Join(",", readBackValues)}");
                    }
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("错误：写入操作超时，请检查设备连接或重试。");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"未知错误：{ex.Message}");
                }

            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"错误：无法找到串口 {_portName}。请检查串口名称是否正确。");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"错误：串口 {_portName} 正在被其他程序占用。");
            }
            catch (TimeoutException)
            {
                Console.WriteLine("错误：串口操作超时，请检查设备连接或重试。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"未知错误：{ex.Message}");
            }
        }
    }
}
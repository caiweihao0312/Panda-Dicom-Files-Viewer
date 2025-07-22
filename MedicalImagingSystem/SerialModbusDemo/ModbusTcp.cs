using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Modbus.Device; // 引用 NModbus4

namespace SerialModbusDemo
{
    public class ModbusTcp
    {
        public static void ReadHoldingRegisters()
        {
            // TCP 连接参数
            string ip = "192.168.1.100"; // 设备 IP
            int port = 502;              // Modbus TCP 默认端口
            byte slaveId = 1;            // 从站地址
            ushort startAddress = 0;     // 起始寄存器地址
            ushort numRegisters = 10;    // 读取数量

            // 建立 TCP 连接
            using (TcpClient client = new TcpClient(ip, port))
            {
                // 创建 Modbus TCP 主站
                var master = ModbusIpMaster.CreateIp(client);

                // 读取保持寄存器
                ushort[] values = master.ReadHoldingRegisters(slaveId, startAddress, numRegisters);

                // 输出结果
                Console.WriteLine("寄存器值：");
                for (int i = 0; i < values.Length; i++)
                {
                    Console.WriteLine($"[{startAddress + i}] = {values[i]}");
                }
            }
        }
    }
}

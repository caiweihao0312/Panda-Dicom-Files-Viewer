using System;

namespace SerialModbusDemo
{
    /// <summary>
    /// 提供 Modbus RTU 协议相关的报文构建与 CRC 校验工具方法。
    /// </summary>
    public static class ModbusRtu
    {
        /// <summary>
        /// 构建 Modbus RTU 读保持寄存器（功能码 0x03）请求报文。
        /// </summary>
        /// <param name="slaveId">从站地址（1 字节）。</param>
        /// <param name="startAddress">起始寄存器地址（2 字节）。</param>
        /// <param name="numberOfPoints">读取寄存器数量（2 字节）。</param>
        /// <returns>包含完整请求帧（含 CRC 校验）的字节数组。</returns>
        public static byte[] BuildReadHoldingRegisters(byte slaveId, ushort startAddress, ushort numberOfPoints)
        {
            // Modbus RTU 报文格式: [SlaveId][Function][StartAddrHi][StartAddrLo][QtyHi][QtyLo][CRCLo][CRCHi]
            byte[] frame = new byte[8];
            frame[0] = slaveId;
            frame[1] = 0x03; // 功能码：读保持寄存器
            frame[2] = (byte)(startAddress >> 8);      // 起始地址高字节
            frame[3] = (byte)(startAddress & 0xFF);    // 起始地址低字节
            frame[4] = (byte)(numberOfPoints >> 8);    // 寄存器数量高字节
            frame[5] = (byte)(numberOfPoints & 0xFF);  // 寄存器数量低字节
            ushort crc = ComputeCrc(frame, 6);         // 计算前 6 字节的 CRC16
            frame[6] = (byte)(crc & 0xFF);             // CRC 低字节
            frame[7] = (byte)(crc >> 8);               // CRC 高字节
            return frame;
        }

        /// <summary>
        /// 校验 Modbus RTU 报文的 CRC16 是否正确。
        /// </summary>
        /// <param name="data">包含 CRC 校验码的完整报文字节数组。</param>
        /// <returns>CRC 校验通过返回 true，否则返回 false。</returns>
        public static bool CheckCrc(byte[] data)
        {
            int len = data.Length;
            if (len < 3) return false; // 至少包含 1 字节数据和 2 字节 CRC
            ushort crcCalc = ComputeCrc(data, len - 2); // 计算除 CRC 外的 CRC16
            ushort crcRec = (ushort)(data[len - 2] | (data[len - 1] << 8)); // 报文中的 CRC
            return crcCalc == crcRec;
        }

        /// <summary>
        /// 计算指定数据的 Modbus RTU CRC16 校验码。
        /// </summary>
        /// <param name="data">待计算的数据字节数组。</param>
        /// <param name="length">参与计算的字节数（不含 CRC 部分）。</param>
        /// <returns>CRC16 校验码。</returns>
        public static ushort ComputeCrc(byte[] data, int length)
        {
            ushort crc = 0xFFFF;
            for (int pos = 0; pos < length; pos++)
            {
                crc ^= data[pos];
                for (int i = 0; i < 8; i++)
                {
                    bool lsb = (crc & 0x0001) != 0;
                    crc >>= 1;
                    if (lsb) crc ^= 0xA001;
                }
            }
            return crc;
        }
    }
}
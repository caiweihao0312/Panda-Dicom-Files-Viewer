using System;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;

namespace SerialModbusDemo
{
    /// <summary>
    /// 主窗体类，实现串口 Modbus RTU 通信演示。
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>
        /// 串口对象，用于与 Modbus 设备通信。
        /// </summary>
        private SerialPort _port;

        /// <summary>
        /// 串口是否已打开标志。
        /// </summary>
        private bool _isOpen = false;

        /// <summary>
        /// 构造函数，初始化窗体组件。
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 窗体加载事件，初始化串口和波特率选项。
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            // 枚举可用串口
            string[] ports = SerialPort.GetPortNames();
            comboBoxPorts.Items.AddRange(ports);

            // 调试输出串口列表
            if (ports.Length == 0)
            {
                MessageBox.Show("未检测到可用串口，请检查硬件连接或驱动安装。", "调试信息");
            }
            else
            {
                string portList = string.Join(", ", ports);
                MessageBox.Show($"检测到串口: {portList}", "调试信息");
                comboBoxPorts.SelectedIndex = 0; // 默认选中第一个串口
            }
            comboBoxBaudRate.SelectedIndex = 4; // 默认115200
        }

        /// <summary>
        /// 打开或关闭串口按钮点击事件。
        /// </summary>
        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (!_isOpen)
            {
                // 当前串口未打开，尝试打开串口
                try
                {
                    // 创建串口对象，参数包括端口号、波特率、校验位、数据位、停止位
                    _port = new SerialPort(
                        comboBoxPorts.SelectedItem?.ToString(),
                        int.Parse(comboBoxBaudRate.SelectedItem?.ToString()),
                        Parity.None, 8, StopBits.One);
                    _port.ReadTimeout = 500;  // 读超时时间，单位毫秒
                    _port.WriteTimeout = 500; // 写超时时间，单位毫秒
                    _port.DataReceived += Port_DataReceived; // 注册数据接收事件
                    _port.Open();             // 打开串口
                    _isOpen = true;           // 标记串口已打开
                    statusLabel.Text = "已连接: " + _port.PortName; // 更新状态栏
                    buttonConnect.Text = "关闭串口";                // 更新按钮文本
                }
                catch (Exception ex)
                {
                    // 打开串口失败，弹窗提示错误信息
                    MessageBox.Show("打开串口失败: " + ex.Message);
                }
            }
            else
            {
                // 当前串口已打开，关闭串口
                _port.Close();
                _isOpen = false;                // 标记串口已关闭
                statusLabel.Text = "未连接";    // 更新状态栏
                buttonConnect.Text = "打开串口";// 更新按钮文本
            }
        }

        /// <summary>
        /// 读取寄存器按钮点击事件，发送 Modbus RTU 读保持寄存器指令并解析响应。
        /// </summary>
        private void buttonRead_Click(object sender, EventArgs e)
        {
            if (!_isOpen)
            {
                // 串口未打开，弹窗提示
                MessageBox.Show("请先打开串口");
                return;
            }

            // 构建 Modbus RTU 读保持寄存器报文
            // 从站地址=1, 起始地址=0, 寄存器数=5
            byte slaveId = 1;         // Modbus 从站地址
            ushort startAddr = 0;     // 起始寄存器地址
            ushort pointCount = 5;    // 读取寄存器数量
            byte[] frame = ModbusRtu.BuildReadHoldingRegisters(slaveId, startAddr, pointCount);

            try
            {
                _port.DiscardInBuffer();           // 清空串口接收缓冲区，避免残留数据影响
                _port.Write(frame, 0, frame.Length); // 发送 Modbus RTU 指令帧

                // 等待设备响应，休眠100毫秒
                Thread.Sleep(100);

                int bytes = _port.BytesToRead;     // 获取接收缓冲区字节数
                if (bytes < 5)
                {
                    // 响应数据长度不足，弹窗提示
                    MessageBox.Show("未收到完整响应");
                    return;
                }

                byte[] resp = new byte[bytes];     // 创建接收缓冲区
                _port.Read(resp, 0, bytes);        // 读取响应数据

                // 校验响应数据的 CRC
                if (!ModbusRtu.CheckCrc(resp))
                {
                    MessageBox.Show("CRC 校验失败");
                    return;
                }

                // 解析响应数据字节
                byte byteCount = resp[2];          // 数据字节数
                listBoxData.Items.Clear();         // 清空数据显示列表
                for (int i = 0; i < byteCount / 2; i++)
                {
                    // 每两个字节为一个寄存器值（高字节在前，低字节在后）
                    ushort val = (ushort)(resp[3 + 2 * i] << 8 | resp[4 + 2 * i]);
                    listBoxData.Items.Add($"寄存器 {startAddr + i} = {val}");
                }
            }
            catch (TimeoutException)
            {
                // 读取超时，弹窗提示
                MessageBox.Show("读取超时");
            }
            catch (Exception ex)
            {
                // 其他通信异常，弹窗提示错误信息
                MessageBox.Show("通信错误: " + ex.Message);
            }
        }

        // 串口数据接收事件处理
        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                int bytes = _port.BytesToRead;
                if (bytes > 0)
                {
                    byte[] buffer = new byte[bytes];
                    _port.Read(buffer, 0, bytes);

                    // 线程安全地更新 UI
                    this.Invoke((Action)(() =>
                    {
                        // 这里可以解析数据并显示到界面
                        string hex = BitConverter.ToString(buffer);
                        listBoxData.Items.Add($"接收: {hex}");
                    }));
                }
            }
            catch (Exception ex)
            {
                // 可选：记录或显示错误
            }
        }

        // 发送按钮点击事件
        private void buttonSend_Click(object sender, EventArgs e)
        {
            if (!_isOpen)
            {
                MessageBox.Show("请先打开串口");
                return;
            }

            string text = textBoxSend.Text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("发送内容不能为空");
                return;
            }

            byte[] data;
            // 尝试解析为整数
            if (int.TryParse(text, out int intValue))
            {
                data = BitConverter.GetBytes(intValue);
                Array.Reverse(data); // 大端序
            }
            // 尝试解析为浮点数
            else if (float.TryParse(text, out float floatValue))
            {
                data = BitConverter.GetBytes(floatValue);
                Array.Reverse(data); // 大端序
            }
            // 默认按字符串发送（按ASCII编码）
            else
            {
                data = System.Text.Encoding.ASCII.GetBytes(text);
            }

            try
            {
                _port.Write(data, 0, data.Length);
                listBoxData.Items.Add($"已发送: {BitConverter.ToString(data)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("发送失败: " + ex.Message);
            }
        }
    }
}
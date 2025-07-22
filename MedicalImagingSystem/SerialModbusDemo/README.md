# SerialModbusDemo

SerialModbusDemo 是一个基于 Windows Forms 的 Modbus RTU 串口通信演示程序，适用于 .NET Framework 4.7.2。

## 功能简介

- 枚举并选择可用串口
- 设置波特率（默认 115200）
- 打开/关闭串口连接
- 发送 Modbus RTU 读保持寄存器指令
- 解析并显示 Modbus 响应数据
- CRC 校验与异常处理

## 使用方法

1. 启动程序，选择串口和波特率。
2. 点击“打开串口”按钮建立连接。
3. 点击“读取”按钮，程序将向从站地址为 1 的设备发送读保持寄存器指令（起始地址 0，数量 5）。
4. 响应数据将以寄存器值列表形式显示在界面上。

## 主要文件说明

- `MainForm.cs`：主窗体逻辑，包含串口操作和 Modbus 通信流程。
- `ModbusRtu.cs`：Modbus RTU 协议相关的报文构建与 CRC 校验。
- `Program.cs`：应用程序入口。
- `ModbusTcp.cs`：Modbus TCP 协议相关实现。

## 环境要求

- Windows 10/11
- Visual Studio 2022
- .NET Framework 4.7.2

## 适用场景

- Modbus RTU 协议学习
- 串口通信测试
- 工业设备调试

## 许可证

MIT License
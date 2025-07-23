# Panda-Dicom-Files-Viewer
# **MedicalImagingSystem——DICOM影像文件查看器**

## 项目概述
**MedicalImagingSystem** 是一款基于 .NET 8 平台开发的医学影像文件查看工具。

---

## 功能特性
主要包括基本的DICOM影像文件查看功能 



**文件操作：**

1、打开文件：打开单个DICOM文件； 
2、打开序列：打开DICOM文件所在文件夹，批量加载DICOM文件； 
3、导出PNG图像：将打开的DICOM文件，导出为PNG格式文件； 
4、清空文件列表：将已加载的文件列表进行清空；



**影像调整：** 

1、窗宽/窗位调整：启用该功能后，通过鼠标在图像上进行拖动控制； 
2、缩放：启动该功能后，通过鼠标在图像上进行滚轮缩放控制； 
3、旋转：点击旋转按钮，图像每次向右旋转90度； 
4、水平镜像：点击该功能后，图像发生水平镜像调整； 
5、垂直镜像：点击该功能后，图像发生垂直镜像调整；



**测量工具：** 

1、测量工具开关：每次进行测量前，需要先点击开启测量工具开关； 
2、长度测量：在图像上，点击鼠标进行拖动测量长度； 
3、角度测量：在图像上，点击鼠标进行拖动测量角度； 
4、清除测量：清除在图像上已操作的测量数据；

---

## 技术栈
- **语言**：C#  
- **框架**：.NET 8  
- **UI 框架**：WPF  
- **DICOM 支持**：fo-dicom  
- **日志**：Serilog  
- **样式**：XAML  

## 截图示例
<img width="2559" height="1516" alt="image" src="https://github.com/user-attachments/assets/eec0f4f4-49aa-4fc4-aaa8-d04996cd0344" />



# **MedicalConsoleApp项目——DICOM 协议测试验证控制台工具**

## 项目简介
一个运行于 Windows 的命令行控制台，用于与 CT、MRI 等医学影像设备通过 DICOM 或 MODBUS 协议进行通信和管理。

MedicalConsoleApp 是一个基于 DICOM 和 MODBUS 协议的医学影像通信与处理系统，旨在实现医学影像的加载、解析、存储以及与远程设备的通信。
项目支持 DICOM 网络操作（如 C-ECHO、C-FIND、C-MOVE 和 C-STORE）以及 MODBUS 通信协议，用于工业设备的控制和数据采集。

## 项目职责
1.	DICOM 通信模块开发：
	实现 DICOM 网络协议的核心功能，包括 C-ECHO 测试、C-FIND 查询、C-MOVE 拉取影像以及 C-STORE 接收影像。
	开发本地 Storage SCP 服务，支持影像文件的接收与存储。
2.	MODBUS 通信模块开发：
	实现 MODBUS RTU 协议的主站功能，支持通过串口与工业设备通信。
	开发寄存器的读写功能，并实现数据验证与错误处理。
3.	用户交互设计：
	提供命令行界面，支持用户选择不同的操作（如 DICOM 操作或 MODBUS 操作）。
	提供详细的日志记录功能，便于用户了解操作状态和调试问题。
4.	异常处理与日志记录：
	捕获并处理网络通信、文件存储和数据解析中的异常。
	使用 Serilog 记录操作日志，便于问题排查和系统维护。


## 功能概览
- DICOM 网络服务  
  - 支持 C-ECHO、C-FIND、C-MOVE 和 C-STORE 操作 
- MODBUS TCP/RTU  
  - 支持通过串口与工业设备通信。 
- 日志记录  
- 简易命令行交互  
- 本地安装调试支持

## 技术栈
- .NET 8.0 Console App  
- C# 12.0  
- DICOM：使用 [fo-dicom](https://github.com/fo-dicom/fo-dicom) 实现 DICOM 文件的加载、解析和网络通信。
- MODBUS：使用 [NModbus](https://github.com/NModbus/NModbus) 实现 MODBUS RTU 协议的通信功能。
- Serilog：用于日志记录。

## 项目功能实现
1.	DICOM 通信模块
	C-ECHO 测试：
	验证与远程 DICOM 服务的连接是否正常。
	C-FIND 查询：
	支持基于患者姓名、检查日期等条件的影像查询。
	C-MOVE 拉取影像：
	从远程服务拉取影像并通过本地 Storage SCP 服务接收。
	C-STORE 接收影像：
	本地 Storage SCP 服务监听指定端口，接收远程服务发送的影像文件。
2.	MODBUS 通信模块
	串口通信：
	支持通过串口与工业设备通信，读取和写入寄存器。
	数据验证：
	在写入寄存器后，读取寄存器值并验证写入是否成功。
	错误处理：
	捕获串口通信中的异常（如超时、端口占用等），并提供友好的错误提示。

## 截图示例
<img width="1981" height="1516" alt="image" src="https://github.com/user-attachments/assets/b67ad6f0-9807-4d98-aa1d-a8dbbaa01c54" />

<img width="1981" height="1090" alt="image" src="https://github.com/user-attachments/assets/94019685-d474-42a3-bf3d-9dfefdf505ba" />


# **CbctHostApp项目——Modbus 协议与硬件设备通信验证WPF程序**

一个基于 WPF 的医学影像系统，主要功能包括通过 Modbus 协议与硬件设备通信、参数下发、图像重建与预览。
项目采用 MVVM 架构，核心逻辑集中在视图模型（MainViewModel）中，支持异步扫描控制和图像展示。

**主要模块与功能：**
1. **Modbus 通信模块**  
   - 通过串口与硬件设备进行数据交互，负责下发扫描参数（如视野、帧率、曝光时间）并启动扫描。
   - 相关接口：`IModbusService`，实现类：`ModbusService`。

2. **图像重建模块**  
   - 异步生成或获取医学影像切片，并在 UI 上实时预览。
   - 相关接口：`IReconstructionService`，实现类：`ReconstructionService`。

3. **参数模型**  
   - `ScanParameters` 类用于封装扫描相关的参数设置，便于数据绑定和传递。

4. **命令与数据绑定**  
   - 使用 `RelayCommand` 实现命令绑定，支持 UI 控件与业务逻辑的解耦。
   - 通过 `ObservableObject` 实现属性通知，保证 UI 实时更新。

5. **异步与取消机制**  
   - 扫描和图像重建过程均采用异步任务，并支持取消操作，提升用户体验和系统响应能力。

**技术栈与特性：**
- C# 12.0，.NET 8，WPF 框架。
- 采用社区 MVVM 工具包（CommunityToolkit.Mvvm）。
- 代码结构清晰，注释完善，便于维护和扩展。

**适用场景：**
本项目适用于需要与医学影像采集硬件交互、实时预览和处理图像的桌面应用场景，具备良好的扩展性和可维护性。





# **DemoHardwareIntegrationApp** 项目

是一个基于 .NET 8 的控制台应用，主要用于演示和实现硬件集成通信，支持串口与 TCP 网络的数据交互。
项目采用 Microsoft.Extensions.Hosting 框架，具备现代后台服务（HostedService）架构，便于扩展和维护。

### 主要功能与结构

1. **串口服务（SerialPortService）**
   - 负责打开和管理串口（如 COM1），异步监听串口数据。
   - 通过事件驱动方式接收数据，并将数据推送到全局 Channel，供其他服务消费。
   - 支持日志记录和异常处理，保证串口通信的稳定性。

2. **TCP 网络服务（TcpNetworkService）**
   - 作为 TCP 客户端连接到指定服务器（如 192.168.1.100:9000）。
   - 实现异步收发数据循环，将串口收到的数据转发到网络，并可处理网络返回的数据。
   - 通过 Channel 实现与串口服务的数据解耦和异步通信。

3. **主程序入口**
   - 使用 Host 构建和启动后台服务，注册串口和网络服务。
   - 配置控制台日志，便于运行时监控和调试。

### 技术特点

- **.NET 8 平台**，利用最新的托管服务和依赖注入特性。
- **事件驱动与异步编程**，提升系统响应能力和并发处理能力。
- **解耦的数据通道（Channel）**，便于服务间数据流转和扩展。
- **高可维护性**，代码结构清晰，易于后续功能拓展（如增加更多硬件协议支持）。

### 适用场景

适用于需要将串口设备与网络服务集成的场景，如工业自动化、设备远程监控、数据采集网关等。
该项目为硬件与网络通信的桥梁，具备良好的可扩展性和工程实践价值。







# SerialModbusDemo项目

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

# MedicalConsoleApp

## 项目简介
一个运行于 Windows 的命令行控制台，用于与 CT、MRI 等医学影像设备通过 DICOM 或 MODBUS 协议进行通信和管理。

MedicalConsoleApp 是一个基于 DICOM 和 MODBUS 协议的医学影像通信与处理系统，
旨在实现医学影像的加载、解析、存储以及与远程设备的通信。
项目支持 DICOM 网络操作（如 C-ECHO、C-FIND、C-MOVE 和 C-STORE）以及 MODBUS 通信协议，
用于工业设备的控制和数据采集。

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

- 
## 快速开始
1. 克隆仓库并进入目录  
   ```bash
   git clone <repo-url>
   cd MedicalConsoleApp
   ```
2. 恢复依赖并编译  
   ```bash
   dotnet restore
   dotnet build
   ```
3. 运行  
   ```bash
   dotnet run -- --protocol dicom --server 127.0.0.1 --port 104
   ```

## 命令行选项
```
--protocol    通信协议 (dicom|modbus)
--server      设备 IP 地址
--port        端口号
--help        查看帮助
```
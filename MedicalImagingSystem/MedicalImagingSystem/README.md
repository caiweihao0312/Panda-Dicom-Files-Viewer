# MedicalImagingSystem

## 项目概述
**MedicalImagingSystem** 是一款基于 .NET 8 平台开发的医学影像文件查看工具。

---

## 功能特性
主要包括基本的DICOM影像文件查看功能 

文件操作：
1、打开文件：打开单个DICOM文件； 
2、打开序列：打开DICOM文件所在文件夹，批量加载DICOM文件； 
3、导出PNG图像：将打开的DICOM文件，导出为PNG格式文件； 
4、清空文件列表：将已加载的文件列表进行清空；

影像调整： 
1、窗宽/窗位调整：启用该功能后，通过鼠标在图像上进行拖动控制； 
2、缩放：启动该功能后，通过鼠标在图像上进行滚轮缩放控制； 
3、旋转：点击旋转按钮，图像每次向右旋转90度； 
4、水平镜像：点击该功能后，图像发生水平镜像调整； 
5、垂直镜像：点击该功能后，图像发生垂直镜像调整；

测量工具： 
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

---

## 安装与运行
### 环境要求
- .NET 8 SDK
- Windows 10 或更高版本

### 安装步骤
1. 克隆项目到本地：
git clone <repository-url> cd MedicalImagingSystem
2. 恢复依赖项：
dotnet restore
3. 构建项目：
dotnet build
4. 运行项目：
dotnet run

---

## 使用说明
1. **启动应用**：运行项目后，主界面将显示影像管理功能。
2. **打开文件**：通过界面上的“打开文件”按钮选择 DICOM 文件进行打开。
3. **查看影像**：点击影像列表中的图像，即可在影像查看窗口中查看详情。


---

## 贡献
欢迎贡献代码或提交问题！请通过以下步骤参与：
1. Fork 本仓库。
2. 创建新分支：
git checkout -b feature/your-feature
3. 提交更改并推送：
git commit -m "Add your message" git push origin feature/your-feature
4. 提交 Pull Request。
## 许可证
本项目基于 MIT 许可证开源，详情请参阅 [LICENSE](LICENSE) 文件。


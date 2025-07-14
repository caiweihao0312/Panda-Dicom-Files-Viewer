using Dicom.Imaging;
using HandyControl.Tools.Extension;
using MedicalImagingSystem.Converters;
using MedicalImagingSystem.Services;
using MedicalImagingSystem.ViewModels;
using MedicalImagingSystem.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MedicalImagingSystem
{
    /// <summary>
    /// 应用程序入口，负责配置依赖注入容器和启动主窗口
    /// </summary>
    public partial class App : Application
    {
        // 全局服务提供者，后续可用于解析服务或ViewModel
        public static IServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        /// 应用程序启动时执行
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            // 在程序启动时注册编码提供器
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            base.OnStartup(e);

            // 检查资源是否已加载
            var converter = Application.Current.Resources["KeyNullToVisibilityConverter"] as NullToVisConverter;
            if (converter == null)
            {
                //throw new InvalidOperationException("NullToVisibilityConverter not found in resources.");
            }

            // 在应用程序启动时，确保设置了正确的图像管理器：
            ImageManager.SetImplementation(WPFImageManager.Instance);

            // 1. 创建服务注册表，配置依赖注入容器
            var services = new ServiceCollection();

            // 2. 注册服务（接口与实现的映射）
            // 注册 DICOM 服务为单例，整个应用只创建一个实例
            services.AddSingleton<IDicomService, DicomService>();

            // 3. 注册 ViewModel
            // 每次请求都会创建新的 ViewModel 实例
            services.AddTransient<MainViewModel>();
            services.AddTransient<DicomImageViewModel>();
            // TODO：这里可以注册其他 ViewModel 或服务

            // 4. 注册主窗口
            // 主窗口依赖 MainViewModel，会自动注入
            services.AddTransient<MainWindow>();
            services.AddTransient<DicomImageView>();

            // 5. 构建服务提供者
            ServiceProvider = services.BuildServiceProvider();

            // 6. 解析主窗口并显示
            // 依赖注入会自动为 MainWindow 注入 MainViewModel
            // 只需通过 App.ServiceProvider.GetRequiredService<MainWindow>() 启动主窗口，所有依赖会自动注入。
            // 若有其他 ViewModel 也需注入服务，均可通过构造函数方式实现
            var mainWindow = App.ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }
}

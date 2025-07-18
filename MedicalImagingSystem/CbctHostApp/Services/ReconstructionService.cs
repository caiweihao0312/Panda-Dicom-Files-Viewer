using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CbctHostApp.Services
{
    /// <summary>
    /// 重建服务实现类，负责生成图像切片（示例为随机灰度图）。
    /// </summary>
    public class ReconstructionService : IReconstructionService
    {
        /// <summary>
        /// 异步生成下一张图像切片，返回 BitmapImage。
        /// 当前实现为生成 256x256 随机灰度图。
        /// </summary>
        /// <param name="token">取消令牌，用于任务取消</param>
        /// <returns>生成的 BitmapImage 对象</returns>
        public Task<BitmapImage> GetNextSliceAsync(CancellationToken token)
        {
            return Task.Run(() =>
            {
                int size = 256; // 图像尺寸
                byte[] pixels = new byte[size * size]; // 灰度像素数据
                new Random().NextBytes(pixels); // 随机填充像素

                // 创建灰度 BitmapSource
                var bitmap = BitmapSource.Create(size, size, 96, 96,
                    System.Windows.Media.PixelFormats.Gray8, null, pixels, size);

                // 使用 PNG 编码器将 BitmapSource 编码为 PNG 格式
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var ms = new MemoryStream();
                encoder.Save(ms);
                ms.Position = 0;

                // 从内存流加载 BitmapImage
                var img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.StreamSource = ms;
                img.EndInit();
                img.Freeze(); // 线程安全

                return img;
            }, token);
        }
    }
}
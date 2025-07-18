using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CbctHostApp.Services
{
    /// <summary>
    /// 图像重建服务接口，定义获取下一张图像切片的方法。
    /// </summary>
    public interface IReconstructionService
    {
        /// <summary>
        /// 异步获取下一张图像切片。
        /// </summary>
        /// <param name="token">取消令牌，用于任务取消</param>
        /// <returns>生成的 BitmapImage 对象</returns>
        Task<BitmapImage> GetNextSliceAsync(CancellationToken token);
    }
}
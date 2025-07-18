using System.Threading.Tasks;
using CbctHostApp.Models;

namespace CbctHostApp.Services
{
    /// <summary>
    /// Modbus 服务接口，定义与 Modbus 设备交互的基本方法。
    /// </summary>
    public interface IModbusService
    {
        /// <summary>
        /// 异步下发扫描参数到 Modbus 设备。
        /// </summary>
        /// <param name="param">扫描参数对象，包括 FOV、帧率、曝光时间等</param>
        /// <returns>表示异步操作的任务</returns>
        Task ConfigureScanAsync(ScanParameters param);
    }
}
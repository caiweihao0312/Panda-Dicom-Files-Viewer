using System.Threading.Tasks;
using CbctHostApp.Models;

namespace CbctHostApp.Services
{
    /// <summary>
    /// Modbus ����ӿڣ������� Modbus �豸�����Ļ���������
    /// </summary>
    public interface IModbusService
    {
        /// <summary>
        /// �첽�·�ɨ������� Modbus �豸��
        /// </summary>
        /// <param name="param">ɨ��������󣬰��� FOV��֡�ʡ��ع�ʱ���</param>
        /// <returns>��ʾ�첽����������</returns>
        Task ConfigureScanAsync(ScanParameters param);
    }
}
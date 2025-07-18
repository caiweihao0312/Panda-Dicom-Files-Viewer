using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CbctHostApp.Services
{
    /// <summary>
    /// ͼ���ؽ�����ӿڣ������ȡ��һ��ͼ����Ƭ�ķ�����
    /// </summary>
    public interface IReconstructionService
    {
        /// <summary>
        /// �첽��ȡ��һ��ͼ����Ƭ��
        /// </summary>
        /// <param name="token">ȡ�����ƣ���������ȡ��</param>
        /// <returns>���ɵ� BitmapImage ����</returns>
        Task<BitmapImage> GetNextSliceAsync(CancellationToken token);
    }
}
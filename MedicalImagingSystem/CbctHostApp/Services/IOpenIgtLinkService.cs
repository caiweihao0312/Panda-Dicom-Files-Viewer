using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CbctHostApp.Services
{
    public interface IOpenIgtLinkService
    {
        Task SendImageAsync(BitmapImage image);
    }
}
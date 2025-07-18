using OpenIGTLink.Net.Client;
using OpenIGTLink.Net.Messages;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CbctHostApp.Services
{
    public class OpenIgtLinkService : IOpenIgtLinkService, IDisposable
    {
        private readonly OpenIgtClient _client;

        public OpenIgtLinkService(string host, int port)
        {
            _client = new OpenIgtClient(host, port);
            _client.Connect();
        }

        public async Task SendImageAsync(BitmapImage image)
        {
            // 将 BitmapImage 转为 raw byte[]
            int width = image.PixelWidth, height = image.PixelHeight;
            int stride = width * 3;
            byte[] data = new byte[height * stride];
            image.CopyPixels(data, stride, 0);

            var imgMsg = new ImageMessage
            {
                Matrix = Matrix4x4.Identity,
                ScalarType = ImageMessage.ScalarTypes.UINT8,
                Dimensions = new int[] { width, height, 1 },
                Spacing = new double[] { 1, 1, 1 },
                Data = data
            };
            await _client.SendAsync(imgMsg);
        }

        public void Dispose() => _client?.Dispose();
    }
}
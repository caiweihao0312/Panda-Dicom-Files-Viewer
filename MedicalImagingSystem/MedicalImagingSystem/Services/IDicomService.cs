using Dicom.Imaging;
using MedicalImagingSystem.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MedicalImagingSystem.Services
{
    public interface IDicomService
    {
        DicomTagInfoDTO LoadDicomImage(string filePath);
        List<BitmapSource> GetMultiframeImages(string filePath);
        ObservableCollection<BitmapSource> LoadDicomSeries(string directoryPath);
        BitmapSource ConvertPixelDataToBitmapSource(byte[] pixelData, int width, int height, double windowWidth, double windowLevel, PixelFormat pixelFormat, bool invert = false);
    }
}

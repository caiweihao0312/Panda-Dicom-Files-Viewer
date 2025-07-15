using FellowOakDicom;
using FellowOakDicom.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MedicalImagingSystem.Helper
{
    public class DicomViewer
    {
        private DicomDataset _dataset;
        private DicomPixelData _pixelData;
        private DicomImage _dicomImage;

        //public void LoadFile(string filePath)
        //{
        //    var dicomFile = DicomFile.Open(filePath);
        //    _dataset = dicomFile.Dataset;

        //    // 处理压缩图像
        //    if (_dataset.InternalTransferSyntax.IsEncapsulated)
        //    {
        //        _dataset = _dataset.Clone(DicomTransferSyntax.ExplicitVRLittleEndian);
        //    }

        //    _pixelData = DicomPixelData.Create(_dataset);
        //    _dicomImage = new DicomImage(_dataset);
        //}

        public PatientInfo GetPatientInfo()
        {
            return new PatientInfo(
                _dataset.GetString(DicomTag.PatientName),
                _dataset.GetString(DicomTag.PatientID),
                _dataset.GetDateTime(DicomTag.PatientBirthDate, DicomTag.PatientBirthTime)
            );
        }

        public ImageParameters GetImageParameters()
        {
            return new ImageParameters(
                _dataset.GetSingleValue<int>(DicomTag.Rows),
                _dataset.GetSingleValue<int>(DicomTag.Columns),
                _dataset.GetValues<double>(DicomTag.PixelSpacing),
                _dataset.GetSingleValue<double>(DicomTag.WindowCenter),
                _dataset.GetSingleValue<double>(DicomTag.WindowWidth)
            );
        }

        //public BitmapSource RenderImage(double windowCenter, double windowWidth)
        //{
        //    _dicomImage.WindowCenter = windowCenter;
        //    _dicomImage.WindowWidth = windowWidth;
        //    return _dicomImage.RenderImage().AsWriteableBitmap();
        //}

        public IList<DicomTagInfo> GetAllTags()
        {
            return _dataset.Select(item => new DicomTagInfo(
                item.Tag.ToString(),
                item.Tag.DictionaryEntry.Name,
                item.ToString()
            )).ToList();
        }
    }

    // 辅助类
    public record PatientInfo(string Name, string Id, DateTime BirthDate);
    public record ImageParameters(int Rows, int Columns, double[] PixelSpacing,
                                  double WindowCenter, double WindowWidth);
    public record DicomTagInfo(string Tag, string Name, string Value);
}

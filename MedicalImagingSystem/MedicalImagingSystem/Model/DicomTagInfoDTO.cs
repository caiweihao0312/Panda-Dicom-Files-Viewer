using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MedicalImagingSystem.Model
{
    /// <summary>
    /// 用于封装 DICOM 文件的元数据和像素数据，便于界面或后续处理使用。
    /// </summary>
    public class DicomTagInfoDTO
    {
        // 患者信息
        public string PatientName;
        public string PatientId;
        public DateTime BirthDate;

        // 检查信息
        public string StudyId;
        public string StudyDate;

        // 设备信息
        public string Modality;
        public string Manufacturer;

        // 图像参数
        public int ImageRows;
        public int ImageColumns;
        public byte[] PixelData;
        public PixelFormat PixelFormat { get; set; }
        //double[] pixelSpacing = dataset.GetValues<double>(DicomTag.PixelSpacing);

        // 窗宽窗位
        public double WindowCenter;
        public double WindowWidth;

        public BitmapSource bitmapSource;
    }
}

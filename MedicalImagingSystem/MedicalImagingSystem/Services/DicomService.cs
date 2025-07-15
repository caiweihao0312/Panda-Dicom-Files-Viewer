using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.Codec;
using HandyControl.Controls;
using MedicalImagingSystem.Helper;
using MedicalImagingSystem.Model;
using Rubyer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace MedicalImagingSystem.Services
{
    /// <summary>
    /// DicomService：实现了 IDicomService 接口，负责 DICOM 医学影像文件的加载、解析、图像处理和元数据提取等功能。
    /// </summary>
    public class DicomService : IDicomService
    {
        /// <summary>
        /// 加载单张 DICOM 文件，读取文件头判断是否为 DICOM 格式。
        /// 用 fo-dicom 库打开 DICOM 文件，获取数据集（DicomDataset）。
        /// 调用 ReadMetadata 读取元数据，GetDicomImage 获取图像。
        /// 自动设置窗宽窗位（SetOptimalWindowing）。
        /// 返回包含所有信息的 DicomTagInfoDTO。
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public DicomTagInfoDTO LoadDicomImage(string filePath)
        {
            try
            {
                // 打开文件并读取文件头
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] preamble = new byte[128];
                    fs.Read(preamble, 0, 128);

                    byte[] dicomPrefix = new byte[4];
                    fs.Read(dicomPrefix, 0, 4);

                    if (dicomPrefix[0] == 0x44 && dicomPrefix[1] == 0x49 && dicomPrefix[2] == 0x43 && dicomPrefix[3] == 0x4D)
                    {
                        Debug.WriteLine("This is a valid DICOM file.");
                    }
                    else
                    {
                        Debug.WriteLine("This is not a valid DICOM file.");
                    }
                }

                // 加载DICOM文件
                var dicomFile = DicomFile.Open(filePath);

                // 如果传输语法是 RLE Lossless，则先做转码
                if (dicomFile.Dataset.InternalTransferSyntax == DicomTransferSyntax.RLELossless)
                {
                    var transcoder = new DicomTranscoder(
                        dicomFile.Dataset.InternalTransferSyntax,
                        DicomTransferSyntax.ExplicitVRLittleEndian);
                    dicomFile = transcoder.Transcode(dicomFile);
                }
                else if (dicomFile.Dataset.InternalTransferSyntax.IsEncapsulated)
                {
                    Debug.WriteLine("检测到压缩传输语法，尝试解码像素数据。");
                    var transcoder = new DicomTranscoder(
                        dicomFile.Dataset.InternalTransferSyntax,
                        DicomTransferSyntax.ExplicitVRLittleEndian);
                    dicomFile = transcoder.Transcode(dicomFile);
                }
                else if (dicomFile.Dataset.InternalTransferSyntax.UID.UID == "1.2.840.10008.1.2.4.100")
                {
                    Debug.WriteLine("检测到 MPEG2 Main Profile / Main Level 传输语法。");
                    throw new NotSupportedException("当前不支持 MPEG2 解码，请使用外部工具或库进行解码。");
                }
                // 获取完整数据集
                DicomDataset dataset = dicomFile.Dataset;

                DicomTagInfoDTO dicomTagInfo = ReadMetadata(dataset);
                //SetOptimalWindowing(dicomTagInfo);
                dicomTagInfo.bitmapSource = GetDicomImage(dicomTagInfo,dataset);
                return dicomTagInfo;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"无法加载 DICOM 文件：{ex.ToString()}");
                //System.Windows.MessageBox.Show("无法加载 DICOM 文件。请检查文件路径或格式是否正确。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return new DicomTagInfoDTO { PatientName = "[无描述异常]" };
            }
        }

        /// <summary>
        /// 读取患者、检查、设备、图像参数等常用 DICOM 标签。
        /// 判断像素数据（PixelData）是否存在，并自动识别像素格式（灰度8/16位或RGB）。
        /// 遍历并输出所有 DICOM 标签及其值，便于调试。
        /// </summary>
        /// <param name="dataset"></param>
        /// <returns></returns>
        public DicomTagInfoDTO ReadMetadata(DicomDataset dataset)
        {
            DicomTagInfoDTO dicomTagInfo = new DicomTagInfoDTO();
            try
            {
                // 读取患者信息
                dicomTagInfo.PatientName = dataset.Contains(DicomTag.PatientName) ? dataset.GetSingleValueOrDefault<string>(DicomTag.PatientName, "[无描述]") : "[无描述]";
                dicomTagInfo.PatientId = dataset.Contains(DicomTag.PatientID) ? dataset.GetString(DicomTag.PatientID) : string.Empty;
                dicomTagInfo.BirthDate = dataset.Contains(DicomTag.PatientBirthDate) ? dataset.GetDateTime(DicomTag.PatientBirthDate, DicomTag.PatientBirthTime) : DateTime.MinValue;
                // 读取检查信息
                dicomTagInfo.StudyId = dataset.Contains(DicomTag.StudyID) ? dataset.GetString(DicomTag.StudyID) : string.Empty;
                dicomTagInfo.StudyDate = dataset.Contains(DicomTag.StudyDate) ? dataset.GetString(DicomTag.StudyDate) : string.Empty;
                // 读取设备信息
                dicomTagInfo.Modality = dataset.Contains(DicomTag.Modality) ? dataset.GetString(DicomTag.Modality) : string.Empty;
                dicomTagInfo.Manufacturer = dataset.Contains(DicomTag.Manufacturer) ? dataset.GetString(DicomTag.Manufacturer) : "[无描述]";
                // 读取图像参数 获取影像宽度和高度
                dicomTagInfo.ImageRows = dataset.Contains(DicomTag.Rows) ? dataset.GetSingleValueOrDefault<int>(DicomTag.Rows, 0) : 0;
                dicomTagInfo.ImageColumns = dataset.Contains(DicomTag.Columns) ? dataset.GetSingleValueOrDefault<int>(DicomTag.Columns, 0) : 0;
                // 获取像素数据
                dicomTagInfo.PixelData = dataset.Contains(DicomTag.PixelData) ? dataset.GetValues<byte>(DicomTag.PixelData) : Array.Empty<byte>();
                //dicomTagInfo.PixelSpacing = dataset.GetValues<double>(DicomTag.PixelSpacing);
                // 读取窗宽窗位
                dicomTagInfo.WindowCenter = dataset.Contains(DicomTag.WindowCenter)
                    ? dataset.GetSingleValue<double>(DicomTag.WindowCenter) : 0;
                dicomTagInfo.WindowWidth = dataset.Contains(DicomTag.WindowWidth)
                    ? dataset.GetSingleValue<double>(DicomTag.WindowWidth) : 0;

                // 判断PixelData是否存在
                if (dataset.Contains(DicomTag.PixelData))
                {
                    // 获取像素数据
                    dicomTagInfo.PixelData = dataset.Contains(DicomTag.PixelData) ? dataset.GetValues<byte>(DicomTag.PixelData) : Array.Empty<byte>();
                    // 自动识别像素格式
                    var pixelData = DicomPixelData.Create(dataset);
                    int samplesPerPixel = pixelData.SamplesPerPixel;
                    int bitsAllocated = pixelData.BitsAllocated;
                    dicomTagInfo.PixelFormat = samplesPerPixel switch
                    {
                        1 when bitsAllocated <= 8 => PixelFormats.Gray8,
                        1 when bitsAllocated <= 16 => PixelFormats.Gray16,
                        3 => PixelFormats.Rgb24,
                        _ => PixelFormats.Gray8
                    };
                }
                else
                {
                    dicomTagInfo.PixelData = Array.Empty<byte>();
                    dicomTagInfo.PixelFormat = PixelFormats.Gray8; // 或其它默认值
                    Debug.WriteLine("警告：DICOM文件不包含像素数据（Pixel Data）。");
                }

                // 读取所有标签信息
                Debug.WriteLine("=== DICOM 数据集完整元数据 ===");
                foreach (var item in dataset)
                {
                    // 获取标签和描述名称
                    string tag = item.Tag.ToString();
                    string name = item.Tag.DictionaryEntry.Name;

                    // 获取值
                    string value = item.ToString();

                    Debug.WriteLine($"{tag} | {name}: {value}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"错误：读取 DICOM 元数据失败 - {ex.Message}");
            }

            return dicomTagInfo;
        }

        /// <summary>
        /// 若无像素数据，返回一张黑色默认图像。
        /// 有像素数据时，使用 fo-dicom 的 DicomImage 渲染为 WPF 可用的 BitmapSource
        /// </summary>
        /// <param name="dataset"></param>
        /// <returns></returns>
        public BitmapSource GetDicomImage(DicomTagInfoDTO dicomTagInfo,DicomDataset dataset)
        {
            if (!dataset.Contains(DicomTag.PixelData))
            {
                Debug.WriteLine("警告：DICOM文件不包含像素数据（Pixel Data），无法显示图像。");
                // 返回一个黑色的默认图像（256x256，8位灰度）
                int widthTemp = 256;
                int heightTemp = 256;
                byte[] blackPixels = new byte[widthTemp * heightTemp]; // 默认全0即黑色
                var blackBitmap = BitmapSource.Create(
                    widthTemp, heightTemp,
                    96, 96,
                    PixelFormats.Gray8,
                    null,
                    blackPixels,
                    widthTemp // stride
                );
                blackBitmap.Freeze();
                return blackBitmap;
            }
            // 创建DicomImage对象进行渲染
            var dicomImage = new DicomImage(dataset);
            // 获取原始像素数据
            var pixelDataInfo = DicomPixelData.Create(dataset);
            int width = pixelDataInfo.Width;
            int height = pixelDataInfo.Height;
            int bitsAllocated = pixelDataInfo.BitsAllocated;
            int samplesPerPixel = pixelDataInfo.SamplesPerPixel;
            //int frames = pixelDataInfo.NumberOfFrames;
            // 获取第一帧像素数据
            var pixelData = pixelDataInfo.GetFrame(0).Data;

            // 验证像素数据长度
            int expectedLength = width * height * (bitsAllocated / 8) * samplesPerPixel;
            if (pixelData.Length < expectedLength)
            {
                Debug.WriteLine($"像素数据长度不足。期望长度: {expectedLength}, 实际长度: {pixelData.Length}");
                // 检查是否为压缩数据或其他特殊情况
                if (dataset.InternalTransferSyntax.IsEncapsulated)
                {
                    Debug.WriteLine("检测到压缩传输语法，可能需要解码。");
                }
                else
                {
                    Debug.WriteLine("像素数据可能不完整，尝试填充或降级显示。");
                }
                // 填充缺失部分
                byte defaultValue = (byte)(bitsAllocated == 8 ? 128 : 0); // 8位图像用中性灰色，16位图像用0
                byte[] paddedPixelData = new byte[expectedLength];
                Array.Copy(pixelData, paddedPixelData, pixelData.Length);
                for (int i = pixelData.Length; i < expectedLength; i++)
                {
                    paddedPixelData[i] = defaultValue;
                }
                pixelData = paddedPixelData;
            }
            // 验证填充后的数据
            if (pixelData.Length != expectedLength)
            {
                Debug.WriteLine("填充后的像素数据长度不正确，返回默认黑色图像。");
                int widthTemp = 256;
                int heightTemp = 256;
                byte[] blackPixels = new byte[widthTemp * heightTemp];
                return BitmapSource.Create(
                    widthTemp, heightTemp,
                    96, 96,
                    PixelFormats.Gray8,
                    null,
                    blackPixels,
                    widthTemp);
            }
            return ConvertPixelDataToBitmapSource(pixelData, width, height, dicomTagInfo.WindowWidth, dicomTagInfo.WindowCenter, dicomTagInfo.PixelFormat, true);
            ///return ConvertPixelDataToBitmapSource(pixelData, width, height, dicomTagInfo.WindowWidth, dicomTagInfo.WindowCenter, pixelDataInfo.BitsAllocated, pixelDataInfo.SamplesPerPixel);
            // 修复后的代码：
            //var renderedImage = dicomImage.RenderImage();
            //Debug.WriteLine($"Rendered image type: {renderedImage.GetType()}");
            //if (renderedImage is IImage image)
            //{
            //    // 获取图像的宽度和高度
            //    int width = dicomImage.Width;
            //    int height = dicomImage.Height;

            //    // 获取像素数据
            //    var pixelData = image.Pixels.Data; // 从 PinnedIntArray 中提取实际的像素数组

            //    // 动态获取像素格式
            //    var pixelDataInfo = DicomPixelData.Create(dataset);
            //    PixelFormat pixelFormat = pixelDataInfo.SamplesPerPixel switch
            //    {
            //        1 when pixelDataInfo.BitsAllocated <= 8 => PixelFormats.Gray8,
            //        1 when pixelDataInfo.BitsAllocated <= 16 => PixelFormats.Gray16,
            //        3 => PixelFormats.Bgr24,
            //        _ => throw new NotSupportedException("不支持的像素格式")
            //    };

            //    // 计算 stride
            //    int stride = (width * pixelFormat.BitsPerPixel + 7) / 8;

            //    // 创建 WriteableBitmap
            //    var writeableBitmap = new WriteableBitmap(width, height, 96, 96, pixelFormat, null);
            //    writeableBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixelData, stride, 0);
            //    return writeableBitmap;
            //}
            //else
            //{
            //    throw new InvalidOperationException("Rendered image is not compatible with WriteableBitmap.");
            //}

            // 不使用 LUT
            //dicomImage.UseVOILUT = false;
            //bool isFrames = dataset.Contains(DicomTag.NumberOfFrames);
            //// 获取原始像素数据
            //var pixelData = DicomPixelData.Create(dataset);
            //var pixelBytes = pixelData.GetFrame(0).Data;  // 第一帧
            //// 获取像素格式参数
            //int bitsAllocated = pixelData.BitsAllocated;
            //int bitsStored = pixelData.BitsStored;
            //int samplesPerPixel = pixelData.SamplesPerPixel;
            //bool isSigned = pixelData.PixelRepresentation == PixelRepresentation.Signed;
            //// 转换为WPF位图
            //PixelFormat pixelFormat = samplesPerPixel switch
            //{
            //    1 when bitsAllocated <= 8 => PixelFormats.Gray8,
            //    1 when bitsAllocated <= 16 => PixelFormats.Gray16,
            //    3 => PixelFormats.Rgb24,
            //    _ => throw new NotSupportedException("不支持的像素格式")
            //};
            //int width = pixelData.Width;
            //int height = pixelData.Height;
            //int stride = width * (bitsAllocated / 8) * samplesPerPixel;
            //var bitmap = BitmapSource.Create(
            //    width, height,
            //    96, 96,
            //    pixelFormat,
            //    null,
            //    pixelBytes,
            //    stride);
            //bitmap.Freeze();  // 跨线程安全
            //return bitmap;
        }

        /// <summary>
        /// 加载多帧 DICOM 文件（如CT/MR序列），逐帧提取像素并生成 BitmapSource 列表。
        /// 	若无多帧，则退化为单帧处理。
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public List<BitmapSource> GetMultiframeImages(DicomDataset dataset)
        {
            var frames = new List<BitmapSource>();
            if (dataset.Contains(DicomTag.NumberOfFrames))
            {
                // 获取帧数
                int frameCount = dataset.GetSingleValue<int>(DicomTag.NumberOfFrames);

                // 获取像素数据
                var pixelData = DicomPixelData.Create(dataset);

                for (int frame = 0; frame < frameCount; frame++)
                {
                    // 获取帧特定元数据
                    var frameDataset = dataset.GetSequence(DicomTag.PerFrameFunctionalGroupsSequence).Items[frame];
                    double[] imagePosition = frameDataset.GetValues<double>(DicomTag.ImagePositionPatient);

                    // 获取当前帧像素数据
                    var frameBytes = pixelData.GetFrame(frame).Data;

                    // 创建位图
                    var bitmap = CreateBitmapFromPixelData(
                        pixelData.Width,
                        pixelData.Height,
                        pixelData.BitsAllocated,
                        pixelData.SamplesPerPixel,
                        frameBytes);

                    frames.Add(bitmap);
                }
            }
            //else
            //{
            //    BitmapSource bitmapSource = GetDicomImage(dataset);
            //    frames.Add(bitmapSource);
            //}
            return frames;
        }

        /// <summary>
        /// 加载 DICOM 序列（多文件）
        /// 加载一个目录下的所有 DICOM 文件，批量渲染为图像序列（如一组切片）。
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <returns></returns>
        public ObservableCollection<BitmapSource> LoadDicomSeries(string directoryPath)
        {
            var images = new ObservableCollection<BitmapSource>();
            foreach (var file in Directory.GetFiles(directoryPath, "*.dcm"))
            {
                var dicomImage = new DicomImage(file);
                // 修复后的代码：
                var renderedImage = dicomImage.RenderImage();
                if (renderedImage is IImage image)
                {
                    images.Add(image.As<WriteableBitmap>());
                    //return image.As<WriteableBitmap>();
                }
                else
                {
                    throw new InvalidOperationException("Rendered image is not compatible with WriteableBitmap.");
                }
            }
            return images;
        }

        /// <summary>
        /// 支持不同像素格式（Gray8、Gray16、Rgb24）的窗宽窗位处理和灰度反转。
        /// 	适用于自定义像素数据的显示。
        /// Gray8：直接对每个像素做窗宽窗位线性拉伸，输出8位灰度。
        /// Gray16：将16位像素（ushort）窗宽窗位拉伸后映射到8位灰度，便于WPF显示。
        /// Rgb24：先转灰度做窗宽窗位，再映射回RGB（伪灰度），医学影像常用。
        /// </summary>
        public BitmapSource ConvertPixelDataToBitmapSource(byte[] pixelData, int width, int height, double windowWidth, double windowLevel, PixelFormat pixelFormat, bool invert = false)
        {
            //return ProcessDicomImage(pixelData, width, height, windowWidth, windowLevel, pixelFormat, invert);

            if (pixelFormat == PixelFormats.Gray8)
            {
                // 灰度图像窗宽窗位处理
                double min = windowLevel - windowWidth / 2.0;
                double max = windowLevel + windowWidth / 2.0;
                byte[] output = new byte[pixelData.Length];
                for (int i = 0; i < pixelData.Length; i++)
                {
                    double value = pixelData[i];
                    byte mapped;
                    if (value <= min)
                        mapped = 0;
                    else if (value > max)
                        mapped = 255;
                    else
                        mapped = (byte)(((value - min) / windowWidth) * 255.0);

                    // 灰度反转
                    output[i] = invert ? (byte)(255 - mapped) : mapped;
                }
                int stride = width;
                var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, output, stride);
                bitmap.Freeze();
                return bitmap;
            }
            else if (pixelFormat == PixelFormats.Gray16)
            {
                // 16位灰度图像窗宽窗位处理
                int pixelCount = width * height;
                byte[] output = new byte[pixelCount];

                // 设置默认窗宽窗位
                if (windowWidth <= 0 || windowLevel <= 0)
                {
                    ushort minPixelValue = ushort.MaxValue;
                    ushort maxPixelValue = ushort.MinValue;

                    for (int i = 0; i < pixelData.Length; i += 2)
                    {
                        ushort value = (ushort)(pixelData[i] | (pixelData[i + 1] << 8));
                        if (value < minPixelValue) minPixelValue = value;
                        if (value > maxPixelValue) maxPixelValue = value;
                    }

                    windowLevel = (minPixelValue + maxPixelValue) / 2.0;
                    windowWidth = maxPixelValue - minPixelValue;
                }
                double min = windowLevel - windowWidth / 2.0;
                double max = windowLevel + windowWidth / 2.0;

                if (min < 0) min = 0;
                if (max > ushort.MaxValue) max = ushort.MaxValue;

                // 映射像素值
                for (int i = 0; i < pixelCount; i++)
                {
                    ushort value = (ushort)(pixelData[i * 2] | (pixelData[i * 2 + 1] << 8));
                    double mapped;

                    if (value <= min)
                        mapped = 0;
                    else if (value > max)
                        mapped = 255;
                    else
                        mapped = ((value - min) / windowWidth) * 255.0;

                    output[i] = invert ? (byte)(255 - Math.Clamp(mapped, 0, 255)) : (byte)Math.Clamp(mapped, 0, 255);
                }
                int stride = width;
                var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, output, stride);
                bitmap.Freeze();
                return bitmap;
            }
            else if (pixelFormat == PixelFormats.Rgb24)
            {
                // 彩色图像：通常不做窗宽窗位处理，直接显示
                //int stride = width * 3;
                //var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Rgb24, null, pixelData, stride);
                //bitmap.Freeze();
                //return bitmap;

                // 对RGB图像，先转灰度做窗宽窗位，再映射回RGB（伪灰度或LUT）
                int expectedLength = width * height * 3;
                if (pixelData.Length < expectedLength)
                {
                    System.Windows.MessageBox.Show("像素数据长度不足，无法进行RGB窗宽窗位处理。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }

                byte[] output = new byte[expectedLength];
                double min = windowLevel - windowWidth / 2.0;
                double max = windowLevel + windowWidth / 2.0;
                for (int i = 0; i < expectedLength; i += 3)
                {
                    double gray = 0.299 * pixelData[i] + 0.587 * pixelData[i + 1] + 0.114 * pixelData[i + 2];
                    double mapped = gray <= min ? 0 : gray > max ? 255 : ((gray - min) / windowWidth) * 255.0;
                    //byte mappedByte = (byte)Math.Clamp(mapped, 0, 255);
                    //output[i] = output[i + 1] = output[i + 2] = mappedByte;
                    // 默认灰度反转
                    byte mappedByte = (byte)(255 - Math.Clamp(mapped, 0, 255));
                    output[i] = output[i + 1] = output[i + 2] = mappedByte;

                }
                int stride = width * 3;
                var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Rgb24, null, output, stride);
                bitmap.Freeze();
                return bitmap;

                // 对RGB图像，先转灰度做窗宽窗位，再映射回RGB（伪灰度）
                //int expectedLength = width * height * 3;
                //if (pixelData.Length < expectedLength)
                //    throw new ArgumentException("像素数据长度不足，无法进行RGB窗宽窗位处理。");

                //byte[] output = new byte[expectedLength];
                //double min = windowLevel - windowWidth / 2.0;
                //double max = windowLevel + windowWidth / 2.0;
                //byte[,] jetLUT = GenerateJetLUT();

                //for (int i = 0; i < expectedLength; i += 3)
                //{
                //    // 1. 计算灰度
                //    double gray = 0.299 * pixelData[i] + 0.587 * pixelData[i + 1] + 0.114 * pixelData[i + 2];
                //    double mapped = gray <= min ? 0 : gray > max ? 255 : ((gray - min) / windowWidth) * 255.0;
                //    int lutIndex = (int)Math.Clamp(mapped, 0, 255);

                //    // 2. LUT查表映射
                //    output[i] = jetLUT[lutIndex, 0]; // R
                //    output[i + 1] = jetLUT[lutIndex, 1]; // G
                //    output[i + 2] = jetLUT[lutIndex, 2]; // B
                //}
                //int stride2 = width * 3;
                //var bitmap2 = BitmapSource.Create(width, height, 96, 96, PixelFormats.Rgb24, null, output, stride2);
                //bitmap2.Freeze();
                //return bitmap2;
            }
            else if (pixelFormat == PixelFormats.Bgr24)
            {
                int stride = width * 3; // 每行字节数
                var bitmap = BitmapSource.Create(
                    width, height,
                    96, 96, // DPI
                    PixelFormats.Bgr24,
                    null, // 调色板（Bgr24 不需要调色板）
                    pixelData,
                    stride);
                bitmap.Freeze(); // 冻结以提高跨线程安全性
                return bitmap;
            }
            else
            {
                System.Windows.MessageBox.Show("不支持的像素格式：" + pixelFormat.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }


        public BitmapSource ProcessDicomImage(byte[] pixelData, int width, int height, double windowWidth, double windowCenter, PixelFormat pixelFormat, bool invert = false)
        {
            if (pixelFormat == PixelFormats.Gray8)
            {
                // 处理 Gray8 图像
                return ProcessGray8(pixelData, width, height, windowWidth, windowCenter, invert);
            }
            else if (pixelFormat == PixelFormats.Gray16)
            {
                // 处理 Gray16 图像
                return ProcessGray16(pixelData, width, height, windowWidth, windowCenter, invert);
            }
            else if (pixelFormat == PixelFormats.Rgb24 || pixelFormat == PixelFormats.Bgr24)
            {
                // 处理 Rgb24 或 Bgr24 图像
                return ProcessRgb24(pixelData, width, height, pixelFormat);
            }
            else
            {
                throw new NotSupportedException($"不支持的像素格式: {pixelFormat}");
            }
        }

        // 处理 Gray8 图像
        private BitmapSource ProcessGray8(byte[] pixelData, int width, int height, double windowWidth, double windowCenter, bool invert)
        {
            double min = windowCenter - windowWidth / 2.0;
            double max = windowCenter + windowWidth / 2.0;

            byte[] output = new byte[pixelData.Length];
            for (int i = 0; i < pixelData.Length; i++)
            {
                double value = pixelData[i];
                byte mapped = value <= min ? (byte)0 : value > max ? (byte)255 : (byte)(((value - min) / windowWidth) * 255.0);
                output[i] = invert ? (byte)(255 - mapped) : mapped;
            }

            int stride = width;
            return BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, output, stride);
        }

        // 处理 Gray16 图像
        private BitmapSource ProcessGray16(byte[] pixelData, int width, int height, double windowWidth, double windowCenter, bool invert)
        {
            int pixelCount = width * height;
            byte[] output = new byte[pixelCount];

            double min = windowCenter - windowWidth / 2.0;
            double max = windowCenter + windowWidth / 2.0;

            for (int i = 0; i < pixelCount; i++)
            {
                ushort value = (ushort)(pixelData[i * 2] | (pixelData[i * 2 + 1] << 8));
                double mapped = value <= min ? 0 : value > max ? 255 : ((value - min) / windowWidth) * 255.0;
                output[i] = invert ? (byte)(255 - Math.Clamp(mapped, 0, 255)) : (byte)Math.Clamp(mapped, 0, 255);
            }

            int stride = width;
            return BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, output, stride);
        }

        // 处理 Rgb24 或 Bgr24 图像
        private BitmapSource ProcessRgb24(byte[] pixelData, int width, int height, PixelFormat pixelFormat)
        {
            int stride = width * 3; // 每行字节数
            return BitmapSource.Create(width, height, 96, 96, pixelFormat, null, pixelData, stride);
        }

        public BitmapSource ConvertPixelDataToBitmapSource(byte[] pixelData, int width, int height, double windowWidth, double windowLevel, int bitsAllocated, int samplesPerPixel)
        {
            PixelFormat pixelFormat = samplesPerPixel switch
            {
                1 when bitsAllocated <= 8 => PixelFormats.Gray8,
                1 when bitsAllocated <= 16 => PixelFormats.Gray16,
                3 => PixelFormats.Bgr24,
                _ => throw new NotSupportedException("不支持的像素格式")
            };

            if (pixelFormat == PixelFormats.Gray8)
            {
                int stride = width;
                var bitmap = BitmapSource.Create(
                    width, height,
                    96, 96,
                    PixelFormats.Gray8,
                    null,
                    pixelData,
                    stride);
                bitmap.Freeze();
                return bitmap;
            }
            else if (pixelFormat == PixelFormats.Gray16)
            {
                // 16位灰度图像窗宽窗位处理
                int pixelCount = width * height;
                byte[] output = new byte[pixelCount];

                // 设置默认窗宽窗位
                if (windowWidth <= 0 || windowLevel <= 0)
                {
                    ushort minPixelValue = ushort.MaxValue;
                    ushort maxPixelValue = ushort.MinValue;

                    for (int i = 0; i < pixelData.Length; i += 2)
                    {
                        ushort value = (ushort)(pixelData[i] | (pixelData[i + 1] << 8));
                        if (value < minPixelValue) minPixelValue = value;
                        if (value > maxPixelValue) maxPixelValue = value;
                    }

                    windowLevel = (minPixelValue + maxPixelValue) / 2.0;
                    windowWidth = maxPixelValue - minPixelValue;
                }

                double min = windowLevel - windowWidth / 2.0;
                double max = windowLevel + windowWidth / 2.0;

                if (min < 0) min = 0;
                if (max > ushort.MaxValue) max = ushort.MaxValue;

                // 映射像素值
                for (int i = 0; i < pixelCount; i++)
                {
                    ushort value = (ushort)(pixelData[i * 2] | (pixelData[i * 2 + 1] << 8));
                    double mapped;

                    if (value <= min)
                        mapped = 0;
                    else if (value > max)
                        mapped = 255;
                    else
                        mapped = ((value - min) / windowWidth) * 255.0;

                    output[i] = (byte)(255 - Math.Clamp(mapped, 0, 255));
                }

                int stride = width;
                var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, output, stride);
                bitmap.Freeze();
                return bitmap;
            }
            else if (pixelFormat == PixelFormats.Bgr24)
            {
                int stride = width * 3;
                var bitmap = BitmapSource.Create(
                    width, height,
                    96, 96,
                    PixelFormats.Bgr24,
                    null,
                    pixelData,
                    stride);
                bitmap.Freeze();
                return bitmap;
            }
            else
            {
                throw new NotSupportedException("不支持的像素格式");
            }
        }
        /// <summary>
        /// 若窗宽窗位未设置，则根据像素数据自动计算最佳窗宽窗位。
        /// </summary>
        /// <param name="dto"></param>
        private void SetOptimalWindowing(DicomTagInfoDTO dto)
        {
            if (dto.WindowWidth <= 0 || dto.WindowCenter <= 0)
            {
                // 以byte为例，灰度图
                var pixelData = dto.PixelData;
                ushort[] histogram = new ushort[ushort.MaxValue + 1];
                int totalPixels = pixelData.Length / 2;

                // 计算直方图
                for (int i = 0; i < pixelData.Length; i += 2)
                {
                    ushort value = (ushort)(pixelData[i] | (pixelData[i + 1] << 8));
                    histogram[value]++;
                }

                // 计算累积直方图
                int[] cumulativeHistogram = new int[histogram.Length];
                cumulativeHistogram[0] = histogram[0];
                for (int i = 1; i < histogram.Length; i++)
                {
                    cumulativeHistogram[i] = cumulativeHistogram[i - 1] + histogram[i];
                }

                // 确定有效像素值范围（排除 2% 和 98% 的像素值）
                int lowerBoundIndex = (int)(totalPixels * 0.05); // 2% 的像素值
                int upperBoundIndex = (int)(totalPixels * 0.95); // 98% 的像素值

                ushort minPixelValue = 0;
                ushort maxPixelValue = 0;

                for (int i = 0; i < cumulativeHistogram.Length; i++)
                {
                    if (cumulativeHistogram[i] >= lowerBoundIndex)
                    {
                        minPixelValue = (ushort)i;
                        break;
                    }
                }

                for (int i = cumulativeHistogram.Length - 1; i >= 0; i--)
                {
                    if (cumulativeHistogram[i] <= upperBoundIndex)
                    {
                        maxPixelValue = (ushort)i;
                        break;
                    }
                }

                // 计算窗宽和窗位
                dto.WindowCenter = (minPixelValue + maxPixelValue) / 2.0;
                dto.WindowWidth = maxPixelValue - minPixelValue ;

            }
        }

        #region 未使用方法

        /// <summary>
        /// 递归构建 DICOM 数据集的树形结构，便于在界面上展示所有标签和序列。
        /// </summary>
        /// <param name="dataset">DICOM 数据集</param>
        /// <returns>根节点 TreeViewItem</returns>
        private TreeViewItem BuildDicomTree(DicomDataset dataset)
        {
            var rootNode = new TreeViewItem { Header = "DICOM 数据集" };

            // 按模块分组标签
            var moduleGroups = dataset.GroupBy(item => item.Tag.Group);

            foreach (var group in moduleGroups)
            {
                var groupNode = new TreeViewItem
                {
                    Header = $"Group: {group.Key:X4}"
                };

                foreach (DicomItem item in group)
                {
                    var itemNode = new TreeViewItem();

                    // 序列处理
                    if (item is DicomSequence sequence)
                    {
                        itemNode.Header = $"{item.Tag} | {item.Tag.DictionaryEntry.Name}";

                        foreach (var sequenceItem in sequence.Items)
                        {
                            var sequenceItemNode = BuildDicomTree(sequenceItem);
                            itemNode.Items.Add(sequenceItemNode);
                        }
                    }
                    // 普通元素
                    else
                    {
                        string value = item switch
                        {
                            DicomElement element => string.Join("\\", element.Get<string[]>()),
                            _ => item.ToString()
                        };

                        itemNode.Header = $"{item.Tag} | {item.Tag.DictionaryEntry.Name}: {value}";
                    }

                    groupNode.Items.Add(itemNode);
                }

                rootNode.Items.Add(groupNode);
            }

            return rootNode;
        }

        /// <summary>
        /// 生成 Jet 伪彩色查找表（LUT），用于灰度到彩色的映射。
        /// </summary>
        /// <returns>256x3 的字节数组，每行为一个颜色（R,G,B）</returns>
        private static byte[,] GenerateJetLUT()
        {
            byte[,] lut = new byte[256, 3];
            for (int i = 0; i < 256; i++)
            {
                double value = i / 255.0;
                double r = Math.Clamp(1.5 - Math.Abs(4.0 * value - 3.0), 0, 1);
                double g = Math.Clamp(1.5 - Math.Abs(4.0 * value - 2.0), 0, 1);
                double b = Math.Clamp(1.5 - Math.Abs(4.0 * value - 1.0), 0, 1);
                lut[i, 0] = (byte)(r * 255);
                lut[i, 1] = (byte)(g * 255);
                lut[i, 2] = (byte)(b * 255);
            }
            return lut;
        }

        /// <summary>
        /// 递归解析嵌套的 DICOM 序列（Sequence），输出所有层级的标签信息。
        /// </summary>
        /// <param name="dataset">DICOM 数据集</param>
        private void ParseNestedSequences(DicomDataset dataset)
        {
            foreach (var element in dataset)
            {
                if (element is DicomSequence sequence)
                {
                    Debug.WriteLine($"发现序列: {sequence.Tag.DictionaryEntry.Name}");

                    // 递归处理嵌套序列
                    foreach (var item in sequence.Items)
                    {
                        ParseNestedSequences(item); // 递归调用
                    }
                }
                else
                {
                    // 处理普通元素
                    Debug.WriteLine($"{element.Tag} | {element.ValueRepresentation.Code}: {element}");
                }
            }
        }

        /// <summary>
        /// 解析指定标签的 DICOM 序列，输出序列内所有元素信息。
        /// </summary>
        /// <param name="dataset">DICOM 数据集</param>
        /// <param name="sequenceTag">序列标签</param>
        private void ParseDicomSequence(DicomDataset dataset, DicomTag sequenceTag)
        {
            // 检查序列是否存在
            if (!dataset.Contains(sequenceTag))
            {
                Console.WriteLine($"序列 {sequenceTag} 不存在");
                return;
            }

            // 获取序列对象
            var sequence = dataset.GetSequence(sequenceTag);
            Console.WriteLine($"=== 序列: {sequenceTag.DictionaryEntry.Name} ===");
            Console.WriteLine($"包含 {sequence.Items.Count} 个项目");

            // 遍历所有项目
            for (int i = 0; i < sequence.Items.Count; i++)
            {
                Console.WriteLine($"\n项目 #{i + 1}");
                var itemDataset = sequence.Items[i];

                // 遍历项目中的所有元素
                foreach (var element in itemDataset)
                {
                    // 跳过序列起始/结束标记
                    if (element.Tag.IsPrivate || element.Tag.Group == 0xFFFE)
                        continue;

                    string value = element switch
                    {
                        DicomElement de => de.Get<string>(),
                        DicomSequence sq => $"[嵌套序列: {sq.Items.Count} 项]",
                        _ => element.ToString()
                    };

                    Console.WriteLine($"{element.Tag} | {element.Tag.DictionaryEntry.Name}: {value}");
                }
            }
        }

        /// <summary>
        /// 创建 BitmapSource 对象，根据像素数据和格式生成 WPF 可用的位图。
        /// </summary>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="bitsAllocated">每像素分配的位数</param>
        /// <param name="samplesPerPixel">每像素采样数（灰度为1，RGB为3）</param>
        /// <param name="pixelBytes">像素数据字节数组</param>
        /// <returns>生成的 BitmapSource 对象</returns>
        private BitmapSource CreateBitmapFromPixelData(int width, int height, ushort bitsAllocated, ushort samplesPerPixel, byte[] pixelBytes)
        {
            // 获取像素格式参数
            PixelFormat pixelFormat = samplesPerPixel switch
            {
                1 when bitsAllocated <= 8 => PixelFormats.Gray8,
                1 when bitsAllocated <= 16 => PixelFormats.Gray16,
                3 => PixelFormats.Rgb24,
                _ => throw new NotSupportedException("不支持的像素格式")
            };

            int stride = width * (bitsAllocated / 8) * samplesPerPixel;

            var bitmap = BitmapSource.Create(
                width, height,
                96, 96,
                pixelFormat,
                null,
                pixelBytes,
                stride);

            bitmap.Freeze(); // 跨线程安全

            return bitmap;
        }

        #endregion
    }
}

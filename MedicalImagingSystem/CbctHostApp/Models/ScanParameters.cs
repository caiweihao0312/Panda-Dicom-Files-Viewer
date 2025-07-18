namespace CbctHostApp.Models
{
    /// <summary>
    /// 扫描参数类，包含扫描过程中的关键设置项。
    /// </summary>
    public class ScanParameters
    {
        /// <summary>
        /// 视野（Field of View），单位为毫米，默认值为 200.0。
        /// </summary>
        public double Fov { get; set; } = 200.0;

        /// <summary>
        /// 帧率（Frame Rate），单位为帧/秒，默认值为 30.0。
        /// </summary>
        public double FrameRate { get; set; } = 30.0;

        /// <summary>
        /// 曝光时间（Exposure Time），单位为毫秒，默认值为 10.0。
        /// </summary>
        public double ExposureTime { get; set; } = 10.0;
    }
}
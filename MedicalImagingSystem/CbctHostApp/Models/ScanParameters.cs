namespace CbctHostApp.Models
{
    /// <summary>
    /// ɨ������࣬����ɨ������еĹؼ������
    /// </summary>
    public class ScanParameters
    {
        /// <summary>
        /// ��Ұ��Field of View������λΪ���ף�Ĭ��ֵΪ 200.0��
        /// </summary>
        public double Fov { get; set; } = 200.0;

        /// <summary>
        /// ֡�ʣ�Frame Rate������λΪ֡/�룬Ĭ��ֵΪ 30.0��
        /// </summary>
        public double FrameRate { get; set; } = 30.0;

        /// <summary>
        /// �ع�ʱ�䣨Exposure Time������λΪ���룬Ĭ��ֵΪ 10.0��
        /// </summary>
        public double ExposureTime { get; set; } = 10.0;
    }
}
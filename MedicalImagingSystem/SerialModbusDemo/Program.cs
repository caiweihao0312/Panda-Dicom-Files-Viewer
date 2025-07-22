using System;
using System.Windows.Forms;

namespace SerialModbusDemo
{
    static class Program
    {
        /// <summary>
        /// 应用程序主入口
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
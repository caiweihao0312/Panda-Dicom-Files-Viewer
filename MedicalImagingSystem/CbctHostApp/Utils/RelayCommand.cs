using System;
using System.Windows.Input;

namespace CbctHostApp.Utils
{
    /// <summary>
    /// RelayCommand 类实现了 ICommand 接口，用于封装命令逻辑，支持 WPF 命令绑定。
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;         // 执行命令的委托
        private readonly Func<bool> _canExecute;  // 判断命令是否可执行的委托

        /// <summary>
        /// 构造函数，初始化命令执行和可执行条件。
        /// </summary>
        /// <param name="execute">命令执行的委托</param>
        /// <param name="canExecute">命令是否可执行的委托（可选）</param>
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        /// <summary>
        /// 判断命令是否可执行。
        /// </summary>
        /// <param name="parameter">命令参数（未使用）</param>
        /// <returns>是否可执行</returns>
        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="parameter">命令参数（未使用）</param>
        public void Execute(object parameter) => _execute();

        /// <summary>
        /// 命令可执行状态变化事件。
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// 触发 CanExecuteChanged 事件，通知界面命令状态变化。
        /// </summary>
        public void RaiseCanExecuteChanged() =>
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
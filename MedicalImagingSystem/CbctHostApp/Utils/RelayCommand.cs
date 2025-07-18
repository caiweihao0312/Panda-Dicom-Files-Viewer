using System;
using System.Windows.Input;

namespace CbctHostApp.Utils
{
    /// <summary>
    /// RelayCommand ��ʵ���� ICommand �ӿڣ����ڷ�װ�����߼���֧�� WPF ����󶨡�
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;         // ִ�������ί��
        private readonly Func<bool> _canExecute;  // �ж������Ƿ��ִ�е�ί��

        /// <summary>
        /// ���캯������ʼ������ִ�кͿ�ִ��������
        /// </summary>
        /// <param name="execute">����ִ�е�ί��</param>
        /// <param name="canExecute">�����Ƿ��ִ�е�ί�У���ѡ��</param>
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        /// <summary>
        /// �ж������Ƿ��ִ�С�
        /// </summary>
        /// <param name="parameter">���������δʹ�ã�</param>
        /// <returns>�Ƿ��ִ��</returns>
        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        /// <summary>
        /// ִ�����
        /// </summary>
        /// <param name="parameter">���������δʹ�ã�</param>
        public void Execute(object parameter) => _execute();

        /// <summary>
        /// �����ִ��״̬�仯�¼���
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// ���� CanExecuteChanged �¼���֪ͨ��������״̬�仯��
        /// </summary>
        public void RaiseCanExecuteChanged() =>
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
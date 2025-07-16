using Dicom;
using Dicom.Imaging.Codec;
using Dicom.Network;
using MedicalConsoleApp.Helper;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MedicalConsoleApp.Services
{
    /// <summary>
    /// DicomService 类：用于处理 DICOM 网络协议的操作，包括 C-ECHO、C-FIND 和 C-MOVE 等。
    /// </summary>
    public class MyDicomService
    {
        /**
        ** 1. `_remoteHost`
           - **含义**：远程 DICOM 服务的主机地址。
           - **用途**：指定目标 DICOM 服务的 IP 地址或主机名，用于与远程 DICOM 服务建立网络连接。
           - **示例**：
             - 如果远程 DICOM 服务运行在本地计算机，可以设置为 `"127.0.0.1"` 或 `"localhost"`。
             - 如果远程服务在局域网中，可以设置为类似 `"192.168.1.100"` 的 IP 地址。

        ** 2. `_remotePort`
           - **含义**：远程 DICOM 服务的端口号。
           - **用途**：指定远程 DICOM 服务监听的端口，用于网络通信。
           - **默认值**：DICOM 通常使用端口 `104` 作为默认端口，但也可以根据实际配置使用其他端口。
           - **示例**：
             - 如果远程服务监听端口为 `104`，则设置为 `104`。
             - 如果服务使用非标准端口（如 `11112`），则需要设置为对应的端口号。
        
        ** 3. `_callingAe`
           - **含义**：本地 AE (Application Entity) Title。
           - **用途**：标识本地 DICOM 应用程序的名称，用于在 DICOM 协议中标识发送方。
           - **默认值**：通常设置为一个简短的字符串（如 `"MEDCONSOLE"`），以便远程服务识别。
           - **注意**：
             - AE Title 是 DICOM 协议中的重要标识符，必须唯一且符合约定。
             - AE Title 的长度不能超过 16 个字符。
        
        ** 4. `_calledAe`
           - **含义**：目标 AE (Application Entity) Title。
           - **用途**：标识远程 DICOM 服务的名称，用于在 DICOM 协议中标识接收方。
           - **默认值**：通常设置为远程服务的 AE Title（如 `"ANY-SCP"` 或其他配置值）。
           - **注意**：
             - AE Title 必须与远程服务的配置一致，否则连接会被拒绝。
             - 如果远程服务允许任意 AE Title，可以使用通配符（如 `"ANY-SCP"`）。
        
        ** 5. `_storagePort`
           - **含义**：本地 C-STORE SCP (Service Class Provider) 的端口号。
           - **用途**：指定本地服务监听的端口，用于接收远程服务通过 C-STORE 操作发送的 DICOM 文件。
           - **默认值**：通常设置为 `104` 或其他未被占用的端口。
           - **示例**：
             - 如果本地服务监听端口为 `105`，则设置为 `105`。
           - **注意**：
             - 确保该端口未被其他服务占用。
             - 如果运行在受限环境（如非管理员权限），可能需要使用高于 `1024` 的端口。

        ** 6. `_storageServer`
           - **含义**：本地 Storage SCP 服务实例。
           - **类型**：`DicomServer<StorageSCP>`，是 `fo-dicom` 库中的一个泛型类。
           - **用途**：
             - 用于在本地启动一个 DICOM 服务，监听指定端口（`_storagePort`），接收远程服务通过 C-STORE 操作发送的 DICOM 文件。
             - `StorageSCP` 是自定义的类，继承自 `DicomService`，实现了 `IDicomServiceProvider` 和 `IDicomCStoreProvider` 接口，用于处理 DICOM 网络请求。
           - **注意**：
             - 该实例在 `StartStorageSCP` 方法中初始化。
             - 在程序退出时，需要调用 `Dispose` 方法释放资源。
        
        ### 参数之间的关系
        - `_remoteHost` 和 `_remotePort`：用于与远程 DICOM 服务建立连接。
        - `_callingAe` 和 `_calledAe`：用于标识本地和远程 DICOM 应用程序。
        - `_storagePort` 和 `_storageServer`：用于在本地启动一个 DICOM 服务，接收远程服务发送的文件。

        ### 示例场景
        假设本地应用程序需要与远程 DICOM 服务交互：
        1. C-ECHO 测试：
           - 本地应用程序（`_callingAe`）向远程服务（`_remoteHost` 和 `_remotePort`，`_calledAe`）发送 C-ECHO 请求，验证连接是否正常。
        2. C-MOVE 操作：
           - 本地应用程序向远程服务发送 C-MOVE 请求，要求远程服务将影像文件通过 C-STORE 操作发送到本地。
           - 本地服务通过 `_storageServer` 在 `_storagePort` 上监听，接收影像文件。

        **/

        /// <summary>
        /// 远程 DICOM 服务主机地址
        /// </summary>
        private readonly string _remoteHost;
        /// <summary>
        /// 远程 DICOM 服务端口
        /// </summary>
        private readonly int _remotePort;
        /// <summary>
        /// 本地 AE Title
        /// </summary>
        private readonly string _callingAe;
        /// <summary>
        /// 目标 AE Title
        /// </summary>
        private readonly string _calledAe;
        /// <summary>
        /// 本地 C-STORE SCP 端口
        /// </summary>
        private readonly int _storagePort;
        /// <summary>
        /// 公共属性，用于存储 DicomServer 对象
        /// </summary>
        public IDicomServer? _storageServer { get; private set; }

        /// <summary>
        /// 构造函数，初始化 DICOM 服务的基本参数。
        /// 监听端口 (默认 DICOM Storage SCP 端口为 104，非特权端口可选 11112)
        /// </summary>
        public MyDicomService(string remoteHost, int remotePort, string callingAe = "TEST-SCU", string calledAe = "TEST-SCP", int storagePort = 11112)
        {
            _remoteHost = remoteHost;
            _remotePort = remotePort;
            _callingAe = callingAe;
            _calledAe = calledAe;
            _storagePort = storagePort;
        }

        /// <summary>
        /// 启动本地 Storage SCP 服务，用于接收 C-STORE 请求。
        /// </summary>
        public void StartStorageSCP()
        {
            // 启动 DICOM Storage SCP 服务
            _storageServer = DicomServer.Create<MyCStoreSCP>(_storagePort);

            Console.WriteLine($"DICOM Storage SCP 服务监听端口： {_storagePort}...");

        }

        /// <summary>
        /// 停止 Storage SCP 服务并释放资源。
        /// </summary>
        public void StopStorageSCP()
        {
            if (_storageServer != null)
            {
                _storageServer.Dispose();
                _storageServer = null;
                Console.WriteLine("停止 Storage SCP 服务并释放资源.");
            }
        }

        /// <summary>
        /// 检查 Storage SCP 服务状态，确保服务已正确启动并配置。
        /// </summary>
        private bool CheckStorageSCP()
        {
            // 确保本地 Storage SCP 已启动
            if (_storageServer == null)
            {
                Console.WriteLine("本地 Storage SCP 未启动，请先调用 StartStorageSCP() 方法。");
                return false;
            }
            // 确保 Storage SCP 监听端口正确
            if (_storagePort <= 0 || _storagePort > 65535)
            {
                Console.WriteLine("无效的 Storage SCP 端口，请检查配置。");
                return false;
            }
            // 确保远程主机和端口有效
            if (string.IsNullOrWhiteSpace(_remoteHost) || _remotePort <= 0 || _remotePort > 65535)
            {
                Console.WriteLine("远程主机或端口无效，请检查配置。");
                return false;
            }
            // 确保 AE Title 有效
            if (string.IsNullOrWhiteSpace(_callingAe) || string.IsNullOrWhiteSpace(_calledAe))
            {
                Console.WriteLine("AE Title 不能为空，请检查配置。");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 启动 DICOM 服务并提供用户交互界面。
        /// </summary>
        public void Run()
        {
            Console.WriteLine($"启动本地 Storage SCP（C-STORE 接收端口：{_storagePort}）");
            StartStorageSCP(); // 启动本地 Storage SCP 服务

            while (true)
            {
                // 提供用户操作选项
                Console.WriteLine("\n请选择操作：");
                Console.WriteLine(" 1. C-ECHO 测试");
                Console.WriteLine(" 2. C-FIND 查询");
                Console.WriteLine(" 3. C-MOVE 拉取并存储影像");
                Console.WriteLine(" 4. C-GET 拉取影像");
                Console.WriteLine(" 5. N-ACTION 操作");
                Console.WriteLine(" 6. N-CREATE 创建对象");
                Console.WriteLine(" 7. N-DELETE 删除对象");
                Console.WriteLine(" 8. C-STORE 发送影像");
                Console.WriteLine(" 9. N-EVENT-REPORT 接收事件通知");
                Console.WriteLine("10. C-CANCEL 取消操作");
                Console.WriteLine("11. C-GET 批量拉取影像");
                Console.WriteLine("12. C-FIND 批量查询");
                Console.WriteLine("13. N-SET 更新对象属性");
                Console.WriteLine("14. C-STORE 批量发送影像");
                Console.WriteLine("15. C-FIND 分页查询");
                Console.WriteLine("16. N-GET 获取对象属性");
                Console.WriteLine("17. C-STORE 压缩传输");
                Console.WriteLine("18. C-STORE 加密发送影像");
                Console.WriteLine(" 0. 退出");
                Console.Write("输入编号：");

                var key = Console.ReadLine();

                // 根据用户输入执行对应操作
                switch (key)
                {
                    case "1":
                        Echo().Wait(); // 执行 C-ECHO 测试
                        break;
                    case "2":
                        Find().Wait(); // 执行 C-FIND 查询
                        break;
                    case "3":
                        Move().Wait(); // 执行 C-MOVE 操作
                        break;
                    case "4":
                        Get().Wait(); // 执行 C-GET 操作
                        break;
                    case "5":
                        Action().Wait(); // 执行 N-ACTION 操作
                        break;
                    case "6":
                        Create().Wait(); // 执行 N-CREATE 操作
                        break;
                    case "7":
                        Delete().Wait(); // 执行 N-DELETE 操作
                        break;
                    case "8":
                        Store().Wait(); // 执行 C-STORE 操作
                        break;
                    case "9":
                        EventReport().Wait(); // 执行 N-EVENT-REPORT 操作
                        break;
                    case "10":
                        Cancel().Wait(); // 执行 C-CANCEL 操作
                        break;
                    case "11":
                        BatchGet().Wait(); // 执行 C-GET 批量拉取影像
                        break;
                    case "12":
                        BatchFind().Wait(); // 执行 C-FIND 批量查询
                        break;
                    case "13":
                        Set().Wait(); // 执行 N-SET 操作
                        break;
                    case "14":
                        BatchStore().Wait(); // 执行 C-STORE 批量发送
                        break;
                    case "15":
                        PagedFind().Wait(); // 执行 C-FIND 分页查询
                        break;
                    case "16":
                        GetAttributes().Wait(); // 执行 N-GET 操作
                        break;
                    case "17":
                        CompressedStore().Wait(); // 执行 C-STORE 压缩传输
                        break;
                    case "18":
                        EncryptedStore().Wait(); // 执行加密的 C-STORE 操作
                        break;
                    case "0":
                        Console.WriteLine("退出程序...");
                        StopStorageSCP(); // 停止服务
                        Console.WriteLine("服务已停止.");
                        return; // 退出循环
                    default:
                        Console.WriteLine("未知操作，请重新输入。");
                        break;
                }

                // 提示用户按回车键继续
                Console.WriteLine("\n按回车键继续...");
                Console.ReadLine();
            }
        }

        /// <summary>
        /// 执行 C-ECHO 测试，验证与远程 DICOM 服务的连接。
        /// 创建 DicomClient 对象，指定远程主机、端口、本地 AE Title 和目标 AE Title。
        /// 	添加 DicomCEchoRequest 请求，并通过 OnResponseReceived 事件处理响应。
        /// 	使用 SendAsync 方法发送请求。
        /// </summary>
        public async Task Echo()
        {
            try
            {
                var client = new Dicom.Network.Client.DicomClient(
                    _remoteHost,
                    _remotePort,
                    false, // 不使用 TLS
                    _callingAe,
                    _calledAe);

                // 添加 C-ECHO 请求
                var echoRequest = new DicomCEchoRequest();
                echoRequest.OnResponseReceived += (req, resp) =>
                {
                    Console.WriteLine($"C-ECHO Response: Status = {resp.Status}");
                };

                // 打印请求信息
                Console.WriteLine($"Sending C-ECHO request to AE Title: {_calledAe}, Host: {_remoteHost}, Port: {_remotePort}");

                // 检查 Storage SCP 服务状态
                if (!CheckStorageSCP())
                {
                    Log.Error("本地 Storage SCP 服务未启动或配置错误。");
                    return;
                }
                await client.AddRequestAsync(echoRequest);
                Console.WriteLine("发送 C-ECHO 请求...");
                await client.SendAsync();   // 发送请求
                Console.WriteLine("C-ECHO 测试完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"C-ECHO failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 C-FIND 查询操作，检索系统中元数据条目的查询服务。
        /// 1.	构建 C-FIND 请求
        /// 使用 DicomCFindRequest 类构建查询请求，指定查询级别（如 Study）和查询条件（如 StudyInstanceUID）。
        /// 2.	处理查询响应
        /// 使用 OnResponseReceived 事件处理查询结果。
        /// 3.	发送请求
        /// 使用 DicomClient 发送 C-FIND 请求到远程 DICOM 服务。
        /// </summary>
        public async Task Find()
        {
            try
            {
                Console.Write("请输入 StudyInstanceUID（或留空查询所有）：");
                var studyUid = Console.ReadLine();

                Console.Write("请输入 PatientName（或留空查询所有）：");
                var patientName = Console.ReadLine();

                Console.Write("请输入 StudyDate（格式：YYYYMMDD，或留空查询所有）：");
                var studyDate = Console.ReadLine();

                // 创建 C-FIND 请求
                var request = new DicomCFindRequest(DicomQueryRetrieveLevel.Study)
                {
                    /*
                     *  1.	动态构建查询参数
                            提示用户输入查询条件（如 StudyInstanceUID、PatientName 等），并将这些条件添加到 DicomCFindRequest 的 Dataset 中。
                        2.	设置默认查询条件
                            如果用户未输入某些参数，可以设置默认值（如查询所有记录）。
                        3.	发送查询请求
                            使用 DicomClient 发送 C-FIND 请求到远程 DICOM 服务。
                     */
                    Dataset = new Dicom.DicomDataset
                {
                    { Dicom.DicomTag.QueryRetrieveLevel, "STUDY" },
                    { Dicom.DicomTag.StudyInstanceUID,   studyUid ?? string.Empty },
                    { Dicom.DicomTag.PatientName,        patientName ?? string.Empty },
                    { Dicom.DicomTag.StudyDate,          studyDate ?? string.Empty }
                }
                };

                var results = new List<Dicom.DicomDataset>();
                request.OnResponseReceived += (req, resp) =>
                {
                    if (resp.Status == DicomStatus.Pending)
                    {
                        results.Add(resp.Dataset);
                        Console.WriteLine($"找到：Patient={resp.Dataset.GetSingleValueOrDefault(Dicom.DicomTag.PatientName, "")}, StudyUID={resp.Dataset.GetSingleValueOrDefault(Dicom.DicomTag.StudyInstanceUID, "")}");
                    }
                };
                var client = new Dicom.Network.Client.DicomClient(
                    _remoteHost,
                    _remotePort,
                    false, // 不使用 TLS
                    _callingAe,
                    _calledAe);

                // 检查 Storage SCP 服务状态
                if (!CheckStorageSCP())
                {
                    Log.Error("本地 Storage SCP 服务未启动或配置错误。");
                    return;
                }
                await client.AddRequestAsync(request); // 添加 C-FIND 请求
                Console.WriteLine("发送 C-FIND 请求...");
                await client.SendAsync(cancellationToken: default); // 发送请求
                Console.WriteLine($"C-FIND 查询完成，共找到 {results.Count} 条结果。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"C-FIND 查询失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 C-FIND 批量查询操作。
        /// </summary>
        public async Task BatchFind()
        {
            try
            {
                Console.Write("请输入 PatientName 列表（用逗号分隔）：");
                var patientNames = Console.ReadLine()?.Split(',');

                if (patientNames == null || patientNames.Length == 0)
                {
                    Console.WriteLine("PatientName 列表不能为空，请输入有效的名称列表。");
                    return;
                }

                // 创建 DICOM 客户端
                var client = new Dicom.Network.Client.DicomClient(
                    _remoteHost,
                    _remotePort,
                    false, // 不使用 TLS
                    _callingAe,
                    _calledAe);

                foreach (var patientName in patientNames)
                {
                    if (string.IsNullOrWhiteSpace(patientName)) continue;

                    // 创建 C-FIND 请求
                    var request = new DicomCFindRequest(DicomQueryRetrieveLevel.Study)
                    {
                        Dataset = new DicomDataset
                {
                    { DicomTag.QueryRetrieveLevel, "STUDY" },
                    { DicomTag.PatientName, patientName.Trim() }
                }
                    };

                    // 处理查询响应
                    request.OnResponseReceived += (req, resp) =>
                    {
                        if (resp.Status == DicomStatus.Pending)
                        {
                            Console.WriteLine($"找到：Patient={resp.Dataset.GetSingleValueOrDefault(DicomTag.PatientName, "")}, StudyUID={resp.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, "")}");
                        }
                    };

                    // 添加请求到客户端
                    await client.AddRequestAsync(request);
                }

                Console.WriteLine("发送 C-FIND 批量请求...");
                await client.SendAsync(cancellationToken: default); // 发送请求
                Console.WriteLine("C-FIND 批量请求完成。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"C-FIND 批量操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 C-FIND 分页查询操作。
        /// </summary>
        public async Task PagedFind()
        {
            try
            {
                Console.Write("请输入分页大小（每页返回的结果数）：");
                if (!int.TryParse(Console.ReadLine(), out var pageSize) || pageSize <= 0)
                {
                    Console.WriteLine("分页大小无效，请输入一个正整数。");
                    return;
                }

                Console.Write("请输入 PatientName（或留空查询所有）：");
                var patientName = Console.ReadLine();

                // 创建 C-FIND 请求
                var request = new DicomCFindRequest(DicomQueryRetrieveLevel.Study)
                {
                    Dataset = new DicomDataset
            {
                { DicomTag.QueryRetrieveLevel, "STUDY" },
                { DicomTag.PatientName, patientName ?? string.Empty }
            }
                };

                var results = new List<DicomDataset>();
                int currentPage = 1;

                // 处理查询响应
                request.OnResponseReceived += (req, resp) =>
                {
                    if (resp.Status == DicomStatus.Pending)
                    {
                        results.Add(resp.Dataset);
                        if (results.Count >= pageSize)
                        {
                            Console.WriteLine($"第 {currentPage} 页结果：");
                            foreach (var result in results)
                            {
                                Console.WriteLine($"Patient={result.GetSingleValueOrDefault(DicomTag.PatientName, "")}, StudyUID={result.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, "")}");
                            }
                            results.Clear();
                            currentPage++;
                        }
                    }
                };

                // 创建 DICOM 客户端
                var client = new Dicom.Network.Client.DicomClient(
                    _remoteHost,
                    _remotePort,
                    false, // 不使用 TLS
                    _callingAe,
                    _calledAe);

                // 添加请求到客户端
                await client.AddRequestAsync(request);
                Console.WriteLine("发送 C-FIND 分页查询请求...");
                await client.SendAsync(cancellationToken: default); // 发送请求

                // 输出最后一页结果
                if (results.Count > 0)
                {
                    Console.WriteLine($"第 {currentPage} 页结果：");
                    foreach (var result in results)
                    {
                        Console.WriteLine($"Patient={result.GetSingleValueOrDefault(DicomTag.PatientName, "")}, StudyUID={result.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, "")}");
                    }
                }

                Console.WriteLine("C-FIND 分页查询完成。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"C-FIND 分页查询失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 C-MOVE 操作，从远程服务拉取影像并存储到本地。
        /// 1.	构建 C-MOVE 请求
        /// 使用 DicomCMoveRequest 类构建移动请求，指定目标 AE Title 和查询条件（如 StudyInstanceUID）。
        /// 2.	处理移动响应
        /// 使用 OnResponseReceived 事件处理移动操作的状态更新。
        /// 3.	发送请求
        /// 使用 DicomClient 发送 C-MOVE 请求到远程 DICOM 服务。
        /// </summary>
        public async Task Move()
        {
            try
            {
                Console.Write("请输入目标 Storage SCP AE Title（一般为本应用 Calling AE）：");
                var destinationAe = Console.ReadLine() ?? _callingAe;
                Console.Write("请输入 StudyInstanceUID：");
                var studyUid = Console.ReadLine();

                // 确保目标 AE Title 有效
                if (string.IsNullOrWhiteSpace(destinationAe))
                {
                    Console.WriteLine("目标 AE Title 不能为空，请输入有效的 AE Title。");
                    return;
                }
                // 确保 StudyInstanceUID 有效
                if (string.IsNullOrWhiteSpace(studyUid))
                {
                    Console.WriteLine("StudyInstanceUID 不能为空，请输入有效的 StudyInstanceUID。");
                    return;
                }
                // 创建 C-MOVE 请求
                var request = new DicomCMoveRequest(_calledAe, destinationAe) // Fix: Pass the correct parameters
                {
                    Dataset = new Dicom.DicomDataset
                    {
                        { Dicom.DicomTag.QueryRetrieveLevel, "STUDY" },
                        { Dicom.DicomTag.StudyInstanceUID, studyUid }
                    }
                };

                // 日志记录请求参数
                Log.Information("C-MOVE 请求已创建：");
                Log.Information("目标 AE Title: {DestinationAe}", destinationAe);
                Log.Information("StudyInstanceUID: {StudyInstanceUID}", studyUid);

                // 处理移动响应
                request.OnResponseReceived += (req, resp) =>
                {
                    Log.Information("C-MOVE 响应状态：{Status}, Remaining={Remaining}, Completed={Completed}, Failures={Failures}",
                                    resp.Status, resp.Remaining, resp.Completed, resp.Failures);
                    Console.WriteLine($"C-MOVE 状态：{resp.Status}，Remaining={resp.Remaining}, Completed={resp.Completed}");
                };

                // 创建 DICOM 客户端
                var client = new Dicom.Network.Client.DicomClient(
                    _remoteHost,
                    _remotePort,
                    false, // 不使用 TLS
                    _callingAe,
                    _calledAe);

                // 检查 Storage SCP 服务状态
                if (!CheckStorageSCP())
                {
                    Log.Error("本地 Storage SCP 服务未启动或配置错误。");
                    return;
                }
                // 添加 C-MOVE 请求到客户端
                await client.AddRequestAsync(request);
                Log.Information("发送 C-MOVE 请求...");
                Console.WriteLine("发送 C-MOVE 请求...");
                await client.SendAsync(cancellationToken: default); // 发送请求
                Log.Information("C-MOVE 请求完成，文件将通过本地 Storage SCP 接收。");
                Console.WriteLine("C-MOVE 请求完成，文件将通过本地 Storage SCP 接收");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"C-MOVE 操作失败: {ex.Message}");
                // 记录异常信息
                Log.Error(ex, "C-MOVE 操作失败：{Message}", ex.Message);
            }
        }

        /// <summary>
        /// 执行 C-GET 操作，从远程服务拉取影像并存储到本地。
        /// </summary>
        public async Task Get()
        {
            try
            {
                Console.Write("请输入 StudyInstanceUID：");
                var studyUid = Console.ReadLine();

                // 确保 StudyInstanceUID 有效
                if (string.IsNullOrWhiteSpace(studyUid))
                {
                    Console.WriteLine("StudyInstanceUID 不能为空，请输入有效的 StudyInstanceUID。");
                    return;
                }

                Dicom.DicomDataset Dataset = new Dicom.DicomDataset
                {
                    { Dicom.DicomTag.QueryRetrieveLevel, "STUDY" },
                    { Dicom.DicomTag.StudyInstanceUID, studyUid }
                };
                // 创建 C-GET 请求
                var request = new DicomCGetRequest(Dataset);

                // 处理影像接收响应
                request.OnResponseReceived += (req, resp) =>
                {
                    Console.WriteLine($"C-GET 状态：{resp.Status}, Remaining={resp.Remaining}, Completed={resp.Completed}");
                };

                // 创建 DICOM 客户端
                var client = new Dicom.Network.Client.DicomClient(
                    _remoteHost,
                    _remotePort,
                    false, // 不使用 TLS
                    _callingAe,
                    _calledAe);

                // 添加 C-GET 请求到客户端
                await client.AddRequestAsync(request);
                Console.WriteLine("发送 C-GET 请求...");
                await client.SendAsync(cancellationToken: default); // 发送请求
                Console.WriteLine("C-GET 请求完成，文件将通过当前连接接收。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"C-GET 操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 C-GET 批量拉取影像操作。
        /// </summary>
        public async Task BatchGet()
        {
            try
            {
                Console.Write("请输入 StudyInstanceUID 列表（用逗号分隔）：");
                var studyUids = Console.ReadLine()?.Split(',');

                if (studyUids == null || studyUids.Length == 0)
                {
                    Console.WriteLine("StudyInstanceUID 列表不能为空，请输入有效的 UID 列表。");
                    return;
                }

                // 创建 DICOM 客户端
                var client = new Dicom.Network.Client.DicomClient(
                    _remoteHost,
                    _remotePort,
                    false, // 不使用 TLS
                    _callingAe,
                    _calledAe);

                foreach (var studyUid in studyUids)
                {
                    if (string.IsNullOrWhiteSpace(studyUid)) continue;

                    // 创建 C-GET 请求
                    var request = new DicomCGetRequest(new DicomDataset
            {
                { DicomTag.QueryRetrieveLevel, "STUDY" },
                { DicomTag.StudyInstanceUID, studyUid.Trim() }
            });

                    // 处理影像接收响应
                    request.OnResponseReceived += (req, resp) =>
                    {
                        Console.WriteLine($"C-GET 状态：{resp.Status}, Remaining={resp.Remaining}, Completed={resp.Completed}");
                    };

                    // 添加请求到客户端
                    await client.AddRequestAsync(request);
                }

                Console.WriteLine("发送 C-GET 批量请求...");
                await client.SendAsync(cancellationToken: default); // 发送请求
                Console.WriteLine("C-GET 批量请求完成，文件将通过当前连接接收。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"C-GET 批量操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 N-ACTION 操作，触发远程服务的操作（如打印影像）。
        /// </summary>
        public async Task Action()
        {
            try
            {
                Console.Write("请输入操作对象 UID：");
                var sopInstanceUid = Console.ReadLine();

                // 确保操作对象 UID 有效
                if (string.IsNullOrWhiteSpace(sopInstanceUid))
                {
                    Console.WriteLine("操作对象 UID 不能为空，请输入有效的 UID。");
                    return;
                }

                // 创建 N-ACTION 请求
                var sopInstanceUidDicom = DicomUID.Parse(sopInstanceUid);
                var request = new DicomNActionRequest(DicomUID.PrintJob, sopInstanceUidDicom, 1);
                // 处理操作响应
                request.OnResponseReceived += (req, resp) =>
                {
                    Console.WriteLine($"N-ACTION 状态：{resp.Status}");
                };

                // 创建 DICOM 客户端
                var client = new Dicom.Network.Client.DicomClient(
                    _remoteHost,
                    _remotePort,
                    false, // 不使用 TLS
                    _callingAe,
                    _calledAe);

                // 添加 N-ACTION 请求到客户端
                await client.AddRequestAsync(request);
                Console.WriteLine("发送 N-ACTION 请求...");
                await client.SendAsync(cancellationToken: default); // 发送请求
                Console.WriteLine("N-ACTION 请求完成。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"N-ACTION 操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 N-CREATE 操作，在远程服务中创建新对象（如创建打印任务或存储新影像）。
        /// </summary>
        public async Task Create()
        {
            try
            {
                Console.Write("请输入对象 UID：");
                var sopInstanceUid = Console.ReadLine();

                // 确保对象 UID 有效
                if (string.IsNullOrWhiteSpace(sopInstanceUid))
                {
                    Console.WriteLine("对象 UID 不能为空，请输入有效的 UID。");
                    return;
                }

                // 创建 N-CREATE 请求
                var sopInstanceUidDicom = DicomUID.Parse(sopInstanceUid);
                var request = new DicomNCreateRequest(DicomUID.BasicFilmSession, sopInstanceUidDicom)
                {
                    Dataset = new Dicom.DicomDataset
                    {
                        { Dicom.DicomTag.NumberOfCopies, "1" },
                        { Dicom.DicomTag.PrinterName, "DefaultPrinter" }
                    }
                };

                // 处理创建响应
                request.OnResponseReceived += (req, resp) =>
                {
                    Console.WriteLine($"N-CREATE 状态：{resp.Status}");
                };

                // 创建 DICOM 客户端
                var client = new Dicom.Network.Client.DicomClient(
                    _remoteHost,
                    _remotePort,
                    false, // 不使用 TLS
                    _callingAe,
                    _calledAe);

                // 添加 N-CREATE 请求到客户端
                await client.AddRequestAsync(request);
                Console.WriteLine("发送 N-CREATE 请求...");
                await client.SendAsync(cancellationToken: default); // 发送请求
                Console.WriteLine("N-CREATE 请求完成。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"N-CREATE 操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 N-DELETE 操作，删除远程服务中的对象（如删除打印任务或影像文件）。
        /// </summary>
        public async Task Delete()
        {
            try
            {
                Console.Write("请输入对象 UID：");
                var sopInstanceUid = Console.ReadLine();

                // 确保对象 UID 有效
                if (string.IsNullOrWhiteSpace(sopInstanceUid))
                {
                    Console.WriteLine("对象 UID 不能为空，请输入有效的 UID。");
                    return;
                }

                // 创建 N-DELETE 请求
                var sopInstanceUidDicom = DicomUID.Parse(sopInstanceUid);
                var request = new DicomNDeleteRequest(DicomUID.BasicFilmSession, sopInstanceUidDicom);

                // 处理删除响应
                request.OnResponseReceived += (req, resp) =>
                {
                    Console.WriteLine($"N-DELETE 状态：{resp.Status}");
                };

                // 创建 DICOM 客户端
                var client = new Dicom.Network.Client.DicomClient(
                    _remoteHost,
                    _remotePort,
                    false, // 不使用 TLS
                    _callingAe,
                    _calledAe);

                // 添加 N-DELETE 请求到客户端
                await client.AddRequestAsync(request);
                Console.WriteLine("发送 N-DELETE 请求...");
                await client.SendAsync(cancellationToken: default); // 发送请求
                Console.WriteLine("N-DELETE 请求完成。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"N-DELETE 操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 C-STORE 操作，将影像文件发送到远程 DICOM 服务。
        /// </summary>
        public async Task Store()
        {
            try
            {
                Console.Write("请输入要发送的 DICOM 文件路径：");
                var filePath = Console.ReadLine();

                // 确保文件路径有效
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    Console.WriteLine("文件路径无效，请输入有效的 DICOM 文件路径。");
                    return;
                }

                // 加载 DICOM 文件
                var dicomFile = DicomFile.Open(filePath);

                // 创建 C-STORE 请求
                var request = new DicomCStoreRequest(dicomFile);

                // 处理发送响应
                request.OnResponseReceived += (req, resp) =>
                {
                    Console.WriteLine($"C-STORE 状态：{resp.Status}");
                };

                // 创建 DICOM 客户端
                var client = new Dicom.Network.Client.DicomClient(
                    _remoteHost,
                    _remotePort,
                    false, // 不使用 TLS
                    _callingAe,
                    _calledAe);

                // 添加 C-STORE 请求到客户端
                await client.AddRequestAsync(request);
                Console.WriteLine("发送 C-STORE 请求...");
                await client.SendAsync(cancellationToken: default); // 发送请求
                Console.WriteLine("C-STORE 请求完成。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"C-STORE 操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 C-STORE 批量发送操作。
        /// </summary>
        public async Task BatchStore()
        {
            try
            {
                Console.Write("请输入 DICOM 文件路径列表（用逗号分隔）：");
                var filePaths = Console.ReadLine()?.Split(',');

                if (filePaths == null || filePaths.Length == 0)
                {
                    Console.WriteLine("文件路径列表不能为空，请输入有效的路径列表。");
                    return;
                }

                // 创建 DICOM 客户端
                var client = new Dicom.Network.Client.DicomClient(
                    _remoteHost,
                    _remotePort,
                    false, // 不使用 TLS
                    _callingAe,
                    _calledAe);

                foreach (var filePath in filePaths)
                {
                    if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) continue;

                    // 加载 DICOM 文件
                    var dicomFile = DicomFile.Open(filePath.Trim());

                    // 创建 C-STORE 请求
                    var request = new DicomCStoreRequest(dicomFile);

                    // 处理发送响应
                    request.OnResponseReceived += (req, resp) =>
                    {
                        Console.WriteLine($"C-STORE 状态：{resp.Status}");
                    };

                    // 添加请求到客户端
                    await client.AddRequestAsync(request);
                }

                Console.WriteLine("发送 C-STORE 批量请求...");
                await client.SendAsync(cancellationToken: default); // 发送请求
                Console.WriteLine("C-STORE 批量请求完成。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"C-STORE 批量操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 C-STORE 压缩传输操作。
        /// </summary>
        public async Task CompressedStore()
        {
            try
            {
                Console.Write("请输入要发送的 DICOM 文件路径：");
                var filePath = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    Console.WriteLine("文件路径无效，请输入有效的 DICOM 文件路径。");
                    return;
                }

                Console.WriteLine("请选择压缩传输语法：");
                Console.WriteLine("1. JPEG Baseline");
                Console.WriteLine("2. JPEG Lossless");
                Console.WriteLine("3. RLE Lossless");
                Console.Write("输入编号：");
                var choice = Console.ReadLine();

                DicomTransferSyntax transferSyntax = choice switch
                {
                    "1" => DicomTransferSyntax.JPEGProcess1,
                    "2" => DicomTransferSyntax.JPEGLSLossless,
                    "3" => DicomTransferSyntax.RLELossless,
                    _ => throw new InvalidOperationException("无效的选择")
                };

                // 加载 DICOM 文件
                var dicomFile = DicomFile.Open(filePath);

                // 转码为指定的传输语法
                var transcoder = new DicomTranscoder(dicomFile.Dataset.InternalTransferSyntax, transferSyntax);
                var compressedFile = transcoder.Transcode(dicomFile);

                // 创建 C-STORE 请求
                var request = new DicomCStoreRequest(compressedFile);

                // 处理发送响应
                request.OnResponseReceived += (req, resp) =>
                {
                    Console.WriteLine($"C-STORE 状态：{resp.Status}");
                };

                // 创建 DICOM 客户端
                var client = new Dicom.Network.Client.DicomClient(
                    _remoteHost,
                    _remotePort,
                    false, // 不使用 TLS
                    _callingAe,
                    _calledAe);

                // 添加请求到客户端
                await client.AddRequestAsync(request);
                Console.WriteLine("发送 C-STORE 压缩传输请求...");
                await client.SendAsync(cancellationToken: default); // 发送请求
                Console.WriteLine("C-STORE 压缩传输完成。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"C-STORE 压缩传输失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 C-STORE 操作，将加密后的影像文件发送到远程 DICOM 服务。
        /// </summary>
        public async Task EncryptedStore()
        {
            try
            {
                Console.Write("请输入要发送的 DICOM 文件路径：");
                var filePath = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    Console.WriteLine("文件路径无效，请输入有效的 DICOM 文件路径。");
                    return;
                }

                // 加载 DICOM 文件
                var dicomFile = DicomFile.Open(filePath);

                // 将 DICOM 文件序列化为字节数组
                using var memoryStream = new MemoryStream();
                dicomFile.Save(memoryStream);
                var dicomBytes = memoryStream.ToArray();

                // 加密 DICOM 文件内容
                var encryptedBytes = DicomEncryptionHelper.Encrypt(dicomBytes);

                // 创建加密后的 DICOM 文件
                var encryptedFilePath = Path.Combine(Path.GetDirectoryName(filePath)!, "Encrypted_" + Path.GetFileName(filePath));
                File.WriteAllBytes(encryptedFilePath, encryptedBytes);

                Console.WriteLine($"加密后的文件已保存到：{encryptedFilePath}");

                // 创建 C-STORE 请求
                var request = new DicomCStoreRequest(DicomFile.Open(encryptedFilePath));

                // 处理发送响应
                request.OnResponseReceived += (req, resp) =>
                {
                    Console.WriteLine($"C-STORE 状态：{resp.Status}");
                };

                // 创建 DICOM 客户端
                var client = new Dicom.Network.Client.DicomClient(
                    _remoteHost,
                    _remotePort,
                    false, // 不使用 TLS
                    _callingAe,
                    _calledAe);

                // 添加请求到客户端
                await client.AddRequestAsync(request);
                Console.WriteLine("发送加密的 C-STORE 请求...");
                await client.SendAsync(cancellationToken: default); // 发送请求
                Console.WriteLine("加密的 C-STORE 请求完成。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加密的 C-STORE 操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 N-EVENT-REPORT 操作，接收远程服务的事件通知。
        /// </summary>
        public async Task EventReport()
        {
            try
            {
                Console.Write("请输入事件对象 UID：");
                var sopInstanceUid = Console.ReadLine();

                // 确保事件对象 UID 有效
                if (string.IsNullOrWhiteSpace(sopInstanceUid))
                {
                    Console.WriteLine("事件对象 UID 不能为空，请输入有效的 UID。");
                    return;
                }

                // 创建 N-EVENT-REPORT 请求
                var sopInstanceUidDicom = DicomUID.Parse(sopInstanceUid);
                var request = new DicomNEventReportRequest(DicomUID.BasicFilmSession, sopInstanceUidDicom, 1);

                // 处理事件报告响应
                request.OnResponseReceived += (req, resp) =>
                {
                    Console.WriteLine($"N-EVENT-REPORT 状态：{resp.Status}");
                };

                // 创建 DICOM 客户端
                var client = new Dicom.Network.Client.DicomClient(
                    _remoteHost,
                    _remotePort,
                    false, // 不使用 TLS
                    _callingAe,
                    _calledAe);

                // 添加 N-EVENT-REPORT 请求到客户端
                await client.AddRequestAsync(request);
                Console.WriteLine("发送 N-EVENT-REPORT 请求...");
                await client.SendAsync(cancellationToken: default); // 发送请求
                Console.WriteLine("N-EVENT-REPORT 请求完成。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"N-EVENT-REPORT 操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 C-CANCEL 操作，取消正在进行的 DICOM 操作（如 C-FIND、C-MOVE 或 C-GET）。
        /// </summary>
        public async Task Cancel()
        {
            try
            {
                Console.WriteLine("正在发送 C-FIND 请求...");
                var cts = new CancellationTokenSource();

                // 创建 C-FIND 请求
                var request = new DicomCFindRequest(DicomQueryRetrieveLevel.Study)
                {
                    Dataset = new Dicom.DicomDataset
            {
                { Dicom.DicomTag.QueryRetrieveLevel, "STUDY" },
                { Dicom.DicomTag.PatientName, "*" }
            }
                };

                // 处理查询响应
                request.OnResponseReceived += (req, resp) =>
                {
                    Console.WriteLine($"C-FIND 状态：{resp.Status}");
                    if (resp.Status == DicomStatus.Pending)
                    {
                        Console.WriteLine("取消操作...");
                        cts.Cancel(); // 取消操作
                    }
                };

                // 创建 DICOM 客户端
                var client = new Dicom.Network.Client.DicomClient(
                    _remoteHost,
                    _remotePort,
                    false, // 不使用 TLS
                    _callingAe,
                    _calledAe);

                // 添加 C-FIND 请求到客户端
                await client.AddRequestAsync(request);
                await client.SendAsync(cts.Token); // 发送请求并支持取消
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("操作已取消。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"C-CANCEL 操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 N-SET 操作，更新远程服务中对象的属性。
        /// </summary>
        public async Task Set()
        {
            try
            {
                Console.Write("请输入对象 UID：");
                var sopInstanceUid = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(sopInstanceUid))
                {
                    Console.WriteLine("对象 UID 不能为空，请输入有效的 UID。");
                    return;
                }

                Console.Write("请输入要更新的属性（如 PrinterStatus）：");
                var attributeName = Console.ReadLine();

                Console.Write("请输入属性的新值：");
                var attributeValue = Console.ReadLine();

                // 创建 N-SET 请求
                var sopInstanceUidDicom = DicomUID.Parse(sopInstanceUid);
                var request = new DicomNSetRequest(DicomUID.BasicFilmSession, sopInstanceUidDicom)
                {
                    Dataset = new DicomDataset
                    {
                        { DicomTag.Parse(attributeName), attributeValue }
                    }
                };

                // 处理更新响应
                request.OnResponseReceived += (req, resp) =>
                {
                    Console.WriteLine($"N-SET 状态：{resp.Status}");
                };

                // 创建 DICOM 客户端
                var client = new Dicom.Network.Client.DicomClient(
                    _remoteHost,
                    _remotePort,
                    false, // 不使用 TLS
                    _callingAe,
                    _calledAe);

                // 添加请求到客户端
                await client.AddRequestAsync(request);
                Console.WriteLine("发送 N-SET 请求...");
                await client.SendAsync(cancellationToken: default); // 发送请求
                Console.WriteLine("N-SET 请求完成。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"N-SET 操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 N-GET 操作，获取远程服务中对象的属性。
        /// </summary>
        public async Task GetAttributes()
        {
            try
            {
                Console.Write("请输入对象 UID：");
                var sopInstanceUid = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(sopInstanceUid))
                {
                    Console.WriteLine("对象 UID 不能为空，请输入有效的 UID。");
                    return;
                }

                Console.Write("请输入要获取的属性（用逗号分隔，例如 PrinterStatus,PrinterName）：");
                var attributes = Console.ReadLine()?.Split(',');

                if (attributes == null || attributes.Length == 0)
                {
                    Console.WriteLine("属性列表不能为空，请输入有效的属性名称。");
                    return;
                }

                // Create N-GET request
                var sopInstanceUidDicom = DicomUID.Parse(sopInstanceUid);
                var request = new DicomNGetRequest(DicomUID.BasicFilmSession, sopInstanceUidDicom);

                foreach (var attribute in attributes)
                {
                    var tag = DicomTag.Parse(attribute.Trim());
                    request.Command.AddOrUpdate<string>(tag, attributes);
                }

                // Handle response
                request.OnResponseReceived += (req, resp) =>
                {
                    Console.WriteLine($"N-GET 状态：{resp.Status}");
                    if (resp.Status == DicomStatus.Success)
                    {
                        foreach (var attribute in attributes)
                        {
                            var tag = DicomTag.Parse(attribute.Trim());
                            Console.WriteLine($"{tag.DictionaryEntry.Name}: {resp.Dataset.GetSingleValueOrDefault(tag, "未找到")}");
                        }
                    }
                };

                // 创建 DICOM 客户端
                var client = new Dicom.Network.Client.DicomClient(
                    _remoteHost,
                    _remotePort,
                    false, // 不使用 TLS
                    _callingAe,
                    _calledAe);

                // 添加请求到客户端
                await client.AddRequestAsync(request);
                Console.WriteLine("发送 N-GET 请求...");
                await client.SendAsync(cancellationToken: default); // 发送请求
                Console.WriteLine("N-GET 请求完成。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"N-GET 操作失败: {ex.Message}");
            }
        }
    }
}
using Dicom;
using Dicom.Network;
using MedicalConsoleApp.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalConsoleApp.Services
{

    /// <summary>
    /// 自定义 Storage SCP 服务
    /// 继承自 DicomService，为 DICOM 服务提供基础通信功能。
    /// 主要用于处理 DICOM 协议中的关联请求、关联释放、C-STORE 请求和 C-ECHO 请求。
    /// 它支持同步和异步操作，并提供了异常处理和日志记录功能，适用于 DICOM 文件的接收和存储场景。
    /// </summary>
    public class MyCStoreSCP : DicomService, Dicom.Network.IDicomServiceProvider, IDicomCStoreProvider,IDicomCEchoProvider,IDicomCFindProvider
    {
        /// <summary>
        /// 构造函数，初始化 Storage SCP 服务
        /// </summary>
        /// <param name="stream">网络流，用于与客户端通信</param>
        /// <param name="fallbackEncoding">回退编码，用于处理非标准字符集</param>
        /// <param name="log">日志记录器</param>
        public MyCStoreSCP(Dicom.Network.INetworkStream stream, Encoding fallbackEncoding, Dicom.Log.Logger log)
            : base(stream, fallbackEncoding, log)
        {
        }

        #region IDicomServiceProvider Members

        /// <summary>
        /// 处理接收到的关联请求（Association Request）
        /// 逻辑：
        /// 1.	遍历客户端的 Presentation Context。
        /// 2.	接受所有支持的 Storage 服务类（C-STORE）。
        /// 3.	对不支持的服务类，返回 RejectAbstractSyntaxNotSupported。
        /// 4.	发送关联接受响应。
        /// 输出：在控制台打印关联请求和接受状态。
        /// </summary>
        /// <param name="association">关联对象，包含客户端的 AE Title 和 Presentation Context 信息</param>
        public void OnReceiveAssociationRequest(Dicom.Network.DicomAssociation association)
        {
            Console.WriteLine($"Received association request from AE '{association.CallingAE}'");

            // 遍历客户端的 Presentation Context，接受所有支持的 Storage 服务类（C-STORE）
            foreach (var pc in association.PresentationContexts)
            {
                if (pc.AbstractSyntax.StorageCategory != Dicom.DicomStorageCategory.None)
                    pc.AcceptTransferSyntaxes((Dicom.DicomTransferSyntax[])pc.GetTransferSyntaxes());
                else
                    pc.SetResult(Dicom.Network.DicomPresentationContextResult.RejectAbstractSyntaxNotSupported);
            }

            // 发送关联接受响应
            SendAssociationAcceptAsync(association);
            Console.WriteLine("Association accepted.");
        }

        /// <summary>
        /// 处理接收到的关联释放请求（Association Release Request）
        /// 逻辑：
        /// 1.	打印关联释放请求的日志。
        /// 2.	发送关联释放响应。
        /// 输出：在控制台打印关联释放状态。
        /// </summary>
        public void OnReceiveAssociationReleaseRequest()
        {
            Console.WriteLine("Association release request received");
            // 发送关联释放响应
            SendAssociationReleaseResponseAsync();
            Console.WriteLine("Association released.");
        }

        /// <summary>
        /// 处理 C-STORE 请求时发生的异常
        /// 逻辑：
        /// 1.	打印异常信息。
        /// 2.	如果存在临时文件，尝试删除。
        /// 3.	如果删除失败，打印删除失败的日志。
        /// 输出：在控制台打印异常和文件删除状态。
        /// </summary>
        /// <param name="tempFileName">临时文件名，存储接收到的部分数据</param>
        /// <param name="e">异常信息</param>
        public void OnCStoreRequestException(string tempFileName, Exception e)
        {
            Console.WriteLine($"C-STORE 请求处理异常: {e.Message}");

            // 如果存在临时文件，尝试删除
            if (!string.IsNullOrEmpty(tempFileName) && File.Exists(tempFileName))
            {
                try
                {
                    File.Delete(tempFileName);
                    Console.WriteLine($"已删除临时文件: {tempFileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"删除临时文件失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 处理接收到的关联中止请求（Abort Request）
        /// </summary>
        /// <param name="source">中止请求的来源</param>
        /// <param name="reason">中止请求的原因</param>
        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
            Console.WriteLine($"Association aborted: source={source}, reason={reason}");
        }

        /// <summary>
        /// 异步处理连接关闭事件
        /// 在控制台打印连接关闭状态。
        /// </summary>
        /// <param name="exception">连接关闭时的异常信息（如果有）</param>
        public void OnConnectionClosed(Exception exception)
        {
            if (exception != null)
            {
                Console.WriteLine($"Connection closed with error: {exception.Message}");
            }
            else
            {
                Console.WriteLine("Connection closed");
            }
        }

        #endregion

        #region IDicomCStoreProvider Members

        /// <summary>
        /// 处理接收到的 C-STORE 请求
        /// 逻辑：
        /// 1.	从请求中提取 StudyInstanceUID、SeriesInstanceUID 和 SOPInstanceUID。
        /// 2.	构造目录结构./DICOM/{Study}/{Series}/。
        /// 3.将接收到的 DICOM 文件保存到本地。
        /// 4.	返回成功响应或处理失败响应。
        /// 输出：在控制台打印文件保存路径或错误信息。
        /// </summary>
        /// <param name="request">C-STORE 请求对象，包含 DICOM 文件的元数据和像素数据</param>
        /// <returns>返回 C-STORE 响应，指示处理结果</returns>
        public Dicom.Network.DicomCStoreResponse OnCStoreRequest(Dicom.Network.DicomCStoreRequest request)
        {
            try
            {
                // 检查是否为 CT Image Storage
                if (request.SOPClassUID == DicomUID.CTImageStorage)
                {
                    Console.WriteLine("Received C-STORE request for CT Image Storage");

                    // 保存接收到的 DICOM 文件
                    var studyInstanceUid = request.Dataset.GetSingleValue<string>(Dicom.DicomTag.StudyInstanceUID);
                    var seriesInstanceUid = request.Dataset.GetSingleValue<string>(Dicom.DicomTag.SeriesInstanceUID);
                    var sopInstanceUid = request.SOPInstanceUID.UID;

                    var folder = Path.Combine("DICOM", studyInstanceUid, seriesInstanceUid);
                    Directory.CreateDirectory(folder);

                    var fileName = Path.Combine(folder, $"{sopInstanceUid}.dcm");
                    request.File.Save(fileName);

                    Console.WriteLine($"Stored DICOM file: {fileName}");
                }
                else
                {  
                    // 保存接收到的 DICOM 对象到本地文件
                    var studyInstanceUid = request.Dataset.GetSingleValue<string>(Dicom.DicomTag.StudyInstanceUID);
                    var seriesInstanceUid = request.Dataset.GetSingleValue<string>(Dicom.DicomTag.SeriesInstanceUID);
                    var sopInstanceUid = request.SOPInstanceUID.UID;

                    // 构造目录结构：./DICOM/{Study}/{Series}/
                    var folder = Path.Combine("DICOM", studyInstanceUid, seriesInstanceUid);
                    Directory.CreateDirectory(folder);

                    // 保存文件名：{SOPInstanceUID}.dcm
                    var fileName = Path.Combine(folder, $"{sopInstanceUid}.dcm");
                    request.File.Save(fileName);

                    Console.WriteLine($"Stored DICOM file: {fileName}");
                }

                // 返回成功响应
                return new DicomCStoreResponse(request, DicomStatus.Success);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing DICOM object: {ex}");
                // 返回处理失败响应
                return new DicomCStoreResponse(request, DicomStatus.ProcessingFailure);
            }
        }

        /// <summary>
        /// 异步处理接收到的 C-STORE 请求（支持加密文件）
        /// 逻辑：
        /// 1.	将接收到的文件保存为临时文件。
        /// 2.	读取文件内容并解密。
        /// 3.	将解密后的文件保存到本地。
        /// 4.	返回成功响应或处理失败响应。
        /// 输出：在控制台打印文件保存路径或错误信息。
        /// </summary>
        /// <param name="request">C-STORE 请求对象</param>
        /// <returns>返回异步任务，包含 C-STORE 响应</returns>
        public Task<DicomCStoreResponse> OnCStoreRequestAsync(DicomCStoreRequest request)
        {
            try
            {
                // 保存接收到的 DICOM 文件
                var tempFilePath = Path.GetTempFileName();
                request.File.Save(tempFilePath);

                Console.WriteLine($"接收到的文件已保存到：{tempFilePath}");

                // 解密文件内容
                var encryptedBytes = File.ReadAllBytes(tempFilePath);
                var decryptedBytes = DicomEncryptionHelper.Decrypt(encryptedBytes);

                // 保存解密后的文件
                var decryptedFilePath = Path.Combine(Path.GetDirectoryName(tempFilePath)!, "Decrypted_" + Path.GetFileName(tempFilePath));
                File.WriteAllBytes(decryptedFilePath, decryptedBytes);

                Console.WriteLine($"解密后的文件已保存到：{decryptedFilePath}");

                return Task.FromResult(new DicomCStoreResponse(request, DicomStatus.Success));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"C-STORE 接收解密失败: {ex.Message}");
                return Task.FromResult(new DicomCStoreResponse(request, DicomStatus.ProcessingFailure));
            }
        }

        /// <summary>
        /// 异步处理接收到的 C-STORE 请求
        /// </summary>
        /// <param name="request">C-STORE 请求对象</param>
        /// <returns>返回异步任务，包含 C-STORE 响应</returns>
        //public Task<DicomCStoreResponse> OnCStoreRequestAsync(DicomCStoreRequest request)
        //{
        //    // 直接转调用同步方法，也可自行实现异步逻辑
        //    return Task.FromResult(OnCStoreRequest(request));
        //}

        /// <summary>
        /// 处理接收到的 C-ECHO 请求
        /// </summary>
        /// <param name="request">C-ECHO 请求对象</param>
        /// <returns>返回 C-ECHO 响应，指示处理结果</returns>
        public DicomCEchoResponse OnCEchoRequest(DicomCEchoRequest request)
        {
            Console.WriteLine("Received C-ECHO request");
            // 支持 DICOM CEcho（C-ECHO）请求
            return new DicomCEchoResponse(request, DicomStatus.Success);
        }

        /// <summary>
        /// 异步处理接收到的关联请求
        /// 逻辑：
        /// 1.	遍历客户端的 Presentation Context。
        /// 2.	接受所有支持的 Storage 服务类（C-STORE）。
        /// 3.	对不支持的服务类，返回 RejectAbstractSyntaxNotSupported。
        /// 4.	发送关联接受响应。
        /// 输出：在控制台打印关联请求和接受状态。
        /// </summary>
        /// <param name="association">关联对象</param>
        public async Task OnReceiveAssociationRequestAsync(DicomAssociation association)
        {
            Console.WriteLine($"[Async] Received association request from AE '{association.CallingAE}'");
            // 打印 Presentation Context 信息
            foreach (var pc in association.PresentationContexts)
            {
                Console.WriteLine($"Presentation Context: AbstractSyntax={pc.AbstractSyntax}, TransferSyntaxes={string.Join(", ", pc.GetTransferSyntaxes())}");
            }
            // 遍历客户端的 Presentation Context，接受所有支持的 Storage 服务类（C-STORE）
            foreach (var pc in association.PresentationContexts)
            {
                // 检查是否为需要支持的 SOP Class
                if (pc.AbstractSyntax == DicomUID.Verification) // Verification SOP Class
                {
                    pc.AcceptTransferSyntaxes(pc.GetTransferSyntaxes().ToArray());
                }
                else if (pc.AbstractSyntax == DicomUID.CTImageStorage) // CT Image Storage SOP Class
                {
                    pc.AcceptTransferSyntaxes(pc.GetTransferSyntaxes().ToArray());
                }
                else if (pc.AbstractSyntax.StorageCategory != Dicom.DicomStorageCategory.None) // 其他 Storage 类
                {
                    pc.AcceptTransferSyntaxes(pc.GetTransferSyntaxes().ToArray());
                }
                else
                {
                    pc.SetResult(Dicom.Network.DicomPresentationContextResult.RejectAbstractSyntaxNotSupported);
                }
            }

            // 发送关联接受响应
            await SendAssociationAcceptAsync(association);
            Console.WriteLine("[Async] Association accepted.");
        }

        /// <summary>
        /// 异步处理接收到的关联释放请求
        /// 在控制台打印关联释放状态。
        /// </summary>
        public async Task OnReceiveAssociationReleaseRequestAsync()
        {
            Console.WriteLine("Association release request received");
            // 发送关联释放响应
            await SendAssociationReleaseResponseAsync();
            Console.WriteLine("[Async] Association released.");
        }

        /// <summary>
        /// 处理接收到的 C-FIND 请求
        /// </summary>
        /// <param name="request">C-FIND 请求对象</param>
        /// <returns>返回 C-FIND 响应，指示处理结果</returns>
        public IEnumerable<DicomCFindResponse> OnCFindRequest(DicomCFindRequest request)
        {
            Console.WriteLine("Received C-FIND request");

            // 获取查询条件
            var patientName = request.Dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
            var studyDate = request.Dataset.GetSingleValueOrDefault(DicomTag.StudyDate, string.Empty);

            // 定义文件系统中的 DICOM 文件目录
            var dicomDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DICOM");

            // 查询文件系统中的 DICOM 文件
            var results = QueryDicomFiles(dicomDirectory, new Dictionary<DicomTag, string>
        {
            { DicomTag.PatientName, patientName },
            { DicomTag.StudyDate, studyDate }
        });

            // 构建并返回 C-FIND 响应
            foreach (var result in results)
            {
                yield return new DicomCFindResponse(request, DicomStatus.Pending)
                {
                    Dataset = result.Metadata
                };
            }

            // 返回完成状态
            yield return new DicomCFindResponse(request, DicomStatus.Success);
        }

        /// <summary>
        /// 从文件系统查询 DICOM 数据
        /// </summary>
        /// <param name="directoryPath">要扫描的目录路径</param>
        /// <param name="queryTags">查询条件（如 PatientName, StudyDate 等）</param>
        /// <returns>符合条件的 DICOM 文件路径和元数据</returns>
        private List<(string FilePath, DicomDataset Metadata)> QueryDicomFiles(string directoryPath, Dictionary<DicomTag, string> queryTags)
        {
            var results = new List<(string FilePath, DicomDataset Metadata)>();

            // 遍历目录中的所有文件
            foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*.dcm", SearchOption.AllDirectories))
            {
                try
                {
                    // 加载 DICOM 文件
                    var dicomFile = DicomFile.Open(filePath);

                    // 检查是否符合查询条件
                    bool isMatch = true;
                    foreach (var queryTag in queryTags)
                    {
                        var tagValue = dicomFile.Dataset.GetSingleValueOrDefault(queryTag.Key, string.Empty);
                        if (!tagValue.Contains(queryTag.Value, StringComparison.OrdinalIgnoreCase))
                        {
                            isMatch = false;
                            break;
                        }
                    }

                    // 如果符合条件，添加到结果列表
                    if (isMatch)
                    {
                        results.Add((filePath, dicomFile.Dataset));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading DICOM file {filePath}: {ex.Message}");
                }
            }

            return results;
        }
        #endregion
    }
}

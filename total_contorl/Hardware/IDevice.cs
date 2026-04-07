using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace GmnPlayer.Hardware
{
    /// <summary>
    /// 결정론적 하네스 호환성을 보장하기 위한 공통 인터페이스
    /// </summary>
    public interface IDevice : IDisposable
    {
        string DeviceId { get; }
        string IpAddress { get; }
        int Port { get; }
        bool IsConnected { get; }

        Task<bool> ConnectAsync(int timeoutMs = 3000);
        void Disconnect();
        Task<bool> SendRawBytesAsync(byte[] data);
    }

    /// <summary>
    /// TCP 통신을 담당하는 비동기 기반 하드웨어 매핑 객체
    /// UI 스레드를 차단하지 않도록 철저한 async/await로 설계 (규정 준수)
    /// </summary>
    public class TcpDevice : IDevice
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // 스레드 안전성 확보

        public string DeviceId { get; }
        public string IpAddress { get; }
        public int Port { get; }

        public bool IsConnected => _client != null && _client.Connected;

        public TcpDevice(string deviceId, string ipAddress, int port)
        {
            DeviceId = deviceId;
            IpAddress = ipAddress;
            Port = port;
        }

        /// <summary>
        /// Timeout을 유연하게 처리하여 무한 대기 현상을 막고 프리징을 차단.
        /// </summary>
        public async Task<bool> ConnectAsync(int timeoutMs = 3000)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (IsConnected) return true;

                _client?.Close();
                _client = new TcpClient();

                // 무한 대기 방어 (Timeout 구현 규칙)
                using (var cts = new CancellationTokenSource(timeoutMs))
                {
                    Task connectTask = _client.ConnectAsync(IpAddress, Port);
                    Task timeoutTask = Task.Delay(timeoutMs, cts.Token);
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                    
                    if (completedTask == timeoutTask)
                    {
                        Console.WriteLine($"[Timeout Error] 연결 시간 초과 ({timeoutMs}ms) - {DeviceId}({IpAddress}:{Port})");
                        _client.Close();
                        return false;
                    }
                    else
                    {
                        await connectTask; // 연결 지연 시 발생한 예외 포착용
                        _stream = _client.GetStream();
                        Console.WriteLine($"[System] 💡 {DeviceId} 접속 성공. ({IpAddress}:{Port})");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Connect Error] {DeviceId} 접근 불가: {ex.Message}");
                _client?.Close();
                return false; // 시스템 Crash 완전 배제
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Disconnect()
        {
            try
            {
                _stream?.Close();
                _client?.Close();
            }
            catch { /* 소멸 단계에서의 추가 예외 무시 */ }
            finally
            {
                _stream = null;
                _client = null;
                Console.WriteLine($"[System] 🔌 {DeviceId} 연결 종료됨.");
            }
        }

        /// <summary>
        /// 하네스 검증 시 타임아웃 발생에도 안전하게 동작하는 핵심 재시도 로직(Retry Logic)
        /// 최대 3번 시도하며, 지속 실패 시 Exception을 유발하지 않고 false 반환 후 에러 보고
        /// </summary>
        public async Task<bool> SendRawBytesAsync(byte[] data)
        {
            int maxRetries = 3;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    if (!IsConnected)
                    {
                        bool isNowConnected = await ConnectAsync(2000);
                        if (!isNowConnected) throw new TimeoutException("장비에 연결하지 못했거나 응답이 없습니다.");
                    }

                    // 비동기 전송: UI 스레드 개입을 0%로 강제
                    using (var cts = new CancellationTokenSource(2000))
                    {
                        await _stream!.WriteAsync(data, 0, data.Length, cts.Token);
                    }
                    return true; // 성공 시퀀스
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Send Error - 강제 복구 시도 {i + 1}/{maxRetries}] {DeviceId}: {ex.Message}");
                    Disconnect(); // 소켓 등 찌꺼기를 날리고 다음 루프에서 재생성하도록 유도

                    if (i == maxRetries - 1)
                    {
                        Console.WriteLine($"[Critical Error] {DeviceId}와 물리적 통신이 단절되었습니다.");
                        return false;
                    }
                    await Task.Delay(1000); // 1초 간격 재시도
                }
            }
            return false;
        }

        public void Dispose()
        {
            Disconnect();
            _semaphore.Dispose(); // 언마네지드 스레드 제어기 해제 (메모리 규정 준수)
        }
    }
}

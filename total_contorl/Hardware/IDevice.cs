using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GmnPlayer.Managers;

namespace GmnPlayer.Hardware
{
    public class DeviceStateEventArgs : EventArgs
    {
        public string DeviceId { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 결정론적 하네스 호환성을 보장하기 위한 공통 인터페이스
    /// </summary>
    public interface IDevice : IDisposable
    {
        string DeviceId { get; }
        string IpAddress { get; }
        int Port { get; }
        bool IsConnected { get; }

        event EventHandler<DeviceStateEventArgs>? StateChanged;

        Task<bool> ConnectAsync(int timeoutMs = 3000);
        void Disconnect();
        
        // IProtocolStrategy에 맞는 패킷을 생성해 전송
        Task<bool> SendCommandAsync(string command, object? parameters = null);
        
        // 물리적 식별(LED 점멸 등)
        Task IdentifyAsync();
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
        private readonly IProtocolStrategy _strategy;

        public string DeviceId { get; }
        public string IpAddress { get; }
        public int Port { get; }
        public bool IsConnected => _client != null && _client.Connected;

        public event EventHandler<DeviceStateEventArgs>? StateChanged;

        public TcpDevice(string deviceId, string ipAddress, int port, IProtocolStrategy strategy)
        {
            DeviceId = deviceId;
            IpAddress = ipAddress;
            Port = port;
            _strategy = strategy;
        }

        private void OnStateChanged(bool isConnected, string message)
        {
            StateChanged?.Invoke(this, new DeviceStateEventArgs
            {
                DeviceId = this.DeviceId,
                IsConnected = isConnected,
                Message = message
            });
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

                using (var cts = new CancellationTokenSource(timeoutMs))
                {
                    Task connectTask = _client.ConnectAsync(IpAddress, Port);
                    Task timeoutTask = Task.Delay(timeoutMs, cts.Token);
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                    
                    if (completedTask == timeoutTask)
                    {
                        SystemLogger.Error(LogCategory.NETWORK, $"Connection Timeout ({timeoutMs}ms) - {DeviceId}({IpAddress}:{Port})");
                        _client.Close();
                        OnStateChanged(false, "Timeout");
                        return false;
                    }
                    else
                    {
                        await connectTask; 
                        _stream = _client.GetStream();
                        SystemLogger.Info(LogCategory.NETWORK, $"💡 {DeviceId} 접속 성공. ({IpAddress}:{Port})");
                        OnStateChanged(true, "Connected");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                SystemLogger.Error(LogCategory.NETWORK, $"{DeviceId} 접근 불가", ex);
                _client?.Close();
                OnStateChanged(false, ex.Message);
                return false; 
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Disconnect()
        {
            if (!IsConnected) return;
            try
            {
                _stream?.Close();
                _client?.Close();
            }
            catch { /* 무시 */ }
            finally
            {
                _stream = null;
                _client = null;
                SystemLogger.Info(LogCategory.NETWORK, $"🔌 {DeviceId} 연결 종료됨.");
                OnStateChanged(false, "Disconnected");
            }
        }

        /// <summary>
        /// 전략(Strategy)을 이용하여 명령을 패킷 단위 변환한 뒤 전송합니다.
        /// 최대 3번 시도
        /// </summary>
        public async Task<bool> SendCommandAsync(string command, object? parameters = null)
        {
            // 인코딩 전략 적용
            byte[] packet = _strategy.Encode(command, parameters);

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

                    // 전송 전 로깅 (Raw vs String 식별 위함)
                    SystemLogger.TxPacketRaw(LogCategory.DID, DeviceId, IpAddress, Port, packet);

                    using (var cts = new CancellationTokenSource(2000))
                    {
                        await _stream!.WriteAsync(packet, 0, packet.Length, cts.Token);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    SystemLogger.Warning(LogCategory.NETWORK, $"[Send Error - 복구 시도 {i + 1}/{maxRetries}] {DeviceId}: {ex.Message}");
                    Disconnect();

                    if (i == maxRetries - 1)
                    {
                        SystemLogger.Alert(LogCategory.NETWORK, $"[Critical Error] {DeviceId} 통신 단절.");
                        return false; // Rollback 정책 없음
                    }
                    await Task.Delay(1000);
                }
            }
            return false;
        }

        public async Task IdentifyAsync()
        {
            SystemLogger.Info(LogCategory.SYSTEM, $"[Device] {DeviceId} Identify Triggered!");
            await SendCommandAsync("IDENTIFY_CMD");
        }

        public void Dispose()
        {
            Disconnect();
            _semaphore.Dispose();
        }
    }
}

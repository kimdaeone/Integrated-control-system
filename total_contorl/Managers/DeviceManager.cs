using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using GmnPlayer.Hardware;
using GmnPlayer.Models;

namespace GmnPlayer.Managers
{
    /// <summary>
    /// 하드웨어 디바이스를 생성하고 생명주기를 관장하며,
    /// 라우팅된 명령 중계 책임을 가지는 싱글톤 매니저 클래스.
    /// Phase 2: 의존성 주입 (전략 패턴) 및 Observer Pattern 연동
    /// </summary>
    public class DeviceManager
    {
        // Thread-Safe Lazy Singleton
        private static readonly Lazy<DeviceManager> _instance = new Lazy<DeviceManager>(() => new DeviceManager());
        public static DeviceManager Instance => _instance.Value;

        // 동시 데이터 접근에 따른 CollectionModified 크래시 방어
        private readonly ConcurrentDictionary<string, IDevice> _devices = new ConcurrentDictionary<string, IDevice>();

        private DeviceManager() { }

        /// <summary>
        /// ProjectConfig 안의 DeviceNodes를 스캔하여 모든 장비 인스턴스를 미리 구성합니다.
        /// </summary>
        public void InitializeFromConfig(ProjectConfig config)
        {
            // 오염 방지: 기존 객체 전부 메모리누수 없이 해제
            foreach (var dev in _devices.Values)
            {
                if (dev != null)
                {
                    dev.StateChanged -= TargetDevice_StateChanged;
                    dev.Dispose();
                }
            }
            _devices.Clear();

            foreach (var node in config.DeviceNodes)
            {
                // 프로토콜 전략 식별 및 인스턴스화
                IProtocolStrategy strategy = node.ProtocolStrategy?.ToUpper() switch
                {
                    "AVCIT" => new AvcitStrategy(),
                    "RS232" => new Rs232Strategy(),
                    _ => new Rs232Strategy() // 기본값
                };

                // 장비에 전략 주입
                var newDevice = new TcpDevice(node.DeviceId, node.IpAddress, node.Port, strategy);
                
                // Observer 연동 (이벤트 구독)
                newDevice.StateChanged += TargetDevice_StateChanged;

                _devices[node.DeviceId] = newDevice;
            }
            
            SystemLogger.Info(LogCategory.SYSTEM, $"[DeviceManager] 가상 장비 {config.DeviceNodes.Count}대 전략 주입 및 준비 완료.");
        }

        private void TargetDevice_StateChanged(object? sender, DeviceStateEventArgs e)
        {
            // 향후 UI 위젯(Grid Cell 등)에 🔴 상태등을 켜기 위한 연동 이벤트 포인트
            LogCategory logCat = e.IsConnected ? LogCategory.NETWORK : LogCategory.SYSTEM;
            
            if (e.IsConnected)
                SystemLogger.Info(logCat, $"Device [{e.DeviceId}] State: CONNECTED");
            else
                SystemLogger.Warning(logCat, $"Device [{e.DeviceId}] State: DISCONNECTED/ERROR ({e.Message})");
        }

        /// <summary>
        /// UI 터치 액션 발생 시 호출되는 중계 지점 
        /// </summary>
        public async Task<bool> SendCommandAsync(string deviceId, string command)
        {
            if (string.IsNullOrWhiteSpace(deviceId) || !_devices.TryGetValue(deviceId, out var targetDevice))
            {
                SystemLogger.Warning(LogCategory.SYSTEM, $"[Routing Error] Device Not Found: ID가 '{deviceId}'인 장비를 찾을 수 없으므로 명령('{command}') 전송 루틴을 스킵합니다.");
                return false;
            }

            SystemLogger.Info(LogCategory.SYSTEM, $"🚀 Routing [{command}] -> Device [{deviceId}]");

            // 전략 패턴을 채택한 SendCommandAsync 호출
            bool result = await targetDevice.SendCommandAsync(command);
            
            if (!result)
            {
                SystemLogger.Warning(LogCategory.DID, $"[DeviceManager] 최종 통신 실패. 장비[{deviceId}]가 응답하지 않습니다. (알람 연동 포인트)");
            }
            return result;
        }

        public async Task IdentifyDeviceAsync(string deviceId)
        {
            if (_devices.TryGetValue(deviceId, out var targetDevice))
            {
                await targetDevice.IdentifyAsync();
            }
        }
    }
}

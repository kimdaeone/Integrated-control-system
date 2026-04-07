using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using GmnPlayer.Hardware;
using GmnPlayer.Models;

namespace GmnPlayer.Managers
{
    /// <summary>
    /// 하드웨어 디바이스를 생성하고 생명주기를 관장하며,
    /// 라우팅된 명령 중계 책임을 가지는 싱글톤 매니저 클래스.
    /// </summary>
    public class DeviceManager
    {
        // Thread-Safe Lazy Singleton
        private static readonly Lazy<DeviceManager> _instance = new Lazy<DeviceManager>(() => new DeviceManager());
        public static DeviceManager Instance => _instance.Value;

        // 동시 데이터 접근에 따른 CollectionModified 크래시 방어 (ConcurrentDictionary 패러다임)
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
                dev.Dispose();
            }
            _devices.Clear();

            foreach (var node in config.DeviceNodes)
            {
                var newDevice = new TcpDevice(node.DeviceId, node.IpAddress, node.Port);
                _devices[node.DeviceId] = newDevice;
            }
            
            Console.WriteLine($"[DeviceManager] 가상 장비 {config.DeviceNodes.Count}대 준비 완료.");
        }

        /// <summary>
        /// UI 터치 액션 발생 시 호출되는 중계 지점 
        /// </summary>
        public async Task SendCommandAsync(string deviceId, string command)
        {
            if (string.IsNullOrWhiteSpace(deviceId) || !_devices.TryGetValue(deviceId, out var targetDevice))
            {
                Console.WriteLine($"[Routing Error] Device Not Found: ID가 '{deviceId}'인 장비를 찾을 수 없으므로 명령('{command}') 전송 루틴을 스킵합니다.");
                return;
            }

            Console.WriteLine($"🚀 Sending [{command}] to [{targetDevice.IpAddress}:{targetDevice.Port}]...");

            // 바이트/엔디안/체크섬 로직은 이 공간 혹은 SendRawBytesAsync 안쪽에서 결정론적으로 연산 될 수 있음.
            byte[] commandBytes = Encoding.UTF8.GetBytes(command + "\r\n");
            
            // Fire And Await (UI 스레드 완전 독립)
            bool result = await targetDevice.SendRawBytesAsync(commandBytes);
            
            if (!result)
            {
                Console.WriteLine($"[DeviceManager] 최종 통신 실패. 장비[{deviceId}]가 응답하지 않습니다. (알람 연동 포인트)");
            }
        }
    }
}

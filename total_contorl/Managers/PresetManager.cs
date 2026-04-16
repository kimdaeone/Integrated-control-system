using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GmnPlayer.Models;

namespace GmnPlayer.Managers
{
    public class PresetManager
    {
        private static readonly Lazy<PresetManager> _instance = new Lazy<PresetManager>(() => new PresetManager());
        public static PresetManager Instance => _instance.Value;

        private PresetManager() { }

        public async Task ExecutePresetAsync(PresetState preset)
        {
            SystemLogger.Info(LogCategory.SYSTEM, $"=== Executing Preset: {preset.Name} ===");

            var tasks = new List<Task>();

            // 장비별 명령어 동시 비동기 전송 (Task.WhenAll 연동)
            foreach (var kvp in preset.DeviceCommands)
            {
                tasks.Add(SendToDevice(kvp.Key, kvp.Value));
            }

            // 그리드 매핑 명령어 전송
            foreach (var kvp in preset.GridMappings)
            {
                string[] parts = kvp.Key.Split('_'); 
                if (parts.Length >= 3)
                {
                    string r = parts[1];
                    string c = parts[2];
                    string sourceId = kvp.Value;
                    string command = $"MUX_{sourceId}_R{r}_C{c}";
                    
                    // 그리드 연동 대상 기기 (DEV_01) 하드코딩 혹은 config 참고
                    tasks.Add(SendToDevice("DEV_01", command)); 
                }
            }

            // 모든 하드웨어 명령을 동시에 원자적으로 발사하고 대기합니다.
            await Task.WhenAll(tasks);
            SystemLogger.Info(LogCategory.SYSTEM, $"=== Preset {preset.Name} Execution Complete ===");
        }

        private async Task SendToDevice(string deviceId, string command)
        {
            try
            {
                bool success = await DeviceManager.Instance.SendCommandAsync(deviceId, command);
                if (!success)
                {
                    // 일부 실패 시에도 롤백 없이 에러 로그만 치명적 알림(Alert)으로 보고 (No Rollback 원칙)
                    SystemLogger.Alert(LogCategory.SYSTEM, $"Preset Atomicity Failure: [{deviceId}] failed to execute [{command}]. System will NOT rollback.");
                }
            }
            catch (Exception ex)
            {
                SystemLogger.Error(LogCategory.SYSTEM, $"Preset error on {deviceId}", ex);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GmnPlayer.Models
{
    /// <summary>
    /// 시스템 전반의 프로젝트 설정 모델
    /// </summary>
    public class ProjectConfig
    {
        /// <summary>
        /// 프로젝트의 고유 ID
        /// </summary>
        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; } = "default_project";

        /// <summary>
        /// 프로젝트에 속한 기기 노드들의 리스트
        /// </summary>
        [JsonPropertyName("deviceNodes")]
        public List<DeviceNode> DeviceNodes { get; set; } = new List<DeviceNode>();

        /// <summary>
        /// 화면에 배치될 UI 컨트롤들의 리스트
        /// </summary>
        [JsonPropertyName("uiControls")]
        public List<UIControl> UIControls { get; set; } = new List<UIControl>();
        
        /// <summary>
        /// 배경 이미지 경로
        /// </summary>
        [JsonPropertyName("backgroundImagePath")]
        public string BackgroundImagePath { get; set; } = string.Empty;
    }

    /// <summary>
    /// 하네스 기반 통신 및 제어 기기 모델. IDevice 호환 목적 설계.
    /// </summary>
    public class DeviceNode
    {
        /// <summary>
        /// 기기 ID
        /// </summary>
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = "unknown_device";

        /// <summary>
        /// 기기 IP 주소
        /// </summary>
        [JsonPropertyName("ipAddress")]
        public string IpAddress { get; set; } = "127.0.0.1";

        /// <summary>
        /// 접속 포트 번호. 잘못된 값이 들어오지 않도록 방어.
        /// </summary>
        private int _port = 8080;
        [JsonPropertyName("port")]
        public int Port
        {
            get => _port;
            set => _port = (value > 0 && value <= 65535) ? value : 8080;
        }
    }

    /// <summary>
    /// 화면에 그려질 동적 UI 컨트롤의 모델 클래스
    /// 음수나 논리적으로 맞지 않는 좌표값 방어를 위한 Get/Set 적용
    /// </summary>
    public class UIControl
    {
        /// <summary>
        /// 컨트롤의 고유 식별자
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        private int _x;
        /// <summary>
        /// X 좌표 (항상 0 이상)
        /// 방어적 코딩: 음수 입력 시 기본값 0으로 복구하여 Crash 방지
        /// </summary>
        [JsonPropertyName("x")]
        public int X
        {
            get => _x;
            set => _x = Math.Max(0, value);
        }

        private int _y;
        /// <summary>
        /// Y 좌표 (항상 0 이상)
        /// 방어적 코딩: 음수 입력 시 기본값 0으로 복구하여 Crash 방지
        /// </summary>
        [JsonPropertyName("y")]
        public int Y
        {
            get => _y;
            set => _y = Math.Max(0, value);
        }

        private int _w = 100;
        /// <summary>
        /// 너비 (최소 크기 10 보장)
        /// 방어적 코딩: 비정상적인 작은 값 또는 음수 입력 시 최소 10 유지
        /// </summary>
        [JsonPropertyName("w")]
        public int W
        {
            get => _w;
            set => _w = Math.Max(10, value);
        }

        private int _h = 100;
        /// <summary>
        /// 높이 (최소 크기 10 보장)
        /// 방어적 코딩: 비정상적인 작은 값 또는 음수 입력 시 최소 10 유지
        /// </summary>
        [JsonPropertyName("h")]
        public int H
        {
            get => _h;
            set => _h = Math.Max(10, value);
        }

        /// <summary>
        /// 명령을 수신할 제어 장비의 아이디 (ID)
        /// </summary>
        [JsonPropertyName("targetDeviceId")]
        public string TargetDeviceId { get; set; } = string.Empty;

        /// <summary>
        /// 해당 컨트롤과 매핑된 제어 명령 (예: "POWER_ON", "VOLUME_UP")
        /// </summary>
        [JsonPropertyName("command")]
        public string Command { get; set; } = string.Empty;

        /// <summary>
        /// 화면 상의 레이어 높이 (Z-Index)
        /// </summary>
        [JsonPropertyName("zIndex")]
        public int ZIndex { get; set; } = 0;

        /// <summary>
        /// 패널 객체를 생성하지 않고 Paint에서 가이드라인/배경으로 렌더링될 정적 요소 여부
        /// </summary>
        [JsonPropertyName("isStatic")]
        public bool IsStatic { get; set; } = false;
    }
}

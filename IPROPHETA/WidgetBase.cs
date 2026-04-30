// 위젯의 부모
// 센서 장비 등의 연동을 위한 클래스 상속 목적의 공간.

using System;
using System.Drawing;
using System.ComponentModel;

namespace IPROPHETA.Core.Models
{
    /// <summary>
    /// 모든 UI 구성 요소의 기본이 되는 추상 클래스입니다.
    /// </summary>
    public abstract class WidgetBase // 상속 받아 draw 메소드를 구현해야 함!
    {
        [Category("Identity"), Description("위젯의 고유 식별자입니다.")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Category("Identity"), Description("트리뷰에 표시될 위젯의 이름입니다.")]
        public string Name { get; set; } = "New Widget";

        [Category("Layout"), Description("위젯의 위치와 크기입니다.")]
        public Rectangle Bounds { get; set; }

        [Category("Layout"), Description("위젯의 겹침 순서입니다. 클수록 위로 올라옵니다.")]
        public int ZIndex { get; set; }

        [Category("Hardware"), Description("연결된 대상 하드웨어의 ID입니다.")]
        public string TargetDeviceId { get; set; }

        [Category("Hardware"), Description("제어 시 송출할 Hex 명령어입니다.")]
        public string ControlCommand { get; set; }

        // 디자이너/플레이어 모드에서 각자 다르게 구현할 렌더링 메서드
        public abstract void Draw(Graphics g, EngineMode mode);
    }
}
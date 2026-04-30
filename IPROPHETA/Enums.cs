// 시스템 상태 및 유형 정의
// 프로그램 전체에서 공통으로 사용할 열거형 데이터입니다. 모드 전환과 위젯 유형을 명확히 구분합니다. 
// 새로운 타입의 장비 이름 추가

namespace IPROPHETA.Core
{
    /// <summary>
    /// 엔진의 현재 작동 모드를 정의합니다.
    /// </summary>
    public enum EngineMode
    {
        Designer, // 제작 툴 모드 (UI 편집 가능)
        Player    // 실행기 모드 (제어 및 모니터링 전용)
    }

    /// <summary>
    /// 위젯의 종류를 정의합니다. 
    /// </summary>
    public enum WidgetType
    {
        Button,     // 제어 버튼
        Display,    // 상태 표시기 (LED 등)
        Sensor,     // 센서 데이터 뷰어
        VideoWall   // 영상 제어 영역
    }

    /// <summary>
    /// 리사이징 시 마우스가 잡고 있는 핸들의 위치입니다.
    /// </summary>
    public enum AnchorType
    {
        None, TopLeft, TopCenter, TopRight,
        MiddleLeft, MiddleRight,
        BottomLeft, BottomCenter, BottomRight
    }
}
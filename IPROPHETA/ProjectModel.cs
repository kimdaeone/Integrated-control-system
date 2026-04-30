// 프로젝트 컨테이너
// 모든 위젯 정보와 캔버스 설정, 프로젝트 메타데이터를 한 곳에 모으는 중심 클래스 입니다.

using System;
using System.Collections.Generic;
using System.Drawing;

namespace IPROPHETA.Core.Models
{
    /// <summary>
    /// 전체 프로젝트의 데이터 구조를 정의하며, 직렬화의 루트 대상이 됩니다.
    /// </summary>
    public class ProjectModel
    {
        // 프로젝트 기본 정보
        public string ProjectName { get; set; } = "New Control Project";
        public DateTime LastModified { get; set; } = DateTime.Now;

        // 캔버스 설정 (현장의 디스플레이 환경에 맞춤)
        public Size Resolution { get; set; } = new Size(1920, 1080);
        
        // 배경 설정
        public string BackgroundImagePath { get; set; }
        public Color BackgroundColor { get; set; } = Color.FromArgb(45, 45, 48);

        // 자산 관리 경로 (하네스 지침: 상대 경로 유지)
        public string AssetsFolder { get; set; } = ".assets";

        /// <summary>
        /// 프로젝트에 포함된 모든 위젯의 리스트입니다.
        /// 이 리스트는 추상 클래스인 WidgetBase를 담으므로, 다형성 직렬화 처리가 필요합니다.
        /// </summary>
        public List<WidgetBase> Widgets { get; set; } = new List<WidgetBase>();

        /// <summary>
        /// 새로운 위젯을 안전하게 추가하기 위한 헬퍼 메서드
        /// </summary>
        public void AddWidget(WidgetBase widget)
        {
            if (widget == null) return;
            
            // Z-Index 자동 할당 (가장 위에 배치)
            int maxZ = 0;
            if (Widgets.Count > 0)
            {
                foreach(var w in Widgets)
                {
                    if (w.ZIndex > maxZ) maxZ = w.ZIndex;
                }
            }
            widget.ZIndex = maxZ + 1;
            
            Widgets.Add(widget);
            LastModified = DateTime.Now;
        }
    }
}
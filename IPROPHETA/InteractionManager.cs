//

using System;
using System.Drawing;
using System.Windows.Forms;
using IPROPHETA.Core.Models;
using IPROPHETA.Services;

namespace IPROPHETA.UI
{
    /// <summary>
    /// 마우스 입력을 받아 위젯의 선택, 이동, 리사이징을 제어합니다.
    /// </summary>
    public class InteractionManager
    {
        private readonly LayoutEngine _engine;
        private WidgetBase _selectedWidget;
        private Point _lastMousePos;
        private bool _isDragging = false;

        public InteractionManager(LayoutEngine engine, Control canvas)
        {
            _engine = engine;
            
            // 캔버스의 마우스 이벤트를 구독합니다.
            canvas.MouseDown += OnMouseDown;
            canvas.MouseMove += OnMouseMove;
            canvas.MouseUp += OnMouseUp;
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (_engine.CurrentMode != Core.EngineMode.Designer) return;

            // 1. 클릭한 위치에 위젯이 있는지 탐색 (Z-Index 역순으로 찾기)
            var project = ProjectManager.Instance.CurrentProject;
            _selectedWidget = null;

            for (int i = project.Widgets.Count - 1; i >= 0; i--)
            {
                if (project.Widgets[i].Bounds.Contains(e.Location))
                {
                    _selectedWidget = project.Widgets[i];
                    break;
                }
            }

            if (_selectedWidget != null)
            {
                _isDragging = true;
                _lastMousePos = e.Location;
                
                // [Harness] 변경 시작 전 히스토리 저장 (Undo용)
                ProjectManager.Instance.SaveHistory();
            }
            
            _engine.Refresh();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || _selectedWidget == null) return;

            // 2. 이동 거리 계산
            int dx = e.X - _lastMousePos.X;
            int dy = e.Y - _lastMousePos.Y;

            // 3. 새로운 위치 계산 (자석 격자 적용)
            // 현재 위치에 이동량을 더한 뒤, LayoutEngine.Snap을 통과시켜 5px 단위로 보정합니다.
            int newX = LayoutEngine.Snap(_selectedWidget.Bounds.X + dx);
            int newY = LayoutEngine.Snap(_selectedWidget.Bounds.Y + dy);

            // 4. 위젯 좌표 갱신
            _selectedWidget.Bounds = new Rectangle(newX, newY, _selectedWidget.Bounds.Width, _selectedWidget.Bounds.Height);
            
            _lastMousePos = e.Location;
            _engine.Refresh(); // 화면 즉시 갱신
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            _isDragging = false;
        }
    }
}
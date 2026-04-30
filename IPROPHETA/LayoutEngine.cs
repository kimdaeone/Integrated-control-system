using System;
using System.Drawing;
using System.Windows.Forms;
using IPROPHETA.Core;
using IPROPHETA.Services;
using System.Linq;

namespace IPROPHETA.UI
{
    public class LayoutEngine
    {
        private readonly Control _canvas;
        private const int GridSize = 5; // [지휘관의 규칙] 5px 보이지 않는 격자
        public EngineMode CurrentMode { get; set; } = EngineMode.Designer;

        public LayoutEngine(Control canvas)
        {
            _canvas = canvas;
            _canvas.Paint += OnCanvasPaint;
            
            // 더블 버퍼링 활성화 (화면 깜빡임 방지)
            var method = typeof(Control).GetMethod("SetStyle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_canvas, new object[] { ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true });
        }

        /// <summary>
        /// [핵심 로직] 입력된 좌표를 가장 가까운 5px 격자점으로 자석처럼 붙입니다.
        /// </summary>
        public static int Snap(int value)
        {
            // 수학적 원리: (value / 5)를 반올림한 뒤 다시 5를 곱함
            return (int)(Math.Round((double)value / GridSize) * GridSize);
        }

        /// <summary>
        /// [핵심 로직] 사각형 영역 전체를 격자에 맞게 보정합니다.
        /// </summary>
        public static Rectangle SnapRectangle(Rectangle rect)
        {
            return new Rectangle(Snap(rect.X), Snap(rect.Y), Snap(rect.Width), Snap(rect.Height));
        }

        public void Refresh() => _canvas.Invalidate();

        private void OnCanvasPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            var project = ProjectManager.Instance.CurrentProject;

            // 1. 배경 처리
            g.Clear(project.BackgroundColor);

            // 2. 격자 그리기 (디자인 모드에서만 매우 연하게 표시)
            if (CurrentMode == EngineMode.Designer)
                DrawSubtleGrid(g);

            // 3. 위젯 렌더링 (Z-Index 순으로 정렬)
            foreach (var widget in project.Widgets.OrderBy(w => w.ZIndex))
            {
                widget.Draw(g, CurrentMode);
            }
        }

        private void DrawSubtleGrid(Graphics g)
        {
            // 지휘관님의 아이디어를 반영하여 '보이지 않는 듯한' 촘촘한 점선 격자
            using (Pen dotPen = new Pen(Color.FromArgb(50, 50, 50), 1))
            {
                for (int x = 0; x < _canvas.Width; x += GridSize * 4) // 20px 간격으로 주요 가이드
                {
                    for (int y = 0; y < _canvas.Height; y += GridSize * 4)
                    {
                        g.DrawRectangle(dotPen, x, y, 1, 1); 
                    }
                }
            }
        }
    }
}
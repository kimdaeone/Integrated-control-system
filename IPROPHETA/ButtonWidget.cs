using System.Drawing;
using IPROPHETA.Core;
using IPROPHETA.Core.Models;

namespace IPROPHETA.UI.Widgets
{
    public class ButtonWidget : WidgetBase
    {
        public ButtonWidget()
        {
            // 기본 크기 설정 (격자 5px의 배수인 100x50)
            Bounds = new Rectangle(0, 0, 100, 50);
            Name = "New Button";
        }

        public override void Draw(Graphics g, EngineMode mode)
        {
            // 1. 배경 및 테두리 설정
            Color bodyColor = mode == EngineMode.Designer ? Color.FromArgb(70, 70, 70) : Color.SteelBlue;
            using (Brush b = new SolidBrush(bodyColor))
            using (Pen p = new Pen(Color.White, 2))
            {
                g.FillRectangle(b, Bounds);
                g.DrawRectangle(p, Bounds);
            }

            // 2. 위젯 이름 출력 (중앙 정렬)
            string displayTag = mode == EngineMode.Designer ? $"[BTN] {Name}" : Name;
            using (Font f = new Font("Malgun Gothic", 9, FontStyle.Bold))
            {
                Size textSize = TextRenderer.MeasureText(displayTag, f);
                float tx = Bounds.X + (Bounds.Width - textSize.Width) / 2f;
                float ty = Bounds.Y + (Bounds.Height - textSize.Height) / 2f;
                g.DrawString(displayTag, f, Brushes.White, tx, ty);
            }

            // 3. 디자인 모드 전용 가이드 (선택되었을 때의 강조 등은 나중에 추가)
            if (mode == EngineMode.Designer)
            {
                using (Pen guidePen = new Pen(Color.Cyan, 1))
                {
                    guidePen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    g.DrawRectangle(guidePen, Bounds);
                }
            }
        }
    }
}
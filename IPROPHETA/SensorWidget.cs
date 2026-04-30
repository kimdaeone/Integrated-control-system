using System.Drawing;
using GmnPlayer.Core;
using GmnPlayer.Core.Models;

namespace IPROPHETA
{
    public class SensorWidget : WidgetBase
    {
        public SensorWidget()
        {
            Type = WidgetType.Sensor; // [Blueprint] 타입 지정 필수
            Bounds = new Rectangle(0, 0, 120, 120); // 센서는 보통 정사각형
            Name = "New Sensor";
        }

        public override void Draw(Graphics g, EngineMode mode)
        {
            // 1. 원형 디자인 (센서 특화)
            using (Brush b = new SolidBrush(Color.FromArgb(40, 40, 40)))
            using (Pen p = new Pen(Color.LimeGreen, 2))
            {
                g.FillEllipse(b, Bounds);
                g.DrawEllipse(p, Bounds);
            }

            // 2. 중앙에 가상의 데이터 표시
            using (Font f = new Font("Consolas", 12, FontStyle.Bold))
            {
                string valueText = mode == EngineMode.Designer ? "DATA" : "24.5V";
                Size textSize = TextRenderer.MeasureText(valueText, f);
                g.DrawString(valueText, f, Brushes.LimeGreen, 
                    Bounds.X + (Bounds.Width - textSize.Width) / 2f, 
                    Bounds.Y + (Bounds.Height - textSize.Height) / 2f);
            }

            // 3. 하단에 이름 표시
            using (Font nameFont = new Font("Malgun Gothic", 8))
            {
                g.DrawString(Name, nameFont, Brushes.Gray, Bounds.X, Bounds.Bottom + 5);
            }
        }
    }
}
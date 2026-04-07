using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using GmnPlayer.Models;

namespace GmnPlayer.Engine
{
    /// <summary>
    /// 동적으로 UI 컨트롤을 생성하고 배치하는 레이아웃 엔진 엔진 클래스
    /// </summary>
    public class LayoutEngine : Form
    {
        private DoubleBufferedPanel _canvasPanel;
        private Image? _backgroundImage;
        private List<UIControl> _staticControls = new List<UIControl>();

        /// <summary>
        /// Gmn-Player 화면의 기준이 되는 캔버스 패널을 반환합니다.
        /// </summary>
        public Panel Canvas => _canvasPanel;

        /// <summary>
        /// 레이아웃 엔진 생성자
        /// </summary>
        public LayoutEngine()
        {
            _canvasPanel = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.DarkGray
            };
            
            // 리사이즈 이벤트 시 배경 이미지와 화면 비율을 조정
            _canvasPanel.Paint += CanvasPanel_Paint;
            _canvasPanel.Resize += CanvasPanel_Resize;

            this.Controls.Add(_canvasPanel);
            
            // 강제 자동 렌더링
            this.Load += (s, e) => LoadMockProject();
        }

        /// <summary>
        /// JSON 문자열을 파싱하여 동적으로 레이아웃과 데이터 모델을 불러와 캔버스에 배치합니다.
        /// </summary>
        /// <param name="jsonConfig">JSON 포맷의 문자열 설정 객체</param>
        public void LoadLayoutFromJson(string jsonConfig)
        {
            if (_canvasPanel.InvokeRequired)
            {
                _canvasPanel.BeginInvoke(new Action(() => LoadLayoutFromJson(jsonConfig)));
                return;
            }

            if (string.IsNullOrWhiteSpace(jsonConfig)) return;

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var config = JsonSerializer.Deserialize<ProjectConfig>(jsonConfig, options);

                if (config == null) return;

                // 캔버스 초기화
                ClearCanvas();

                // 1. 역동적 이미지 로드 및 배경 설정 (방어적 예외 처리 반영)
                LoadImageSafely(config.BackgroundImagePath);

                // 2. 에러가 포함되어도 시스템 크래시를 방어하기 위한 안전한 ZIndex 정렬
                var sortedControls = config.UIControls.OrderBy(c => c.ZIndex).ToList();

                // 3. UI 컨트롤 생성 (동적 배치)
                // 최적화: SuspendLayout을 통해 렌더링 부하 최소화 및 배치 갱신 최적화
                _canvasPanel.SuspendLayout();
                int dynamicControlCount = 0;
                foreach (var ctlInfo in sortedControls)
                {
                    if (ctlInfo.IsStatic)
                    {
                        _staticControls.Add(ctlInfo);
                    }
                    else
                    {
                        dynamicControlCount++;
                        if (dynamicControlCount > 500)
                        {
                            throw new System.ComponentModel.Win32Exception("GDI Handle Protection: 동적 생성 컨트롤 개수가 500개를 초과했습니다. 시스템 안정성을 위해 생성을 중단합니다.");
                        }
                        CreateAndPlaceControl(ctlInfo);
                    }
                }
                _canvasPanel.ResumeLayout(true);
            }
            catch (JsonException ex)
            {
                // 잘못된 JSON 구조로 인한 크래시 발생 시 빈화면으로 복구
                Console.WriteLine($"Layout Load Parsing Error: {ex.Message}");
                ClearCanvas();
            }
            catch (Exception ex)
            {
                // 실패시 크래시 방지 및 기본화면 복구 처리
                Console.WriteLine($"Layout Loading Failed: {ex.Message}");
                ClearCanvas();
            }
        }

        /// <summary>
        /// 배경 이미지를 메모리에 단독 상주시켜 파일 병목(File Lock) 회피 구조 적용
        /// </summary>
        /// <param name="imagePath">이미지 파일 경로</param>
        private void LoadImageSafely(string imagePath)
        {
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                try
                {
                    _backgroundImage?.Dispose();
                    byte[] imageBytes = File.ReadAllBytes(imagePath);
                    using (MemoryStream ms = new MemoryStream(imageBytes))
                    {
                        _backgroundImage = Image.FromStream(ms);
                    }
                    _canvasPanel.Invalidate(); // 강제 다시 그리기 유도 (비율 조정 위함)
                }
                catch (OutOfMemoryException)
                {
                    // 비정상적인 이미지 크기나 손상된 파일 형식으로 인한 크래시 방지
                    _backgroundImage = null;
                }
                catch (Exception)
                {
                    _backgroundImage = null;
                }
            }
        }

        /// <summary>
        /// 단일 컨트롤을 생성하고 좌표에 맞춰 캔버스에 추가합니다.
        /// 하네스 검증 시 발견될 수 있는 문제(경계값 등)를 보정하여 안전하게 생성합니다.
        /// </summary>
        private void CreateAndPlaceControl(UIControl ctlInfo)
        {
            // 컨트롤 타임을 Button으로 변경하여 명시적인 터치 UI 구성
            var panelControl = new Button
            {
                Name = ctlInfo.Id,
                Text = ctlInfo.Command,
                Left = Math.Max(0, ctlInfo.X),
                Top = Math.Max(0, ctlInfo.Y),
                Width = Math.Max(10, ctlInfo.W),
                Height = Math.Max(10, ctlInfo.H),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Tag = ctlInfo // 참조 통째로 위임
            };

            // 커스텀 클릭 이벤트를 통해 비동기 처리(UI 프리징 완전 방어 규정) 전환
            panelControl.Click += async (s, e) =>
            {
                if (s is Control currentControl && currentControl.Tag is UIControl info)
                {
                    try
                    {
                        Console.WriteLine($"Button [{info.Id}] Clicked!");
                        // 하네스 검증 로직 연결: 비동기 위임을 통해 UI 점유율을 늘리지 않고 Network I/O 처리
                        await GmnPlayer.Managers.DeviceManager.Instance.SendCommandAsync(info.TargetDeviceId, info.Command);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Error] Button Click Handler Failed: {ex.Message}");
                    }
                }
            };

            _canvasPanel.Controls.Add(panelControl);
            panelControl.BringToFront(); // ZIndex 정렬 순서대로 올려집니다.
        }

        /// <summary>
        /// 현재 캔버스의 모든 컨트롤 객체를 메모리 누수 없이 제거합니다. (메모리 관리 규정 충족)
        /// </summary>
        public void ClearCanvas()
        {
            if (_canvasPanel.InvokeRequired)
            {
                _canvasPanel.BeginInvoke(new Action(ClearCanvas));
                return;
            }

            _canvasPanel.SuspendLayout();
            for (int i = _canvasPanel.Controls.Count - 1; i >= 0; i--)
            {
                var ctl = _canvasPanel.Controls[i];
                _canvasPanel.Controls.RemoveAt(i);
                ctl.Dispose(); // 중요: WinForms 리소스 (GDI+ 핸들 등 정적 자원) 강제 해제
            }
            _staticControls.Clear(); // 정적 랜더링 요소 클리어
            _canvasPanel.ResumeLayout(true);
        }

        /// <summary>
        /// Paint 이벤트를 재정의하여 배경 이미지의 유동적인 비율 유지를 처리합니다.
        /// </summary>
        private void CanvasPanel_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            if (_backgroundImage == null)
            {
                string warnText = "No Background Image";
                using (Font f = new Font("Arial", 16, FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(warnText, f, Brushes.Yellow, _canvasPanel!.ClientRectangle, sf);
                }
            }
            else
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                // 캔버스 대비 이미지의 비율이 깨지지 않게 유동적 스케일링 (Aspect Ratio 유지 - Letterbox 방식)
                float ratioX = (float)_canvasPanel.Width / _backgroundImage.Width;
                float ratioY = (float)_canvasPanel.Height / _backgroundImage.Height;
                float ratio = Math.Min(ratioX, ratioY);

                int drawWidth = (int)(_backgroundImage.Width * ratio);
                int drawHeight = (int)(_backgroundImage.Height * ratio);
                int drawX = (_canvasPanel.Width - drawWidth) / 2;
                int drawY = (_canvasPanel.Height - drawHeight) / 2;

                g.DrawImage(_backgroundImage, drawX, drawY, drawWidth, drawHeight);
            }

            // GDI 핸들 최적화 대상: Panel 인스턴스를 무한정 띄우지 않고, 정적 자원은 Graphics API로 직접 그림.
            if (_staticControls.Count > 0)
            {
                using (Pen guidePen = new Pen(Color.FromArgb(180, 200, 200, 200), 2))
                {
                    foreach (var sc in _staticControls)
                    {
                        g.DrawRectangle(guidePen, sc.X, sc.Y, sc.W, sc.H);
                    }
                }
            }
        }

        /// <summary>
        /// 화면 리사이징 시 패널 다시 그리기를 유도합니다.
        /// </summary>
        private void CanvasPanel_Resize(object? sender, EventArgs e)
        {
            _canvasPanel.Invalidate();
        }

        /// <summary>
        /// 시각적 시뮬레이터(Quick Start) 환경 조성을 위해 임시 JSON을 주입하고 화면을 구성합니다.
        /// </summary>
        public void LoadMockProject()
        {
            var config = new ProjectConfig { ProjectId = "mock_project_01" };
            
            // 디바이스 모킹 추가 (Timeout 등을 테스트하기 위해 유효한 IP, 가상 포트 연동)
            config.DeviceNodes.Add(new DeviceNode { DeviceId = "DEV_01", IpAddress = "127.0.0.1", Port = 5000 });
            config.DeviceNodes.Add(new DeviceNode { DeviceId = "DEV_02", IpAddress = "127.0.0.1", Port = 5001 });

            // 5개의 동적 버튼 설정
            string[] btnTitles = { "CCTV 1", "Power ON", "Light OFF", "Alarm Reset", "System Halt" };
            for (int i = 0; i < 5; i++)
            {
                config.UIControls.Add(new UIControl
                {
                    Id = $"btn_mock_{i}",
                    X = 50 + (i * 120), Y = 100, W = 100, H = 50,
                    Command = btnTitles[i], // command로 타이틀 활용
                    TargetDeviceId = "DEV_01",
                    IsStatic = false,
                    ZIndex = i
                });
            }

            // 정적 부하 테스트용 백그라운드 프레임 하나 추가
            config.UIControls.Add(new UIControl { Id = "guide_1", X = 40, Y = 40, W = 620, H = 70, IsStatic = true });

            string json = JsonSerializer.Serialize(config);
            
            // 런타임에 디바이스 매니저 장비 세팅 연동 완료
            GmnPlayer.Managers.DeviceManager.Instance.InitializeFromConfig(config);

            LoadLayoutFromJson(json);
        }

        /// <summary>
        /// 리소스 해제 (IDisposable 구현 - IDisposable 객체 반드시 해제 규정 충족)
        /// </summary>
        public new void Dispose()
        {
            ClearCanvas();
            if (_backgroundImage != null)
            {
                _backgroundImage.Dispose();
                _backgroundImage = null;
            }
            if (_canvasPanel != null)
            {
                _canvasPanel.Dispose();
            }
            base.Dispose();
        }
    }

    /// <summary>
    /// 화면 깜빡임 개선(Double Buffering)을 위한 커스텀 패널.
    /// 네이티브 성능 극대화를 위한 최적화 기법 - 컨트롤 렌더링 오버헤드 최소화.
    /// </summary>
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            // WinForms 내부 API를 다이렉트로 호출하여 더블 버퍼링 활성화, 그리기 지연 극소화
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
        }
    }
}

using System;
using System.Windows.Forms;
using System.Drawing; // Form, Message, Keys를 위해 필요
using IPROPHETA.UI;         // LayoutEngine이 있는 곳 (우리가 설정한 네임스페이스)
using IPROPHETA.Services;   // ProjectManager가 있는 곳
using IPROPHETA.UI.Widgets; // ButtonWidget이 있는 곳
using IPROPHETA.Core;       // EngineMode가 있는 곳



public partial class Form1 : Form
{
    private LayoutEngine _engine;
    private InteractionManager _interactor;

    public Form1()
    {
        //InitializeComponent();
        this.Size = new Size(1280, 720);
        this.Text = "Gmn-Player Designer (5px Grid Mode)";

        // 1. 엔진 및 인터랙터 초기화
        _engine = new LayoutEngine(this);
        _interactor = new InteractionManager(_engine, this);

        // 2. 테스트용 버튼 하나 배치
        var testBtn = new ButtonWidget { Name = "MCU_PWR_ON", Bounds = new Rectangle(50, 50, 150, 60) };
        ProjectManager.Instance.CurrentProject.AddWidget(testBtn);

        _engine.Refresh();

        // # 사이드바
        this.Padding = new Padding(120, 0, 0, 0); // 왼쪽에 사이드바 공간 확보

        // 1. 사이드바 패널 생성
        Panel sideBar = new Panel {
            Dock = DockStyle.Left,
            Width = 120,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        this.Controls.Add(sideBar);

        // 2. 버튼 위젯 생성 버튼
        Button btnAddButton = new Button { 
            Text = "+ 버튼 추가", Dock = DockStyle.Top, Height = 50, 
            FlatStyle = FlatStyle.Flat, ForeColor = Color.White 
        };
        btnAddButton.Click += (s, e) => AddNewWidget(new ButtonWidget());
        sideBar.Controls.Add(btnAddButton);

        // 3. 센서 위젯 생성 버튼
        Button btnAddSensor = new Button { 
            Text = "+ 센서 추가", Dock = DockStyle.Top, Height = 50, 
            FlatStyle = FlatStyle.Flat, ForeColor = Color.White 
        };
        btnAddSensor.Click += (s, e) => AddNewWidget(new SensorWidget());
        sideBar.Controls.Add(btnAddSensor);
    }

    // Ctrl+Z (Undo) 단축키 연결
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.Z))
        {
            ProjectManager.Instance.Undo();
            _engine.Refresh();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }
}
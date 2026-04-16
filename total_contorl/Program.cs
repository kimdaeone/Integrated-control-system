namespace total_contorl;

using System;
using System.Windows.Forms;
using System.Runtime.Versioning;

[SupportedOSPlatform("windows")]
static class Program
{
    [STAThread]
    static void Main()
    {
        // 로그 텍스트 한글 깨짐 원천 차단 (UTF-8 인코딩 강제)
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        ApplicationConfiguration.Initialize();

        // 1. Auto-Start Logic: .gmn 파일 존재유무 파악
        bool hasProject = GmnPlayer.Managers.ProjectManager.Instance.LoadLastProject();
        GmnPlayer.UI.LayoutEngine.EngineMode mode = hasProject ? GmnPlayer.UI.LayoutEngine.EngineMode.Player : GmnPlayer.UI.LayoutEngine.EngineMode.Designer;

        Application.Run(new GmnPlayer.UI.LayoutEngine(mode));
    }    
}
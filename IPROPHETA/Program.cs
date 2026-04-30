using System;
using System.Windows.Forms;

namespace IPROPHETA
{
    static class Program
    {
        [STAThread] // WinForms 실행을 위한 필수 속성
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // [핵심] 여기서 Form1의 인스턴스를 생성하여 실행해야 합니다!
            Application.Run(new Form1()); 
        }
    }
}
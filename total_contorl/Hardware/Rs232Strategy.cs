using System.Text;

namespace GmnPlayer.Hardware
{
    /// <summary>
    /// 표준 시리얼 통신(RS232)을 위한 명령 문자열 생성 전략.
    /// 문자열 끝에 CR LF를 부착하여 전송합니다.
    /// </summary>
    public class Rs232Strategy : IProtocolStrategy
    {
        public byte[] Encode(string command, object? parameters = null)
        {
            // RS232 통신 특성 반영: 종결 문자열 추가 (\r\n)
            return Encoding.ASCII.GetBytes(command + "\r\n");
        }
    }
}

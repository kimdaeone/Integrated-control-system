namespace GmnPlayer.Hardware
{
    /// <summary>
    /// 추상화된 제어 명령을 각 하드웨어의 프로토콜 규격에 맞게 인코딩하는 전략 인터페이스.
    /// </summary>
    public interface IProtocolStrategy
    {
        byte[] Encode(string command, object? parameters = null);
    }
}

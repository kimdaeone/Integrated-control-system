using System;
using System.Text;

namespace GmnPlayer.Hardware
{
    /// <summary>
    /// AVCIT 디코더 전용 TCP 패킷 생성 전략.
    /// Header + Command Body + Checksum 형태의 바이너리 구조를 생성합니다.
    /// </summary>
    public class AvcitStrategy : IProtocolStrategy
    {
        public byte[] Encode(string command, object? parameters = null)
        {
            // 헤더 정의 (예제스펙: 0xAA 0xBB)
            byte[] header = { 0xAA, 0xBB };
            byte[] body = Encoding.UTF8.GetBytes(command);
            
            // Checksum 연산 (Body의 단순 합)
            byte checksum = 0;
            foreach (var b in body)
            {
                checksum += b;
            }
            
            byte[] packet = new byte[header.Length + body.Length + 1];
            Buffer.BlockCopy(header, 0, packet, 0, header.Length);
            Buffer.BlockCopy(body, 0, packet, header.Length, body.Length);
            
            // 패킷 마지막은 Checksum
            packet[^1] = checksum;
            
            return packet;
        }
    }
}

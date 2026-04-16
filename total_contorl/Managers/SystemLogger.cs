using System;

namespace GmnPlayer.Managers
{
    public enum LogCategory
    {
        SYSTEM,
        DID,
        AUDIO,
        NETWORK,
        DB,
        UI_EVENT
    }

    /// <summary>
    /// 터미널 가시성을 극대화하기 위한 중앙 집중형 로거.
    /// 카테고리별로 색상을 분리하여 수천 줄의 로그에서도 식별이 쉽도록 최적화됨.
    /// </summary>
    public static class SystemLogger
    {
        private static void PrintPrefix(LogCategory category)
        {
            switch (category)
            {
                case LogCategory.DID:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case LogCategory.AUDIO:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
                case LogCategory.UI_EVENT:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;
                case LogCategory.NETWORK:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;
                case LogCategory.DB:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
            Console.Write($"[{category,-7}] ");
            Console.ResetColor();
            Console.Write($"{DateTime.Now:HH:mm:ss.fff} - ");
        }

        public static void Info(LogCategory category, string message)
        {
            PrintPrefix(category);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void Warning(LogCategory category, string message)
        {
            PrintPrefix(category);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARN] {message}");
            Console.ResetColor();
        }

        public static void Error(LogCategory category, string message, Exception? ex = null)
        {
            PrintPrefix(category);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {message}");
            if (ex != null)
            {
                Console.WriteLine($"   Exception: {ex.Message}");
            }
            Console.ResetColor();
        }

        public static void TxPacket(LogCategory category, string deviceId, string ip, int port, string command)
        {
            PrintPrefix(category);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[TX  ] -> [{deviceId}] ({ip}:{port}) Payload: {command}");
            Console.ResetColor();
        }

        public static void TxPacketRaw(LogCategory category, string deviceId, string ip, int port, byte[] raw)
        {
            PrintPrefix(category);
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            string hex = BitConverter.ToString(raw).Replace("-", " ");
            Console.WriteLine($"[TX  ] -> [{deviceId}] ({ip}:{port}) Hex: {hex}");
            Console.ResetColor();
        }

        public static void RxPacket(LogCategory category, string deviceId, string data)
        {
            PrintPrefix(category);
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"[RX  ] <- [{deviceId}] Payload: {data}");
            Console.ResetColor();
        }

        public static void Alert(LogCategory category, string message)
        {
            PrintPrefix(category);
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"[CRITICAL/ALERT] {message}");
            Console.ResetColor();
        }
    }
}

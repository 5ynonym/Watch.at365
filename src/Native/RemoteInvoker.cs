using System.Runtime.InteropServices;

namespace at365.Native365
{
    public class RemoteInvoker
    {
        [DllImport("expandedresources.dll")]
        public static extern bool HasExpandedResources();

        static void Main()
        {
            bool isGameMode = HasExpandedResources();
            Console.WriteLine($"ゲームモード: {(isGameMode ? "有効" : "無効")}");
        }

        [DllImport("kernel32.dll")]
        private static extern nint OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll")]
        private static extern nint VirtualAllocEx(nint hProcess, nint lpAddress, uint dwSize, uint flAllocationType, uint flProtect);
        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(nint hProcess, nint lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);
        [DllImport("kernel32.dll")]
        private static extern nint CreateRemoteThread(nint hProcess, nint lpThreadAttributes, uint dwStackSize, nint lpStartAddress, nint lpParameter, uint dwCreationFlags, out nint lpThreadId);
        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(nint hObject);

        public void Invoke(int targetProcessId)
        {
            const int PROCESS_ALL_ACCESS = 0x001F0FFF;

            nint hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, targetProcessId);
            if (hProcess == nint.Zero) return;

            // ターゲットプロセスにメモリを割り当てる（ここではダミーのデータを書き込む例）
            string dllPath = @"C:\example.dll"; // DLLのパスを指定
            byte[] buffer = System.Text.Encoding.ASCII.GetBytes(dllPath);
            nint allocMemAddress = VirtualAllocEx(hProcess, nint.Zero, (uint)buffer.Length, 0x3000, 0x40);

            if (allocMemAddress == nint.Zero)
            {
                Console.WriteLine("メモリの割り当てに失敗しました。");
                CloseHandle(hProcess);
                return;
            }

            // 書き込む
            if (!WriteProcessMemory(hProcess, allocMemAddress, buffer, (uint)buffer.Length, out _))
            {
                Console.WriteLine("プロセスメモリの書き込みに失敗しました。");
                CloseHandle(hProcess);
                return;
            }

            // リモートスレッドを作成
            nint threadId;
            nint hThread = CreateRemoteThread(hProcess, nint.Zero, 0, allocMemAddress, nint.Zero, 0, out threadId);

            if (hThread == nint.Zero)
            {
                Console.WriteLine("リモートスレッドの作成に失敗しました。");
                CloseHandle(hProcess);
                return;
            }

            Console.WriteLine("リモートスレッドを作成しました。");

            // ハンドルを閉じる
            CloseHandle(hThread);
            CloseHandle(hProcess);
        }
    }
}

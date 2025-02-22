using System.Runtime.InteropServices;

namespace ffxiv_dresser_analyze_client
{
    public static partial class WinApi
    {
        [LibraryImport("kernel32.dll", SetLastError = true)]
        public static partial IntPtr OpenProcess(uint processAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint processId);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, IntPtr dwSize, IntPtr lpNumberOfBytesRead);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool CloseHandle(IntPtr hObject);

        [LibraryImport("kernel32.dll", EntryPoint = "QueryFullProcessImageNameW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, [Out] char[] lpExeName, ref int lpdwSize);

        public static string QueryFullProcessImageName(int processId)
        {
            var hProcess = OpenProcess(0x00001000, false, (uint)processId);
            var length = 256;
            var buffer = new char[length];
            QueryFullProcessImageName(hProcess, 0, buffer, ref length);
            CloseHandle(hProcess);
            return new string(buffer, 0, length);
        }
    }
}

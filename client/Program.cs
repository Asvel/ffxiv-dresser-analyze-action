using System.Diagnostics;
using ffxiv_dresser_analyze_client;

try
{
    Console.WriteLine("最终幻想14投影台整理助手<https://github.com/Asvel/ffxiv-dresser-analyze>");

    Console.Write("正在获取游戏进程...  ");
    Process? process = null;
    var processes = Process.GetProcessesByName("ffxiv_dx11");
    if (processes.Length == 1)
    {
        process = processes[0];
        Console.WriteLine($"PID={process.Id}");
    }
    if (processes.Length > 1)
    {
        Console.WriteLine();
        Console.Write($"发现多个 ffxiv_dx11.exe 进程（{string.Join(',', processes.Select(p => p.Id))}），请输入目标进程 PID：");
        var pid = int.Parse(Console.ReadLine()!);
        process = Array.Find(processes, p => p.Id == pid);
    }
    if (process == null) throw new InvalidOperationException("未找到游戏进程");

    Console.WriteLine("正在定位投影台数据...");
    var dresserData = new DresserData(process);

    Console.Write("正在读取游戏数据包...  ");
    var sqpackPath = Path.Combine(Path.GetDirectoryName(WinApi.QueryFullProcessImageName(process.Id))!, "sqpack");
    Console.WriteLine(sqpackPath);
    var staticData = new StaticData(sqpackPath);

    Console.WriteLine("正在启动网页服务端...");
    var server = new WebServer(staticData, dresserData);
    server.Run();
}
finally
{
    Console.ReadKey();
}

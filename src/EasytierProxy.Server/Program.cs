using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;

namespace Snavi;

[Command("run")]
public partial class RunCommand : ICommand
{
    [CommandOption("easytier-core")]
    public string EasytierCore { get; set; } = "easytier-core";

    [CommandOption("easytier-cli")]
    public string EasytierCli { get; set; } = "easytier-cli";

    public async ValueTask ExecuteAsync(IConsole console)
    {
        
    }
}
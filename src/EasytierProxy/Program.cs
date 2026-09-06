using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using Snavi.Core;
using Snavi.Executing;
using Snavi.Interacting;

namespace Snavi;

[Command("run")]
public partial class RunCommand : ICommand
{
    [CommandOption("cheats", 'c')]
    public required IReadOnlyList<FileInfo> Cheats { get; set; }

    [CommandOption("dotnet")]
    public string Dotnet { get; set; } = "dotnet";

    [CommandOption("fzf")]
    public string Fzf { get; set; } = "fzf";

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var result = await new SnaviApp(Cheats, new UserInterfaceFzf(Fzf), new ArgumentSuggesterExector(Dotnet)).RunAsync(console.RegisterCancellationHandler());
        console.WriteLine(result);
    }
}
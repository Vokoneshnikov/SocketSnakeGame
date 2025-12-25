using System.Threading;
using System.Threading.Tasks;

namespace SnakeGame.Client;

public interface IClientCommandHandler
{
    Task InvokeAsync(byte[] payload, CancellationToken ct = default);
}
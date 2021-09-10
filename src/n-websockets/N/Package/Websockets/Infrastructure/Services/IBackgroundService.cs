using System.Threading.Tasks;

namespace n_websockets.N.Package.Websockets.Infrastructure.Services
{
    public interface IBackgroundService
    {
        public Task RunAsync();
        public Task HaltAsync();
        bool Running { get; }
    }
}
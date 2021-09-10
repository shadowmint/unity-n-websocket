using System.Threading.Tasks;
using UnityEngine;

namespace n_websockets.N.Package.Websockets.Infrastructure.Services
{
    public class NBackgroundRunner : MonoBehaviour
    {
        public IBackgroundService Service { get; set; }

        public void Update()
        {
            if (Service.Running) return;
            Task.Run(async () => { await Service.RunAsync(); });
        }

        public void OnDestroy()
        {
            Task.Run(async () => { await Service.HaltAsync(); });
        }
    }
}
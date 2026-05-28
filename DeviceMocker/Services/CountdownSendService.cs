using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceMocker.Services
{
    public class CountdownSendService
    {
        public event Action<int>? CountdownTick;
        public event Action? CountdownCompleted;

        public async Task StartCountdownAsync(int seconds, Func<Task> sendAction, CancellationToken cancellationToken = default)
        {
            for (int i = seconds; i > 0; i--)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CountdownTick?.Invoke(i);
                await Task.Delay(1000, cancellationToken);
            }

            CountdownCompleted?.Invoke();
            await sendAction();
        }
    }
}

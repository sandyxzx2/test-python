namespace testSoulChat;

public sealed class RandomDelayScheduler : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly int _minMs;
    private readonly int _maxMs;
    private readonly Action _callback;

    public RandomDelayScheduler(int minMs, int maxMs, Action callback)
    {
        if (minMs <= 0 || maxMs < minMs)
        {
            throw new ArgumentOutOfRangeException(nameof(minMs), "Invalid random delay range.");
        }

        _minMs = minMs;
        _maxMs = maxMs;
        _callback = callback;

        _timer = new System.Windows.Forms.Timer();
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            _callback();
        };
    }

    public int ScheduleNext()
    {
        var next = Random.Shared.Next(_minMs, _maxMs + 1);
        _timer.Interval = next;
        _timer.Start();
        return next;
    }

    public void Stop() => _timer.Stop();

    public void Dispose() => _timer.Dispose();
}

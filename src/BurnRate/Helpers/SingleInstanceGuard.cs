namespace BurnRate.Helpers;

public sealed class SingleInstanceGuard : IDisposable
{
    private const string MutexName = "Global\\ClaudMon_SingleInstance_F3A7B2C1";
    private readonly Mutex _mutex;
    private bool _hasHandle;

    public SingleInstanceGuard()
    {
        _mutex = new Mutex(false, MutexName);
    }

    public bool TryAcquire()
    {
        try
        {
            _hasHandle = _mutex.WaitOne(0, false);
        }
        catch (AbandonedMutexException)
        {
            _hasHandle = true;
        }
        return _hasHandle;
    }

    public void Dispose()
    {
        if (_hasHandle)
        {
            _mutex.ReleaseMutex();
        }
        _mutex.Dispose();
    }
}

using System.Diagnostics;
using UnityEngine;

public struct ScopedStopwatch : System.IDisposable
{
    public string name;
    public Stopwatch stopwatch;

    public ScopedStopwatch(string name)
    {
        this.name = name;
        stopwatch = new Stopwatch();
        stopwatch.Start();
    }

    public void Dispose()
    {
        stopwatch.Stop();
        MonoBehaviour.print(name + " ms:" + stopwatch.ElapsedMilliseconds);
    }
}

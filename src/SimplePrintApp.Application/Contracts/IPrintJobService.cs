using SimplePrintApp.Domain;

namespace SimplePrintApp.Application.Contracts;

public interface IPrintJobService : IPrintJob
{
    (JobState State, TimeSpan Remaining) GetSnapshot();
    void Start();
    void Pause();
    void Resume();
    void Stop();
    void Tick(); 
}
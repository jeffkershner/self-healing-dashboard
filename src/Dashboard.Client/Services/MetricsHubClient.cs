using Dashboard.Client.Auth;
using Dashboard.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Dashboard.Client.Services;

/// <summary>Owns the SignalR connection to the metrics hub and buffers the live series.</summary>
public sealed class MetricsHubClient(NavigationManager navigation, IApiTokenSource tokens) : IAsyncDisposable
{
    private const int Capacity = 120;
    private HubConnection? _connection;
    private readonly List<MetricSample> _samples = [];

    public IReadOnlyList<MetricSample> Samples => _samples;
    public string State => _connection?.State.ToString() ?? "Disconnected";
    public event Action? Changed;

    public async Task StartAsync()
    {
        if (_connection is not null)
        {
            return;
        }

        _connection = new HubConnectionBuilder()
            .WithUrl(navigation.ToAbsoluteUri(HubPaths.Metrics), options =>
            {
                options.AccessTokenProvider = tokens.GetTokenAsync;
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<List<MetricSample>>(HubEvents.HistoryReceived, history =>
        {
            _samples.Clear();
            _samples.AddRange(history.TakeLast(Capacity));
            Changed?.Invoke();
        });

        _connection.On<MetricSample>(HubEvents.MetricReceived, sample =>
        {
            _samples.Add(sample);
            if (_samples.Count > Capacity)
            {
                _samples.RemoveAt(0);
            }
            Changed?.Invoke();
        });

        _connection.Reconnecting += _ => { Changed?.Invoke(); return Task.CompletedTask; };
        _connection.Reconnected += _ => { Changed?.Invoke(); return Task.CompletedTask; };
        _connection.Closed += _ => { Changed?.Invoke(); return Task.CompletedTask; };

        await _connection.StartAsync();
        Changed?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}

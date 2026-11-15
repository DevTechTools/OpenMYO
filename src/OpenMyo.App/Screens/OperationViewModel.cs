using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace OpenMyo.App.Screens;

/// <summary>
/// Base for screens that run long operations: tracks busy state, a status line, cancellation and
/// errors. This is the UI boundary, so it is the one place that catches all exceptions: each failure is logged in
/// full and shown to the user as a short, plain message.
/// </summary>
public abstract partial class OperationViewModel(ILogger logger) : ObservableObject
{
    private CancellationTokenSource? _operation;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string? _status;

    [ObservableProperty]
    private string? _error;

    [ObservableProperty]
    private double _progress;

    /// <summary>Runs an operation unless one is already running. Failures are shown in <see cref="Error"/>.</summary>
    protected async Task RunAsync(string status, Func<CancellationToken, Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (IsBusy)
        {
            return;
        }

        using var cancellation = new CancellationTokenSource();
        _operation = cancellation;
        (IsBusy, Status, Error, Progress) = (true, status, null, 0);
        try
        {
            await operation(cancellation.Token);
            Status = null;
        }
        catch (OperationCanceledException)
        {
            Status = "Cancelled";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Operation} failed", status);
            Error = ToUserMessage(exception);
            Status = null;
        }
        finally
        {
            _operation = null;
            IsBusy = false;
        }
    }

    /// <summary>For background work this screen starts without awaiting; see <see cref="Services.BackgroundWork"/>.</summary>
    protected ILogger Logger => logger;

    protected void ReportProgress(double fraction, string? status = null)
    {
        Progress = fraction;
        Status = status ?? Status;
    }

    [RelayCommand(CanExecute = nameof(IsBusy))]
    private void Cancel() => _operation?.Cancel();

    [RelayCommand]
    private void DismissError() => Error = null;

    // Messages from OpenMyo's own exceptions are written for users (CODESTANDARDS §5), so they are shown as they
    // are; system exceptions get a plain explanation instead of their technical text.
    private static string ToUserMessage(Exception exception) => exception switch
    {
        HttpRequestException => "Couldn't reach the server. Check your internet connection and try again.",
        UnauthorizedAccessException => "OpenMyo isn't allowed to open that file or folder. Check its permissions and try again.",
        IOException io => $"Couldn't read or write a file ({io.Message}). Check that it still exists and isn't open in another app.",
        _ => exception.Message,
    };
}

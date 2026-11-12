using System.Threading;
using System.ComponentModel;
using System.Windows.Input;

namespace Helix.Wpf;

/// <summary>
/// A command for async work: disabled while it runs (no double click), errors shown as a toast, cancellable.
/// A HelixButton bound to it shows its spinner while it runs.
/// <code>SaveCommand = new HelixAsyncCommand(async ct => await api.SaveAsync(model, ct));</code>
/// </summary>
public class HelixAsyncCommand : ICommand, INotifyPropertyChanged
{
    private readonly Func<object?, CancellationToken, Task> _execute;
    private readonly Func<object?, bool>? _canExecute;
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public HelixAsyncCommand(Func<CancellationToken, Task> execute, Func<bool>? canExecute = null)
        : this((_, ct) => execute(ct), canExecute is null ? null : _ => canExecute())
    {
    }

    public HelixAsyncCommand(Func<object?, CancellationToken, Task> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
        CancelCommand = new HelixCommand(Cancel);
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>True while the work runs.</summary>
    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            _isRunning = value;
            PropertyChanged?.Invoke(this, new(nameof(IsRunning)));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>Shows exceptions as an error toast (true). Turn it off to handle them yourself (OnError).</summary>
    public bool ShowErrors { get; set; } = true;

    /// <summary>Title of the error toast (the built-in "Error" text by default).</summary>
    public string? ErrorTitle { get; set; }

    /// <summary>Called with any exception other than a cancellation.</summary>
    public Action<Exception>? OnError { get; set; }

    /// <summary>Cancels the running work (its token).</summary>
    public ICommand CancelCommand { get; }

    public bool CanExecute(object? parameter) => !IsRunning && (_canExecute?.Invoke(parameter) ?? true);

    public async void Execute(object? parameter) => await ExecuteAsync(parameter);

    /// <summary>Runs the work (also usable from code and tests).</summary>
    public async Task ExecuteAsync(object? parameter = null)
    {
        if (!CanExecute(parameter)) return;
        _cts = new CancellationTokenSource();
        IsRunning = true;
        try
        {
            await _execute(parameter, _cts.Token);
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
            // Cancelled by the user: nothing to report.
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
            if (ShowErrors) HelixToasts.Error(ex.Message, ErrorTitle ?? HelixTexts.Current.LogError);
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            IsRunning = false;
        }
    }

    public void Cancel() => _cts?.Cancel();
}

using NeeLaboratory.IO;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NeeView
{
    public class AppRemoteCommandServer : RemoteCommandServer, IDisposable
    {
        private readonly static Lazy<AppRemoteCommandServer> _current = new();
        public static AppRemoteCommandServer Current => _current.Value;

        public static string PipeName { get; } = AppRemoteCommandTools.CreatePipeName(Process.GetCurrentProcess());
        private Task? _task;
        private Task? _diagnosticTask;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _disposedValue;

        public AppRemoteCommandServer() : base(PipeName, AppRemoteCommandJsonContext.Default)
        {
            RegisterMethod<string[]>(nameof(Restart), Restart);
            RegisterMethod<int, bool>(nameof(IsHideWindow), IsHideWindow);
            RegisterMethod<string, string>(nameof(DiagnosticRequest), DiagnosticRequest);
            RegisterMethod<int, string>(nameof(DiagnosticState), DiagnosticState);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Stop();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            if (_task is not null) return;

            _cancellationTokenSource = new();
            _task = StartAsync(_cancellationTokenSource.Token);
            var diagnosticPipeName = PipeName + ".diagnostic." + Guid.NewGuid().ToString("N");
            DiagnosticPipeName = diagnosticPipeName;
            _diagnosticTask = Task.Run(() => StartDiagnosticServerAsync(diagnosticPipeName, _cancellationTokenSource.Token));
            Trace.WriteLine($"Diagnostic server started: {diagnosticPipeName}");
        }

        public static string? DiagnosticPipeName { get; private set; }

        public void Stop()
        {
            if (_task is null) return;

            _cancellationTokenSource?.Cancel();
            _task = null;
            _diagnosticTask = null;
        }

        private async Task StartDiagnosticServerAsync(string pipeName, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await using var pipe = new System.IO.Pipes.NamedPipeServerStream(pipeName, System.IO.Pipes.PipeDirection.InOut, 1, System.IO.Pipes.PipeTransmissionMode.Byte, System.IO.Pipes.PipeOptions.Asynchronous | System.IO.Pipes.PipeOptions.CurrentUserOnly);
                    await pipe.WaitForConnectionAsync(cancellationToken);
                    await ProcessDiagnosticRequestAsync(pipe, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Diagnostic pipe error: {ex}");
                }
            }
        }

        private async Task ProcessDiagnosticRequestAsync(System.IO.Pipes.NamedPipeServerStream pipe, CancellationToken cancellationToken)
        {
            try
            {
                var sizeBuffer = new byte[4];
                await pipe.ReadExactlyAsync(sizeBuffer, cancellationToken);
                var methodLength = BitConverter.ToInt32(sizeBuffer);
                if (methodLength < 1 || methodLength > 1024)
                {
                    throw new InvalidDataException("Invalid method header length.");
                }
                var methodBuffer = new byte[methodLength];
                await pipe.ReadExactlyAsync(methodBuffer, cancellationToken);
                var method = System.Text.Encoding.UTF8.GetString(methodBuffer);
                await pipe.ReadExactlyAsync(sizeBuffer, cancellationToken);
                var payloadLength = BitConverter.ToInt32(sizeBuffer);
                if (payloadLength < 0 || payloadLength > 16 * 1024 * 1024)
                {
                    throw new InvalidDataException("Request payload exceeds 16 MiB.");
                }
                var payload = new byte[payloadLength];
                await pipe.ReadExactlyAsync(payload, cancellationToken);
                object argument = method switch
                {
                    nameof(DiagnosticState) => JsonSerializer.Deserialize<int>(payload),
                    nameof(DiagnosticRequest) => JsonSerializer.Deserialize<string>(payload),
                    _ => throw new ArgumentException($"Unknown diagnostic method: {method}")
                };
                var response = method == nameof(DiagnosticState)
                    ? await Task.Run(() => DiagnosticState((int)argument!), cancellationToken)
                    : await Task.Run(() => DiagnosticRequest((string)argument!), cancellationToken);
                var responseBytes = JsonSerializer.SerializeToUtf8Bytes(response);
                await pipe.WriteAsync(new byte[] { 1 }, cancellationToken);
                await pipe.WriteAsync(BitConverter.GetBytes(responseBytes.Length), cancellationToken);
                await pipe.WriteAsync(responseBytes, cancellationToken);
            }
            catch (Exception ex)
            {
                var errorBytes = System.Text.Encoding.UTF8.GetBytes(ex.Message);
                await pipe.WriteAsync(new byte[] { 0 }, cancellationToken);
                await pipe.WriteAsync(BitConverter.GetBytes(errorBytes.Length), cancellationToken);
                await pipe.WriteAsync(errorBytes, cancellationToken);
            }
        }


        public void Restart(string[] args)
        {
            App.Current.Dispatcher.BeginInvoke(async () =>
            {
                try
                {
                    await AppState.Current.ResumeAsync(args, CancellationToken.None);
                }
                catch
                {
                }
            });
        }

        public bool IsHideWindow(int _)
        {
            try
            {
                return AppState.Current.IsTaskTrayEnabled && AppState.Current.IsHideWindow;
            }
            catch
            {
                return false;
            }
        }

        public string DiagnosticRequest(string request)
        {
            using var document = JsonDocument.Parse(request);
            var root = document.RootElement;
            var action = root.GetProperty("action").GetString();
            var timeout = root.TryGetProperty("timeoutMs", out var timeoutValue) ? timeoutValue.GetInt32() : 30000;
            if (timeout < 1 || timeout > 120000)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), "timeoutMs must be between 1 and 120000.");
            }

            if (action == "open")
            {
                var path = root.GetProperty("path").GetString() ?? throw new ArgumentException("path is required.");
                App.Current.Dispatcher.Invoke(() => BookHub.Current.RequestLoad(this, path, null, BookLoadOption.None, true));
                WaitForDiagnosticState(timeout, state => !state.IsLoading && state.BookPath == path && state.PageCount > 0);
                return DiagnosticState(0);
            }

            if (action == "next")
            {
                var previousEntry = App.Current.Dispatcher.Invoke(() => MainViewComponent.Current.PageFrameBoxPresenter.SelectedPages.FirstOrDefault()?.EntryName);
                App.Current.Dispatcher.Invoke(() => BookOperation.Current.Control.MoveNext(this));
                WaitForDiagnosticState(timeout, state => !state.IsLoading && App.Current.Dispatcher.Invoke(() => MainViewComponent.Current.PageFrameBoxPresenter.SelectedPages.FirstOrDefault()?.EntryName != previousEntry));
                return DiagnosticState(0);
            }

            if (action == "screenshot")
            {
                var path = root.GetProperty("path").GetString() ?? throw new ArgumentException("path is required.");
                App.Current.Dispatcher.Invoke(() => CaptureDiagnosticScreenshot(path));
                return JsonSerializer.Serialize(new { status = "completed", path });
            }

            throw new ArgumentException($"Unknown diagnostic action: {action}");
        }

        public string DiagnosticState(int _)
        {
            return App.Current.Dispatcher.Invoke(() =>
            {
                var book = BookHub.Current.GetCurrentBook();
                var page = book?.CurrentPage;
                var selectedEntry = MainViewComponent.Current.PageFrameBoxPresenter.SelectedPages.FirstOrDefault()?.EntryName;
                return JsonSerializer.Serialize(new DiagnosticSpiderState(
                    book is null ? "idle" : BookHub.Current.IsLoading ? "loading" : "ready",
                    Environment.ProcessId,
                    Environment.AssemblyLocation,
                    Environment.LocalApplicationDataPath,
                    BookHub.Current.Address,
                    book?.Pages.Count ?? 0,
                    selectedEntry ?? page?.EntryName,
                    BookHub.Current.IsLoading,
                    Environment.Revision,
                    DiagnosticPipeName ?? ""));
            });
        }

        private void WaitForDiagnosticState(int timeoutMs, Func<DiagnosticStateInfo, bool> predicate)
        {
            var timer = Stopwatch.StartNew();
            while (timer.ElapsedMilliseconds < timeoutMs)
            {
                var state = App.Current.Dispatcher.Invoke(() =>
                {
                    var book = BookHub.Current.GetCurrentBook();
                    return new DiagnosticStateInfo(BookHub.Current.IsLoading, BookHub.Current.Address, book?.Pages.Count ?? 0);
                });
                if (predicate(state))
                {
                    return;
                }
                Thread.Sleep(100);
            }
            throw new TimeoutException("Diagnostic operation timed out; inspect state before retrying.");
        }

        private static void CaptureDiagnosticScreenshot(string path)
        {
            var window = MainWindow.Current;
            var width = (int)Math.Ceiling(window.ActualWidth);
            var height = (int)Math.Ceiling(window.ActualHeight);
            if (width <= 0 || height <= 0)
            {
                throw new InvalidOperationException("Main window has no rendered size yet.");
            }
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(window);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            using var stream = File.Create(path);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(stream);
        }

        private sealed record DiagnosticStateInfo(bool IsLoading, string? BookPath, int PageCount);
    }

}


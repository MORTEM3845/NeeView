# NeeView local diagnostic spider

The spider controls an isolated NeeView process through the application's existing local named pipe. It can open an image/archive, move to the next page, report the selected entry and capture the rendered WPF main window.

## Build and start

From the repository root, build `NeeView\NeeView.csproj` in Debug, then run:

```powershell
.\docs\development\diagnostic-spider\spider.ps1 start -Executable .\NeeView\bin\Debug\net10.0-windows\win-x64\NeeView.exe
```

The default workspace is `%LOCALAPPDATA%\NeeView-DiagnosticSpider\<checkout-folder>`. The app gets `NEEVIEW_PROFILE` set to its dedicated `profile` directory and writes its startup trace to `profile\Spider.log`. Session identity is stored in `session.json`; evidence belongs in `artifacts`.

## Commands

```powershell
spider.ps1 status
spider.ps1 state
spider.ps1 request -RequestJson '{"action":"open","path":"C:\\fixtures\\sample.zip"}'
spider.ps1 request -RequestJson '{"action":"next"}'
spider.ps1 screenshot -Path C:\temp\neeview.png
spider.ps1 stop
```

`open` and `next` wait up to 30 seconds by default (maximum 120 seconds); an error says to inspect `state` before retrying. A timeout does not cancel an app operation. `screenshot` writes a PNG from the app's WPF visual tree. Native child HWND/GPU surfaces are outside that capture guarantee.

## Real navigation check

Generate a disposable ZIP archive containing multiple valid images outside the repository, start the spider, open that archive, and verify `state` changes to `ready` with the ZIP path, nonzero page count, and a current entry. Issue `next`, verify the current entry changes, capture a screenshot, and inspect the PNG. Keep evidence in the spider workspace; do not add local evidence or fixture archives to a code change.

The session descriptor includes protocol, PID, executable, start time, profile, workspace and source revision. `status` validates both process executable and a live state response. `stop` stops only that descriptor's process.

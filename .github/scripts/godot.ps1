# Runs the headless editor with its output in files, not a pipe: processes its .NET publish leaves behind inherit the
# output, and a step waits for its pipe to close, so each export idled for a quarter of an hour after Godot exited
$arguments = @('--headless', '--path', '.') + $args | ForEach-Object { "`"$_`"" }
$output = New-TemporaryFile
$errors = New-TemporaryFile

$process = Start-Process $env:GODOT $arguments -NoNewWindow -PassThru `
  -RedirectStandardOutput $output -RedirectStandardError $errors
$null = $process.Handle
$process.WaitForExit()

Get-Content $output, $errors
exit $process.ExitCode

Set objShell = CreateObject("Shell.Application")
objShell.ShellExecute WScript.Arguments(0), "", "", "runas", 1

param (
    [switch]$DryRun = $false
)

$excludeFolders = @('.git', '.vs', 'bin', 'obj', 'node_modules', 'dist', '.github', 'artifacts', 'TestResults')
$includeExtensions = @('.cs', '.js', '.ts', '.tsx', '.css', '.scss', '.html', '.json', '.md', '.xml', '.csproj', '.sln', '.ps1', '.yml', '.yaml')

function Remove-TrailingNewlines {
    param (
        [string]$Path
    )

    try {
        $content = Get-Content -Path $Path -Raw -ErrorAction Stop
        if ([string]::IsNullOrEmpty($content)) {
            return
        }

        # Check if the file ends with multiple newlines or no newline
        # We want exactly one newline at the end if the file is not empty.
        # But wait, standard git practice is usually just ensuring a single newline at EOF,
        # OR removing *empty lines* at the very end.
        
        # The user asked: "remove empty lines at the end".
        # This usually means:
        # Code...
        # }
        # <empty>
        # <empty>
        # ->
        # Code...
        # }
        # <empty> (one newline at end of file is standard)
        
        $trimmed = $content.TrimEnd()
        
        # Add back a single newline if the file wasn't empty to begin with
        if ($trimmed.Length -gt 0) {
            $newContent = $trimmed + [Environment]::NewLine
        } else {
            $newContent = ""
        }

        if ($content -ne $newContent) {
            if ($DryRun) {
                Write-Host "Would modify: $Path" -ForegroundColor Yellow
            } else {
                $newContent | Set-Content -Path $Path -NoNewline -Encoding UTF8
                Write-Host "Fixed: $Path" -ForegroundColor Green
            }
        }
    }
    catch {
        Write-Host "Error processing $Path : $_" -ForegroundColor Red
    }
}

Get-ChildItem -Path . -Recurse -File | ForEach-Object {
    $relativePath = $_.FullName.Substring($PWD.Path.Length + 1)
    $parts = $relativePath.Split([IO.Path]::DirectorySeparatorChar)
    
    # Check exclude folders
    $shouldSkip = $false
    foreach ($part in $parts) {
        if ($excludeFolders -contains $part) {
            $shouldSkip = $true
            break
        }
    }

    if (-not $shouldSkip) {
        if ($includeExtensions -contains $_.Extension) {
            Remove-TrailingNewlines -Path $_.FullName
        }
    }
}

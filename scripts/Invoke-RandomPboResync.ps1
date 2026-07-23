[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$TargetPath,

    [ValidateRange(1, [int]::MaxValue)]
    [int]$Count = 10
)

$target = Get-Item -LiteralPath $TargetPath -ErrorAction Stop
if (-not $target.PSIsContainer) {
    throw "TargetPath must be an existing directory: $TargetPath"
}

$pboFiles = @(Get-ChildItem -LiteralPath $target.FullName -Recurse -File -Filter '*.pbo' |
    Where-Object { $_.Length -ge 20 })

if ($pboFiles.Count -eq 0) {
    Write-Warning "No PBO files at least 20 bytes long were found under '$($target.FullName)'."
    return
}

$selectionCount = [Math]::Min($Count, $pboFiles.Count)
if ($selectionCount -lt $Count) {
    Write-Warning "Only $selectionCount eligible PBO file(s) were found; selecting all of them."
}

$selectedFiles = $pboFiles | Get-Random -Count $selectionCount
foreach ($file in $selectedFiles) {
    $offset = $file.Length - 20 + (Get-Random -Minimum 0 -Maximum 20)
    $relativePath = $file.FullName.Substring($target.FullName.TrimEnd('\', '/').Length).TrimStart('\', '/')

    if (-not $PSCmdlet.ShouldProcess($file.FullName, "Alter embedded PBO hash byte at offset $offset")) {
        continue
    }

    $stream = [System.IO.File]::Open($file.FullName, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite,
        [System.IO.FileShare]::None)
    try {
        $stream.Position = $offset
        $originalByte = $stream.ReadByte()
        if ($originalByte -lt 0) {
            throw "Unable to read the embedded PBO hash byte at offset $offset in '$($file.FullName)'."
        }

        $replacementByte = ($originalByte + 1) % 256
        $stream.Position = $offset
        $stream.WriteByte($replacementByte)
        $stream.Flush($true)
        Write-Host "Modified $relativePath (offset $offset, $originalByte -> $replacementByte)."
    }
    finally {
        $stream.Dispose()
    }
}

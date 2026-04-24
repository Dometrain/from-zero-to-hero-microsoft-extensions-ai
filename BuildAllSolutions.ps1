# Define the current folder as the search folder
$SearchFolder = "$(Get-Location)"

# Recursively find all `.sln` files
Write-Host "Searching for solution files (*.slnx) in '$SearchFolder'..." -ForegroundColor Cyan
$slnFiles = Get-ChildItem -Path $SearchFolder -Recurse -Filter *.slnx -ErrorAction SilentlyContinue

if ($slnFiles.Count -eq 0) {
    Write-Host "No solution files (*.slnx) found in '$SearchFolder'." -ForegroundColor Yellow
    exit 0
}

# Define a flag to track errors
$global:hasError = $false

# Build `.slnx` files in parallel
$slnFiles | ForEach-Object -Parallel {
    if ($global:hasError) { throw "Stopping due to previous error" }

    $slnxFile = $_

    Write-Host "Formatting $($slnxFile)"
    dotnet format $slnxFile

    try {
        Write-Host "Building solution: $($slnxFile)"
        $buildOutput = dotnet build $slnxFile 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Build succeeded for: $($slnxFile)" -ForegroundColor Green
        } else {
            Write-Host "Build failed for: $($slnxFile)" -ForegroundColor Red
            Write-Host "Build output:" -ForegroundColor Yellow
            Write-Output $buildOutput -ForegroundColor Yellow
            $global:hasError = $true
            throw "Build failed for $slnxFile"
        }
    }
    catch {
        $global:hasError = $true
        Write-Host "Error building: $slnxFile" -ForegroundColor Red
    }
}

# Check if any builds failed and fail the workflow if necessary
if ($global:hasError) {
    Write-Host "One or more solutions failed to build." -ForegroundColor Red
    exit 1
}

Write-Host "Build process completed." -ForegroundColor Cyan
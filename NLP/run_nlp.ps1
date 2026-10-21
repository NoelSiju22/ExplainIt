$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
if (-not (Test-Path '.venv')) {
    py -3 -m venv .venv
}
$python = Join-Path $PSScriptRoot '.venv\Scripts\python.exe'
$fastapiPath = Join-Path $PSScriptRoot '.venv\Lib\site-packages\fastapi'
$installedSklearn = ''
if (Test-Path $fastapiPath) {
    $installedSklearn = & $python -c "import sklearn; print(sklearn.__version__)" 2>$null
}
if (-not (Test-Path $fastapiPath) -or $installedSklearn.Trim() -ne '1.9.1') {
    & $python -m pip install --upgrade pip
    & $python -m pip install -r requirements.txt
}
if (-not (Test-Path 'model.joblib')) {
    & $python train.py
}
& $python -m uvicorn app:app --host 127.0.0.1 --port 8000

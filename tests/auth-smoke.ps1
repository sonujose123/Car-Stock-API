param([string]$BaseUrl = 'http://127.0.0.1:5187')
$ErrorActionPreference = 'Stop'
function Request($Path, $Body, $Token) {
    $parameters = @{ Uri = "$BaseUrl$Path"; SkipHttpErrorCheck = $true }
    if ($null -ne $Body) {
        $parameters.Method = 'Post'
        $parameters.ContentType = 'application/json'
        $parameters.Body = $Body | ConvertTo-Json
    }
    if ($Token) { $parameters.Headers = @{ Authorization = "Bearer $Token" } }
    Invoke-WebRequest @parameters
}
function Assert($Condition, $Message) {
    if (!$Condition) { throw $Message }
    Write-Host "PASS: $Message"
}
$first = Request '/api/auth/login' @{email='dealer1@example.com';password='DemoPassword123!';dealerId=999} $null
Assert ($first.StatusCode -eq 200) 'Valid login succeeds'
$login = $first.Content | ConvertFrom-Json
Assert ($login.tokenType -eq 'Bearer') 'Response declares Bearer token'
Assert (([datetime]$login.expiresAtUtc - [datetime]::UtcNow).TotalMinutes -gt 29) 'Token expires in approximately 30 minutes'
$me = Request '/api/auth/me' $null $login.accessToken
Assert ($me.StatusCode -eq 200) 'Valid token authorizes protected endpoint'
$id1 = ($me.Content | ConvertFrom-Json).dealerId
Assert ($id1 -ne 999) 'Client-supplied dealer ID is ignored'
$second = Request '/api/auth/login' @{email='dealer2@example.com';password='DemoPassword123!'} $null
Assert ($second.StatusCode -eq 200) 'Second dealer can log in'
$me2 = Request '/api/auth/me' $null ($second.Content | ConvertFrom-Json).accessToken
Assert (($me2.Content | ConvertFrom-Json).dealerId -ne $id1) 'Dealers receive distinct authenticated IDs'
$wrong = Request '/api/auth/login' @{email='dealer1@example.com';password='wrong'} $null
$unknown = Request '/api/auth/login' @{email='unknown@example.com';password='wrong'} $null
Assert ($wrong.StatusCode -eq 401 -and $unknown.StatusCode -eq 401 -and ($wrong.Content | ConvertFrom-Json).message -eq ($unknown.Content | ConvertFrom-Json).message) 'Invalid credentials return the same 401 error'
$invalid = Request '/api/auth/login' @{email='bad';password=''} $null
Assert ($invalid.StatusCode -eq 400) 'Invalid input returns 400'
$missing = Request '/api/auth/me' $null $null
Assert ($missing.StatusCode -eq 401) 'Missing token returns 401'
$parts = $login.accessToken.Split('.')
$parts[2] = $(if ($parts[2][0] -eq 'a') {'b'} else {'a'}) + $parts[2].Substring(1)
$tampered = Request '/api/auth/me' $null ($parts -join '.')
Assert ($tampered.StatusCode -eq 401) 'Tampered signature returns 401'


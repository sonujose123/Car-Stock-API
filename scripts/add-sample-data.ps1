param([string]$BaseUrl = 'http://127.0.0.1:5187')
$ErrorActionPreference = 'Stop'
$samples = @(
    @{Email='dealer1@example.com';Make='Audi';Model='A4';Year=2018;Stock=3},
    @{Email='dealer1@example.com';Make='Toyota';Model='Corolla';Year=2022;Stock=5},
    @{Email='dealer1@example.com';Make='Ford';Model='Focus';Year=2020;Stock=0},
    @{Email='dealer2@example.com';Make='Audi';Model='A4';Year=2018;Stock=8},
    @{Email='dealer2@example.com';Make='Mazda';Model='CX-5';Year=2021;Stock=2},
    @{Email='dealer2@example.com';Make='Hyundai';Model='i30';Year=2023;Stock=4}
)
foreach ($sample in $samples) {
    $login = Invoke-RestMethod "$BaseUrl/api/auth/login" -Method Post -ContentType application/json -Body (@{email=$sample.Email;password='DemoPassword123!'} | ConvertTo-Json)
    $response = Invoke-WebRequest "$BaseUrl/api/cars" -Method Post -ContentType application/json -Headers @{Authorization="Bearer $($login.accessToken)"} -Body (@{make=$sample.Make;model=$sample.Model;year=$sample.Year;stockLevel=$sample.Stock} | ConvertTo-Json) -SkipHttpErrorCheck
    if ($response.StatusCode -notin @(201,409)) { throw "Seed failed: $($response.Content)" }
    Write-Host "$($sample.Email): $($sample.Make) $($sample.Model) - HTTP $($response.StatusCode)"
}

param([string]$BaseUrl = 'http://127.0.0.1:5187')
$ErrorActionPreference = 'Stop'
function Request($Method, $Path, $Body, $Token) {
    $parameters = @{ Uri = "$BaseUrl$Path"; Method = $Method; SkipHttpErrorCheck = $true }
    if ($null -ne $Body) {
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
$login1 = Request 'POST' '/api/auth/login' @{email='dealer1@example.com';password='DemoPassword123!'} $null
$login2 = Request 'POST' '/api/auth/login' @{email='dealer2@example.com';password='DemoPassword123!'} $null
$token1 = ($login1.Content | ConvertFrom-Json).accessToken
$token2 = ($login2.Content | ConvertFrom-Json).accessToken
$make = 'Test' + [guid]::NewGuid().ToString('N')
$id1 = $null
$id2 = $null
try {
    $body = @{ make = " $make "; model = ' A4 '; year = 2018; stockLevel = 3; dealerId = 999 }
    $created = Request 'POST' '/api/cars' $body $token1
    Assert ($created.StatusCode -eq 201) 'Add returns 201'
    $car = $created.Content | ConvertFrom-Json
    $id1 = $car.id
    Assert ($car.make -eq $make -and $car.model -eq 'A4' -and $car.stockLevel -eq 3) 'Create trims names and returns stock'
    Assert ($created.Headers.Location -eq "/api/cars/$id1") 'Create returns Location header'
    $own = Request 'GET' "/api/cars/$id1" $null $token1
    Assert ($own.StatusCode -eq 200) 'Owner can get car; supplied dealer ID was ignored'
    $body.make = $make.ToLowerInvariant()
    $duplicate = Request 'POST' '/api/cars' $body $token1
    Assert ($duplicate.StatusCode -eq 409) 'Case-insensitive duplicate returns 409'
    $other = Request 'POST' '/api/cars' $body $token2
    Assert ($other.StatusCode -eq 201) 'Different dealer can hold same make/model/year'
    $id2 = ($other.Content | ConvertFrom-Json).id
    $list = Request 'GET' '/api/cars' $null $token1
    $items = @($list.Content | ConvertFrom-Json)
    Assert ($items.id -contains $id1 -and $items.id -notcontains $id2) 'List includes own stock and excludes other dealer'
    $search = Request 'GET' "/api/cars?make=$($make.ToLowerInvariant())&model=a" $null $token1
    $found = @($search.Content | ConvertFrom-Json)
    Assert ($search.StatusCode -eq 200 -and $found.Count -eq 1 -and $found[0].id -eq $id1) 'Combined partial search ignores case and isolates dealer'
    $noMatch = Request 'GET' "/api/cars?make=$make&model=NoSuchModel" $null $token1
    Assert (@($noMatch.Content | ConvertFrom-Json).Count -eq 0) 'Combined filters require both matches'
    $injection = Request 'GET' '/api/cars?make=%27%20OR%201%3D1%20--' $null $token1
    Assert ($injection.StatusCode -eq 200 -and @($injection.Content | ConvertFrom-Json).Count -eq 0) 'SQL-like search text is treated as data'
    foreach ($method in @('GET', 'PUT', 'DELETE')) {
        $path = if ($method -eq 'PUT') { "/api/cars/$id1/stock" } else { "/api/cars/$id1" }
        $data = if ($method -eq 'PUT') { @{stockLevel=99;dealerId=999;id=$id2} } else { $null }
        $denied = Request $method $path $data $token2
        Assert ($denied.StatusCode -eq 404) "Other dealer cannot $method car"
    }
    foreach ($case in @(
        @{Token=$token1;Own=$id1;Foreign=$id2;Label='Dealer 1'},
        @{Token=$token2;Own=$id2;Foreign=$id1;Label='Dealer 2'}
    )) {
        foreach ($query in @('/api/cars', "/api/cars?make=$make", '/api/cars?model=A4', "/api/cars?make=$make&model=A4")) {
            $response = Request 'GET' $query $null $case.Token
            $ids = @($response.Content | ConvertFrom-Json).id
            Assert ($response.StatusCode -eq 200 -and $ids -contains $case.Own -and $ids -notcontains $case.Foreign) "$($case.Label): list/search excludes foreign stock"
        }
        foreach ($method in @('GET', 'PUT', 'DELETE')) {
            $route = if ($method -eq 'PUT') { "/api/cars/$($case.Foreign)/stock" } else { "/api/cars/$($case.Foreign)" }
            $payload = if ($method -eq 'PUT') { @{stockLevel=99;dealerId=999;id=$case.Own} } else { $null }
            $denied = Request $method $route $payload $case.Token
            Assert ($denied.StatusCode -eq 404) "$($case.Label): foreign $method rejected"
        }
        $ownCar = Request 'GET' "/api/cars/$($case.Own)" $null $case.Token
        Assert ($ownCar.StatusCode -eq 200 -and ($ownCar.Content | ConvertFrom-Json).stockLevel -eq 3) "$($case.Label): own stock is still accessible and unchanged"
    }
    $unchanged = Request 'GET' "/api/cars/$id1" $null $token1
    Assert (($unchanged.Content | ConvertFrom-Json).stockLevel -eq 3) 'Unauthorized writes did not change stock'
    foreach ($unused in 1..2) {
        $updated = Request 'PUT' "/api/cars/$id1/stock" @{stockLevel=10;id=$id2} $token1
        Assert ($updated.StatusCode -eq 200 -and ($updated.Content | ConvertFrom-Json).stockLevel -eq 10) 'PUT sets stock to 10 and route controls car ID'
    }
    foreach ($badStock in @(@{stockLevel=-1}, @{})) {
        $invalid = Request 'PUT' "/api/cars/$id1/stock" $badStock $token1
        Assert ($invalid.StatusCode -eq 400) 'Negative or missing stock is rejected'
    }
    $invalidCar = Request 'POST' '/api/cars' @{make=' ';model='';year=1800;stockLevel=-1} $token1
    Assert ($invalidCar.StatusCode -eq 400) 'Invalid car fields return 400'
    $longSearch = Request 'GET' ("/api/cars?make=" + ('a' * 101)) $null $token1
    Assert ($longSearch.StatusCode -eq 400) 'Overlong search is rejected'
    $anonymous = Request 'GET' '/api/cars' $null $null
    Assert ($anonymous.StatusCode -eq 401) 'Anonymous list is rejected'
    $deleted = Request 'DELETE' "/api/cars/$id1" $null $token1
    Assert ($deleted.StatusCode -eq 200 -and ($deleted.Content | ConvertFrom-Json).message) 'Owner deletes car with JSON confirmation'
    $missing = Request 'GET' "/api/cars/$id1" $null $token1
    Assert ($missing.StatusCode -eq 404) 'Deleted car returns 404'
    $again = Request 'DELETE' "/api/cars/$id1" $null $token1
    Assert ($again.StatusCode -eq 404) 'Repeated delete returns 404'
    $missingUpdate = Request 'PUT' "/api/cars/$id1/stock" @{stockLevel=1} $token1
    Assert ($missingUpdate.StatusCode -eq 404) 'Updating deleted car returns 404'
    $stillThere = Request 'GET' "/api/cars/$id2" $null $token2
    Assert ($stillThere.StatusCode -eq 200 -and ($stillThere.Content | ConvertFrom-Json).stockLevel -eq 3) 'Other dealer stock remains unchanged'
}
finally {
    if ($id1) { $null = Request 'DELETE' "/api/cars/$id1" $null $token1 }
    if ($id2) { $null = Request 'DELETE' "/api/cars/$id2" $null $token2 }
}


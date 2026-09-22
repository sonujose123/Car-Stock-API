param([string]$BaseUrl = 'http://127.0.0.1:5187')
$ErrorActionPreference = 'Stop'
function Check($Method, $Path, $Body, $Token, $Expected, $Field = $null, $ContentType = 'application/json') {
    $p = @{Uri="$BaseUrl$Path";Method=$Method;SkipHttpErrorCheck=$true}
    if ($null -ne $Body) { $p.Body=$Body; $p.ContentType=$ContentType }
    if ($Token) { $p.Headers=@{Authorization="Bearer $Token"} }
    $response = Invoke-WebRequest @p
    if ($response.StatusCode -ne $Expected) { throw "$Method $Path expected $Expected; got $($response.StatusCode): $($response.Content)" }
    $errorBody = $response.Content | ConvertFrom-Json
    if ($errorBody.status -ne $Expected -or !$errorBody.message -or !$errorBody.traceId -or $null -eq $errorBody.errors) { throw "Wrong error shape: $($response.Content)" }
    if ($response.Headers.'Content-Type' -notmatch 'application/json') { throw 'Wrong content type' }
    if ($Field -and !$errorBody.errors.$Field) { throw "Missing field error: $Field" }
    Write-Host "PASS: $Method $Path returns consistent JSON $Expected"
}
$login = Invoke-RestMethod "$BaseUrl/api/auth/login" -Method Post -ContentType application/json -Body '{"email":"dealer1@example.com","password":"DemoPassword123!"}'
$token = $login.accessToken
Check GET /api/cars $null $null 401
Check GET /api/cars $null 'invalid.token.here' 401
Check GET /not-a-route $null $null 404
Check PATCH /api/cars $null $token 405
Check POST /api/auth/login '{"email":"bad","password":""}' $null 400 email
Check POST /api/auth/login '{"email":"dealer1@example.com","password":"wrong"}' $null 401
foreach ($body in @('{}', '{"make":null,"model":" ","year":1800,"stockLevel":-1}')) {
    Check POST /api/cars $body $token 400 make
}
foreach ($body in @('{', 'null', '[]', '{"year":"oops"}', '{"stockLevel":1.5}', '{"stockLevel":2147483648}')) {
    Check POST /api/cars $body $token 400
}
Check POST /api/cars '{}' $token 415 $null 'text/plain'
foreach ($id in @('0','-1','abc','9223372036854775808')) {
    Check GET "/api/cars/$id" $null $token 400 id
    Check PUT "/api/cars/$id/stock" '{"stockLevel":1}' $token 400 id
    Check DELETE "/api/cars/$id" $null $token 400 id
}
Check PUT /api/cars/1/stock '{}' $token 400 stockLevel
Check PUT /api/cars/1/stock '{"stockLevel":-1}' $token 400 stockLevel
Check GET '/api/cars/9223372036854775807' $null $token 404
Check GET ("/api/cars?make=" + ('a' * 101)) $null $token 400 make
Check GET ("/api/cars?model=" + ('a' * 101)) $null $token 400 model

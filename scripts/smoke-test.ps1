[CmdletBinding()]
param(
    [string]$BaseUrl = 'http://localhost:5110',
    [string]$Email = 'demo@keyloop.local',
    [string]$Password = 'Demo123!ChangeMe'
)

$ErrorActionPreference = 'Stop'

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "== $Message ==" -ForegroundColor Cyan
}

Write-Step "Health"
$live = Invoke-WebRequest "$BaseUrl/health/live" -UseBasicParsing
$ready = Invoke-WebRequest "$BaseUrl/health/ready" -UseBasicParsing
Write-Host "live=$($live.StatusCode) ready=$($ready.Content)"

Write-Step "Login"
$login = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/v1/authentication/login" `
    -ContentType 'application/json' `
    -Body (@{ email = $Email; password = $Password } | ConvertTo-Json -Compress)
$headers = @{ Authorization = "Bearer $($login.accessToken)" }
Write-Host "authenticated as $($login.email) ($($login.role))"

Write-Step "Reference data"
$dealerships = Invoke-RestMethod "$BaseUrl/api/v1/dealerships" -Headers $headers
$serviceTypes = Invoke-RestMethod "$BaseUrl/api/v1/servicetypes" -Headers $headers
$customers = Invoke-RestMethod "$BaseUrl/api/v1/customers" -Headers $headers
$vehicles = Invoke-RestMethod "$BaseUrl/api/v1/customers/1/vehicles" -Headers $headers
Write-Host "dealerships=$($dealerships.Count) serviceTypes=$($serviceTypes.Count) customers=$($customers.Count) vehiclesForCustomer1=$($vehicles.Count)"

$date = (Get-Date).Date.AddDays(1)
while ($date.DayOfWeek -in @([DayOfWeek]::Saturday, [DayOfWeek]::Sunday)) {
    $date = $date.AddDays(1)
}
$dateText = $date.ToString('yyyy-MM-dd')

Write-Step "Availability on $dateText"
$slots = Invoke-RestMethod "$BaseUrl/api/v1/dealerships/1/availability?serviceTypeId=1&date=$dateText" -Headers $headers
if (-not $slots -or $slots.Count -eq 0) {
    throw "No availability returned for $dateText. Choose another date."
}
$slot = $slots | Select-Object -First 1
Write-Host "availableSlots=$($slots.Count) firstSlot=$($slot.startUtc)"

Write-Step "Create appointment"
$startTime = ([DateTimeOffset]$slot.startUtc).ToString('o')
$payload = @{
    customerId = 1
    vehicleId = 1
    dealershipId = 1
    serviceTypeId = 1
    startTime = $startTime
} | ConvertTo-Json -Compress

$created = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/v1/appointments" `
    -Headers $headers -ContentType 'application/json' -Body $payload
Write-Host "created=$($created.id) status=$($created.status)"
Write-Host "technician=$($created.technician.name) bay=$($created.serviceBay.name)"
Write-Host "window=$($created.startUtc) .. $($created.endUtc)"

Write-Step "Read appointment"
$fetched = Invoke-RestMethod "$BaseUrl/api/v1/appointments/$($created.id)" -Headers $headers
Write-Host "status=$($fetched.status) customer=$($fetched.customer.fullName)"
Write-Host "vehicle=$($fetched.vehicle.make) $($fetched.vehicle.model) ($($fetched.vehicle.vin))"

Write-Step "Cancel appointment (idempotent)"
$cancelled = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/v1/appointments/$($created.id)/cancel" -Headers $headers
$cancelledAgain = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/v1/appointments/$($created.id)/cancel" -Headers $headers
Write-Host "first=$($cancelled.status) second=$($cancelledAgain.status)"

Write-Step "Result"
Write-Host "Smoke test passed." -ForegroundColor Green

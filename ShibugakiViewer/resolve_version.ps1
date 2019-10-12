$fileContents = Get-Content "VersionInformation_base.txt"

$gitRevShort = (git log -1 --format="%h")
$gitRevShortNewStr='GitRevisionShort = "'+$gitRevShort+'";'
$fileContents = $fileContents -replace 'GitRevisionShort = .*;$', $gitRevShortNewStr


$gitRevLong = (git log -1 --format="%H")
$gitRevLongNewStr='GitRevision = "'+$gitRevLong+'";'
$fileContents = $fileContents -replace 'GitRevision = .*;$', $gitRevLongNewStr

$nowUnixTime = [Math]::Floor(((Get-Date) - (Get-Date("1970/1/1 0:0:0 GMT"))).TotalSeconds)
$buildTimeNewStr='BuildTime = '+$nowUnixTime+';'
$fileContents = $fileContents -replace 'BuildTime = .*;$', $buildTimeNewStr

$fileContents > "VersionInformation.cs"


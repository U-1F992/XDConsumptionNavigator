.\publish.bat
$now = [DateTime]::Now.ToString("yyyyMMddhhmmss")
git tag $now
git push origin $now
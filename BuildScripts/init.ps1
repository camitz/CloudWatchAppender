$parsedReleaseBuildVersion = $env:APPVEYOR_REPO_TAG_NAME -Match "(\d+.\d+.\d+(.\d+)?)"
    
If($env:appveyor_repo_tag -AND $parsedReleaseBuildVersion) {
	$env:BuildVersion = $matches[0]
	$env:IsGithubRelease = $TRUE
}
else {
	$env:BuildVersion = $env:appveyor_build_version
	$env:IsGithubRelease = ""
}

Write-Host "Build Version: " $env:BuildVersion

Write-Host "appveyor_build_version Variable: " $env:appveyor_build_version

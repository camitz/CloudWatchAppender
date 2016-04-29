$parsedReleaseBuildVersion = $env:APPVEYOR_REPO_TAG_NAME -Match "\d+\.\d+(\.\d+)?(\.\d+)?(-(alpha|beta|pre)\.?\d*)?"
    
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

try
{
	if($env:IsGithubRelease)
	{
		$origin = git config --get remote.origin.url
		if ($origin -like "https://github.com/*.git")
		{
			$startToTrim = "https://github.com/"
			$endToTrim = ".git"

			$releaseUrl = $origin.Substring($startToTrim.Length, $origin.Length - $startToTrim.Length - $endToTrim.Length)
			
			$releaseUrl = "https://api.github.com/repos/" + $releaseUrl + "/releases/tags/" + $env:APPVEYOR_REPO_TAG_NAME

			$resp = invoke-webrequest $releaseUrl
			$release = $resp.Content | ConvertFrom-Json

			foreach ($nuspecPath in (Get-ChildItem -Filter *.nuspec -Recurse ))
			{

				[xml]$nuspec = Get-Content $nuspecPath.FullName

				$nuspec.package.metadata.releaseNotes = $release.Body
				$nuspec.package.metadata.version = $env:BuildVersion
				
				$nuspec.Save( $nuspecPath.FullName )
			}
		}
	}
}
catch 
{
	Write-Host $_.Exception.GetType().FullName, $_.Exception.Message
}

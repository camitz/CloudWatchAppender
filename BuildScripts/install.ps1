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

			$nuspecPath = (Get-ChildItem -Filter *.nuspec -Recurse )[0].FullName
			[xml]$nuspec = Get-Content $nuspecPath

			$nuspec.package.metadata.releaseNotes = $release.Body
			$nuspec.package.metadata.version = $env:BuildVersion
			echo 
			$nuspec.Save( $nuspecPath )
		}
	}
}
catch 
{
	Write-Host $_.Exception.GetType().FullName, $_.Exception.Message
}

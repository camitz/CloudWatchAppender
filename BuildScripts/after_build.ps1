#Create Folder Structure
New-Item -Force -ItemType directory -Path build\lib\net35
New-Item -Force -ItemType directory -Path build\lib\net45

#Copy Files into folders
Copy-Item -Force "CloudWatchAppender3.5\bin\Release\CloudWatchAppender.dll" "build\lib\net35"
Copy-Item -Force "CloudWatchAppender3.5\bin\Release\CloudWatchAppender.pdb" "build\lib\net35"
Copy-Item -Force "CloudWatchAppender\bin\Release\CloudWatchAppender.pdb" "build\lib\net45"
Copy-Item -Force "CloudWatchAppender\bin\Release\CloudWatchAppender.dll" "build\lib\net45"

$nuspecPath = (Get-ChildItem -Filter *.nuspec -Recurse )[0].FullName

nuget pack $nuspecPath

Remove-Item -Force build\lib\net35
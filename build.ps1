Remove-Item -Recurse dist

mkdir dist\dblyr

Push-Location dblyr\server
dotnet publish -c Release
Pop-Location

Copy-Item -Recurse -Force dblyr\__resource.lua dist\dblyr
Copy-Item -Recurse -Force dblyr\include dist\dblyr\include
Copy-Item -Recurse -Force dblyr-compat dist\dblyr-compat

mkdir dist\dblyr\server\bin\Release\netstandard2.0\publish\
Copy-Item -Recurse -Force dblyr\server\bin\Release\netstandard2.0\publish dist\dblyr\server\bin\Release\netstandard2.0\

Compress-Archive -Path dist\* -CompressionLevel Optimal -DestinationPath dist\dblyr
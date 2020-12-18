$configuration = 'Release'
$framework = 'netcoreapp3.0'
$ostw_version = 'v2.0'

# Cross platform, no runtime included.
'* Publishing self-contained'
dotnet publish --configuration $configuration --framework $framework
# Folder: [project_file_folder]/bin/[configuration]/[framework]/publish/
$cross_platform_info = 'crossplatform{0}{1}' -f ([Environment]::NewLine), $ostw_version
New-Item -Path ./bin/$configuration/$framework/publish/ -Name 'Version' -Value $cross_platform_info
Compress-Archive -Path ./bin/$configuration/$framework/publish -DestinationPath ./bin/$ostw_version-crossplatform.zip -CompressionLevel Optimal

# Runtimes
$runtimes = 'win-x64', 'win-x86', 'linux-x64'
foreach ($runtime in $runtimes) {
    '* Publishing ' + $runtime
    dotnet publish --runtime $runtime --configuration $configuration --framework $framework
    # Folder: [project_file_folder]/bin/[configuration]/[framework]/[runtime]/publish/
    $runtime_info = '{0}{1}{2}' -f $runtime, ([Environment]::NewLine), $ostw_version
    New-Item -Path ./bin/$configuration/$framework/$runtime/publish/ -Name 'Version' -Value $runtime_info
    Compress-Archive -Path ./bin/$configuration/$framework/$runtime/publish -DestinationPath ./bin/$ostw_version-$runtime.zip -CompressionLevel Optimal
}

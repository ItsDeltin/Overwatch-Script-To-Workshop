$configuration = 'Release'
$framework = 'netcoreapp3.0'
$ostw_version = 'v2.0-beta.11'

# Cross platform, no runtime included.
'* Publishing self-contained'
$cross_platform_folder_name = '{0}-crossplatform' -f $ostw_version

dotnet publish --configuration $configuration --framework $framework --output bin/$configuration/$cross_platform_folder_name
# Folder: [project_file_folder]/bin/[configuration]/[framework]/publish/
$cross_platform_info = 'crossplatform{0}{1}' -f ([Environment]::NewLine), $ostw_version
New-Item -Path ./bin/$configuration/$cross_platform_folder_name/ -Name 'Version' -Value $cross_platform_info
Compress-Archive -Path ./bin/$configuration/$cross_platform_folder_name -DestinationPath ./bin/$cross_platform_folder_name.zip -CompressionLevel Optimal

# Runtimes
$runtimes = 'win-x64', 'win-x86', 'linux-x64'
foreach ($runtime in $runtimes) {
    '* Publishing ' + $runtime
    $runtime_folder_name = '{0}-{1}' -f $ostw_version, $runtime

    dotnet publish --runtime $runtime --configuration $configuration --framework $framework --output bin/$configuration/$runtime_folder_name
    # Folder: [project_file_folder]/bin/[configuration]/[framework]/[runtime]/publish/
    $runtime_info = '{0}{1}{2}' -f $runtime, ([Environment]::NewLine), $ostw_version
    New-Item -Path ./bin/$configuration/$runtime_folder_name/ -Name 'Version' -Value $runtime_info
    Compress-Archive -Path ./bin/$configuration/$runtime_folder_name -DestinationPath ./bin/$runtime_folder_name.zip -CompressionLevel Optimal
}

@echo off

echo Publishing...
dotnet publish -c Release -o published

IF "%1"=="full" (
    echo Full deployment...

    echo Clearing remote folder...
    ssh voice "rm -rf ~/api/*"

    echo Uploading full contents...
    scp -r published/* voice:~/api
) ELSE (
    echo Partial deployment...

    echo Uploading WhisperAPI.dll only...
    scp published/WhisperAPI.dll voice:~/api/
)

echo Cleaning up...
rmdir /s /q published

echo Done.

@echo off

echo Publishing...
dotnet publish -c Release -o published

echo Clearing remote folder...
ssh voice-pi1 "rm -rf ~/runme/*"

echo Uploading...
scp -r published/* voice-pi1:~/runme

echo Cleaning up...
rmdir /s /q published

echo Done.

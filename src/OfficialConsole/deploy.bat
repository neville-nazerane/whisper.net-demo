@echo off

echo Publishing...
dotnet publish -c Release -o published

echo Clearing remote folder...
ssh voice "rm -rf ~/official/*"

echo Uploading...
scp -r published/* voice:~/official

echo Cleaning up...
rmdir /s /q published

echo Done.

@echo off

echo Publishing...
dotnet publish -c Release -o published

echo Clearing remote folder...
ssh voice "rm -rf ~/api/*"

echo Uploading...
scp -r published/* voice:~/api

echo Cleaning up...
rmdir /s /q published

echo Done.

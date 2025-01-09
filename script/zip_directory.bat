@echo off
setlocal

:: D�finir les variables
set "sourceDirectory=%1"
set "zipFile=%sourceDirectory%.zip"

:: V�rifier si le r�pertoire source existe
if not exist "%sourceDirectory%" (
    echo Le r�pertoire source n'existe pas.
    exit /b 1
)

:: Supprimer le fichier ZIP s'il existe d�j�
if exist "%zipFile%" (
    del "%zipFile%"
)

:: Utiliser PowerShell pour zipper les fichiers
powershell -command "Compress-Archive -Path '%sourceDirectory%\*' -DestinationPath '%zipFile%'"

:: V�rifier si le fichier ZIP a �t� cr�� avec succ�s
if exist "%zipFile%" (
    echo Les fichiers ont �t� zipp�s avec succ�s dans %zipFile%.
) else (
    echo Erreur lors de la cr�ation de l'archive ZIP.
    exit /b 1
)

endlocal
exit /b 0

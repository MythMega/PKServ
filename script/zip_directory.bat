@echo off
setlocal

:: Définir les variables
set "sourceDirectory=%1"
set "zipFile=%sourceDirectory%.zip"

:: Vérifier si le répertoire source existe
if not exist "%sourceDirectory%" (
    echo Le répertoire source n'existe pas.
    exit /b 1
)

:: Supprimer le fichier ZIP s'il existe déjà
if exist "%zipFile%" (
    del "%zipFile%"
)

:: Utiliser PowerShell pour zipper les fichiers
powershell -command "Compress-Archive -Path '%sourceDirectory%\*' -DestinationPath '%zipFile%'"

:: Vérifier si le fichier ZIP a été créé avec succès
if exist "%zipFile%" (
    echo Les fichiers ont été zippés avec succès dans %zipFile%.
) else (
    echo Erreur lors de la création de l'archive ZIP.
    exit /b 1
)

endlocal
exit /b 0

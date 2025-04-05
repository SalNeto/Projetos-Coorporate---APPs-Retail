@echo off
setlocal enabledelayedexpansion

set "sourcePath=C:\Temp_Arq_Calc_Tax\Arq_Cone\IN\Erro"
set "destinationPath=C:\Temp_Arq_Calc_Tax\Arq_Cone\IN"
set "searchString=baseId\": 999,"
set "replaceString=baseId\": 21,"

:: Gera o nome do arquivo de log com base na data e hora
set "logFile=C:\Temp_Arq_Calc_Tax\Log_Taxrule\Log_%date:~-4%_%date:~3,2%_%date:~0,2%_%time:~0,2%_%time:~3,2%_%time:~6,2%.txt"

:: Lista os arquivos que contêm a string a ser alterada
echo Arquivos a serem alterados:
for %%i in ("%sourcePath%\*.json") do (
    findstr /C:"%searchString%" "%%i" >nul
    if !errorlevel! equ 0 (
        echo %%i
    )
)

:: Gera o arquivo de log com a data e hora da execução
echo Execução do script em !date! !time! >> "%logFile%"
echo. >> "%logFile%"
echo Arquivos a serem alterados: >> "%logFile%"
for %%i in ("%sourcePath%\*.json") do (
    findstr /C:"%searchString%" "%%i" >nul
    if !errorlevel! equ 0 (
        echo %%i >> "%logFile%"
    )
)

:: Realiza a substituição em todas as ocorrências da string no diretório de origem
set "counter=0"
for %%i in ("%sourcePath%\*.json") do (
    findstr /C:"%searchString%" "%%i" >nul
    if !errorlevel! equ 0 (
        echo %%i
        set "outputFile=%destinationPath%\%%~nxi"
        powershell -command "& {(Get-Content '%%i') -replace '%searchString%', '%replaceString%' | Set-Content -Path '!outputFile!'}"
        move /Y "%outputFile%" "%destinationPath%"
        del "%%i" /Q
        set /a counter+=1
    )
)

echo.
echo Quantidade total de registros alterados: %counter%
echo Quantidade total de registros alterados: %counter% >> "%logFile%"

echo.
echo Log gerado em: %logFile%

endlocal

@echo off

set "dirs=C:\Temp_Arq_Calc_Tax\Arq_Gamma\Processed C:\Temp_Arq_Calc_Tax\Arq_Sovos\Request\Processed C:\Temp_Arq_Calc_Tax\Arq_Sovos\Responser\Processed C:\Temp_Arq_Calc_Tax\Arq_Cone\IN\Processed"

for %%d in (%dirs%) do (
    pushd "%%d"
    del /q /s *.* > nul 2>&1
    popd
)

exit /b 0

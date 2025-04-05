net use z: \\10.11.18.86\Brazil
copy Z:\Production\C-ONE-TXT\IN\*.TXT C:\Temp_Arq_Calc_Tax\Arq_Gamma\Arq_Masterdata
move Z:\Production\C-ONE-TXT\IN\*.TXT Z:\Production\C-ONE-TXT\IN\BKP
copy C:\Temp_Arq_Calc_Tax\Arq_Gamma\Arq_Masterdata\Processed\*.txt Z:\Production\C-ONE-TXT\IN\Processed /Y
del C:\Temp_Arq_Calc_Tax\Arq_Gamma\Arq_Masterdata\Processed\*.txt /Y
net use z: /delete /Y
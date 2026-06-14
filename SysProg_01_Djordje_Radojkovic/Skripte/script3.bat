@echo off
:: This loop runs your command 5 times sequentially
for /L %%i in (1, 1, 50) do (
    echo Running iteration %%i
    curl http://localhost:5050/fajl3.txt
)
pause
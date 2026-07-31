to delete bin obj and unwanted folders created by npm run ng serve and dotnet run


find . -type d \( -name bin -o -name obj -o -name node_modules -o -name dist -o -name build -o -name .angular -o -name coverage -o -name TestResults \) -prune -exec rm -rf {} +
# Vault

Расширение Autodesk Vault Explorer, добавляющее кнопку в редакторе Item для автоматического заполнения свойств `Part Number (Item)` и `REV (Item)` текущего редактируемого элемента.

## Сборка

1. Установите Autodesk Vault Client на рабочую станцию. Необходимые библиотеки (`Autodesk.Connectivity.Extensibility.Framework.dll`, `Autodesk.Connectivity.Explorer.Extensibility.dll`, `Autodesk.Connectivity.WebServices.dll`, `Autodesk.Connectivity.WebServicesTools.dll`) входят в поставку клиента.
2. При необходимости скорректируйте пути к библиотекам в файле проекта `src/AddItemProperties/AddItemProperties.csproj`.
3. Выполните сборку проекта:

   ```bash
   dotnet build src/AddItemProperties/AddItemProperties.csproj
   ```

Полученный DLL-файл расположен в `src/AddItemProperties/bin/Debug/net48` (или `Release` при соответствующей конфигурации).

## Развертывание

1. Скопируйте `AddItemProperties.dll` в папку `%ProgramData%\Autodesk\Vault 2024\Extensions` (путь может отличаться для другой версии Vault).
2. Перезапустите Vault Explorer. В редакторе Item появится кнопка **Добавить Part/REV**.

## Использование

1. Откройте Item в Vault Explorer и возьмите его на редактирование.
2. Нажмите кнопку **Добавить Part/REV** на панели команд редактора.
3. Расширение считает значения из текущего Item и заполнит свойства `Part Number (Item)` и `REV (Item)` (если свойства существуют в хранилище).

При ошибках загрузки или отсутствии нужных свойств будет показано окно с описанием проблемы.

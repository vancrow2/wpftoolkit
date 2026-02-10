# InfoScope Developer Tool-Kit

A **Windows 10+** célra készült, hordozható WPF asztali alkalmazás, amely plugin-szerű fejlesztői eszközöket futtat.

## Fő jellemzők

- C# + WPF (.NET 8 LTS)
- MVVM architektúra `CommunityToolkit.Mvvm` csomaggal
- Eszközök automatikus felismerése `tools` mappából reflection segítségével
- Valós idejű napló konzol + futási folyamatjelző
- Futtatás / megszakítás támogatás (`CancellationToken`)
- Beállítások mentése JSON fájlba (`%AppData%\InfoScope Developer Tool-Kit\settings.json`)
- Diagnosztikai csomag export (beállítások + logok ZIP)

## Projektstruktúra

- `App` – WPF felület és MVVM réteg
- `Core` – domain logika, interfészek, szolgáltatások
- `Tools.Sample` – minta plugin eszközök
- `Tests` – egységtesztek

## Build lépések

```bash
dotnet restore
 dotnet build InfoScopeDeveloperToolkit.sln
```

## Futtatás fejlesztői módban

```bash
dotnet run --project App/App.csproj
```

Induláskor az alkalmazás a futtatási mappában található `tools` könyvtárból tölti be a pluginokat.

## Publikálás (hordozható mappa)

```bash
dotnet publish App/App.csproj -c Release -r win-x64 --self-contained false -o publish
```

A `publish` mappa közvetlenül másolható és futtatható telepítő nélkül.

## Minta eszközök

1. **Fájl SHA-256 hash**
   - bemenet: fájlútvonal
   - opcionális kimenet: hash mentése fájlba
2. **Mappa tartalom export CSV**
   - bemenet: mappa útvonala
   - kimenet: CSV fájl

## Új eszköz hozzáadása

1. Hozz létre új Class Library projektet (pl. `Tools.MyTools`).
2. Hivatkozd a `Core` projektet.
3. Implementáld az `ITool` interfészt:
   - `Id`, `Name`, `Description`
   - `ParameterDefinitions`
   - `RunAsync(ToolExecutionContext, CancellationToken)`
4. Buildeld a projektet, majd másold a DLL-t az alkalmazás `tools` mappájába.
5. Következő induláskor az alkalmazás automatikusan felismeri.

## Tesztek

```bash
dotnet test InfoScopeDeveloperToolkit.sln
```

A tesztek lefedik a plugin felismerést, a beállítások mentés/betöltés működést és a core futtatási logikát.

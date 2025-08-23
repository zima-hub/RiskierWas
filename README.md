# Riskier was! (WPF, .NET 8)

## Build
- Öffnen Sie den Ordner `RiskierWas` in Visual Studio 2022 oder `dotnet`-CLI.
- .NET 8 SDK erforderlich.
- Startprojekt: `RiskierWas.csproj`

## Starten
- Drücken Sie F5 oder `dotnet run` im Ordner `RiskierWas`.
- Die App lädt automatisch `Data/questions.json` neben der EXE, wenn vorhanden.
- Alternativ können Sie über den Startbildschirm eine JSON-Datei laden.

## Veröffentlichen (eine EXE)
```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```
Kopieren Sie den Veröffentlichungsordner und die Datei `Data/questions.json` zusammen.

## Format der Fragen (JSON)
```json
[
  {
    "text": "Fragetext",
    "selected": true,
    "answers": [
      {"text":"Antwort A","correct": true, "comment":"optional"}
    ]
  }
]
```

## Hinweise
- Punkte: +50 pro richtiger Antwort, Falsch -> nächstes Team.
- Option: Punkteverfall (-10% alle 10s) auf dem Startbildschirm aktivierbar.
- Automatisches Aufdecken: sobald alle richtigen **oder** alle falschen Antworten aufgedeckt sind.
- Editor: Fragen links auswählen, Text und Antworten rechts bearbeiten; Export über „JSON exportieren…“.
- Nach einer tollen Excel-Makro-Vorlage von Thommes Volz mit superkreativen Fragen
- Programmiert von Matthias Zimmer mit massiver Hilfe von ChatGPT.

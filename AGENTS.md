# AGENTS.md — KaiserVox

## Projekt
**KaiserVox** — Voice Dictation App für Windows. Echtzeit Speech-to-Text mit Whisper, CUDA GPU, Multi-Language.
**Repo:** `https://github.com/DerNander/kaiservox`
**Stack:** C# / WPF / .NET 8 / Windows-only

## Versioning
- **Aktuell: v1.5.0**
- **Naming:** `KaiserVox-v{VERSION}-win-x64.exe`
- **Schema:** Semantic Versioning (MAJOR.MINOR.PATCH)
- **⚠️ IMMER `gh release list` checken bevor ein Release erstellt wird!**

### Release-Historie
| Version | Datum | Highlights |
|---------|-------|------------|
| v1.5.0 | 08.03.2026 | Autostart Fix (robust registry + verification) |
| v1.4.1 | 28.02.2026 | Latest stable vor Autostart-Fix |
| v1.4.0 | 27.02.2026 | — |
| v1.3.0 | 25.02.2026 | — |
| v1.2.0 | 25.02.2026 | CUDA GPU Support + Model Selection (ERSTES Release!) |
| v1.1.0 | pre-fork | Legacy EasyDictate (NICHT NUTZEN!) |

## Git Config (PFLICHT!)
```bash
git config user.name "DerNander"
git config user.email "5221954+DerNander@users.noreply.github.com"
```
**⚠️ Email MUSS `5221954+DerNander@` sein für GitHub Commit-Linking!**

## Workflow (JEDER Task!)

1. `git pull origin master` — Aktuellen Stand holen
2. Code ändern
3. `dotnet build src/EasyDictate/EasyDictate.csproj` — **MUSS BAUEN! 0 Errors!**
4. `git add -A && git commit -m "<type>: <beschreibung>"` — Conventional Commits
5. `git push origin master` — **PFLICHT! Kein Task ist fertig ohne Push!**

### Release erstellen
```bash
# 1. Version bestimmen (IMMER zuerst checken!)
gh release list --limit 5

# 2. Publishen
dotnet publish src/EasyDictate/EasyDictate.csproj -c Release -r win-x64 \
  --self-contained -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish

# 3. Rename + Release
cp ./publish/KaiserVox.exe "./publish/KaiserVox-v{VERSION}-win-x64.exe"
gh release create v{VERSION} "./publish/KaiserVox-v{VERSION}-win-x64.exe" \
  --title "KaiserVox v{VERSION} - {TITLE}" --notes "{CHANGELOG}"
```

### Commit-Typen
- `fix:` Bugfix | `feat:` Feature | `refactor:` Umbau | `docs:` Docs | `chore:` Build/Config

## Regeln
- **Build MUSS durchlaufen** — 0 Errors
- **Push ist PFLICHT** — Kein Code bleibt lokal
- **Release-Naming:** `KaiserVox-v{VERSION}-win-x64.exe` — NICHT `KaiserVox.exe`!
- **Keine funktionalen Tests auf Linux** — WPF = Windows-only, Alex testet
- **v1.1.0 = LEGACY** — Nicht anfassen, gehört zum alten EasyDictate Fork
- **README.md aktuell halten** bei Feature-Änderungen

## Naming
- **App:** KaiserVox (NICHT EasyDictate!)
- **EXE:** KaiserVox.exe (intern), KaiserVox-v{X}-win-x64.exe (Release)
- **Projekt-Ordner:** `src/EasyDictate/` (historisch, NICHT umbenennen — bricht Build)

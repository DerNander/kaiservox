# AGENTS.md — KaiserVox

## Projekt
**KaiserVox** — Voice Dictation App, WPF .NET 8, CUDA GPU, Multi-Language.
**Repo:** `https://github.com/DerNander/kaiservox`
**Stack:** C# / WPF / .NET 8 / Windows-only (WPF = kein Cross-Platform)

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

### Commit-Typen
- `fix:` — Bugfix
- `feat:` — Neues Feature
- `refactor:` — Code-Umbau ohne Funktionsänderung
- `docs:` — Nur Docs/README
- `chore:` — Build, Config, Dependencies

## Regeln
- **Build MUSS durchlaufen** — 0 Errors, Warnings sind OK
- **Push ist PFLICHT** — Kein Code bleibt lokal
- **Keine funktionalen Tests auf Linux** — WPF läuft nur auf Windows, Alex testet
- **README.md aktuell halten** bei Feature-Änderungen

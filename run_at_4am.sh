#!/bin/bash
# Script schedulato: esegue il task NpcDnaProfile alle 4:00 AM ora italiana
# Creato automaticamente da Claude Code

LOGFILE="/home/user/faster-than-rim-mvp/task_4am.log"
REPO="/home/user/faster-than-rim-mvp"

echo "[$(TZ='Europe/Rome' date)] Script avviato, in attesa delle 4:00 AM..." >> "$LOGFILE"

# Aspetta le 4:00 AM ora italiana (CEST)
while true; do
    HOUR=$(TZ="Europe/Rome" date +%H)
    MIN=$(TZ="Europe/Rome" date +%M)
    if [ "$HOUR" = "04" ] && [ "$MIN" -ge "00" ]; then
        echo "[$(TZ='Europe/Rome' date)] Ora 4:00 AM raggiunta. Avvio task." >> "$LOGFILE"
        break
    fi
    sleep 30
done

cd "$REPO"

PROMPT='Sei Claude Code. Esegui questo task di sviluppo per il progetto Arcontio Unity (C#).

STEP 1 — Leggi i documenti dal branch origin/v0.04:
- Esegui: git fetch origin v0.04
- Esegui: git show origin/v0.04:ARCONTIO_Roadmap_Notion.md
  → Identifica ESATTAMENTE il punto 1 della sezione v0.04 e tienilo in memoria
- Esegui: git show origin/v0.04:"ARCONTIO NPC Architecture v2.docx" > /tmp/npc_arch.docx
- Estrai il testo dal docx:
  python3 -c "import zipfile,re; z=zipfile.ZipFile('"'"'/tmp/npc_arch.docx'"'"'); t=re.sub('"'"'<[^>]+>'"'"','"'"' '"'"',z.open('"'"'word/document.xml'"'"').read().decode()); print(re.sub('"'"' +'"'"','"'"' '"'"',t))"

STEP 2 — Crea il branch di lavoro:
- git checkout -b v0.04.01.a-NPCDnaProfile origin/v0.04

STEP 3 — Implementa il punto 1 della sezione v0.04:
- Basati ESCLUSIVAMENTE su quanto letto nel roadmap (punto 1, v0.04) e sull'"'"'architettura NPC nel docx
- Rispetta le convenzioni del progetto (CLAUDE.md): commenti in italiano, pattern Command, separazione World/View
- Crea i file C# necessari in Assets/Scripts/Core/NPC/ o dove appropriato secondo l'"'"'architettura

STEP 4 — Commit e push:
- git add <file creati/modificati>
- git commit -m "feat(v0.04.01.a): NpcDnaProfile - punto 1 sessione v0.04"
- git push -u origin v0.04.01.a-NPCDnaProfile

IMPORTANTE: Leggi prima i documenti, poi implementa. Non inventare nulla — attieniti a quanto scritto nel roadmap e nel documento architetturale.'

echo "[$(TZ='Europe/Rome' date)] Lancio claude CLI..." >> "$LOGFILE"

claude --dangerously-skip-permissions -p "$PROMPT" >> "$LOGFILE" 2>&1

echo "[$(TZ='Europe/Rome' date)] Task completato. Exit code: $?" >> "$LOGFILE"

# Pulisci lo script
rm -f "$REPO/run_at_4am.sh"

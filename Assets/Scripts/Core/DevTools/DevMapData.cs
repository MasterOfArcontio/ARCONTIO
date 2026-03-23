// Assets/Scripts/Core/DevTools/DevMapData.cs
using System;
using System.Collections.Generic;

namespace Arcontio.Core.DevTools
{
    /// <summary>
    /// DevMapData:
    /// formato serializzabile (JSON) per salvare/caricare rapidamente lo stato della mappa
    /// durante Runtime Developer Mode (DevTools).
    ///
    /// IMPORTANTISSIMO:
    /// - Questo NON è persistence di gameplay.
    /// - È un formato "debug / test map" pensato per:
    ///   - riprodurre scenari
    ///   - creare mappe di test senza ricompilare
    ///
    /// Policy (DevMode v0 - MVP):
    /// - salviamo SOLO gli oggetti piazzati sulla griglia (World.Objects)
    /// - NON salviamo NPC (devmode v1+).
    /// - non salviamo componenti runtime effimeri (UseState, occupazione, ecc.).
    /// </summary>
    [Serializable]
    public sealed class DevMapData
    {
        public int Version = 1;

        // Dimensione della griglia al momento del salvataggio.
        public int Width;
        public int Height;

        // Lista oggetti.
        public List<DevMapObject> Objects = new();

        // Metadata opzionale per debug (non usato in v0).
        public string Note;
    }

    /// <summary>
    /// Record serializzabile per un singolo oggetto piazzato nella griglia.
    /// </summary>
    [Serializable]
    public sealed class DevMapObject
    {
        public string DefId;
        public int X;
        public int Y;

        // Ownership (oggettivo) dell'istanza.
        // Nota: usiamo stringa per stabilità JSON e leggibilità umana.
        public string OwnerKind = "None";
        public int OwnerId = -1;
    }
}

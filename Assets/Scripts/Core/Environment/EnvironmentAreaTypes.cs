using System;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentAreaId
    // =============================================================================
    /// <summary>
    /// <para>
    /// Identificatore value-only di un'area ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: aree come layer, non oggetti</b></para>
    /// <para>
    /// Fertilita', acqua, vegetazione, stanze, territori e aree di caccia devono
    /// poter vivere come layer sovrapposti. Un id dedicato evita di confondere
    /// queste aree con <c>WorldObject</c> o con dati visuali ArcGraph.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Value</b>: intero positivo per aree valide; zero rappresenta assenza.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentAreaId : IEquatable<EnvironmentAreaId>
    {
        public readonly int Value;

        public bool IsValid => Value > 0;

        // =============================================================================
        // EnvironmentAreaId
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un id area normalizzando valori negativi ad assenza.
        /// </para>
        /// </summary>
        public EnvironmentAreaId(int value)
        {
            // Gli id negativi non hanno semantica nel registry ambientale.
            Value = value < 0 ? 0 : value;
        }

        public bool Equals(EnvironmentAreaId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is EnvironmentAreaId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return IsValid ? Value.ToString() : "None";
        }

        public static EnvironmentAreaId None => new EnvironmentAreaId(0);
    }

    // =============================================================================
    // EnvironmentAreaKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tipo logico di area ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: tassonomia stratificata</b></para>
    /// <para>
    /// Una cella puo' appartenere contemporaneamente a piu' aree di tipo diverso.
    /// Questa enum non impone un singolo id per cella: definisce il vocabolario
    /// minimo dei layer ambientali futuri.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Generic</b>: area neutra o temporanea.</item>
    ///   <item><b>Fertility</b>: area di fertilita' o suolo.</item>
    ///   <item><b>Water</b>: area acqua, fiume, lago o pozzanghera.</item>
    ///   <item><b>Vegetation</b>: area di vegetazione diffusa e seed bank.</item>
    ///   <item><b>Room</b>: area interna/chiusa futura.</item>
    ///   <item><b>Territory</b>: territorio sociale, animale o istituzionale futuro.</item>
    /// </list>
    /// </summary>
    public enum EnvironmentAreaKind
    {
        Generic = 0,
        Fertility = 10,
        Water = 20,
        Vegetation = 30,
        Room = 40,
        Territory = 50
    }

    // =============================================================================
    // EnvironmentAreaBounds
    // =============================================================================
    /// <summary>
    /// <para>
    /// Bounding box discreta di un'area ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: compressione prima della maschera completa</b></para>
    /// <para>
    /// La foundation conserva solo limiti rettangolari e livello Z. Maschere
    /// irregolari, connected components e chunk masks potranno essere aggiunte sopra
    /// questo contratto senza cambiare l'identita' delle aree.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>MinX/MinY</b>: angolo minimo incluso.</item>
    ///   <item><b>MaxX/MaxY</b>: angolo massimo incluso.</item>
    ///   <item><b>Z</b>: livello discreto dell'area.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentAreaBounds
    {
        public readonly int MinX;
        public readonly int MinY;
        public readonly int MaxX;
        public readonly int MaxY;
        public readonly int Z;

        public int Width => MaxX >= MinX ? (MaxX - MinX) + 1 : 0;
        public int Height => MaxY >= MinY ? (MaxY - MinY) + 1 : 0;
        public bool IsValid => Width > 0 && Height > 0;

        // =============================================================================
        // EnvironmentAreaBounds
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce bounds ordinando automaticamente minimi e massimi.
        /// </para>
        /// </summary>
        public EnvironmentAreaBounds(int minX, int minY, int maxX, int maxY, int z = 0)
        {
            // Normalizziamo gli assi per rendere robusti i dati provenienti da
            // editor, JSON o generatori futuri.
            MinX = Math.Min(minX, maxX);
            MinY = Math.Min(minY, maxY);
            MaxX = Math.Max(minX, maxX);
            MaxY = Math.Max(minY, maxY);
            Z = z;
        }

        // =============================================================================
        // Contains
        // =============================================================================
        /// <summary>
        /// <para>
        /// Indica se una cella appartiene al bounding box.
        /// </para>
        /// </summary>
        public bool Contains(EnvironmentCellCoord cell)
        {
            // Il controllo resta volutamente rettangolare: non sostituisce una futura
            // maschera irregolare, ma offre un filtro economico preliminare.
            return cell.Z == Z
                   && cell.X >= MinX
                   && cell.X <= MaxX
                   && cell.Y >= MinY
                   && cell.Y <= MaxY;
        }
    }

    // =============================================================================
    // EnvironmentAreaDefinition
    // =============================================================================
    /// <summary>
    /// <para>
    /// Definizione minima e passiva di un'area ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: registry dichiarativo</b></para>
    /// <para>
    /// Questa struttura descrive cosa e' un'area, non come viene simulata. I payload
    /// specializzati di fertilita', acqua o vegetazione restano separati per evitare
    /// un contenitore monolitico con campi ambigui.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AreaId</b>: identita' stabile dell'area.</item>
    ///   <item><b>Kind</b>: layer logico a cui appartiene.</item>
    ///   <item><b>Bounds</b>: bounding box iniziale.</item>
    ///   <item><b>PhysicalPlantDominance01</b>: bilanciamento tra vegetazione diffusa e piante fisiche.</item>
    ///   <item><b>Priority</b>: precedenza futura dentro lo stesso layer.</item>
    ///   <item><b>IsEnabled</b>: gate passivo di utilizzo.</item>
    ///   <item><b>Key</b>: chiave opzionale leggibile per config/debug.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentAreaDefinition
    {
        public readonly EnvironmentAreaId AreaId;
        public readonly EnvironmentAreaKind Kind;
        public readonly EnvironmentAreaBounds Bounds;
        public readonly int CenterX;
        public readonly int CenterY;
        public readonly int RadiusCells;
        public readonly float Irregularity01;
        public readonly float PhysicalPlantDominance01;
        public readonly int Priority;
        public readonly bool IsEnabled;
        public readonly string Key;

        // =============================================================================
        // EnvironmentAreaDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una definizione area priva di payload simulativo.
        /// </para>
        /// </summary>
        public EnvironmentAreaDefinition(
            EnvironmentAreaId areaId,
            EnvironmentAreaKind kind,
            EnvironmentAreaBounds bounds,
            int priority,
            bool isEnabled,
            string key)
            : this(
                areaId,
                kind,
                bounds,
                (bounds.MinX + bounds.MaxX) / 2,
                (bounds.MinY + bounds.MaxY) / 2,
                0,
                0.5f,
                0.045f,
                priority,
                isEnabled,
                key)
        {
        }

        public EnvironmentAreaDefinition(
            EnvironmentAreaId areaId,
            EnvironmentAreaKind kind,
            EnvironmentAreaBounds bounds,
            int centerX,
            int centerY,
            int radiusCells,
            int priority,
            bool isEnabled,
            string key)
            : this(
                areaId,
                kind,
                bounds,
                centerX,
                centerY,
                radiusCells,
                0.5f,
                0.045f,
                priority,
                isEnabled,
                key)
        {
        }

        public EnvironmentAreaDefinition(
            EnvironmentAreaId areaId,
            EnvironmentAreaKind kind,
            EnvironmentAreaBounds bounds,
            int centerX,
            int centerY,
            int radiusCells,
            float irregularity01,
            float physicalPlantDominance01,
            int priority,
            bool isEnabled,
            string key)
        {
            AreaId = areaId;
            Kind = kind;
            Bounds = bounds;
            CenterX = centerX;
            CenterY = centerY;
            RadiusCells = radiusCells < 0 ? 0 : radiusCells;
            Irregularity01 = EnvironmentMath.Clamp01(irregularity01);
            PhysicalPlantDominance01 = EnvironmentMath.Clamp01(physicalPlantDominance01);
            Priority = priority;
            IsEnabled = isEnabled;
            Key = key ?? string.Empty;
        }

        public bool UsesCircularArea => RadiusCells > 0;

        public bool ContainsCell(int x, int y, int z = 0)
        {
            if (z != Bounds.Z)
                return false;

            if (!UsesCircularArea)
                return Bounds.Contains(new EnvironmentCellCoord(x, y, z));

            int dx = x - CenterX;
            int dy = y - CenterY;
            int r = RadiusCells;
            return (dx * dx) + (dy * dy) <= r * r;
        }
    }
}

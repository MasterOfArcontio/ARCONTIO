using Arcontio.View.ArcGraph;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // ArcGraphTerrainUvInsetQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per l'inset UV del terrain atlas ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: qualita' visuale senza autorita' simulativa</b></para>
    /// <para>
    /// Le lineette tra celle sono un problema di resa del renderer terrain, non un
    /// fatto del <c>World</c>. Questi test lavorano quindi solo sulla mappa UV
    /// value-only: non aprono scene, non caricano texture, non modificano asset e
    /// non toccano import settings. Verificano che la correzione resti confinata
    /// al campionamento dell'atlas.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Inset default</b>: le UV registrate non cadono sui bordi esatti della slice.</item>
    ///   <item><b>Legacy opt-out</b>: inset zero conserva il mapping storico.</item>
    ///   <item><b>Clamp</b>: valori troppo grandi non invertono mai il quad UV.</item>
    ///   <item><b>Fallback</b>: tile mancante usa comunque UV sicure.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphTerrainUvInsetQaTests
    {
        // =============================================================================
        // RegisteredTileUsesInsetInsideAtlasSlice
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una tile 32x32 dentro atlas 512x512 venga campionata mezzo
        /// pixel dentro la slice, non sul bordo esatto.
        /// </para>
        /// </summary>
        [Test]
        public void RegisteredTileUsesInsetInsideAtlasSlice()
        {
            var map = new ArcGraphTerrainTileUvMap(
                atlasWidthPixels: 512,
                atlasHeightPixels: 512,
                tilePixels: 32,
                uvInsetPixels: 0.5f);
            map.Register(7, uvX: 1, uvY: 2);

            bool found = map.TryGetUvQuad(
                7,
                out Vector2 uv0,
                out Vector2 uv1,
                out Vector2 uv2,
                out Vector2 uv3);

            Assert.That(found, Is.True);
            Assert.That(uv0.x, Is.GreaterThan(32f / 512f));
            Assert.That(uv1.x, Is.LessThan(64f / 512f));
            Assert.That(uv0.y, Is.GreaterThan(416f / 512f));
            Assert.That(uv2.y, Is.LessThan(448f / 512f));
            Assert.That(uv3.x, Is.EqualTo(uv0.x).Within(0.000001f));
            Assert.That(uv2.x, Is.EqualTo(uv1.x).Within(0.000001f));
        }

        // =============================================================================
        // ZeroInsetKeepsLegacyAtlasBorders
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che <c>uvInsetPixels = 0</c> resti un opt-out esplicito per il
        /// mapping legacy sui bordi esatti.
        /// </para>
        /// </summary>
        [Test]
        public void ZeroInsetKeepsLegacyAtlasBorders()
        {
            var map = new ArcGraphTerrainTileUvMap(
                atlasWidthPixels: 512,
                atlasHeightPixels: 512,
                tilePixels: 32,
                uvInsetPixels: 0f);
            map.Register(7, uvX: 1, uvY: 2);

            map.TryGetUvQuad(
                7,
                out Vector2 uv0,
                out Vector2 uv1,
                out _,
                out _);

            Assert.That(uv0.x, Is.EqualTo(32f / 512f).Within(0.000001f));
            Assert.That(uv1.x, Is.EqualTo(64f / 512f).Within(0.000001f));
        }

        // =============================================================================
        // OversizedInsetIsClampedBeforeUvInversion
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un inset assurdo non possa invertire uMin/uMax o vMin/vMax.
        /// </para>
        /// </summary>
        [Test]
        public void OversizedInsetIsClampedBeforeUvInversion()
        {
            var map = new ArcGraphTerrainTileUvMap(
                atlasWidthPixels: 4,
                atlasHeightPixels: 4,
                tilePixels: 2,
                uvInsetPixels: 999f);
            map.Register(1, uvX: 0, uvY: 0);

            map.TryGetUvQuad(
                1,
                out Vector2 uv0,
                out Vector2 uv1,
                out Vector2 uv2,
                out _);

            Assert.That(map.UvInsetPixels, Is.LessThan(1f));
            Assert.That(uv0.x, Is.LessThan(uv1.x));
            Assert.That(uv0.y, Is.LessThan(uv2.y));
        }

        // =============================================================================
        // MissingTileFallbackStillUsesSafeInset
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il fallback per tile mancante continui a restituire UV
        /// utilizzabili e applichi lo stesso inset di sicurezza.
        /// </para>
        /// </summary>
        [Test]
        public void MissingTileFallbackStillUsesSafeInset()
        {
            var map = new ArcGraphTerrainTileUvMap(
                atlasWidthPixels: 512,
                atlasHeightPixels: 512,
                tilePixels: 32,
                uvInsetPixels: 0.5f);

            bool found = map.TryGetUvQuad(
                999,
                out Vector2 uv0,
                out Vector2 uv1,
                out _,
                out _);

            Assert.That(found, Is.False);
            Assert.That(uv0.x, Is.GreaterThan(0f));
            Assert.That(uv1.x, Is.LessThan(32f / 512f));
        }
    }
}

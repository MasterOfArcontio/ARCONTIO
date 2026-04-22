using Arcontio.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // ReservationStoreQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per record e store delle prenotazioni del Job System.
    /// </para>
    ///
    /// <para><b>Contesa risorse testabile</b></para>
    /// <para>
    /// Lo store viene esercitato senza oggetti runtime reali: i target sono gia'
    /// risolti in id o celle e lo store decide solo conflitti e scadenze.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>TryReserve</b>: accetta target libero.</item>
    ///   <item><b>Conflict</b>: rifiuta target gia' posseduto da altro job.</item>
    ///   <item><b>Release/Prune</b>: libera per job o per scadenza.</item>
    /// </list>
    /// </summary>
    public sealed class ReservationStoreQaTests
    {
        // =============================================================================
        // ReservationStoreRejectsConflictingTarget
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che due job diversi non possano prenotare la stessa cella.
        /// </para>
        ///
        /// <para><b>Single owner per target</b></para>
        /// <para>
        /// La contesa viene riconosciuta dal target logico, non dal nome della
        /// prenotazione, cosi' id diversi non aggirano il lock.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>First</b>: job-a prenota la cella.</item>
        ///   <item><b>Second</b>: job-b prova la stessa cella con id diverso.</item>
        ///   <item><b>Assert</b>: conflitto e record esistente restituito.</item>
        /// </list>
        /// </summary>
        [Test]
        public void ReservationStoreRejectsConflictingTarget()
        {
            // Arrange: lo store parte vuoto e non legge World.
            var store = new ReservationStore();
            var cell = new Vector2Int(3, 4);
            var first = new ReservationRecord("res-a", "job-a", 1, ReservationTargetKind.Cell, cell, -1, 0, 10);
            var second = new ReservationRecord("res-b", "job-b", 2, ReservationTargetKind.Cell, cell, -1, 1, 11);

            // Act: la prima prenotazione entra, la seconda incontra contesa.
            var firstAccepted = store.TryReserve(first, out _);
            var secondAccepted = store.TryReserve(second, out var existing);

            // Assert: il proprietario originale resta visibile.
            Assert.That(firstAccepted, Is.True);
            Assert.That(secondAccepted, Is.False);
            Assert.That(existing.JobId, Is.EqualTo("job-a"));
            Assert.That(store.Count, Is.EqualTo(1));
        }

        // =============================================================================
        // ReservationStoreReleasesByJobAndPrunesExpired
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica rilascio esplicito per job e pulizia delle prenotazioni scadute.
        /// </para>
        ///
        /// <para><b>Cleanup deterministico</b></para>
        /// <para>
        /// La cleanup deve poter essere chiamata da state machine e sistemi runtime
        /// senza dover conoscere il tipo della risorsa prenotata.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>ReleaseByJob</b>: libera tutte le risorse di job-a.</item>
        ///   <item><b>PruneExpired</b>: rimuove il residuo al tick di scadenza.</item>
        ///   <item><b>Assert</b>: conteggi deterministici.</item>
        /// </list>
        /// </summary>
        [Test]
        public void ReservationStoreReleasesByJobAndPrunesExpired()
        {
            // Arrange: due record con proprietari e scadenze diverse.
            var store = new ReservationStore();
            store.TryReserve(new ReservationRecord("res-a", "job-a", 1, ReservationTargetKind.Object, Vector2Int.zero, 10, 0, 100), out _);
            store.TryReserve(new ReservationRecord("res-b", "job-b", 2, ReservationTargetKind.Object, Vector2Int.zero, 11, 0, 5), out _);

            // Act: prima cleanup esplicita, poi cleanup temporale.
            var released = store.ReleaseByJob("job-a");
            var prunedBefore = store.PruneExpired(4);
            var prunedAtExpiry = store.PruneExpired(5);

            // Assert: i conteggi raccontano esattamente cosa e' stato rimosso.
            Assert.That(released, Is.EqualTo(1));
            Assert.That(prunedBefore, Is.EqualTo(0));
            Assert.That(prunedAtExpiry, Is.EqualTo(1));
            Assert.That(store.Count, Is.EqualTo(0));
        }
    }
}

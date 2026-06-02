using Arcontio.Core;
using Arcontio.Core.Config;
using Arcontio.Core.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // RuntimeCostObserverQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per la configurazione minima dell'osservatorio costi runtime
    /// introdotto in `v0.17b`.
    /// </para>
    ///
    /// <para><b>Principio architetturale: diagnostica congelabile</b></para>
    /// <para>
    /// Il requisito principale non e' ancora misurare i sistemi, ma garantire che il
    /// percorso disattivo non crei oggetti diagnostici. Se il parametro resta spento,
    /// il `World` deve esporre `RuntimeCostObserver == null`.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Default spento</b>: nuovo `SimulationParams` non crea osservatorio.</item>
    ///   <item><b>Config accesa</b>: abilita il punto di aggancio nel `World`.</item>
    ///   <item><b>Layout logging</b>: la sezione diagnostica canonica puo' alimentare il parametro.</item>
    /// </list>
    /// </summary>
    public sealed class RuntimeCostObserverQaTests
    {
        // =============================================================================
        // RunFromCommandLine
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point minimale per eseguire questi test da Unity batchmode anche
        /// quando il test runner standard non produce il file XML dei risultati.
        /// </para>
        /// </summary>
        public static void RunFromCommandLine()
        {
            try
            {
                var tests = new RuntimeCostObserverQaTests();
                tests.RuntimeCostObserverIsNullByDefault();
                tests.RuntimeCostObserverIsCreatedOnlyWhenExplicitlyEnabled();
                tests.RuntimeCostObserverCanLoadFromDiagnosticsLoggingSection();
                tests.RuntimeCostObserverAccumulatesNumericSamplesAndCounters();
                tests.RuntimeCostObserverDoesNotCreateNpcStoreWhenPerNpcTrackingIsDisabled();
                tests.RuntimeCostObserverTracksTopNpcCostsWhenEnabled();
                tests.RuntimeCostObserverCopiesSnapshotsWithoutAllocatingOwnedLists();
                tests.RuntimeCostObserverWritesJsonlSnapshotOnlyWhenEnabled();

                Debug.Log("[RuntimeCostObserverQaTests] PASS");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[RuntimeCostObserverQaTests] FAIL\n" + ex);
                EditorApplication.Exit(1);
            }
        }

        [Test]
        public void RuntimeCostObserverIsNullByDefault()
        {
            var world = new World(new WorldConfig(new SimulationParams()));

            Assert.That(world.RuntimeCostObserver, Is.Null);
        }

        [Test]
        public void RuntimeCostObserverIsCreatedOnlyWhenExplicitlyEnabled()
        {
            var sim = new SimulationParams();
            sim.runtime_cost_observer.enabled = true;
            sim.runtime_cost_observer.sampleEveryTicks = 5;

            var world = new World(new WorldConfig(sim));

            Assert.That(world.RuntimeCostObserver, Is.Not.Null);
            Assert.That(world.RuntimeCostObserver.ShouldSample(10), Is.True);
            Assert.That(world.RuntimeCostObserver.ShouldSample(11), Is.False);
        }

        [Test]
        public void RuntimeCostObserverCanLoadFromDiagnosticsLoggingSection()
        {
            const string json = @"{
  ""logging"": {
    ""runtime_cost_observer"": {
      ""enabled"": true,
      ""sampleEveryTicks"": 4,
      ""trackPerNpc"": true,
      ""writeJsonl"": false
    }
  }
}";

            var sim = JsonUtility.FromJson<SimulationParams>(json);
            sim.ApplyRuntimeDiagnosticsLayout();
            var world = new World(new WorldConfig(sim));

            Assert.That(world.RuntimeCostObserver, Is.Not.Null);
            Assert.That(world.RuntimeCostObserver.TrackPerNpc, Is.True);
            Assert.That(world.RuntimeCostObserver.WriteJsonl, Is.False);
            Assert.That(world.RuntimeCostObserver.ShouldSample(8), Is.True);
            Assert.That(world.RuntimeCostObserver.ShouldSample(9), Is.False);
        }

        [Test]
        public void RuntimeCostObserverAccumulatesNumericSamplesAndCounters()
        {
            var observer = RuntimeCostObserver.CreateIfEnabled(new RuntimeCostObserverParams
            {
                enabled = true,
                sampleEveryTicks = 1
            });

            long start = observer.BeginSample();
            observer.AddCounter(RuntimeCostCounter.ObjectPerceptionNpcScans, 3);
            observer.AddCounter(RuntimeCostCounter.ObjectPerceptionNpcScans, 2);
            observer.EndSample(RuntimeCostChannel.ObjectPerception, start);

            Assert.That(observer.GetSampleCount(RuntimeCostChannel.ObjectPerception), Is.EqualTo(1));
            Assert.That(observer.GetDurationTicks(RuntimeCostChannel.ObjectPerception), Is.GreaterThanOrEqualTo(0));
            Assert.That(observer.GetCounter(RuntimeCostCounter.ObjectPerceptionNpcScans), Is.EqualTo(5));
        }

        [Test]
        public void RuntimeCostObserverDoesNotCreateNpcStoreWhenPerNpcTrackingIsDisabled()
        {
            var observer = RuntimeCostObserver.CreateIfEnabled(new RuntimeCostObserverParams
            {
                enabled = true,
                trackPerNpc = false
            });

            observer.AddNpcWork(1, 10);

            Assert.That(observer.TrackPerNpc, Is.False);
            Assert.That(observer.TrackedNpcCount, Is.EqualTo(0));
        }

        [Test]
        public void RuntimeCostObserverTracksTopNpcCostsWhenEnabled()
        {
            var observer = RuntimeCostObserver.CreateIfEnabled(new RuntimeCostObserverParams
            {
                enabled = true,
                trackPerNpc = true
            });
            var top = new System.Collections.Generic.List<RuntimeNpcCostSnapshot>();

            observer.AddNpcWork(1, 4);
            observer.AddNpcWork(2, 9);
            observer.AddNpcWork(1, 3);
            observer.CopyTopNpcCostsTo(top, 2);

            Assert.That(observer.TrackedNpcCount, Is.EqualTo(2));
            Assert.That(top.Count, Is.EqualTo(2));
            Assert.That(top[0].NpcId, Is.EqualTo(2));
            Assert.That(top[0].Score, Is.EqualTo(9));
            Assert.That(top[1].NpcId, Is.EqualTo(1));
            Assert.That(top[1].Score, Is.EqualTo(7));
        }

        [Test]
        public void RuntimeCostObserverCopiesSnapshotsWithoutAllocatingOwnedLists()
        {
            var observer = RuntimeCostObserver.CreateIfEnabled(new RuntimeCostObserverParams
            {
                enabled = true,
                sampleEveryTicks = 1
            });
            var channels = new List<RuntimeCostChannelSnapshot>();
            var counters = new List<RuntimeCostCounterSnapshot>();

            long start = observer.BeginSample();
            observer.AddCounter(RuntimeCostCounter.MoveToExecutions, 2);
            observer.EndSample(RuntimeCostChannel.JobExecution, start);
            observer.CopyChannelSnapshotsTo(channels);
            observer.CopyCounterSnapshotsTo(counters);

            Assert.That(channels.Count, Is.EqualTo((int)RuntimeCostChannel.Count));
            Assert.That(counters.Count, Is.EqualTo((int)RuntimeCostCounter.Count));
            Assert.That(observer.GetCounter(RuntimeCostCounter.MoveToExecutions), Is.EqualTo(2));
        }

        [Test]
        public void RuntimeCostObserverWritesJsonlSnapshotOnlyWhenEnabled()
        {
            string path = Path.Combine(Application.temporaryCachePath, "runtime_cost_observer_qa_{yyyyMMdd_HHmmss}.jsonl");
            JsonlRuntimeLogHub.Shutdown();
            JsonlRuntimeLogHub.Configure(new LoggerJsonlParams
            {
                enabled = true,
                flush_interval_seconds = 0.01,
                max_queue_size = 32,
                max_batch_size = 16
            });

            try
            {
                var disabled = RuntimeCostObserver.CreateIfEnabled(new RuntimeCostObserverParams
                {
                    enabled = true,
                    writeJsonl = false,
                    jsonLogFileNamePattern = path
                });
                disabled.TryWriteJsonlSnapshot(10);
                Assert.That(JsonlRuntimeLogHub.WriterCount, Is.EqualTo(0));

                var enabled = RuntimeCostObserver.CreateIfEnabled(new RuntimeCostObserverParams
                {
                    enabled = true,
                    writeJsonl = true,
                    jsonlFlushIntervalTicks = 5,
                    jsonLogFileNamePattern = path
                });
                enabled.AddCounter(RuntimeCostCounter.ObjectPerceptionDebugFovCells, 7);
                enabled.TryWriteJsonlSnapshot(10);
                enabled.TryWriteJsonlSnapshot(11);

                Assert.That(JsonlRuntimeLogHub.WriterCount, Is.EqualTo(1));
                JsonlRuntimeLogHub.FlushAll(force: true);
            }
            finally
            {
                JsonlRuntimeLogHub.Shutdown();
                JsonlRuntimeLogHub.Configure(new LoggerJsonlParams());
            }
        }
    }
}

using System;
using System.Collections.Generic;
using Arcontio.Core;
using SocialViewer.UI;
using UnityEngine;

namespace Arcontio.View.MapGrid
{
    // =============================================================================
    // MapGridRuntimeDebugAudioFeedback
    // =============================================================================
    /// <summary>
    /// <para>
    /// Sistema audio di debug della MapGrid. Osserva lo stato gia' prodotto dal
    /// <see cref="World"/> e riproduce suoni brevi per eventi utili durante il test:
    /// passo, decisione/job avviato e fallimento job.
    /// </para>
    ///
    /// <para><b>Principio architetturale: presentazione non autoritativa</b></para>
    /// <para>
    /// Questo componente non modifica il mondo, non produce comandi, non crea job e
    /// non scrive memoria. E' uno strato view-only: legge posizione NPC, flash
    /// decisionale e stato job per fornire feedback sonoro all'operatore.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Configurazione JSON</b>: legge <c>Arcontio/Config/debug_audio_config</c>.</item>
    ///   <item><b>Filtro NPC</b>: NPC selezionato a volume pieno, tre NPC vicini a volume ridotto.</item>
    ///   <item><b>Guardie costo</b>: se disattivo non carica clip e non osserva il runtime.</item>
    ///   <item><b>Anti rumore</b>: limita il numero massimo di suoni per secondo.</item>
    /// </list>
    /// </summary>
    public sealed class MapGridRuntimeDebugAudioFeedback
    {
        private const string ConfigResourcePath = "Arcontio/Config/debug_audio_config";

        private readonly Dictionary<int, Vector2Int> _lastNpcCells = new(128);
        private readonly Dictionary<int, int> _lastDecisionFlashTicks = new(128);
        private readonly Dictionary<int, string> _lastActiveJobIds = new(128);
        private readonly Dictionary<int, JobFailureReason> _lastFailureReasons = new(128);
        private readonly List<int> _candidateNpcIds = new(128);
        private readonly List<NpcDistance> _nearbyBuffer = new(16);

        private GameObject _root;
        private AudioSource _source;
        private DebugAudioConfig _config;
        private AudioClip _stepClip;
        private AudioClip _decisionClip;
        private AudioClip _jobFailedClip;
        private AudioClip _jobRecoveryClip;
        private float _soundWindowStartRealtime;
        private int _soundsInCurrentWindow;
        private bool _loaded;

        public void AttachTo(Transform parent)
        {
            if (_root != null)
                return;

            _root = new GameObject("MapGridRuntimeDebugAudioFeedback");
            _root.transform.SetParent(parent, false);

            _source = _root.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.loop = false;
            _source.volume = 1f;
        }

        public void ResetState()
        {
            _lastNpcCells.Clear();
            _lastDecisionFlashTicks.Clear();
            _lastActiveJobIds.Clear();
            _lastFailureReasons.Clear();
            _candidateNpcIds.Clear();
            _nearbyBuffer.Clear();
        }

        public void Tick(World world)
        {
            EnsureLoaded();

            if (!_loaded || _config == null || !_config.enabled)
                return;

            if (world == null || _source == null)
                return;

            int selectedNpcId = NPCSelection.SelectedNpcId;
            if (selectedNpcId <= 0 || !world.ExistsNpc(selectedNpcId))
                return;

            BuildAudibleNpcSet(world, selectedNpcId);

            for (int i = 0; i < _candidateNpcIds.Count; i++)
            {
                int npcId = _candidateNpcIds[i];
                float volume = ResolveVolume(npcId, selectedNpcId);
                if (volume <= 0f)
                    continue;

                ObserveMovement(world, npcId, volume);
                ObserveDecision(world, npcId, volume);
                ObserveJobState(world, npcId, volume);
            }
        }

        private void EnsureLoaded()
        {
            if (_loaded)
                return;

            _loaded = true;
            _config = LoadConfig();

            if (_config == null || !_config.enabled)
                return;

            _stepClip = LoadClip(_config.sounds.step);
            _decisionClip = LoadClip(_config.sounds.decision);
            _jobFailedClip = LoadClip(_config.sounds.jobFailed);
            _jobRecoveryClip = LoadClip(_config.sounds.jobRecovery);
        }

        private static DebugAudioConfig LoadConfig()
        {
            var asset = Resources.Load<TextAsset>(ConfigResourcePath);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                return new DebugAudioConfig { enabled = false };

            try
            {
                var config = JsonUtility.FromJson<DebugAudioConfig>(asset.text);
                return config ?? new DebugAudioConfig { enabled = false };
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MapGridRuntimeDebugAudioFeedback] Config debug audio non valida: " + ex.Message);
                return new DebugAudioConfig { enabled = false };
            }
        }

        private static AudioClip LoadClip(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
                return null;

            var clip = Resources.Load<AudioClip>(resourcePath);
            if (clip == null)
                Debug.LogWarning("[MapGridRuntimeDebugAudioFeedback] AudioClip mancante: Resources/" + resourcePath);

            return clip;
        }

        private void BuildAudibleNpcSet(World world, int selectedNpcId)
        {
            _candidateNpcIds.Clear();
            _nearbyBuffer.Clear();
            _candidateNpcIds.Add(selectedNpcId);

            if (!world.GridPos.TryGetValue(selectedNpcId, out var selectedCell))
                return;

            foreach (var pair in world.GridPos)
            {
                int npcId = pair.Key;
                if (npcId == selectedNpcId)
                    continue;

                int distance = Math.Abs(pair.Value.X - selectedCell.X) + Math.Abs(pair.Value.Y - selectedCell.Y);
                _nearbyBuffer.Add(new NpcDistance(npcId, distance));
            }

            _nearbyBuffer.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            int count = Math.Max(0, _config.nearbyNpcCount);
            for (int i = 0; i < _nearbyBuffer.Count && i < count; i++)
                _candidateNpcIds.Add(_nearbyBuffer[i].NpcId);
        }

        private float ResolveVolume(int npcId, int selectedNpcId)
        {
            if (npcId == selectedNpcId)
                return Mathf.Clamp01(_config.selectedNpcVolume);

            return Mathf.Clamp01(_config.nearbyNpcVolume);
        }

        private void ObserveMovement(World world, int npcId, float volume)
        {
            if (!_config.playFootsteps)
                return;

            if (!world.GridPos.TryGetValue(npcId, out var pos))
                return;

            var cell = new Vector2Int(pos.X, pos.Y);
            if (!_lastNpcCells.TryGetValue(npcId, out var previous))
            {
                _lastNpcCells[npcId] = cell;
                return;
            }

            if (previous == cell)
                return;

            _lastNpcCells[npcId] = cell;
            Play(_stepClip, volume);
        }

        private void ObserveDecision(World world, int npcId, float volume)
        {
            if (!_config.playDecision)
                return;

            if (!world.TryGetNpcDecisionFlashTick(npcId, out int flashTick))
                return;

            if (_lastDecisionFlashTicks.TryGetValue(npcId, out int previous) && previous >= flashTick)
                return;

            _lastDecisionFlashTicks[npcId] = flashTick;
            Play(_decisionClip, volume);
        }

        private void ObserveJobState(World world, int npcId, float volume)
        {
            var runtime = world.JobRuntimeState;
            if (runtime == null)
                return;

            int tick = (int)Math.Min(int.MaxValue, Math.Max(0, TickContext.CurrentTickIndex));
            var snapshot = runtime.GetSnapshot(npcId, tick);

            if (_config.playDecision && snapshot.HasActiveJob)
            {
                if (!_lastActiveJobIds.TryGetValue(npcId, out var previousJobId) || previousJobId != snapshot.CurrentJobId)
                {
                    _lastActiveJobIds[npcId] = snapshot.CurrentJobId;
                    Play(_decisionClip, volume);
                }
            }
            else if (!snapshot.HasActiveJob)
            {
                _lastActiveJobIds[npcId] = string.Empty;
            }

            if (!_config.playJobFailure)
                return;

            if (!_lastFailureReasons.TryGetValue(npcId, out var previousFailure))
            {
                _lastFailureReasons[npcId] = snapshot.LastFailureReason;
                return;
            }

            if (snapshot.LastFailureReason == JobFailureReason.None || snapshot.LastFailureReason == previousFailure)
                return;

            _lastFailureReasons[npcId] = snapshot.LastFailureReason;
            Play(_jobFailedClip, volume);
        }

        private void Play(AudioClip clip, float volume)
        {
            if (clip == null || _source == null)
                return;

            if (!CanPlayAnotherSound())
                return;

            _source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private bool CanPlayAnotherSound()
        {
            float now = Time.unscaledTime;
            if (now - _soundWindowStartRealtime >= 1f)
            {
                _soundWindowStartRealtime = now;
                _soundsInCurrentWindow = 0;
            }

            int max = Math.Max(1, _config.maxSoundsPerSecond);
            if (_soundsInCurrentWindow >= max)
                return false;

            _soundsInCurrentWindow++;
            return true;
        }

        private readonly struct NpcDistance
        {
            public readonly int NpcId;
            public readonly int Distance;

            public NpcDistance(int npcId, int distance)
            {
                NpcId = npcId;
                Distance = distance;
            }
        }

        [Serializable]
        private sealed class DebugAudioConfig
        {
            public bool enabled = false;
            public float selectedNpcVolume = 1f;
            public float nearbyNpcVolume = 0.3f;
            public int nearbyNpcCount = 3;
            public int maxSoundsPerSecond = 6;
            public bool playFootsteps = true;
            public bool playDecision = true;
            public bool playJobFailure = true;
            public bool playJobRecovery = false;
            public DebugAudioSoundConfig sounds = new();
        }

        [Serializable]
        private sealed class DebugAudioSoundConfig
        {
            public string step = "Arcontio/Audio/Debug/npc_step";
            public string decision = "Arcontio/Audio/Debug/npc_decision";
            public string jobFailed = "Arcontio/Audio/Debug/job_failed";
            public string jobRecovery = "Arcontio/Audio/Debug/job_recovery";
        }
    }
}

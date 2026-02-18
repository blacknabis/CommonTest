using System;
using UnityEngine;

namespace Kingdom.Game
{
    public enum GameFlowState
    {
        Prepare,
        WaveReady,
        WaveRunning,
        WaveBreak,
        Result,
        Pause
    }

    /// <summary>
    /// GameScene의 전투 상태 흐름을 제어하는 최소 FSM.
    /// </summary>
    public class GameStateController : MonoBehaviour
    {
        [Header("Flow")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private int totalWaves = 3;
        [SerializeField] private float prepareDuration = 1.0f;
        [SerializeField] private float waveReadyDuration = 3.0f;
        [SerializeField] private float waveDuration = 8.0f;
        [SerializeField] private float waveBreakDuration = 2.0f;

        public GameFlowState CurrentState { get; private set; } = GameFlowState.Prepare;
        public int CurrentWave { get; private set; }
        public int TotalWaves => Mathf.Max(1, totalWaves);
        public bool IsPaused => CurrentState == GameFlowState.Pause;
        public float WaveReadyDuration => Mathf.Max(0f, waveReadyDuration);
        public float WaveDuration => Mathf.Max(0f, waveDuration);
        public float StateElapsedScaled => Mathf.Max(0f, Time.time - _stateStartedAtScaledTime);
        public float StateElapsedUnscaled => Mathf.Max(0f, Time.unscaledTime - _stateStartedAtUnscaledTime);
        public float WaveReadyRemaining => CurrentState == GameFlowState.WaveReady
            ? Mathf.Max(0f, WaveReadyDuration - StateElapsedScaled)
            : 0f;

        public event Action<GameFlowState> StateChanged;
        public event Action<int, int> WaveChanged;
        public event Action<float> WaveReadyTimeChanged;

        private GameFlowState _stateBeforePause = GameFlowState.Prepare;
        private float _stateStartedAtScaledTime;
        private float _stateStartedAtUnscaledTime;
        private float _pausedStateElapsedScaled;
        private float _pausedStateElapsedUnscaled;

        private void Start()
        {
            if (autoStart)
            {
                StartFlow();
            }
        }

        private void Update()
        {
            if (CurrentState == GameFlowState.Pause || CurrentState == GameFlowState.Result)
            {
                return;
            }

            float elapsed = Time.time - _stateStartedAtScaledTime;

            switch (CurrentState)
            {
                case GameFlowState.Prepare:
                    if (elapsed >= prepareDuration)
                    {
                        CurrentWave = 1;
                        WaveChanged?.Invoke(CurrentWave, TotalWaves);
                        ChangeState(GameFlowState.WaveReady);
                    }
                    break;

                case GameFlowState.WaveReady:
                    WaveReadyTimeChanged?.Invoke(WaveReadyRemaining);
                    if (elapsed >= waveReadyDuration)
                    {
                        ChangeState(GameFlowState.WaveRunning);
                    }
                    break;

                case GameFlowState.WaveRunning:
                    if (elapsed >= waveDuration)
                    {
                        if (CurrentWave >= TotalWaves)
                        {
                            ChangeState(GameFlowState.Result);
                        }
                        else
                        {
                            ChangeState(GameFlowState.WaveBreak);
                        }
                    }
                    break;

                case GameFlowState.WaveBreak:
                    if (elapsed >= waveBreakDuration)
                    {
                        CurrentWave++;
                        WaveChanged?.Invoke(CurrentWave, TotalWaves);
                        ChangeState(GameFlowState.WaveReady);
                    }
                    break;
            }
        }

        public void StartFlow()
        {
            CurrentWave = 0;
            Time.timeScale = 1f;
            ChangeState(GameFlowState.Prepare);
        }

        public void TogglePause()
        {
            if (CurrentState == GameFlowState.Result)
            {
                return;
            }

            if (CurrentState == GameFlowState.Pause)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        public void Pause()
        {
            if (CurrentState == GameFlowState.Pause || CurrentState == GameFlowState.Result)
            {
                return;
            }

            _pausedStateElapsedScaled = StateElapsedScaled;
            _pausedStateElapsedUnscaled = StateElapsedUnscaled;
            _stateBeforePause = CurrentState;
            Time.timeScale = 0f;
            ChangeState(GameFlowState.Pause);
        }

        public void Resume()
        {
            if (CurrentState != GameFlowState.Pause)
            {
                return;
            }

            Time.timeScale = 1f;
            ChangeState(_stateBeforePause);
            _stateStartedAtScaledTime = Time.time - Mathf.Max(0f, _pausedStateElapsedScaled);
            _stateStartedAtUnscaledTime = Time.unscaledTime - Mathf.Max(0f, _pausedStateElapsedUnscaled);
        }

        public void ForceResult()
        {
            Time.timeScale = 1f;
            ChangeState(GameFlowState.Result);
        }

        public bool TryEarlyCallNextWave()
        {
            if (CurrentState != GameFlowState.WaveReady)
            {
                return false;
            }

            ChangeState(GameFlowState.WaveRunning);
            return true;
        }

        public bool TryCompleteCurrentWave()
        {
            if (CurrentState != GameFlowState.WaveRunning)
            {
                return false;
            }

            if (CurrentWave >= TotalWaves)
            {
                ChangeState(GameFlowState.Result);
                return true;
            }

            ChangeState(GameFlowState.WaveBreak);
            return true;
        }

        public void SetTotalWaves(int waveCount)
        {
            totalWaves = Mathf.Max(1, waveCount);
        }

        private void ChangeState(GameFlowState next)
        {
            CurrentState = next;
            _stateStartedAtScaledTime = Time.time;
            _stateStartedAtUnscaledTime = Time.unscaledTime;

            Debug.Log($"[GameStateController] State -> {CurrentState} (Wave {CurrentWave}/{TotalWaves})");
            StateChanged?.Invoke(CurrentState);
            if (CurrentState == GameFlowState.WaveReady)
            {
                WaveReadyTimeChanged?.Invoke(WaveReadyDuration);
            }
        }
    }
}

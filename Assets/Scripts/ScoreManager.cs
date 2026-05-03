using System;
using UnityEngine;

/// <summary>
/// Score system (GDD §2.2, §10.2).
/// Base score = survival time × 10.
/// Kill bonus = enemy HP × 5.
/// Power-up collection = +50 flat.
/// High score persisted via PlayerPrefs.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    const string HighScoreKey = "NeonRunner_HighScore";

    float _currentScore;
    float _highScore;
    bool _isRunning;

    /// <summary>Fired whenever the score changes. Arg = current score.</summary>
    public event Action<float> OnScoreChanged;

    public float CurrentScore => _currentScore;
    public float HighScore    => _highScore;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _highScore = PlayerPrefs.GetFloat(HighScoreKey, 0f);
    }

    void Update()
    {
        if (!_isRunning) return;

        // Base score: survival time × 10 (GDD §2.2)
        AddTime(Time.deltaTime);
    }

    /// <summary>Start accumulating time-based score.</summary>
    public void StartScoring()
    {
        _isRunning = true;
    }

    /// <summary>Stop accumulating time-based score (on death / game over).</summary>
    public void StopScoring()
    {
        _isRunning = false;
    }

    /// <summary>Reset score for a new run.</summary>
    public void ResetScore()
    {
        _currentScore = 0f;
        _isRunning = false;
        OnScoreChanged?.Invoke(_currentScore);
    }

    /// <summary>Add time-based score: deltaTime × 10.</summary>
    public void AddTime(float deltaTime)
    {
        _currentScore += deltaTime * 10f;
        OnScoreChanged?.Invoke(_currentScore);
    }

    /// <summary>Kill bonus: enemy max HP × 5 (GDD §2.2).</summary>
    public void AddKill(float enemyMaxHP)
    {
        _currentScore += enemyMaxHP * 5f;
        OnScoreChanged?.Invoke(_currentScore);
    }

    /// <summary>Flat +50 bonus on power-up collection (GDD §2.2).</summary>
    public void AddPowerUpBonus()
    {
        _currentScore += 50f;
        OnScoreChanged?.Invoke(_currentScore);
    }

    /// <summary>Get the final score and persist high score if beaten.</summary>
    public float GetFinalScore()
    {
        if (_currentScore > _highScore)
        {
            _highScore = _currentScore;
            PlayerPrefs.SetFloat(HighScoreKey, _highScore);
            PlayerPrefs.Save();
        }
        return _currentScore;
    }
}

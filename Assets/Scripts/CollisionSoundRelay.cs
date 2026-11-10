using UnityEngine;

/// <summary>
/// Avisa a BowlingAudio cuando este objeto choca con algo.
/// BowlingAudio lo agrega automáticamente a la bola y a los pinos al iniciar:
/// no hace falta ponerlo a mano.
/// </summary>
public class CollisionSoundRelay : MonoBehaviour
{
    public bool IsBall { get; private set; }

    private BowlingAudio audioManager;

    public void Init(BowlingAudio manager, bool isBall)
    {
        audioManager = manager;
        IsBall = isBall;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (audioManager != null)
        {
            audioManager.HandleCollision(this, collision);
        }
    }
}

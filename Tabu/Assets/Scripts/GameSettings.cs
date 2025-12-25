using UnityEngine;

[CreateAssetMenu(menuName = "Game/Game Settings")]
public class GameSettings : ScriptableObject
{
    public float roundTime = 60f;
    public int maxPass = 3;

    public TeamData[] teams;
}

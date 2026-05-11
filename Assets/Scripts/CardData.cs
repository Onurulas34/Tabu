using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Card Data/Creat Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    public int cardScore;
    public int negativeScore;
    public string[] forbiddenWord;
    
}

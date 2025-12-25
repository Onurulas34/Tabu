using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardScript : MonoBehaviour
{
    public SpriteRenderer firstColor_;
    public SpriteRenderer secondColor_;
    public TextMeshProUGUI cardName_;
    public TextMeshProUGUI forbiddenWord_;
    public TextMeshProUGUI forbiddenWord2_;
    public TextMeshProUGUI forbiddenWord3_;
    public TextMeshProUGUI forbiddenWord4_;
    public Image playerPhoto;
    public TextMeshProUGUI cardScore;
    public TextMeshProUGUI negativeCardScore;
    private CardData currentCard;

    public void SetCard(CardData newCard)
    {
        currentCard = newCard;

        firstColor_.color = currentCard.firstColor;
        secondColor_.color = currentCard.secondColor;
        cardName_.text = currentCard.cardName;

        forbiddenWord_.text = currentCard.forbiddenWord[0];
        forbiddenWord2_.text = currentCard.forbiddenWord[1];
        forbiddenWord3_.text = currentCard.forbiddenWord[2];
        forbiddenWord4_.text = currentCard.forbiddenWord[3];

        playerPhoto.sprite = currentCard.photo;
        negativeCardScore.text = "-" + currentCard.negativeScore.ToString();
        cardScore.text = currentCard.cardScore.ToString();
    }
}

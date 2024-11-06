using UnityEngine;



public class CardScript : MonoBehaviour
{
    private Animator cardAnimator;
    private GameManager gameManager;

    private bool isFlipped = false;
    public int cardID;

    void Start()
    {
        cardAnimator = GetComponent<Animator>();
        gameManager = GameManager.Instance;
    }

    void OnMouseDown()
    {
        if (gameManager != null && !isFlipped)
        {
            ShowCard();
            gameManager.CardTouched(gameObject);
        }
    }

    public void ShowCard()
    {
        cardAnimator.SetTrigger("FigureShowTrigger");
        isFlipped = true;
    }

    public void HideCard()
    {
        cardAnimator.SetTrigger("FigureHideTrigger");
        isFlipped = false;
    }
}

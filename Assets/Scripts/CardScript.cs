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
        if (gameManager != null && !isFlipped && gameManager.CanPlaceCard())  // Verifica si se pueden levantar cartas
        {
            ShowCard();
            gameManager.cardTouched(gameObject);  // Llama al GameManager para procesar la carta tocada
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public float TotalTime = 60f;  // Temps total per al joc
    public TextMeshProUGUI BestScore;
    public TextMeshProUGUI Countdown;
    public TextMeshProUGUI Tries;
    public TextMeshProUGUI GameFinished;
    public TextMeshProUGUI Score;

    public AudioClip CardFlipSound;
    public AudioClip MatchFoundSound;
    public AudioClip IncorrectMatchSound;
    public AudioClip GameStartSound;
    public AudioClip GameEndSound;

    public Button StartButton;
    public Transform CardsParent;
    public int GridRows = 4;  // Nombre de files de cartes
    public int GridCols = 4;  // Nombre de columnes de cartes
    public float HorizontalSpacing = 2f;  // Espai entre cartes en horitzontal
    public float VerticalSpacing = 2.5f;  // Espai entre cartes en vertical
    public Vector3 GridStartPosition;  // Posició inicial de la graella

    public GameObject Cube1;
    public GameObject Cube2;
    public GameObject Cube3;
    public GameObject Cube4;
    public GameObject Cube5;
    public GameObject Cube6;
    public GameObject Cube7;
    public GameObject Cube8;

    private float countdown;  // Variable per a contar el temps restant
    private int bestScore = 0;  // Millor puntuació aconseguida
    private int matchedPairs = 0;  // Nombre de parelles emparellades
    private int tries = 0;  // Nombre d'intents
    public int score = 0;  // Puntuació actual del jugador
    private bool gameActive = false;  // Indica si el joc està actiu
    private GameObject firstCard = null;  // Referència a la primera carta girada
    private GameObject secondCard = null;  // Referència a la segona carta girada
    private bool isProcessing = false;  // Per evitar que es seleccionin més cartes mentre es processem les parelles

    private List<GameObject> cardInstances = new List<GameObject>();  // Llista de cartes creades

    // Temps per a que les cartes desapareguin després de coincidir
    public float timeToDisappear = 1.5f;  // Temps en segons abans de desaparèixer les cartes

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // Impedeix que es destroyi el GameManager entre escenes
        }
        else
        {
            Destroy(gameObject);  // Si ja existeix una instància, destrueix aquesta nova
        }
    }

    void Start()
    {
        GameFinished.gameObject.SetActive(false);  // Amaga el text de "Game Over"
        BestScore.text = "Best Score: " + bestScore;  // Mostra la millor puntuació
        Tries.text = "Tries: " + tries;  // Mostra els intents
        Countdown.text = "Time: " + TotalTime;  // Mostra el temps total del joc

        StartButton.onClick.AddListener(StartGame);  // Afegeix l'escolta per començar el joc
    }

    // Funció per començar el joc
    void StartGame()
    {
        gameActive = true;  // Activa el joc
        countdown = TotalTime;  // Inicialitza el comptador del temps
        tries = 0;  // Reinicia els intents
        score = 0;  // Reinicia la puntuació
        Tries.text = "Tries: " + tries;
        Score.text = "Score: " + score;
        GameFinished.gameObject.SetActive(false);  // Amaga el panell de "Game Over"

        AudioSource.PlayClipAtPoint(GameStartSound, Camera.main.transform.position);  // Reprodueix l'efecte de so d'inici

        // Elimina les cartes anteriors si n'hi ha
        foreach (GameObject card in cardInstances)
        {
            Destroy(card);
        }
        cardInstances.Clear();  // Esborra la llista de cartes

        // Crear una llista de IDs per a les 8 parelles de cartes
        List<int> cardIDs = new List<int>();
        for (int i = 0; i < 8; i++)  // Creem 8 tipus de cartes (cada tipus apareix 2 vegades)
        {
            cardIDs.Add(i);    // Afegim l'ID de la carta
            cardIDs.Add(i);    // Duplicar per crear la parella
        }

        ShuffleList(cardIDs);  // Barallem la llista per disposar les cartes aleatòriament

        // Crear i posicionar les cartes en una graella
        int cardIndex = 0;
        for (int row = 0; row < GridRows; row++)  // Filades de la graella
        {
            for (int col = 0; col < GridCols; col++)  // Columnes de la graella
            {
                Vector3 position = GridStartPosition + new Vector3(col * HorizontalSpacing, 0, row * VerticalSpacing);  // Càlcul de la posició

                // Obtenim el prefab de la carta segons el seu ID
                GameObject cardPrefab = GetCardPrefab(cardIDs[cardIndex]);

                // Instanciamos la carta i la posicionem en la graella
                GameObject card = Instantiate(cardPrefab, position, Quaternion.identity, CardsParent);
                CardScript cardScript = card.GetComponent<CardScript>();
                cardScript.cardID = cardIDs[cardIndex];  // Assignem l'ID a la carta
                cardInstances.Add(card);
                cardIndex++;
            }
        }

        StartButton.gameObject.SetActive(false);  // Amaga el botó "Start" un cop comenci el joc
    }

    void Update()
    {
        if (gameActive)
        {
            countdown -= Time.deltaTime;  // Actualitza el temps restant
            Countdown.text = "Time: " + Mathf.CeilToInt(countdown);

            if (countdown <= 0)  // Si s'ha acabat el temps, finalitza el joc
            {
                EndGame();
            }
        }
    }

    // Funció per gestionar quan es toca una carta
    public void CardTouched(GameObject touchedCard)
    {
        if (!gameActive || isProcessing) return;  // Si el joc no està actiu o estem processant una parella, no fem res

        CardScript touchedCardScript = touchedCard.GetComponent<CardScript>();

        if (firstCard == null)
        {
            firstCard = touchedCard;
            touchedCardScript.ShowCard();  // Mostra la primera carta
        }
        else if (secondCard == null)  // Només es pot seleccionar una segona carta
        {
            secondCard = touchedCard;
            touchedCardScript.ShowCard();  // Mostra la segona carta
            isProcessing = true;  // Marquem que estem processant la parella
            StartCoroutine(ProcessCards(firstCard, secondCard));  // Comencem el procés per comparar les cartes
        }
    }

    // Funció per comparar les dues cartes seleccionades
    IEnumerator ProcessCards(GameObject card1, GameObject card2)
    {
        CardScript card1Script = card1.GetComponent<CardScript>();
        CardScript card2Script = card2.GetComponent<CardScript>();

        // Incrementem el nombre d'intents per cada parella comparada
        tries++;
        Tries.text = "Tries: " + tries;

        // Si les cartes coincideixen
        if (card1Script.cardID == card2Script.cardID)
        {
            AudioSource.PlayClipAtPoint(MatchFoundSound, Camera.main.transform.position);
            score += 1;  // Afegim un punt per cada parella encertada
            Score.text = "Score: " + score;
            matchedPairs++;

            // Desapareix les cartes després d'un temps de coincidència
            yield return new WaitForSeconds(timeToDisappear);
            card1.SetActive(false);
            card2.SetActive(false);

            if (matchedPairs == 8)  // Si totes les parelles han estat trobades
            {
                EndGame();  // Finalitzar el joc
            }
        }
        else
        {
            AudioSource.PlayClipAtPoint(IncorrectMatchSound, Camera.main.transform.position);
            yield return new WaitForSeconds(1f);  // Esperem un segon abans de girar les cartes de nou
            card1.GetComponent<CardScript>().HideCard();
            card2.GetComponent<CardScript>().HideCard();
        }

        // Reiniciar les cartes per poder seleccionar-ne de noves
        firstCard = null;
        secondCard = null;
        isProcessing = false;
    }

    // Finalitzar el joc
    void EndGame()
    {
        gameActive = false;

        // Mostrem el missatge de "Game Over"
        GameFinished.gameObject.SetActive(true);
        GameFinished.text = "Game Over!";

        // Amaguem els altres textos
        Countdown.gameObject.SetActive(false);
        Tries.gameObject.SetActive(false);
        Score.gameObject.SetActive(false);

        AudioSource.PlayClipAtPoint(GameEndSound, Camera.main.transform.position);  // So de finalització del joc

        // Comprovem si el jugador ha aconseguit una nova millor puntuació
        if (score > bestScore)
        {
            bestScore = score;
            BestScore.text = "Best Score: " + bestScore;
        }

        // Activar el botó "Start" per tornar a jugar
        StartButton.gameObject.SetActive(true);
    }

    // Funció per a barallar els elements d'una llista
    void ShuffleList<T>(List<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    // Retorna el prefab adequat segons l'ID de la carta
    GameObject GetCardPrefab(int cardID)
    {
        switch (cardID)
        {
            case 0:
                return Cube1;
            case 1:
                return Cube2;
            case 2:
                return Cube3;
            case 3:
                return Cube4;
            case 4:
                return Cube5;
            case 5:
                return Cube6;
            case 6:
                return Cube7;
            case 7:
                return Cube8;
            default:
                return Cube1;
        }
    }
}

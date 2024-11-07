using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // Instància estàtica per accedir al GameManager des de qualsevol lloc
    public static GameManager Instance;

    // Variables públiques per a la configuració i la UI
    public float TotalTime = 60f;  // Temps total del joc
    public TextMeshProUGUI BestScore;  // Text per mostrar el millor score
    public TextMeshProUGUI Countdown;  // Text per mostrar el compte enrere
    public TextMeshProUGUI Tries;  // Text per mostrar els intents
    public TextMeshProUGUI GameFinished;  // Text per mostrar el missatge de finalització
    public TextMeshProUGUI Score;  // Text per mostrar el score actual

    // Sons del joc
    public AudioClip CardFlipSound;  // So quan es gira una carta
    public AudioClip MatchFoundSound;  // So quan es troba una parella
    public AudioClip IncorrectMatchSound;  // So quan les cartes no coincideixen
    public AudioClip GameStartSound;  // So quan comença el joc
    public AudioClip GameEndSound;  // So quan acaba el joc

    // Botons i objectes 3D per al joc
    public Button StartButton;  // Botó per començar el joc
    public Transform CardsParent;  // Lloc on es generen les cartes
    public int GridRows = 4;  // Nombre de files de cartes
    public int GridCols = 4;  // Nombre de columnes de cartes
    public float HorizontalSpacing = 2f;  // Espai horitzontal entre cartes
    public float VerticalSpacing = 2.5f;  // Espai vertical entre cartes
    public Vector3 GridStartPosition;  // Posició inicial per generar les cartes

    // Prefabs de les cartes
    public GameObject Cube1;
    public GameObject Cube2;
    public GameObject Cube3;
    public GameObject Cube4;
    public GameObject Cube5;
    public GameObject Cube6;
    public GameObject Cube7;
    public GameObject Cube8;

    // Variables internes per a la lògica del joc
    private float countdown;  // Comptador del temps
    private int bestScore = 0;  // Millor score registrat
    private int matchedPairs = 0;  // Nombre de parelles trobades
    private int tries = 0;  // Nombre d'intents
    public int score = 0;  // Score actual
    private bool gameActive = false;  // Estat del joc (actiu o no)

    private GameObject firstCard = null;  // Referència a la primera carta seleccionada
    private GameObject secondCard = null;  // Referència a la segona carta seleccionada
    private bool isProcessing = false;  // Indica si s'estan processant les cartes

    private List<GameObject> cardInstances = new List<GameObject>();  // Llista de les cartes generades

    public float timeToDisappear = 1.5f;  // Temps fins que les cartes desapareixen

    // Funció que s'executa quan l'script s'instància
    void Awake()
    {
        // Assegura't que només hi ha una instància del GameManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // Manté el GameManager entre escenes
        }
        else
        {
            Destroy(gameObject);  // Elimina l'instància duplicada
        }
    }

    // Funció que s'executa quan el joc comença
    void Start()
    {
        // Inicialització de la UI
        GameFinished.gameObject.SetActive(false);
        BestScore.text = "Best Score: " + bestScore;
        Tries.text = "Tries: " + tries;
        Countdown.text = "Time: " + TotalTime;

        // Afegir l'escoltador d'esdeveniments al botó de començar
        StartButton.onClick.AddListener(StartGame);
    }

    // Funció que inicia el joc
    void StartGame()
    {
        // Si el joc ha acabat, es reinicia tot
        if (!gameActive)
        {
            // Configura les variables per començar el joc
            gameActive = true;
            countdown = TotalTime;
            tries = 0;
            score = 0;
            matchedPairs = 0; // Reseteja el nombre de parelles trobades
            Tries.text = "Tries: " + tries;
            Score.text = "Score: " + score;
            GameFinished.gameObject.SetActive(false);

            // Reprodueix el so d'inici
            AudioSource.PlayClipAtPoint(GameStartSound, Camera.main.transform.position);

            // Neteja les cartes anteriors (si n'hi ha)
            foreach (GameObject card in cardInstances)
            {
                Destroy(card);
            }
            cardInstances.Clear();

            // Crea les cartes i les baralla
            List<int> cardIDs = new List<int>();
            for (int i = 0; i < 8; i++)
            {
                cardIDs.Add(i);
                cardIDs.Add(i);
            }

            ShuffleList(cardIDs);  // Baralla les cartes

            // Genera les cartes a la graella
            int cardIndex = 0;
            for (int row = 0; row < GridRows; row++)
            {
                for (int col = 0; col < GridCols; col++)
                {
                    Vector3 position = GridStartPosition + new Vector3(col * HorizontalSpacing, 0, row * VerticalSpacing);
                    GameObject cardPrefab = GetCardPrefab(cardIDs[cardIndex]);
                    GameObject card = Instantiate(cardPrefab, position, Quaternion.identity, CardsParent);
                    CardScript cardScript = card.GetComponent<CardScript>();
                    cardScript.cardID = cardIDs[cardIndex];  // Assigna un ID a la carta
                    cardInstances.Add(card);
                    cardIndex++;
                }
            }

            // Oculta el botó de començar
            StartButton.gameObject.SetActive(false);
        }
    }

    // Funció que s'executa cada frame
    void Update()
    {
        if (gameActive)
        {
            countdown -= Time.deltaTime;  // Redueix el temps
            Countdown.text = "Time: " + Mathf.CeilToInt(countdown);

            if (countdown <= 0)
            {
                EndGame();  // Acaba el joc quan s'acaba el temps
            }
        }
    }

    // Funció que retorna si es poden col·locar cartes
    public bool CanPlaceCard()
    {
        return firstCard == null || secondCard == null;  // Permet seleccionar cartes si no hi ha dues seleccionades
    }

    // Funció que es crida quan es toca una carta
    public void cardTouched(GameObject cardTouched)
    {
        // Si és la primera carta seleccionada
        if (firstCard == null)
        {
            firstCard = cardTouched;
        }
        // Si és la segona carta seleccionada
        else if (secondCard == null && cardTouched != firstCard)
        {
            secondCard = cardTouched;
            StartCoroutine(ProcessCards(firstCard, secondCard));  // Processa les cartes seleccionades
        }
    }

    // Funció que gestiona el procés de comparar les cartes seleccionades
    IEnumerator ProcessCards(GameObject card1, GameObject card2)
    {
        CardScript card1Script = card1.GetComponent<CardScript>();
        CardScript card2Script = card2.GetComponent<CardScript>();

        tries++;  // Incrementa els intents
        Tries.text = "Tries: " + tries;

        // Comprova si les cartes coincideixen
        if (card1Script.cardID == card2Script.cardID)
        {
            AudioSource.PlayClipAtPoint(MatchFoundSound, Camera.main.transform.position);  // So quan les cartes coincideixen
            score += 1;  // Augmenta el score
            Score.text = "Score: " + score;
            matchedPairs++;  // Augmenta el nombre de parelles trobades

            yield return new WaitForSeconds(timeToDisappear + 1.5f);  // Espera per un temps determinat

            // Desactiva les cartes coincidides
            card1.SetActive(false);
            card2.SetActive(false);

            if (matchedPairs == 8)  // Si s'han trobat totes les parelles
            {
                EndGame();  // Finalitza el joc
            }
        }
        else
        {
            AudioSource.PlayClipAtPoint(IncorrectMatchSound, Camera.main.transform.position);  // So quan les cartes no coincideixen
            yield return new WaitForSeconds(1.5f);  // Espera per mostrar les cartes incorrectes
            card1.GetComponent<CardScript>().HideCard();  // Torna a amagar les cartes
            card2.GetComponent<CardScript>().HideCard();
        }

        // Reinicia les cartes seleccionades
        firstCard = null;
        secondCard = null;
        isProcessing = false;
    }

    // Funció que acaba el joc
    void EndGame()
    {
        gameActive = false;
        AudioSource.PlayClipAtPoint(GameEndSound, Camera.main.transform.position);  // So quan acaba el joc

        // Actualitza el millor score si és necessari
        if (score > bestScore)
        {
            bestScore = score;
            BestScore.text = "Best Score: " + bestScore;
        }

        // Mostra el missatge de finalització
        GameFinished.gameObject.SetActive(true);
        StartButton.gameObject.SetActive(true);  // Mostra el botó de començar per reiniciar
    }

    // Funció per barallar la llista de cartes
    void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    // Funció que retorna el prefab de la carta en funció de l'ID
    GameObject GetCardPrefab(int id)
    {
        switch (id)
        {
            case 0: return Cube1;
            case 1: return Cube2;
            case 2: return Cube3;
            case 3: return Cube4;
            case 4: return Cube5;
            case 5: return Cube6;
            case 6: return Cube7;
            case 7: return Cube8;
            default: return Cube1;
        }
    }
}

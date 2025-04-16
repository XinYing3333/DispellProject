using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Ink.Runtime;
using Player;
using UnityEngine.EventSystems;

public class DialogManager : MonoBehaviour
{
    [Header("Params")] 
    [SerializeField] private float typingSpeed = 0.03f;
    
    [Header("Globals Ink File")] 
    //[SerializeField] private InkFile globalsInkFile;
    
    [Header("DialogUI")] 
    [SerializeField] private GameObject dialogFrame;
    [SerializeField] private GameObject dialogIcon;
    [SerializeField] private TextMeshProUGUI dialogText;
    
    [SerializeField] private TextMeshProUGUI displayNameText;
    [SerializeField] private Animator portraitAnimator;
    private Animator layoutAnimator;

    [Header("ChoicesUI")] 
    [SerializeField] private GameObject[] choices;
    private TextMeshProUGUI[] choicesText;
    
    private Story currentStory;
    public bool dialogIsPlaying { get; private set; }

    private bool canContinueToNextLine = false;
    private Coroutine displayLineCoroutine;
    
    private static DialogManager instance;

    private const string SPEAKER_TAG = "speaker";
    private const string PORTRAIT_TAG = "portrait";
    private const string LAYOUT_TAG = "layout";
    
    private PlayerInputHandler _playerInputHandler;

    
    //private DialogVariables dialogVariables;
    
    void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("Found more than one DialogManager in scene");
        }
        instance = this;

        _playerInputHandler = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInputHandler>();

        //dialogVariables = new DialogVariables(globalsInkFile.filePath);
    }
    
    public static DialogManager GetInstance()
    {
        return instance;
    }
    
    private void Start()
    {
        dialogIsPlaying = false;
        dialogFrame.SetActive(false);

        layoutAnimator = dialogFrame.GetComponent<Animator>();

        choicesText = new TextMeshProUGUI[choices.Length];
        int index = 0;
        foreach (GameObject choice in choices)
        {
            choicesText[index] = choice.GetComponentInChildren<TextMeshProUGUI>();
            index++;
        }
    }

    private void Update()
    {
        if (!dialogIsPlaying)
        {
            return;
        }

        if(canContinueToNextLine && _playerInputHandler.InteractPressed)
        {
            ContinueStory();
        }
    }

    public void EnterDialogMode(TextAsset inkJSON) //SceneSwitcher sceneSwitcher)
    {
        currentStory = new Story(inkJSON.text);
        dialogIsPlaying = true;
        dialogFrame.SetActive(true);
        
        //dialogVariables.StartListening(currentStory);
        
        //進入游戲
        /*currentStory.BindExternalFunction("enterGame", (string gameName) =>
        {
            if (sceneSwitcher != null)
            {
                if (gameName == "game01")
                {
                    sceneSwitcher.EnterGame01();
                    ExitDialogMode();
                }
                if (gameName == "game02")
                {
                    sceneSwitcher.EnterGame02();
                    ExitDialogMode();
                }
                if (gameName == "showUI")
                {
                    sceneSwitcher.TrolleyUI();
                    ExitDialogMode();
                }
                if (gameName == "cutGrass")
                {
                    sceneSwitcher.CutGrass();
                    ExitDialogMode();
                }
                if (gameName == "restartAll")
                {
                    sceneSwitcher.RestartGame();
                    ExitDialogMode();
                }
            }
            else
            {
                Debug.LogWarning("No sceneSwitcher was founded");
            }
        });*/
        
        displayNameText.text = "Speaker";
        portraitAnimator.Play("barth_default");
        layoutAnimator.Play("bottom");
        
        ContinueStory();
    }

    private void ExitDialogMode()
    {
        //currentStory.UnbindExternalFunction("enterGame");
        
        //dialogVariables.StopListening(currentStory);
        
        dialogIsPlaying = false;
        dialogFrame.SetActive(false);
        dialogText.text = "";
    }

    private void ContinueStory()
    {
        if (currentStory.canContinue)
        {
            if (displayLineCoroutine != null)
            {
                StopCoroutine(displayLineCoroutine);
            }
            displayLineCoroutine = StartCoroutine(DisplayLine(currentStory.Continue()));
            HandleTags(currentStory.currentTags);
        }
        else
        {
            ExitDialogMode();
        }
    }

    private IEnumerator DisplayLine(string line)
    {
        dialogText.text = "";
        
        dialogIcon.SetActive(false);
        HideChoices();
        canContinueToNextLine = false;

        foreach (char letter in line.ToCharArray())
        {
            dialogText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        
        dialogIcon.SetActive(true);
        DisplayChoices();
        
        canContinueToNextLine = true;
    }

    private void HideChoices()
    {
        foreach (GameObject choiceButton in choices)
        {
            choiceButton.SetActive(false);
        }
    }
    
    private void HandleTags(List<string> currentTags)
    {
        foreach (string tag in currentTags)
        {
            string[] splitTag = tag.Split(':');
            if (splitTag.Length != 2)
            {
                Debug.LogError("Tag could not be appropriately parsed:" + tag);
            }

            string tagKey = splitTag[0].Trim();
            string tagValue = splitTag[1].Trim();

            switch (tagKey)
            {
                case SPEAKER_TAG:
                    displayNameText.text = tagValue;
                    break;
                case PORTRAIT_TAG:
                    portraitAnimator.Play(tagValue);
                    break;
                case LAYOUT_TAG:
                    layoutAnimator.Play(tagValue);
                    break;
                default:
                    Debug.Log("Tag came in but not currently being handled :" + tag);
                    break;
            }
        }
    }
    
    private void DisplayChoices()
    {
        List<Choice> currentChoices = currentStory.currentChoices;

        if (currentChoices.Count > choices.Length)
        {
            Debug.LogError("More choices were given than UI can support. Number of choices given:" 
                           + currentChoices.Count);
        }

        int index = 0;
        foreach (Choice choice in currentChoices)
        {
            choices[index].gameObject.SetActive(true);
            choicesText[index].text = choice.text;
            index++;
        }

        for (int i = index; i < choices.Length; i++)
        {
            choices[i].gameObject.SetActive(false);
        }

        StartCoroutine(SelectFirstChoice());
    }

    private IEnumerator SelectFirstChoice()
    {
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();
        EventSystem.current.SetSelectedGameObject(choices[0].gameObject);
    }

    public void MakeChoices(int choiceIndex)
    {
        if (canContinueToNextLine)
        {
            currentStory.ChooseChoiceIndex(choiceIndex);
        }
    }
}

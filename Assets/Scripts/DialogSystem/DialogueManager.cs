using System.Collections;
using System.Collections.Generic;
using DefaultNamespace.EventBus;
using DefaultNamespace.EventBus.Events.Dialog;
using DefaultNamespace.EventBus.Events.UI; // LanguageChanged
using Ink.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using DialogSystem;
using Player;
using UI.Localization;

/// <summary>
/// 語言狀態全面交給 LocalizationService：
/// - DialogueManager 不存語言、不讀寫 PlayerPrefs、不做 zh/en/jp 判斷
/// - 只在進入/重啟對話時把 LocalizationService.CurrentInkLangCode 寫入 Ink 變數 "lang"
/// - 收到 LanguageChanged 時若正在對話就重啟（force restart）
/// </summary>
public class DialogueManager : MonoBehaviour
{
    [Header("Params")]
    [SerializeField] private float typingSpeed = 0.09f;

    [Header("Load Globals JSON")]
    [SerializeField] private TextAsset loadGlobalsJSON;

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private GameObject continueIcon;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI displayNameText;
    [SerializeField] private Animator portraitAnimator;
    [SerializeField] private Animator layoutAnimator;

    [Header("Choices UI")]
    [SerializeField] private GameObject[] choices;
    private TextMeshProUGUI[] choicesText;

    [Header("Audio")]
    [SerializeField] private DialogueAudioInfoSO defaultAudioInfo;
    [SerializeField] private DialogueAudioInfoSO[] audioInfos;
    [SerializeField] private bool makePredictable;
    private DialogueAudioInfoSO currentAudioInfo;
    private Dictionary<string, DialogueAudioInfoSO> audioInfoDictionary;
    private AudioSource audioSource;

    private Story currentStory;
    public bool dialogueIsPlaying { get; private set; }

    private bool canContinueToNextLine = false;
    private Coroutine displayLineCoroutine;

    private static DialogueManager instance;

    private const string SPEAKER_TAG = "speaker";
    private const string PORTRAIT_TAG = "portrait";
    private const string LAYOUT_TAG = "layout";
    private const string AUDIO_TAG = "audio";

    private DialogueVariables dialogueVariables;
    private InkExternalFunctions inkExternalFunctions;

    private bool isAutoDisplay;

    private float submitLockTimer = 0f; // 倒數用
    private bool SubmitPressedNow => submitLockTimer <= 0f && PlayerInputHandler.Instance.InteractPressed;

    // 記住最後一次進入對話的參數，讓切語言可「強制重啟」
    private TextAsset lastInkJSON;
    private Animator lastEmoteAnimator;
    private bool lastAutoDisplay;
    private bool lastLockMovement;

    // 監聽語言變更
    private EventBinding<LanguageChanged> _bindLang;

    private void LockSubmit(float seconds = 0.08f)
    {
        submitLockTimer = Mathf.Max(submitLockTimer, seconds);
    }

    private void Awake()
    {
        if (instance != null)
            Debug.LogWarning("Found more than one Dialogue Manager in the scene");

        instance = this;

        dialogueVariables = new DialogueVariables(loadGlobalsJSON);
        inkExternalFunctions = new InkExternalFunctions();

        audioSource = gameObject.AddComponent<AudioSource>();
        currentAudioInfo = defaultAudioInfo;
    }

    public static DialogueManager GetInstance() => instance;

    private void OnEnable()
    {
        _bindLang = new EventBinding<LanguageChanged>(_ => OnLanguageChanged());
        EventBus<LanguageChanged>.Register(_bindLang);
    }

    private void OnDisable()
    {
        EventBus<LanguageChanged>.Deregister(_bindLang);
        _bindLang = null;
    }

    private void Start()
    {
        dialogueIsPlaying = false;
        if (dialoguePanel) dialoguePanel.SetActive(false);

        // get all of the choices text
        choicesText = new TextMeshProUGUI[choices.Length];
        for (int i = 0; i < choices.Length; i++)
            choicesText[i] = choices[i].GetComponentInChildren<TextMeshProUGUI>();

        InitializeAudioInfoDictionary();
    }

    private void InitializeAudioInfoDictionary()
    {
        audioInfoDictionary = new Dictionary<string, DialogueAudioInfoSO>();
        audioInfoDictionary.Add(defaultAudioInfo.id, defaultAudioInfo);

        foreach (DialogueAudioInfoSO audioInfo in audioInfos)
            audioInfoDictionary.Add(audioInfo.id, audioInfo);
    }

    private void SetCurrentAudioInfo(string id)
    {
        audioInfoDictionary.TryGetValue(id, out var audioInfo);
        if (audioInfo != null) currentAudioInfo = audioInfo;
        else Debug.LogWarning("Failed to find audio info for id: " + id);
    }

    private void Update()
    {
        /*if (Input.GetKeyDown(KeyCode.Y)) // TODO:切語言接口
        {
            var loc = LocalizationService.Instance;
            if (!loc) return;

            var next = loc.CurrentAppLanguage == Language.en ? Language.zh : Language.en;
            loc.SetLanguage(next);
            Debug.Log("AppLanguage = " + next);
        }*/

        if (!dialogueIsPlaying) return;

        // 沒有選項：按下提交才繼續
        if (canContinueToNextLine
            && currentStory.currentChoices.Count == 0
            && PlayerInputHandler.Instance.InteractPressed)
        {
            ContinueStory();
            return;
        }

        if (canContinueToNextLine
            && currentStory.currentChoices.Count == 0
            && isAutoDisplay)
        {
            ContinueStory();
            return;
        }

        // 有選項：按下提交則送出目前選到的選項
        if (canContinueToNextLine
            && currentStory.currentChoices.Count > 0
            && PlayerInputHandler.Instance.InteractPressed)
        {
            int idx = GetSelectedChoiceIndex();
            if (idx < 0) idx = 0;
            MakeChoice(idx);
            return;
        }

        if (submitLockTimer > 0f)
        {
            submitLockTimer -= Time.unscaledDeltaTime;
            if (submitLockTimer <= 0f) submitLockTimer = 0f;
        }
    }

    private int GetSelectedChoiceIndex()
    {
        var es = EventSystem.current;
        if (es == null) return -1;

        GameObject selected = es.currentSelectedGameObject;
        if (selected == null) return -1;

        for (int i = 0; i < choices.Length; i++)
        {
            if (choices[i].activeSelf && choices[i] == selected)
                return i;
        }
        return -1;
    }

    public void EnterDialogueMode(TextAsset inkJSON, Animator emoteAnimator = null, bool autoDisplay = false, bool lockMovement = true)
    {
        // 記住參數，方便切語言重啟
        lastInkJSON = inkJSON;
        lastEmoteAnimator = emoteAnimator;
        lastAutoDisplay = autoDisplay;
        lastLockMovement = lockMovement;

        if (lockMovement) PlayerInputHandler.Instance.SetLockMovement(true);

        EventBus<OnDialogueStarted>.Raise(new OnDialogueStarted());

        currentStory = new Story(inkJSON.text);
        inkExternalFunctions.Bind(currentStory, emoteAnimator);

        dialogueIsPlaying = true;
        if (dialoguePanel) dialoguePanel.SetActive(true);
        isAutoDisplay = autoDisplay;

        // globals 先灌進去
        dialogueVariables.StartListening(currentStory);

        // 把目前語言灌到 Ink 變數 lang
        ApplyLanguageToStory(currentStory);

        // reset portrait, layout, and speaker
        if (displayNameText) displayNameText.text = "???";
        if (portraitAnimator) portraitAnimator.Play("default");
        if (layoutAnimator) layoutAnimator.Play("layout1");

        ContinueStory();
    }

    private void OnLanguageChanged()
    {
        // 切語言：若正在對話，強制重啟（你原本需求）
        if (dialogueIsPlaying && lastInkJSON != null)
            RestartDialogue();
    }

    private void RestartDialogue()
    {
        // 停掉正在打字
        if (displayLineCoroutine != null)
        {
            StopCoroutine(displayLineCoroutine);
            displayLineCoroutine = null;
        }

        // 清掉舊 story 的綁定與監聽
        if (currentStory != null)
        {
            inkExternalFunctions.Unbind(currentStory);
            dialogueVariables.StopListening(currentStory);
        }

        // 重新建立 story
        currentStory = new Story(lastInkJSON.text);
        inkExternalFunctions.Bind(currentStory, lastEmoteAnimator);
        dialogueVariables.StartListening(currentStory);

        // 重新灌語言
        ApplyLanguageToStory(currentStory);

        // reset UI
        if (displayNameText) displayNameText.text = "???";
        if (portraitAnimator) portraitAnimator.Play("default");
        if (layoutAnimator) layoutAnimator.Play("layout1");

        if (continueIcon || !isAutoDisplay) continueIcon.SetActive(false);
        HideChoices();
        canContinueToNextLine = false;

        // 若你希望切語言後保留 autoDisplay/lockMovement 行為：
        isAutoDisplay = lastAutoDisplay;
        if (lastLockMovement) PlayerInputHandler.Instance.SetLockMovement(true);

        ContinueStory();
    }

    private IEnumerator ExitDialogueMode()
    {
        yield return new WaitForSeconds(0.2f);

        inkExternalFunctions.Unbind(currentStory);
        Debug.Log("Exiting dialogue mode");
        PlayerInputHandler.Instance.SetLockMovement(false);
        EventBus<OnDialogueEnded>.Raise(new OnDialogueEnded());

        dialogueVariables.StopListening(currentStory);
        dialogueVariables.SaveVariables();

        dialogueIsPlaying = false;
        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (dialogueText) dialogueText.text = "";

        // go back to default audio
        SetCurrentAudioInfo(defaultAudioInfo.id);
    }

    private void ContinueStory()
    {
        if (!currentStory.canContinue)
        {
            StartCoroutine(ExitDialogueMode());
            return;
        }

        if (displayLineCoroutine != null)
            StopCoroutine(displayLineCoroutine);

        string nextLine = currentStory.Continue();

        // 吃掉「只有 tag 的空輸出」
        while (string.IsNullOrWhiteSpace(nextLine) && currentStory.canContinue)
        {
            HandleTags(currentStory.currentTags);
            nextLine = currentStory.Continue();
        }

        if (string.IsNullOrWhiteSpace(nextLine) && !currentStory.canContinue)
        {
            StartCoroutine(ExitDialogueMode());
            return;
        }

        HandleTags(currentStory.currentTags);
        displayLineCoroutine = StartCoroutine(DisplayLine(nextLine));
    }

    private IEnumerator DisplayLine(string line)
    {
        if (dialogueText)
        {
            dialogueText.text = line;
            dialogueText.maxVisibleCharacters = 0;
        }

        if (continueIcon || !isAutoDisplay) continueIcon.SetActive(false);
        HideChoices();
        canContinueToNextLine = false;

        LockSubmit(0.05f);
        PlayDialogueLineSound();

        bool isAddingRichTextTag = false;

        for (int i = 0; i < line.Length;)
        {
            if (SubmitPressedNow)
            {
                if (dialogueText) dialogueText.maxVisibleCharacters = line.Length;
                break;
            }

            char c = line[i];

            if (c == '<' || isAddingRichTextTag)
            {
                isAddingRichTextTag = true;
                i++;
                if (dialogueText) dialogueText.maxVisibleCharacters = i;
                if (c == '>') isAddingRichTextTag = false;
                continue;
            }

            i++;
            if (dialogueText) dialogueText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingSpeed);
        }

        if (isAutoDisplay) yield return new WaitForSeconds(2f);
        if (!isAutoDisplay && continueIcon) continueIcon.SetActive(true);

        DisplayChoices();
        canContinueToNextLine = true;
    }

    private void PlayDialogueLineSound()
    {
        AudioClip[] clips = currentAudioInfo.dialogueTypingSoundClips;
        if (clips == null || clips.Length == 0) return;

        if (currentAudioInfo.stopAudioSource)
            audioSource.Stop();

        AudioClip clip = clips[0];
        if (!makePredictable)
        {
            int rand = Random.Range(0, clips.Length);
            clip = clips[rand];
        }

        audioSource.pitch = Random.Range(currentAudioInfo.minPitch, currentAudioInfo.maxPitch);
        audioSource.PlayOneShot(clip);
    }

    private void HideChoices()
    {
        foreach (GameObject choiceButton in choices)
            choiceButton.SetActive(false);
    }

    private void HandleTags(List<string> currentTags)
    {
        foreach (string tag in currentTags)
        {
            string[] splitTag = tag.Split(':');
            if (splitTag.Length != 2)
            {
                Debug.LogError("Tag could not be appropriately parsed: " + tag);
                continue;
            }

            string tagKey = splitTag[0].Trim();
            string tagValue = splitTag[1].Trim();

            switch (tagKey)
            {
                case SPEAKER_TAG:
                    if (displayNameText) displayNameText.text = tagValue;
                    break;

                case PORTRAIT_TAG:
                    if (portraitAnimator) portraitAnimator.Play(tagValue);
                    break;

                case LAYOUT_TAG:
                    if (layoutAnimator) layoutAnimator.Play(tagValue);
                    break;

                case AUDIO_TAG:
                    SetCurrentAudioInfo(tagValue);
                    break;

                default:
                    Debug.LogWarning("Tag came in but is not currently being handled: " + tag);
                    break;
            }
        }
    }

    private void DisplayChoices()
    {
        List<Choice> currentChoices = currentStory.currentChoices;

        if (currentChoices.Count > choices.Length)
            Debug.LogError("More choices were given than the UI can support. Number of choices given: " + currentChoices.Count);

        int index = 0;
        foreach (Choice choice in currentChoices)
        {
            choices[index].gameObject.SetActive(true);
            choicesText[index].text = choice.text;
            index++;
        }

        for (int i = index; i < choices.Length; i++)
            choices[i].gameObject.SetActive(false);

        StartCoroutine(SelectFirstChoice());
    }

    private IEnumerator SelectFirstChoice()
    {
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();
        EventSystem.current.SetSelectedGameObject(choices[0].gameObject);
    }

    public void MakeChoice(int choiceIndex)
    {
        if (!canContinueToNextLine) return;

        currentStory.ChooseChoiceIndex(choiceIndex);
        ContinueStory();
    }

    public Ink.Runtime.Object GetVariableState(string variableName)
    {
        Ink.Runtime.Object variableValue = null;
        dialogueVariables.variables.TryGetValue(variableName, out variableValue);
        if (variableValue == null)
            Debug.LogWarning("Ink Variable was found to be null: " + variableName);

        return variableValue;
    }

    public void OnApplicationQuit()
    {
        dialogueVariables.SaveVariables();
    }

    // 只從 LocalizationService 取得 Ink 語言代碼
    private void ApplyLanguageToStory(Story story)
    {
        if (story == null) return;

        var loc = LocalizationService.Instance;
        story.variablesState["lang"] = (loc != null) ? loc.CurrentInkLangCode : "en"; // "zh"/"en"/"jp"
    }
}

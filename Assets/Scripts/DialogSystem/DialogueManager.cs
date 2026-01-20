using System.Collections;
using System.Collections.Generic;
using DefaultNamespace.EventBus.Events.Dialog;
using UnityEngine;
using TMPro;
using Ink.Runtime;
using UnityEngine.EventSystems;
using DialogSystem;
using Player;

public enum DialogueLanguage
{
    zh,
    en,
    jp
}

public class DialogueManager : MonoBehaviour
{
    [Header("Params")]
    [SerializeField] private float typingSpeed = 0.09f;

    [Header("Language")]
    [SerializeField] private DialogueLanguage startLanguage = DialogueLanguage.en;
    private const string LANG_PREF_KEY = "DIALOGUE_LANG";
    public DialogueLanguage CurrentLanguage { get; private set; }

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

    // ===== 新增：記住最後一次進入對話的參數，讓你可以「切語言強制重啟」=====
    private TextAsset lastInkJSON;
    private Animator lastEmoteAnimator;
    private bool lastAutoDisplay;
    private bool lastLockMovement;

    private void LockSubmit(float seconds = 0.08f)
    {
        submitLockTimer = Mathf.Max(submitLockTimer, seconds);
    }

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("Found more than one Dialogue Manager in the scene");
        }
        instance = this;

        dialogueVariables = new DialogueVariables(loadGlobalsJSON);
        inkExternalFunctions = new InkExternalFunctions();

        audioSource = this.gameObject.AddComponent<AudioSource>();
        currentAudioInfo = defaultAudioInfo;

        // ===== 新增：載入語言 =====
        LoadLanguage();
    }

    public static DialogueManager GetInstance()
    {
        return instance;
    }

    private void Start()
    {
        dialogueIsPlaying = false;
        dialoguePanel.SetActive(false);

        // get all of the choices text
        choicesText = new TextMeshProUGUI[choices.Length];
        int index = 0;
        foreach (GameObject choice in choices)
        {
            choicesText[index] = choice.GetComponentInChildren<TextMeshProUGUI>();
            index++;
        }

        InitializeAudioInfoDictionary();
    }

    private void InitializeAudioInfoDictionary()
    {
        audioInfoDictionary = new Dictionary<string, DialogueAudioInfoSO>();
        audioInfoDictionary.Add(defaultAudioInfo.id, defaultAudioInfo);
        foreach (DialogueAudioInfoSO audioInfo in audioInfos)
        {
            audioInfoDictionary.Add(audioInfo.id, audioInfo);
        }
    }

    private void SetCurrentAudioInfo(string id)
    {
        DialogueAudioInfoSO audioInfo = null;
        audioInfoDictionary.TryGetValue(id, out audioInfo);
        if (audioInfo != null)
        {
            this.currentAudioInfo = audioInfo;
        }
        else
        {
            Debug.LogWarning("Failed to find audio info for id: " + id);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y)) //Debug
        {
            var next = (CurrentLanguage == DialogueLanguage.en) ? DialogueLanguage.zh : DialogueLanguage.en;
            SetLanguage(next, forceRestartIfPlaying: true);
            Debug.Log("Lang = " + next);
        }

        if (!dialogueIsPlaying) return;

        // 沒有選項：按下提交才繼續
        if (canContinueToNextLine
            && currentStory.currentChoices.Count == 0 && PlayerInputHandler.Instance.InteractPressed)
        {
            ContinueStory();
            return;
        }
        if (canContinueToNextLine
            && currentStory.currentChoices.Count == 0 && isAutoDisplay)
        {
            ContinueStory();
            return;
        }

        // 有選項：按下提交則送出目前選到的選項
        if (canContinueToNextLine
            && currentStory.currentChoices.Count > 0 && PlayerInputHandler.Instance.InteractPressed)
        {
            int idx = GetSelectedChoiceIndex();
            if (idx < 0) idx = 0;
            MakeChoice(idx);
            return;
        }

        if (submitLockTimer > 0f)
        {
            submitLockTimer -= Time.unscaledDeltaTime;
            if (submitLockTimer <= 0f)
                submitLockTimer = 0f;
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
        // ===== 新增：記住參數，方便切語言重啟 =====
        lastInkJSON = inkJSON;
        lastEmoteAnimator = emoteAnimator;
        lastAutoDisplay = autoDisplay;
        lastLockMovement = lockMovement;

        if (lockMovement) PlayerInputHandler.Instance.SetLockMovement(true);

        EventBus<OnDialogueStarted>.Raise(new OnDialogueStarted());

        currentStory = new Story(inkJSON.text);
        inkExternalFunctions.Bind(currentStory, emoteAnimator);

        dialogueIsPlaying = true;
        dialoguePanel.SetActive(true);
        isAutoDisplay = autoDisplay;

        // globals 先灌進去（注意：如果你的 globals 也有 VAR lang，這裡會覆寫它）
        dialogueVariables.StartListening(currentStory);

        // ===== 新增：把目前語言灌到 Ink 變數 lang（要在 StartListening 後做，避免被 globals 覆寫）=====
        ApplyLanguageToStory(currentStory);

        // reset portrait, layout, and speaker
        displayNameText.text = "???";
        portraitAnimator.Play("default");
        layoutAnimator.Play("layout1");

        ContinueStory();
    }

    // ===== 新增：外部切語言入口（你要接到設定 UI 按鈕）=====
    public void SetLanguage(DialogueLanguage lang, bool forceRestartIfPlaying = true)
    {
        if (CurrentLanguage == lang && !forceRestartIfPlaying) return;

        CurrentLanguage = lang;
        PlayerPrefs.SetString(LANG_PREF_KEY, CurrentLanguage.ToString());

        // 你說「切換語言會強制重啟」：正在對話就從頭播
        if (forceRestartIfPlaying && dialogueIsPlaying && lastInkJSON != null)
        {
            RestartDialogue();
        }
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
        ApplyLanguageToStory(currentStory);

        // reset UI
        displayNameText.text = "???";
        portraitAnimator.Play("default");
        layoutAnimator.Play("layout1");

        continueIcon.SetActive(false);
        HideChoices();
        canContinueToNextLine = false;

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
        dialoguePanel.SetActive(false);
        dialogueText.text = "";

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
        {
            StopCoroutine(displayLineCoroutine);
        }

        string nextLine = currentStory.Continue();

        // ★ 關鍵：吃掉「只有 tag 的空輸出」
        while (string.IsNullOrWhiteSpace(nextLine) && currentStory.canContinue)
        {
            // 空行也可能帶 tags，一定要先處理
            HandleTags(currentStory.currentTags);

            nextLine = currentStory.Continue();
        }

        // 如果最後一個也是空行且不能繼續，正常結束
        if (string.IsNullOrWhiteSpace(nextLine) && !currentStory.canContinue)
        {
            StartCoroutine(ExitDialogueMode());
            return;
        }

        // 正常顯示
        HandleTags(currentStory.currentTags);
        displayLineCoroutine = StartCoroutine(DisplayLine(nextLine));
    }


    private IEnumerator DisplayLine(string line)
    {
        dialogueText.text = line;
        dialogueText.maxVisibleCharacters = 0;
        continueIcon.SetActive(false);
        HideChoices();
        canContinueToNextLine = false;

        LockSubmit(0.05f);

        PlayDialogueLineSound();

        bool isAddingRichTextTag = false;

        for (int i = 0; i < line.Length;)
        {
            if (SubmitPressedNow)
            {
                dialogueText.maxVisibleCharacters = line.Length;
                break;
            }

            char c = line[i];

            if (c == '<' || isAddingRichTextTag)
            {
                isAddingRichTextTag = true;
                i++;
                dialogueText.maxVisibleCharacters = i;
                if (c == '>') isAddingRichTextTag = false;
                continue;
            }

            i++;
            dialogueText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingSpeed);
        }

        if (isAutoDisplay) yield return new WaitForSeconds(0.8f);

        if (!isAutoDisplay)
        {
            continueIcon.SetActive(true);
        }

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
                Debug.LogError("Tag could not be appropriately parsed: " + tag);
                continue;
            }
            string tagKey = splitTag[0].Trim();
            string tagValue = splitTag[1].Trim();

            switch (tagKey)
            {
                case SPEAKER_TAG:
                    displayNameText.text = tagValue;
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
        {
            Debug.LogError("More choices were given than the UI can support. Number of choices given: "
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

    public void MakeChoice(int choiceIndex)
    {
        if (canContinueToNextLine)
        {
            currentStory.ChooseChoiceIndex(choiceIndex);
            ContinueStory();
        }
    }

    public Ink.Runtime.Object GetVariableState(string variableName)
    {
        Ink.Runtime.Object variableValue = null;
        dialogueVariables.variables.TryGetValue(variableName, out variableValue);
        if (variableValue == null)
        {
            Debug.LogWarning("Ink Variable was found to be null: " + variableName);
        }
        return variableValue;
    }

    public void OnApplicationQuit()
    {
        dialogueVariables.SaveVariables();
    }

    // ===== 新增：語言灌入 Ink 的共用方法 =====
    private void ApplyLanguageToStory(Story story)
    {
        if (story == null) return;
        story.variablesState["lang"] = CurrentLanguage.ToString(); // "zh"/"en"/"jp"
    }

    private void LoadLanguage()
    {
        if (PlayerPrefs.HasKey(LANG_PREF_KEY))
        {
            var s = PlayerPrefs.GetString(LANG_PREF_KEY, startLanguage.ToString());
            if (System.Enum.TryParse(s, out DialogueLanguage lang))
                CurrentLanguage = lang;
            else
                CurrentLanguage = startLanguage;
        }
        else
        {
            CurrentLanguage = startLanguage;
            PlayerPrefs.SetString(LANG_PREF_KEY, CurrentLanguage.ToString());
        }
    }
}

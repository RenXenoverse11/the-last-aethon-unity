using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLastAethon.UI
{
    public enum DialogueSpeaker
    {
        Narrator,
        Ren,
        Vesper
    }

    public struct DialogueLine
    {
        public DialogueSpeaker Speaker;
        public string Text;
    }

    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private GameObject portraitLeft;
        [SerializeField] private GameObject portraitRight;
        [SerializeField] private RectTransform dialogueBox;
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private TextMeshProUGUI continueHint;
        [SerializeField] private float charDelay = 0.028f;

        private const float BoxCenterX = 0f;
        private const float BoxLeftX = -300f;
        private const float BoxRightX = 300f;

        private InputActionMap gameplayMap;
        private InputActionMap dialogueMap;
        private InputAction advanceAction;

        private DialogueLine[] lines;
        private int lineIndex;
        private bool isTyping;
        private Coroutine typeCoroutine;
        private Action onComplete;

        private void Awake()
        {
            gameplayMap = inputActions.FindActionMap("Gameplay");
            dialogueMap = inputActions.FindActionMap("Dialogue");
            advanceAction = dialogueMap.FindAction("Advance");
            gameObject.SetActive(false);
        }

        public void Play(DialogueLine[] sequence, Action onCompleteCallback)
        {
            lines = sequence;
            onComplete = onCompleteCallback;
            lineIndex = -1;
            gameObject.SetActive(true);
            gameplayMap.Disable();
            dialogueMap.Enable();
            ShowNextLine();
        }

        private void Update()
        {
            if (advanceAction.WasPressedThisFrame())
            {
                if (isTyping)
                {
                    SkipTyping();
                }
                else
                {
                    ShowNextLine();
                }
            }

            if (continueHint.gameObject.activeSelf)
            {
                float alpha = Mathf.PingPong(Time.unscaledTime * 2f, 1f);
                Color c = continueHint.color;
                c.a = alpha;
                continueHint.color = c;
            }
        }

        private void ShowNextLine()
        {
            lineIndex++;
            if (lineIndex >= lines.Length)
            {
                EndDialogue();
                return;
            }

            DialogueLine line = lines[lineIndex];
            ApplySpeakerVisuals(line.Speaker);
            dialogueText.text = string.Empty;
            continueHint.gameObject.SetActive(false);

            if (typeCoroutine != null)
            {
                StopCoroutine(typeCoroutine);
            }
            typeCoroutine = StartCoroutine(TypeLine(line.Text));
        }

        private void ApplySpeakerVisuals(DialogueSpeaker speaker)
        {
            portraitLeft.SetActive(speaker == DialogueSpeaker.Ren);
            portraitRight.SetActive(speaker == DialogueSpeaker.Vesper);
            nameLabel.gameObject.SetActive(speaker != DialogueSpeaker.Narrator);
            nameLabel.text = speaker == DialogueSpeaker.Ren ? "Ren" : speaker == DialogueSpeaker.Vesper ? "Vesper" : string.Empty;

            float boxX = speaker == DialogueSpeaker.Ren ? BoxRightX : speaker == DialogueSpeaker.Vesper ? BoxLeftX : BoxCenterX;
            dialogueBox.anchoredPosition = new Vector2(boxX, dialogueBox.anchoredPosition.y);
        }

        private IEnumerator TypeLine(string text)
        {
            isTyping = true;
            for (int i = 0; i <= text.Length; i++)
            {
                dialogueText.text = text.Substring(0, i);
                yield return new WaitForSeconds(charDelay);
            }
            isTyping = false;
            continueHint.gameObject.SetActive(true);
        }

        private void SkipTyping()
        {
            StopCoroutine(typeCoroutine);
            dialogueText.text = lines[lineIndex].Text;
            isTyping = false;
            continueHint.gameObject.SetActive(true);
        }

        private void EndDialogue()
        {
            gameObject.SetActive(false);
            dialogueMap.Disable();
            gameplayMap.Enable();
            Action callback = onComplete;
            onComplete = null;
            callback?.Invoke();
        }
    }
}

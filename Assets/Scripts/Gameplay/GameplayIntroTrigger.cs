using UnityEngine;
using TheLastAethon.UI;

namespace TheLastAethon.Gameplay
{
    public class GameplayIntroTrigger : MonoBehaviour
    {
        [SerializeField] private DialogueUI dialogueUI;

        private void Start()
        {
            DialogueLine[] lines =
            {
                new DialogueLine { Speaker = DialogueSpeaker.Narrator, Text = "Ashenveil Forest. Ten years after the fall of the Aethon Clan." },
                new DialogueLine { Speaker = DialogueSpeaker.Ren, Text = "Still here. Still breathing. That's enough for today." },
                new DialogueLine { Speaker = DialogueSpeaker.Narrator, Text = "He had been running for as long as he could remember. But lately... something felt different." },
                new DialogueLine { Speaker = DialogueSpeaker.Ren, Text = "The patrols are getting closer. They're not just searching anymore." },
                new DialogueLine { Speaker = DialogueSpeaker.Vesper, Text = "You're not as hard to find as they say." },
                new DialogueLine { Speaker = DialogueSpeaker.Ren, Text = "..." },
                new DialogueLine { Speaker = DialogueSpeaker.Vesper, Text = "The last person you'll ever meet. Now stop talking." },
            };

            dialogueUI.Play(lines, null);
        }
    }
}

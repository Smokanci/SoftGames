using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DialogueRowView : MonoBehaviour
{
    [SerializeField] private HorizontalLayoutGroup layout;
    [SerializeField] private TMP_Text              speakerLabel;
    [SerializeField] private TMP_Text              bodyLabel;
    [SerializeField] private Image                 portrait;
    [SerializeField] private TMP_Text              initialsLabel;

    public void Bind(DialogueLine line)
    {
        speakerLabel.SetText(line.Speaker);
        speakerLabel.gameObject.SetActive(!string.IsNullOrEmpty(line.Speaker));
        bodyLabel.SetText(EmojiSpriteMarkup.Apply(line.Text));

        initialsLabel.SetText(line.SpeakerInitials);
        ShowInitials();

        var onTheRight = line.Side == DialogueSide.Right;

        // The bubble fills the row, so the side has to read from where the avatar sits and
        // which edge the words start at.
        layout.reverseArrangement = onTheRight;
        speakerLabel.alignment    = onTheRight ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
        bodyLabel.alignment       = onTheRight ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
    }

    // Hides the bound text and counts it back in, rather than growing the string a letter at a
    // time. The bubble is laid out against the whole line from the first frame, so the row never
    // reflows mid-reveal and an emoji arrives as one whole sprite. Hiding here rather than in
    // Bind keeps a row that nobody reveals readable.
    public async Awaitable Reveal(float charactersPerSecond, CancellationToken cancellationToken)
    {
        bodyLabel.ForceMeshUpdate();

        var total = bodyLabel.textInfo.characterCount;
        var shown = 0f;

        bodyLabel.maxVisibleCharacters = 0;

        while (shown < total)
        {
            await Awaitable.NextFrameAsync(cancellationToken);

            // Accumulated rather than one character per frame, so a speed above the frame rate
            // still reads as that speed instead of stalling at the refresh rate.
            shown                          += charactersPerSecond * Time.deltaTime;
            bodyLabel.maxVisibleCharacters =  Mathf.Min(total, Mathf.FloorToInt(shown));
        }

        bodyLabel.maxVisibleCharacters = int.MaxValue;
    }

    public void SetAvatar(Sprite sprite)
    {
        portrait.sprite       = sprite;
        portrait.enabled      = true;
        initialsLabel.enabled = false;
    }

    // The portrait arrives a few frames after the row, or never. The initials hold the space
    // either way, so a row never resizes when an image lands.
    private void ShowInitials()
    {
        portrait.enabled      = false;
        initialsLabel.enabled = true;
    }
}

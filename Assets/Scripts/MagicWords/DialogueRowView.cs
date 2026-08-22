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
        bodyLabel.SetText(line.Text);

        initialsLabel.SetText(line.SpeakerInitials);
        ShowInitials();

        var onTheRight = line.Side == DialogueSide.Right;

        // The bubble fills the row, so the side has to read from where the avatar sits and
        // which edge the words start at.
        layout.reverseArrangement = onTheRight;
        speakerLabel.alignment    = onTheRight ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
        bodyLabel.alignment       = onTheRight ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
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

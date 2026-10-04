using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkUI : MonoBehaviour
{
    [Header("Przyciski UI")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    [Header("Pole tekstowe do kodu")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Awake()
    {
        hostButton.onClick.AddListener(async () =>
        {
            if (statusText != null) statusText.text = "Tworzenie pokoju...";
            string code = await RelayManager.Instance.CreateRelay();

            if (!string.IsNullOrEmpty(code))
            {
                if (statusText != null) statusText.text = $"Twój KOD: {code}";
                HideButtons();
            }
        });

        clientButton.onClick.AddListener(async () =>
        {
            string code = joinCodeInput.text.Trim();
            if (string.IsNullOrEmpty(code))
            {
                if (statusText != null) statusText.text = "Wpisz kod połączenia!";
                return;
            }

            if (statusText != null) statusText.text = "Łączenie...";
            bool success = await RelayManager.Instance.JoinRelay(code);

            if (success)
            {
                HideUI();
            }
        });
    }

    private void HideButtons()
    {
        hostButton.gameObject.SetActive(false);
        clientButton.gameObject.SetActive(false);
        if (joinCodeInput != null) joinCodeInput.gameObject.SetActive(false);
    }

    private void HideUI()
    {
        gameObject.SetActive(false);
    }
}
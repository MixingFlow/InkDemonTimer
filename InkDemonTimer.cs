using System;
using BepInEx;
using TMPro;
using UnityEngine;

namespace InkDemonTimerMod
{
    [BepInPlugin("com.mixingflow.inkdemontimer", "Ink Demon Timer", "1.0.0")]
    public class InkDemonTimerPlugin : BaseUnityPlugin
    {
        void Awake()
        {
            // Separate GameObject so the game doesn't destroy it during scene changes
            var controllerObject = new GameObject("InkDemonTimerController");
            controllerObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(controllerObject);
            controllerObject.AddComponent<InkDemonTimerController>();
        }
    }

    public class InkDemonTimerController : MonoBehaviour
    {
        private TextMeshProUGUI timerText;
        private UIHUD currentHUD;
        private bool isVisible = true;

        private readonly Color normalColor = new Color32(167, 113, 42, 255);
        private readonly Color urgentColor = new Color32(163, 98, 36, 255);

        void OnDestroy()
        {
            // Clean up text object
            if (timerText != null && timerText.gameObject != null)
            {
                Destroy(timerText.gameObject);
            }
        }

        void Update()
        {
            // F9 toggles visibility
            if (Input.GetKeyDown(KeyCode.F9))
            {
                isVisible = !isVisible;
                if (timerText != null)
                {
                    timerText.enabled = isVisible;
                }
            }

            // Check if we need to find a new HUD (destroyed, deactivated, or scene changed)
            bool needToFindHUD = currentHUD == null || currentHUD.gameObject == null || timerText == null || !currentHUD.gameObject.activeInHierarchy;
            if (needToFindHUD)
            {
                UIHUD activeHUD = FindObjectOfType<UIHUD>();
                if (activeHUD != null)
                {
                    // If new HUD found -> clean up old text
                    if (activeHUD != currentHUD)
                    {
                        if (timerText != null && timerText.gameObject != null)
                        {
                            Destroy(timerText.gameObject);
                        }
                        timerText = null;
                        currentHUD = activeHUD;
                    }

                    // Attach the timer text to the active HUD
                    if (timerText == null)
                    {
                        AttachToHUD(currentHUD);
                    }
                }
            }

            UpdateTimerText();
        }

        private void AttachToHUD(UIHUD hud)
        {
            // Setup the text on the game's native HUD
            var textObject = new GameObject("InkDemonTimerText");
            textObject.transform.SetParent(hud.transform, false);

            timerText = textObject.AddComponent<TextMeshProUGUI>();
            timerText.fontSize = 40f;
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.raycastTarget = false;
            timerText.enabled = isVisible;

            // Anchor to the top
            var rect = timerText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -10f);
            rect.sizeDelta = new Vector2(400f, 100f);
            
            timerText.enableAutoSizing = false;
            timerText.enableWordWrapping = false;
            timerText.overflowMode = TextOverflowModes.Overflow;
        }

        private void UpdateTimerText()
        {
            // Skip update if timer text is not initialized or invisible
            if (timerText == null || !isVisible)
            {
                return;
            }

            // Scan the game's memory for the custom font
            if (timerText.font == null || !timerText.font.name.Contains("CaviarDreams"))
            {
                foreach (var font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                {
                    if (font.name.Contains("CaviarDreams"))
                    {
                        timerText.font = font;
                        break;
                    }
                }
            }

            var gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                timerText.text = "";
                return;
            }

            var demonManager = gameManager.InkDemonManager;
            if (demonManager == null || !demonManager.IsActive)
            {
                timerText.text = "";
                return;
            }

            // Calculate remaining time
            float remainingSeconds = Mathf.Max(0f, demonManager.TimerLimit - demonManager.Timer);
            bool isUrgent = remainingSeconds <= 5f;

            int minutes = Mathf.FloorToInt(remainingSeconds / 60f);
            int seconds = Mathf.FloorToInt(remainingSeconds % 60f);
            
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            // Handle color and blinking for the final 5 seconds
            Color targetColor = isUrgent ? urgentColor : normalColor;
            float blinkAlpha = isUrgent ? (0.5f + 0.5f * Mathf.Sin(Time.time * 8f)) : 1f;
            
            timerText.color = new Color(targetColor.r, targetColor.g, targetColor.b, targetColor.a * blinkAlpha);
        }
    }
}

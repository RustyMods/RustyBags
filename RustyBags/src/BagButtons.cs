using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RustyBags;

public class BagButtons : MonoBehaviour
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    private static class InventoryGui_Awake_Patch
    {
        private static void Prefix(InventoryGui __instance)
        {
            GameObject ButtonContainer = new GameObject("bag_buttons");
            RectTransform rect = ButtonContainer.AddComponent<RectTransform>();
            rect.SetParent(__instance.m_container, false);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            ButtonContainer.AddComponent<BagButtons>();
        }
    }

    public static BagButtons? instance;

    private ButtonElement hide = null!;
    private ButtonElement auto = null!;
    
    public void Awake()
    {
        instance = this;
        InventoryGui? gui = GetComponentInParent<InventoryGui>();
        Button? stackAll = gui.m_stackAllButton;
        HorizontalLayoutGroup? layout = gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.childControlHeight = false;
        layout.childControlWidth = false;
        layout.spacing = 2f;
        layout.padding.top = 10;

        ButtonElement template = new ButtonElement(stackAll.gameObject);
        
        auto = template.Create(layout.transform, "RustyBag_Auto_Button");
        hide = template.Create(layout.transform, "RustyBag_Hide_Button");
        
        hide.AddListener(OnHide);
        auto.AddListener(OnAuto);
        
        hide.SetLabel("$bag_hide");
        auto.SetLabel("$bag_auto");
        
        hide.SetGamePadKey("JoyRTrigger");
        auto.SetGamePadKey("JoyLTrigger");
        
        Hide();
    }

    public void OnDestroy()
    {
        instance = null;
    }
    
    public static bool IsVisible() => instance?.gameObject.activeInHierarchy ?? false;

    public void Show(Bag bag)
    {
        gameObject.SetActive(true);
        hide.SetLabel(bag.hidden ? "$bag_show" : "$bag_hide");
        auto.SetLabel(bag.autoOpen ? "$bag_manual" : "$bag_auto");
    }
    public void Hide() => gameObject.SetActive(false);

    public void OnHide()
    {
        if (BagGui.m_currentBag == null) return;
        if (BagGui.m_currentBag.hidden)
        {
            BagGui.m_currentBag.SetVisible(true);
            hide.SetLabel("$bag_hide");
        }
        else
        {
            BagGui.m_currentBag.SetVisible(false);
            hide.SetLabel("$bag_show");
        }
    }

    public void OnAuto()
    {
        if (BagGui.m_currentBag == null) return;
        if (BagGui.m_currentBag.autoOpen)
        {
            BagGui.m_currentBag.SetAuto(false);
            auto.SetLabel("$bag_auto");
        }
        else
        {
            BagGui.m_currentBag.SetAuto(true);
            auto.SetLabel("$bag_manual");
        }
    }

    private class ButtonElement
    {
        public readonly GameObject go;
        
        public RectTransform rect;
        public readonly Button button;
        public ButtonSfx buttonSfx;
        public readonly UIGamePad uiGamePad;

        public readonly TMPro.TMP_Text text;
        public UIInputHint inputHint;
        public readonly TMPro.TMP_Text inputText;
        
        public ButtonElement(GameObject source)
        {
            go = source;
            rect = source.GetComponent<RectTransform>();
            button = source.GetComponent<Button>();
            buttonSfx = source.GetComponent<ButtonSfx>();
            uiGamePad = source.GetComponent<UIGamePad>();
            text = source.transform.Find("Text").GetComponent<TMPro.TMP_Text>();
            inputHint = source.transform.Find("gamepad_hint (1)").GetComponent<UIInputHint>();
            inputText = inputHint.transform.Find("Text").GetComponent<TMPro.TMP_Text>();
        }

        public ButtonElement Create(Transform parent, string name)
        {
            GameObject clone = UnityEngine.Object.Instantiate(go, parent);
            clone.name = name;
            return new ButtonElement(clone);
        }

        public void AddListener(UnityAction action) => button.onClick.AddListener(action);

        public void SetLabel(string label) => text.text = Localization.instance.Localize(label);

        public void SetGamePadKey(string key)
        {
            uiGamePad.m_zinputKey = key;
            inputText.text = Localization.instance.Localize(ZInput.instance.GetBoundKeyString(key, true));
        }
    }
    
}
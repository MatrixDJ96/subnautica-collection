using BetterQuickSlots.Utility;
using UnityEngine;
using BetterSubnautica.MonoBehaviours;
using TMPro;
using Text = TMPro.TextMeshProUGUI;

namespace BetterQuickSlots.MonoBehaviours
{
    public class QuickSlotsController : AbstractAwakeSingleton<QuickSlotsController>
    {
        private uGUI_QuickSlots component = null;
        public uGUI_QuickSlots Component
        {
            get
            {
                if (component == null)
                {
                    component = gameObject.GetComponent<uGUI_QuickSlots>();
                }
                return component;
            }
        }

        public uGUI_ItemIcon[] Icons => Component != null ? component.icons : null;

        public QuickSlots Target => Component != null ? component.target as QuickSlots : null;

        public bool ForceUpdate { get; set; } = true;

#if SUBNAUTICA
        // The game owns the slot bindings: refresh the labels whenever the player rebinds
        // them in the options panel.
        protected void OnEnable()
        {
            GameInput.OnBindingsChanged += OnBindingsChanged;
        }

        protected void OnDisable()
        {
            GameInput.OnBindingsChanged -= OnBindingsChanged;
        }

        private void OnBindingsChanged()
        {
            ForceUpdate = true;
        }
#endif

        protected void Update()
        {
            if (Component != null && Target != null)
            {
                var slotCount = Core.Settings.SlotCount;
                var textFontSize = Core.Settings.TextFontSize;
                var textOffsetY = Core.Settings.TextOffsetY;

                if (ForceUpdate)
                {
                    var newBinding = new InventoryItem[slotCount];
                    var oldBinding = Target.binding;

                    for (int i = 0; i < oldBinding.Length; i++)
                    {
                        if (i < newBinding.Length)
                        {
                            newBinding[i] = oldBinding[i];
                        }
                    }

                    Target.binding = newBinding;
                    Target.slotCount = slotCount;

                    component.Init(Target);

                    if (Icons != null && Icons.Length == slotCount)
                    {
                        var defaultText = HandReticle.main.compTextHand;

                        var labels = new Text[slotCount];

                        for (int i = 0; i < labels.Length; i++)
                        {
                            labels[i] = Instantiate(defaultText, Icons[i].transform, false);

                            labels[i].enabled = true;
                            labels[i].gameObject.SetActive(true);

                            labels[i].text = SlotsUtility.GetInputSlotName(i);
                            labels[i].fontSize = textFontSize;

                            labels[i].alignment = TextAlignmentOptions.Center;
                            labels[i].overflowMode = TextOverflowModes.Overflow;

                            labels[i].rectTransform.localScale = Vector3.one;
                            labels[i].rectTransform.localPosition = Vector3.zero;
                            labels[i].rectTransform.localRotation = Quaternion.identity;

                            labels[i].rectTransform.pivot = new Vector2(0.5f, 0.5f);
                            labels[i].rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                            labels[i].rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

                            labels[i].rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, labels[i].preferredHeight);
                            labels[i].rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, labels[i].preferredWidth);

                            labels[i].rectTransform.anchoredPosition = new Vector2(0f, Icons[i].rectTransform.rect.y - textOffsetY);
                        }

#if BELOWZERO
                        SlotsUtility.UpdateSlotBindings();
#endif
                        TooltipFactory.RefreshActionStrings();

                        ForceUpdate = false;
                    }
                }
            }
            else
            {
                ForceUpdate = true;
            }
        }
    }
}

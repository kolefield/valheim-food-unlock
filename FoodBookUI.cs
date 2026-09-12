using System;
using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace FoodUnlock
{
    public sealed partial class Plugin
    {
        private GameObject panel;
        private RectTransform listContent;
        private ScrollRect foodScroll;
        private Text statusText;
        private readonly Text[] slotLabels = new Text[3];
        private readonly Image[] slotIcons = new Image[3];
        private readonly UITooltip[] slotTips = new UITooltip[3];
        private readonly List<Button> eatButtons = new List<Button>();
        private readonly List<Text[]> assignmentLabels = new List<Text[]>();
        private readonly List<Button> sortButtons = new List<Button>();
        private GameObject tooltipPrefab;
        private int sortMode;
        private float nextBookUpdate;
        private static readonly Color Gold = new Color(1f, 0.73f, 0.30f);
        private static readonly Vector2 TopLeft = new Vector2(0, 1);

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = TopLeft;
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private Text Label(Transform parent, string text, float x, float y, float width, float height,
            int size = 18, bool heading = false)
        {
            var obj = GUIManager.Instance.CreateText(text, parent, TopLeft, TopLeft, Vector2.zero,
                heading ? GUIManager.Instance.NorseBold : GUIManager.Instance.AveriaSerif,
                size, heading ? Gold : Color.white, true, Color.black, width, height, false);
            Place(obj.GetComponent<RectTransform>(), x, y, width, height);
            var label = obj.GetComponent<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;
            return label;
        }

        private Button ButtonAt(Transform parent, string text, float x, float y, float width, float height, Action action)
        {
            var obj = GUIManager.Instance.CreateButton(text, parent, TopLeft, TopLeft, Vector2.zero, width, height);
            Place(obj.GetComponent<RectTransform>(), x, y, width, height);
            var button = obj.GetComponent<Button>();
            button.onClick.AddListener(() => action());
            // Food hotkeys are keyboard-driven; prevent navigation stealing focus after clicks.
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        private static Image ImageAt(Transform parent, float x, float y, float width, float height, Color color)
        {
            var obj = new GameObject("Food image", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            Place(obj.GetComponent<RectTransform>(), x, y, width, height);
            var image = obj.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private UITooltip Tooltip(GameObject target, ItemDrop.ItemData food)
        {
            var tip = target.AddComponent<UITooltip>();
            tip.m_tooltipPrefab = tooltipPrefab;
            SetTooltip(tip, food);
            return tip;
        }

        private static void SetTooltip(UITooltip tip, ItemDrop.ItemData food)
        {
            tip.m_topic = food != null ? DisplayName(food) : "Unassigned";
            tip.m_text = food != null ? Localization.instance.Localize(food.GetTooltip())
                : "Choose a food below and click Assign.";
        }

        private bool CreateBook()
        {
            if (GUIManager.CustomGUIFront == null || InventoryGui.instance == null) return false;
            try
            {
                var sourceTip = InventoryGui.instance.m_playerGrid.m_elementPrefab.GetComponentInChildren<UITooltip>(true);
                if (sourceTip == null || sourceTip.m_tooltipPrefab == null)
                    throw new InvalidOperationException("Valheim's inventory tooltip prefab is unavailable.");
                tooltipPrefab = sourceTip.m_tooltipPrefab;
                panel = GUIManager.Instance.CreateWoodpanel(GUIManager.CustomGUIFront.transform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 800, 760, false);
                var root = panel.GetComponent<RectTransform>();
                root.pivot = new Vector2(0.5f, 0.5f);
                panel.name = "FoodUnlockBook";
                Label(root, "FOOD BOOK", 28, 18, 560, 46, 34, true);
                Label(root, "Eat once to unlock. Hover a food for details.", 28, 65, 740, 28);
                for (var i = 0; i < 3; i++)
                {
                    var slot = i;
                    var x = 28 + i * 250;
                    var card = ImageAt(root, x, 106, 240, 104, new Color(0, 0, 0, 0.28f));
                    slotIcons[i] = ImageAt(card.transform, 10, 12, 48, 48, Color.white);
                    slotIcons[i].preserveAspect = true;
                    slotTips[i] = Tooltip(card.gameObject, null);
                    slotLabels[i] = Label(card.transform, "", 66, 8, 166, 58, 17);
                    ButtonAt(card.transform, "Clear", 66, 68, 100, 28, () =>
                    {
                        bookPlayer.m_customData.Remove(SlotKey + slot);
                        RefreshPermanentFood(bookPlayer);
                        RefreshAssignments();
                    });
                }
                Label(root, "Sort by", 28, 224, 84, 32);
                var names = new[] { "Name", "Health", "Stamina", "Eitr" };
                for (var i = 0; i < names.Length; i++)
                {
                    var mode = i;
                    sortButtons.Add(ButtonAt(root, names[i], 116 + i * 142, 224, 130, 34, () =>
                    {
                        sortMode = mode;
                        SortFoods();
                        RebuildRows();
                    }));
                }

                var viewport = ImageAt(root, 28, 274, 744, 366, new Color(0, 0, 0, 0.22f));
                viewport.gameObject.AddComponent<RectMask2D>();
                foodScroll = viewport.gameObject.AddComponent<ScrollRect>();
                foodScroll.viewport = viewport.rectTransform;
                foodScroll.horizontal = false;
                foodScroll.movementType = ScrollRect.MovementType.Clamped;
                foodScroll.scrollSensitivity = 320;
                var content = new GameObject("Foods", typeof(RectTransform));
                content.transform.SetParent(viewport.transform, false);
                listContent = content.GetComponent<RectTransform>();
                foodScroll.content = listContent;
                statusText = Label(root, "", 28, 650, 744, 42, 17);
                Label(root, "Scroll for more  •  World is not paused", 28, 705, 540, 30, 16);
                ButtonAt(root, "Close", 630, 701, 140, 36, Close);
                RebuildRows();
                UpdateBookScale();
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError("Could not create food book: " + ex);
                DestroyBook();
                bookPlayer.Message(MessageHud.MessageType.TopLeft, "Food book could not open. Check the mod log.");
                return false;
            }
        }

        private void SortFoods()
        {
            foods.Sort((a, b) =>
            {
                var compare = sortMode == 1 ? b.m_shared.m_food.CompareTo(a.m_shared.m_food)
                    : sortMode == 2 ? b.m_shared.m_foodStamina.CompareTo(a.m_shared.m_foodStamina)
                    : sortMode == 3 ? b.m_shared.m_foodEitr.CompareTo(a.m_shared.m_foodEitr) : 0;
                return compare != 0 ? compare : string.Compare(DisplayName(a), DisplayName(b), StringComparison.CurrentCulture);
            });
        }

        private void RebuildRows()
        {
            foreach (Transform child in listContent) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            eatButtons.Clear();
            assignmentLabels.Clear();
            const float rowHeight = 132;
            const float rowSpacing = 140;
            Place(listContent, 0, 0, 744, Mathf.Max(366, foods.Count * rowSpacing));
            for (var index = 0; index < foods.Count; index++)
            {
                var food = foods[index];
                var row = ImageAt(listContent, 4, index * rowSpacing + 4, 736, rowHeight,
                    new Color(0, 0, 0, index % 2 == 0 ? 0.23f : 0.10f));
                var icon = ImageAt(row.transform, 12, 18, 64, 64, Color.white);
                icon.sprite = food.GetIcon();
                icon.preserveAspect = true;
                Tooltip(row.gameObject, food);
                Label(row.transform, DisplayName(food), 92, 4, 474, 31, 22, true);
                var s = food.m_shared;
                Label(row.transform, $"Health {s.m_food:0}   Stamina {s.m_foodStamina:0}   Eitr {s.m_foodEitr:0}",
                    92, 35, 510, 27, 18);
                Label(row.transform, $"Health regen {s.m_foodRegen:0.##} HP / 10 sec (base)",
                    92, 62, 510, 25, 18);
                eatButtons.Add(ButtonAt(row.transform, "Eat", 616, 16, 102, 38, () => Eat(bookPlayer, food)));
                var labels = new Text[3];
                for (var i = 0; i < 3; i++)
                {
                    var slot = i;
                    var button = ButtonAt(row.transform, "", 92 + i * 164, 92, 152, 30, () =>
                    {
                        bookPlayer.m_customData[SlotKey + slot] = food.m_dropPrefab.name;
                        RefreshPermanentFood(bookPlayer);
                        status = DisplayName(food) + " assigned to " + slotKeys[slot].Value + ".";
                        RefreshAssignments();
                    });
                    labels[i] = button.GetComponentInChildren<Text>();
                }
                assignmentLabels.Add(labels);
            }
            if (foods.Count == 0) Label(listContent, "No foods unlocked yet. Eat a food to begin.", 24, 26, 680, 60, 22);
            foodScroll.verticalNormalizedPosition = 1;
            for (var i = 0; i < sortButtons.Count; i++) sortButtons[i].interactable = i != sortMode;
            RefreshAssignments();
            nextBookUpdate = 0;
        }

        private void RefreshAssignments()
        {
            for (var i = 0; i < 3; i++)
            {
                var name = AssignedFood(bookPlayer, i);
                var food = foods.Find(f => f.m_dropPrefab.name == name);
                slotLabels[i].text = slotKeys[i].Value + "\n" + (food != null ? DisplayName(food)
                    : name.Length == 0 ? "Unassigned" : "Unavailable");
                slotIcons[i].sprite = food?.GetIcon();
                slotIcons[i].enabled = food != null;
                SetTooltip(slotTips[i], food);
                for (var row = 0; row < foods.Count; row++)
                    if (assignmentLabels[row][i] != null)
                        assignmentLabels[row][i].text = (foods[row].m_dropPrefab.name == name ? "Assigned " : "Assign ") + slotKeys[i].Value;
            }
        }

        private void UpdateBookScale()
        {
            if (panel == null) return;
            var parent = panel.transform.parent as RectTransform;
            if (parent == null) return;
            var scale = Mathf.Min(1, (parent.rect.width - 32) / 800, (parent.rect.height - 32) / 760);
            panel.transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        }

        private void UpdateBook()
        {
            if (panel == null) { Close(); return; }
            if (Time.unscaledTime < nextBookUpdate) return;
            nextBookUpdate = Time.unscaledTime + 0.2f;
            UpdateBookScale();
            for (var i = 0; i < eatButtons.Count; i++) eatButtons[i].interactable = bookPlayer.CanEat(foods[i], false);
            statusText.text = string.IsNullOrEmpty(status)
                ? (permanentAssignedFood.Value
                    ? "Assigned foods stay at full strength through death. Clear a slot to remove its effect."
                    : "Assign three foods above, then use their keys during play.") : status;
        }

        private void DestroyBook()
        {
            if (panel != null) { panel.SetActive(false); Destroy(panel); }
            panel = null;
            eatButtons.Clear();
            assignmentLabels.Clear();
            sortButtons.Clear();
        }
    }
}

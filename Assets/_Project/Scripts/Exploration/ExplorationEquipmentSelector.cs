using UnityEngine;
using UnityEngine.UI;
using YesterdayMap.Core;
using YesterdayMap.Scavenge;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YesterdayMap.Exploration
{
    public enum ExplorationEquipment
    {
        None,
        KitchenKnife,
        BaseballBat
    }

    public sealed class ExplorationEquipmentSelector : MonoBehaviour
    {
        private const string Weapon1AssetPath =
            "Assets/_Project/Scenes/UI/item_icon/weapon1.png";
        private const string Weapon2AssetPath =
            "Assets/_Project/Scenes/UI/item_icon/weapon2.png";

        private static readonly Color AvailableColor = Color.white;
        private static readonly Color OwnedUnselectedColor =
            new(0.35f, 0.35f, 0.35f, 0.82f);
        private static readonly Color MissingSilhouetteColor =
            new(0f, 0f, 0f, 1f);
        private static readonly Color TransparentBackground = new(0f, 0f, 0f, 0f);

        [Header("Legacy")]
        [SerializeField] private Dropdown equipmentDropdown;
        [SerializeField] private Text availabilityText;

        [Header("Image selection")]
        [SerializeField] private Sprite weapon1Sprite;
        [SerializeField] private Sprite weapon2Sprite;
        [SerializeField] private Texture2D weapon1Texture;
        [SerializeField] private Texture2D weapon2Texture;
        [SerializeField] private Button weapon1Button;
        [SerializeField] private Button weapon2Button;
        [SerializeField] private Image weapon1Icon;
        [SerializeField] private Image weapon2Icon;

        private Button weapon1HitAreaButton;
        private Button weapon2HitAreaButton;
        private bool hasKnife;
        private bool hasBat;
        private bool externalInteractable = true;

        public ExplorationEquipment SelectedEquipment { get; private set; }
        public bool UseEquipment => SelectedEquipment != ExplorationEquipment.None;

        public void Configure(Dropdown dropdown, Text statusText)
        {
            equipmentDropdown = dropdown;
            availabilityText = statusText;
            EnsureImageSelectionUI();
            BindButtons();
            Refresh();
        }

        public void ConfigureImageSelection(
            Button knifeButton,
            Button batButton,
            Image knifeIcon,
            Image batIcon,
            Text statusText)
        {
            weapon1Button = knifeButton;
            weapon2Button = batButton;
            weapon1Icon = knifeIcon;
            weapon2Icon = batIcon;
            availabilityText = statusText;
            RestoreWeaponVisualLayout();
            EnsureWeaponHitAreas();
            BindButtons();
            Refresh();
        }

        private void OnEnable()
        {
            EnsureImageSelectionUI();
            BindButtons();
            Refresh();
        }

        private void OnDestroy() => UnbindButtons();

        public void Refresh()
        {
            EnsureImageSelectionUI();
            ReloadEditorAssetsIfMissing();
            weapon1Sprite = ResolveSprite(weapon1Sprite, weapon1Texture);
            weapon2Sprite = ResolveSprite(weapon2Sprite, weapon2Texture);
            SetIconSprite(weapon1Icon, weapon1Sprite);
            SetIconSprite(weapon2Icon, weapon2Sprite);
            GameSession session = GameSession.Instance;
            hasKnife = session != null && session.GetCollected(ScavengeItemKind.KitchenKnife) > 0;
            hasBat = session != null && session.GetCollected(ScavengeItemKind.BaseballBat) > 0;

            if ((SelectedEquipment == ExplorationEquipment.KitchenKnife && !hasKnife) ||
                (SelectedEquipment == ExplorationEquipment.BaseballBat && !hasBat))
                SelectedEquipment = ExplorationEquipment.None;

            RefreshVisuals();
        }

        public void SetInteractable(bool interactable)
        {
            externalInteractable = interactable;
            RefreshVisuals();
        }

        private void SelectKnife()
        {
            if (!externalInteractable || !hasKnife) return;
            SelectedEquipment = SelectedEquipment == ExplorationEquipment.KitchenKnife
                ? ExplorationEquipment.None
                : ExplorationEquipment.KitchenKnife;
            RefreshVisuals();
        }

        private void SelectBat()
        {
            if (!externalInteractable || !hasBat) return;
            SelectedEquipment = SelectedEquipment == ExplorationEquipment.BaseballBat
                ? ExplorationEquipment.None
                : ExplorationEquipment.BaseballBat;
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            SetWeaponVisual(weapon1Button, weapon1Icon, hasKnife,
                SelectedEquipment == ExplorationEquipment.KitchenKnife);
            SetWeaponVisual(weapon2Button, weapon2Icon, hasBat,
                SelectedEquipment == ExplorationEquipment.BaseballBat);
            SetHitAreaInteractable(weapon1HitAreaButton, hasKnife);
            SetHitAreaInteractable(weapon2HitAreaButton, hasBat);

            if (availabilityText == null) return;
            availabilityText.text = SelectedEquipment switch
            {
                ExplorationEquipment.KitchenKnife => "선택 장비: 식칼",
                ExplorationEquipment.BaseballBat => "선택 장비: 야구방망이",
                _ when !hasKnife && !hasBat => "보유한 무기가 없습니다",
                _ => "무기 없이 탐사 가능"
            };
        }

        private void SetWeaponVisual(Button button, Image icon, bool owned, bool selected)
        {
            if (button != null)
            {
                button.interactable = externalInteractable && owned;
                if (button.targetGraphic is Image background)
                    background.color = TransparentBackground;
            }
            if (icon != null)
            {
                icon.color = !owned
                    ? MissingSilhouetteColor
                    : selected
                        ? AvailableColor
                        : OwnedUnselectedColor;
            }
        }

        private void EnsureImageSelectionUI()
        {
            if (weapon1Button != null && weapon2Button != null)
            {
                RestoreWeaponVisualLayout();
                EnsureWeaponHitAreas();
                if (equipmentDropdown != null) equipmentDropdown.gameObject.SetActive(false);
                return;
            }
            if (equipmentDropdown == null) return;
            RectTransform legacyRect = equipmentDropdown.transform as RectTransform;
            Transform parent = equipmentDropdown.transform.parent;
            if (legacyRect == null || parent == null) return;

            equipmentDropdown.gameObject.SetActive(false);
            if (availabilityText != null && availabilityText.transform is RectTransform statusRect)
            {
                statusRect.anchorMin = new Vector2(0.12f, 0.44f);
                statusRect.anchorMax = new Vector2(0.88f, 0.49f);
                statusRect.offsetMin = Vector2.zero;
                statusRect.offsetMax = Vector2.zero;
            }
            RectTransform root = parent.Find("WeaponImageSelection") as RectTransform;
            if (root == null)
            {
                root = new GameObject("WeaponImageSelection", typeof(RectTransform)).GetComponent<RectTransform>();
                root.SetParent(parent, false);
            }
            root.anchorMin = new Vector2(0.15f, 0.28f);
            root.anchorMax = new Vector2(0.85f, 0.46f);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.anchoredPosition = new Vector2(0f, -30f);

            weapon1Button = EnsureWeaponButton(root, "Weapon1Button", weapon1Sprite,
                new Vector2(0f, 0f), new Vector2(0.65f, 1f), out weapon1Icon);
            weapon2Button = EnsureWeaponButton(root, "Weapon2Button", weapon2Sprite,
                new Vector2(0.35f, 0f), new Vector2(1f, 1f), out weapon2Icon);
            RestoreWeaponVisualLayout();
            EnsureWeaponHitAreas();
        }

        private void RestoreWeaponVisualLayout()
        {
            SetButtonAnchors(
                weapon1Button,
                new Vector2(0f, 0f),
                new Vector2(0.65f, 1f));
            SetButtonAnchors(
                weapon2Button,
                new Vector2(0.35f, 0f),
                new Vector2(1f, 1f));
        }

        private void EnsureWeaponHitAreas()
        {
            if (weapon1Button == null || weapon2Button == null)
            {
                return;
            }

            Transform parent = weapon1Button.transform.parent;
            if (parent == null || weapon2Button.transform.parent != parent)
            {
                return;
            }

            if (weapon1Button.targetGraphic != null)
            {
                weapon1Button.targetGraphic.raycastTarget = false;
            }
            if (weapon2Button.targetGraphic != null)
            {
                weapon2Button.targetGraphic.raycastTarget = false;
            }

            weapon1HitAreaButton = EnsureHitAreaButton(
                parent,
                "Weapon1HitArea",
                new Vector2(0f, 0f),
                new Vector2(0.58f, 1f));
            weapon2HitAreaButton = EnsureHitAreaButton(
                parent,
                "Weapon2HitArea",
                new Vector2(0.60f, 0f),
                new Vector2(1f, 1f));
        }

        private static Button EnsureHitAreaButton(
            Transform parent,
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            Transform existing = parent.Find(objectName);
            GameObject root = existing != null
                ? existing.gameObject
                : new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = root.GetComponent<Image>();
            image.color = TransparentBackground;
            image.raycastTarget = true;

            Button button = root.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            root.transform.SetAsLastSibling();
            return button;
        }

        private static void SetButtonAnchors(
            Button button,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            if (button == null ||
                button.transform is not RectTransform rect)
            {
                return;
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void SetHitAreaInteractable(Button button, bool owned)
        {
            if (button != null)
            {
                button.interactable = externalInteractable && owned;
            }
        }

        private Button EnsureWeaponButton(Transform parent, string objectName, Sprite iconSprite,
            Vector2 anchorMin, Vector2 anchorMax, out Image icon)
        {
            Transform existing = parent.Find(objectName);
            GameObject root = existing != null ? existing.gameObject :
                new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image background = root.GetComponent<Image>();
            background.color = TransparentBackground;
            Button button = root.GetComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            icon = EnsureChildImage(root.transform, "ItemIcon", iconSprite);
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0.125f, 0.125f);
            iconRect.anchorMax = new Vector2(0.875f, 0.875f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            Transform obsoleteCircle = root.transform.Find("SelectionCircle");
            if (obsoleteCircle != null) obsoleteCircle.gameObject.SetActive(false);
            Transform obsoleteMark = root.transform.Find("UnavailableMark");
            if (obsoleteMark != null) obsoleteMark.gameObject.SetActive(false);
            return button;
        }

        private static Image EnsureChildImage(Transform parent, string objectName, Sprite sprite)
        {
            Transform existing = parent.Find(objectName);
            GameObject child = existing != null ? existing.gameObject :
                new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child.transform.SetParent(parent, false);
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(1f, 1f);
            rect.offsetMax = new Vector2(-1f, -1f);
            Image image = child.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = sprite != null;
            return image;
        }

        private static void SetIconSprite(Image image, Sprite sprite)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        private void ReloadEditorAssetsIfMissing()
        {
#if UNITY_EDITOR
            if (weapon1Sprite == null)
                weapon1Sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Weapon1AssetPath);
            if (weapon2Sprite == null)
                weapon2Sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Weapon2AssetPath);
            if (weapon1Texture == null)
                weapon1Texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Weapon1AssetPath);
            if (weapon2Texture == null)
                weapon2Texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Weapon2AssetPath);
#endif
        }

        private static Sprite ResolveSprite(Sprite sprite, Texture2D texture)
        {
            if (sprite != null || texture == null) return sprite;
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        private void BindButtons()
        {
            UnbindButtons();
            Button knifeClickButton = weapon1HitAreaButton != null
                ? weapon1HitAreaButton
                : weapon1Button;
            Button batClickButton = weapon2HitAreaButton != null
                ? weapon2HitAreaButton
                : weapon2Button;
            knifeClickButton?.onClick.AddListener(SelectKnife);
            batClickButton?.onClick.AddListener(SelectBat);
        }

        private void UnbindButtons()
        {
            if (weapon1Button != null) weapon1Button.onClick.RemoveListener(SelectKnife);
            if (weapon2Button != null) weapon2Button.onClick.RemoveListener(SelectBat);
            if (weapon1HitAreaButton != null)
                weapon1HitAreaButton.onClick.RemoveListener(SelectKnife);
            if (weapon2HitAreaButton != null)
                weapon2HitAreaButton.onClick.RemoveListener(SelectBat);
        }
    }
}

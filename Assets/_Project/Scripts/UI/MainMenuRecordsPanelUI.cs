using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using YesterdayMap.Core;

namespace YesterdayMap.UI
{
    /// <summary>
    /// 메인 메뉴의 저장 슬롯과 생존 기록을 보여 주는 전용 패널입니다.
    /// 비활성화된 상태에서도 MainMenuController가 찾아 열 수 있습니다.
    /// </summary>
    public sealed class MainMenuRecordsPanelUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text contentText;
        [SerializeField] private Button closeButton;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        public void Configure(GameObject root, Text content, Button close)
        {
            panelRoot = root;
            contentText = content;
            closeButton = close;
            BindCloseButton();
            Close();
        }

        private void Awake()
        {
            BindCloseButton();
            Close();
        }

        private void OnDestroy()
        {
            closeButton?.onClick.RemoveListener(Close);
        }

        public void Open()
        {
            if (panelRoot == null) return;

            RefreshRecords();
            panelRoot.SetActive(true);

            // 메뉴와 다른 팝업 위에 확실히 표시되도록 최상위 형제로 올립니다.
            transform.SetAsLastSibling();
            panelRoot.transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        public void RefreshRecords()
        {
            if (contentText == null) return;

            StringBuilder builder = new StringBuilder(512);
            bool hasAnyRecord = false;

            for (int slot = 1; slot <= SaveGameManager.SlotCount; slot++)
            {
                if (!SaveGameManager.HasSaveInSlot(slot) ||
                    !SaveGameManager.TryReadSave(slot, out SaveGameData data))
                {
                    builder.Append(slot)
                        .Append("번 기록  |  비어 있음")
                        .AppendLine()
                        .AppendLine();
                    continue;
                }

                hasAnyRecord = true;
                int day = data.dayCycle != null ? Mathf.Max(1, data.dayCycle.currentDay) : 1;

                builder.Append(slot)
                    .Append("번 기록  |  ")
                    .Append(day)
                    .Append("일차");

                if (DateTime.TryParse(data.savedAtUtc, out DateTime savedAt))
                    builder.Append("  |  ").Append(savedAt.ToLocalTime().ToString("yyyy.MM.dd  HH:mm"));

                builder.AppendLine();

                string[] records = data.character != null
                    ? data.character.survivalRecords
                    : null;

                if (records == null || records.Length == 0)
                {
                    builder.AppendLine("  아직 남겨진 생존 기록이 없습니다.");
                }
                else
                {
                    int first = Mathf.Max(0, records.Length - 2);
                    for (int index = first; index < records.Length; index++)
                    {
                        string record = CleanRecord(records[index]);
                        if (!string.IsNullOrEmpty(record))
                            builder.Append("  • ").AppendLine(record);
                    }
                }

                builder.AppendLine();
            }

            if (!hasAnyRecord)
            {
                builder.Insert(0,
                    "아직 저장된 생존 기록이 없습니다.\n새로운 생존을 시작하면 이곳에 기록이 남습니다.\n\n");
            }

            contentText.text = builder.ToString().TrimEnd();
        }

        private void BindCloseButton()
        {
            if (closeButton == null) return;
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        private static string CleanRecord(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            string cleaned = value
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();

            const int maximumLength = 92;
            return cleaned.Length <= maximumLength
                ? cleaned
                : cleaned.Substring(0, maximumLength - 1) + "…";
        }
    }
}

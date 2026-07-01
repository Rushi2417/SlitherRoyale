using System;
using System.Collections.Generic;
using System.Linq;
using PlayFab.ClientModels;
using SlitherRoyale.Client.Audio;
using SlitherRoyale.Client.Backend;
using UnityEngine;
using UnityEngine.UI;

namespace SlitherRoyale.Client.UI
{
    /// <summary>
    /// Friends List Screen.
    /// Doc 05 Â§3.10: View friends, add friend via PlayFab ID, delete/remove friends.
    /// </summary>
    public class FriendsListScreen : UIScreen
    {
        private Transform _scrollContent;
        private InputField _addField;
        private Text _statusText;
        private Text _feedbackText;
        private List<GameObject> _entryRows = new List<GameObject>();

        protected override void Awake()
        {
            base.Awake();
            BuildUI();
        }

        private void BuildUI()
        {
            AddFullBg(InkVoid);

            // Subtle top gradient
            var topGrad = AddImage("TopGrad",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -240f), Vector2.zero,
                new Color(ArcViolet.r, ArcViolet.g, ArcViolet.b, 0.08f));
            topGrad.raycastTarget = false;

            // â”€â”€ Header â”€â”€
            var header = AddImage("Header",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -110f), Vector2.zero,
                new Color(0.04f, 0.05f, 0.08f, 0.95f));

            AddAnchoredText("FRIENDS LIST", Color.white, 24, FontStyle.Bold,
                Vector2.zero, Vector2.one, new Vector2(0f, -10f), Vector2.zero, header.transform);

            // Back button
            MakeBtn("â† BACK", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(8f, 8f), new Vector2(110f, -8f),
                new Color(0f, 0f, 0f, 0f), FogGrey, 16, header.transform,
                () => {
                    AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
                    ScreenManager.Instance.NavigateTo<HomeScreen>();
                });

            // â”€â”€ Add Friend Bar â”€â”€
            var addBar = AddImage("AddBar",
                new Vector2(0.05f, 1f), new Vector2(0.95f, 1f),
                new Vector2(0f, -180f), new Vector2(0f, -120f),
                CardBg);
            addBar.sprite = MakeRoundRectSprite(900, 60, 16);
            addBar.type = Image.Type.Sliced;

            // Input Field
            _addField = AddInputField(new Vector2(0.02f, 0.1f), new Vector2(0.72f, 0.9f), addBar.transform);

            // Add Button
            var addBtnGo = new GameObject("AddBtn");
            addBtnGo.transform.SetParent(addBar.transform, false);
            var art = addBtnGo.AddComponent<RectTransform>();
            art.anchorMin = new Vector2(0.75f, 0.15f); art.anchorMax = new Vector2(0.98f, 0.85f);
            art.offsetMin = art.offsetMax = Vector2.zero;

            var aImg = addBtnGo.AddComponent<Image>();
            aImg.color = BioMint;
            aImg.sprite = MakeRoundRectSprite(200, 42, 10);
            aImg.type = Image.Type.Sliced;

            var addBtn = addBtnGo.AddComponent<Button>();
            addBtn.targetGraphic = aImg;
            addBtn.onClick.AddListener(OnAddFriend);
            AddAnchoredText("ADD FRIEND", Color.black, 12, FontStyle.Bold, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, addBtnGo.transform);

            // Feedback Status Rows
            _statusText = AddText("", FogGrey, 13, new Vector2(0f, -200f));
            _statusText.GetComponent<RectTransform>().sizeDelta = new Vector2(800f, 30f);

            _feedbackText = AddText("", EmberCoral, 13, new Vector2(0f, -230f));
            _feedbackText.GetComponent<RectTransform>().sizeDelta = new Vector2(800f, 30f);

            // â”€â”€ Scroll List for Friends â”€â”€
            var scrollGo = new GameObject("FriendsScrollView");
            scrollGo.transform.SetParent(transform, false);
            var srt = scrollGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.05f, 0.05f);
            srt.anchorMax = new Vector2(0.95f, 0.85f);
            srt.offsetMin = new Vector2(0f, 40f);
            srt.offsetMax = new Vector2(0f, -40f);

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical   = true;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var vrt = viewport.AddComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.offsetMin = vrt.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.05f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = vrt;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var crt = content.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot     = new Vector2(0.5f, 1f);
            crt.offsetMin = new Vector2(0f, -800f);
            crt.offsetMax = Vector2.zero;
            _scrollContent = content.transform;
            scrollRect.content = crt;

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.padding = new RectOffset(8, 8, 16, 16);

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        public override async void OnEnter(ScreenManager sm, object data)
        {
            base.OnEnter(sm, data);
            _statusText.text = "";
            _feedbackText.text = "";
            if (_addField) _addField.text = "";
            await RefreshFriends();
        }

        private async System.Threading.Tasks.Task RefreshFriends()
        {
            // Clear old rows
            foreach (var row in _entryRows) Destroy(row);
            _entryRows.Clear();

            try
            {
                var friendsList = await PlayFabEconomy.GetFriendsListAsync();
                if (friendsList == null || friendsList.Count == 0)
                {
                    BuildEmptyRow("Your friends list is currently empty. Add a player by PlayFab ID!");
                }
                else
                {
                    foreach (var f in friendsList)
                    {
                        BuildFriendRow(f);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FriendsListScreen] Refresh failed: {e.Message}");
                BuildEmptyRow("Offline mode: Connect to internet to manage friends.");
            }
        }

        private void BuildFriendRow(FriendInfo friend)
        {
            var row = new GameObject($"FriendRow_{friend.FriendPlayFabId}");
            row.transform.SetParent(_scrollContent, false);
            var rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(860f, 84f);

            var bgImg = row.AddComponent<Image>();
            bgImg.color = CardBg;
            bgImg.sprite = MakeRoundRectSprite(860, 84, 12);
            bgImg.type = Image.Type.Sliced;
            _entryRows.Add(row);

            // Green/Grey Dot indicating presence
            bool isOnline = friend.Tags != null && friend.Tags.Contains("online");
            var dot = MakeAnchoredImage("StatusDot",
                new Vector2(0.03f, 0.5f), new Vector2(0.03f, 0.5f),
                new Vector2(-8f, -8f), new Vector2(8f, 8f),
                isOnline ? BioMint : FogGrey, row.transform);
            dot.sprite = MakeCircleSprite(Color.white, 32);

            // Name
            string name = friend.TitleDisplayName ?? friend.Username ?? "Unknown Friend";
            var nTxt = AddAnchoredText(name.ToUpper(), Color.white, 14, FontStyle.Bold,
                new Vector2(0.08f, 0.5f), new Vector2(0.6f, 0.9f),
                Vector2.zero, Vector2.zero, row.transform);
            nTxt.alignment = TextAnchor.MiddleLeft;

            // Status message
            string status = isOnline ? "ONLINE NOW" : "OFFLINE";
            var sTxt = AddAnchoredText(status, isOnline ? BioMint : FogGrey, 11, FontStyle.Bold,
                new Vector2(0.08f, 0.15f), new Vector2(0.6f, 0.45f),
                Vector2.zero, Vector2.zero, row.transform);
            sTxt.alignment = TextAnchor.MiddleLeft;

            // Remove Button on right
            var removeBtnGo = new GameObject("RemoveBtn");
            removeBtnGo.transform.SetParent(row.transform, false);
            var rrt = removeBtnGo.AddComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.72f, 0.2f); rrt.anchorMax = new Vector2(0.96f, 0.8f);
            rrt.offsetMin = rrt.offsetMax = Vector2.zero;

            var rImg = removeBtnGo.AddComponent<Image>();
            rImg.color = new Color(0.24f, 0.1f, 0.1f);
            rImg.sprite = MakeRoundRectSprite(200, 48, 10);
            rImg.type = Image.Type.Sliced;

            var btn = removeBtnGo.AddComponent<Button>();
            btn.targetGraphic = rImg;
            btn.onClick.AddListener(async () =>
            {
                AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
                btn.interactable = false;
                try
                {
                    bool ok = await PlayFabEconomy.RemoveFriendAsync(friend.FriendPlayFabId);
                    if (ok)
                    {
                        _statusText.text = "Friend removed.";
                        _statusText.color = BioMint;
                        await RefreshFriends();
                    }
                }
                catch (Exception ex)
                {
                    _feedbackText.text = "Failed to remove: " + ex.Message;
                }
                btn.interactable = true;
            });
            AddAnchoredText("REMOVE", EmberCoral, 11, FontStyle.Bold, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, removeBtnGo.transform);
        }

        private void BuildEmptyRow(string message)
        {
            var row = new GameObject("EmptyRow");
            row.transform.SetParent(_scrollContent, false);
            var rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(860f, 100f);

            var bgImg = row.AddComponent<Image>();
            bgImg.color = CardBg;
            bgImg.sprite = MakeRoundRectSprite(860, 100, 16);
            bgImg.type = Image.Type.Sliced;
            _entryRows.Add(row);

            AddAnchoredText(message, FogGrey, 13, FontStyle.Normal,
                new Vector2(0.05f, 0f), new Vector2(0.95f, 1f),
                Vector2.zero, Vector2.zero, row.transform);
        }

        private async void OnAddFriend()
        {
            string id = _addField.text.Trim();
            if (string.IsNullOrEmpty(id)) return;

            AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
            _statusText.text = "Sending friend request...";
            _statusText.color = FogGrey;
            _feedbackText.text = "";

            try
            {
                await PlayFabEconomy.AddFriendAsync(id);
                _statusText.text = "Friend added successfully!";
                _statusText.color = BioMint;
                _addField.text = "";
                await RefreshFriends();
            }
            catch (Exception ex)
            {
                _statusText.text = "";
                _feedbackText.text = "Failed to add friend. Verify PlayFab ID.";
                Debug.LogWarning($"[FriendsListScreen] Add failed: {ex.Message}");
            }
        }

        // â”€â”€ Input Field Builder Helper â”€â”€

        private InputField AddInputField(Vector2 ancMin, Vector2 ancMax, Transform parent)
        {
            var go = new GameObject("InputField");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = new Vector2(10f, 6f); rt.offsetMax = new Vector2(-10f, -6f);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.06f, 0.07f, 0.10f);
            img.sprite = MakeRoundRectSprite(500, 50, 8);
            img.type = Image.Type.Sliced;

            // Viewport text holder
            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var trt = txtGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(12f, 4f); trt.offsetMax = new Vector2(-12f, -4f);

            var txt = txtGo.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 15;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;

            var input = go.AddComponent<InputField>();
            input.textComponent = txt;

            // Placeholder
            var phGo = new GameObject("Placeholder");
            phGo.transform.SetParent(go.transform, false);
            var phrt = phGo.AddComponent<RectTransform>();
            phrt.anchorMin = Vector2.zero; phrt.anchorMax = Vector2.one;
            phrt.offsetMin = new Vector2(12f, 4f); phrt.offsetMax = new Vector2(-12f, -4f);

            var placeholderText = phGo.AddComponent<Text>();
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholderText.fontSize = 14;
            placeholderText.text = "Enter PlayFab ID...";
            placeholderText.color = FogGrey;
            placeholderText.alignment = TextAnchor.MiddleLeft;

            input.placeholder = placeholderText;
            return input;
        }

        // â”€â”€ General helpers â”€â”€

        private Text AddAnchoredText(string content, Color color, int size, FontStyle style,
            Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax, Transform parent)
        {
            var go = new GameObject("Txt");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            var txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = content; txt.color = color; txt.fontSize = size;
            txt.fontStyle = style; txt.alignment = TextAnchor.MiddleCenter;
            txt.supportRichText = true;
            return txt;
        }

        private Image MakeAnchoredImage(string name,
            Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax,
            Color color, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private void MakeBtn(string label, Vector2 ancMin, Vector2 ancMax,
            Vector2 offMin, Vector2 offMax, Color bg, Color textColor, int fontSize, Transform parent, Action onClick)
        {
            var go = new GameObject("Btn");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            var img = go.AddComponent<Image>();
            img.color = bg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
            AddAnchoredText(label, textColor, fontSize, FontStyle.Normal,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, go.transform);
        }
    }
}

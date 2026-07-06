using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BetterSubnautica.MonoBehaviours
{
    public class DebuggerController : AbstractSingletonContainer<DebuggerController, string, DebuggerController.Message>
    {
        public struct Message
        {
            public bool Prefix;
            public string Text;
        }

        public static string Prefix { get; set; } = Core.Harmony.Id;

        public override IDictionary<string, Message> Dict { get; } = new SortedDictionary<string, Message>();
        public IList<Message> List { get; } = new List<Message>();

        public GUIStyle Style { get; set; }

        public Rect Position { get; set; }

        public string Text
        {
            get
            {
                var sb = new StringBuilder();
                foreach (var item in Dict)
                {
                    sb.AppendLine(GenerateString(item.Value.Text, item.Key, item.Value.Prefix));
                }
                if (Dict.Count > 0 && List.Count > 0)
                {
                    sb.AppendLine();
                }
                foreach (var item in List)
                {
                    sb.AppendLine(GenerateString(item.Text, null, item.Prefix));
                }
                return sb.ToString();
            }
        }

        public int MinimumFontSize { get; } = 8;

        protected void Awake()
        {
            Style = new GUIStyle
            {
                clipping = TextClipping.Overflow
            };

            Style.normal.textColor = Color.white;

            Position = new Rect(0, 0, 0, 0);
        }

        protected override void Update()
        {
            Style.fontSize = (int)(MinimumFontSize * (Screen.width / 1920f) + MinimumFontSize * (Screen.height / 1080f));

            if (Input.GetKeyDown(KeyCode.Delete))
            {
                ClearMessages();
            }
        }

        protected void OnGUI()
        {
            GUI.Label(Position, Text, Style);
        }

        public void ShowWarning(string text, string key, bool prefix)
        {
            ErrorMessage.AddWarning(GenerateString(text, key, prefix));
        }

        private void AddMessage(Message message, string key = null)
        {
            if (key != null)
            {
                Dict[key] = message;
            }
            else
            {
                List.Add(message);
            }
        }

        public void AddMessage(string text, string key = null, bool prefix = true)
        {
            AddMessage(new Message() { Text = text, Prefix = prefix }, key);
        }

        public void RemoveMessage(string key)
        {
            if (Dict.ContainsKey(key))
            {
                Dict.Remove(key);
            }
        }

        public void ClearMessages()
        {
            Dict.Clear();
            List.Clear();
        }

        public string GenerateString(string value, string key, bool prefix = true)
        {
            return !string.IsNullOrWhiteSpace(value) ? (prefix ? "[" + Prefix + "] " : "") + (key != null ? key + ": " : string.Empty) + value : string.Empty;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WormCore
{
    public static class Localization
    {
        public enum Language { English, Spanish, Portuguese, Hindi, Indonesian }

        public static Language CurrentLanguage { get; set; } = Language.English;

        private static readonly Dictionary<string, string[]> Strings = new Dictionary<string, string[]>
        {
            ["play"] = new[] { "Play", "Jugar", "Jogar", "खेलें", "Main" },
            ["settings"] = new[] { "Settings", "Ajustes", "Configurações", "सेटिंग्स", "Pengaturan" },
            ["shop"] = new[] { "Shop", "Tienda", "Loja", "दुकान", "Toko" },
            ["battle_pass"] = new[] { "Battle Pass", "Pase de Batalla", "Passe de Batalha", "युद्ध पास", "Battle Pass" },
            ["leaderboard"] = new[] { "Leaderboard", "Clasificación", "Classificação", "लीडरबोर्ड", "Papan Peringkat" },
            ["friends"] = new[] { "Friends", "Amigos", "Amigos", "मित्र", "Teman" },
            ["play_now"] = new[] { "Play Now", "Jugar Ahora", "Jogar Agora", "अब खेलें", "Main Sekarang" },
            ["matchmaking"] = new[] { "Searching", "Buscando", "Procurando", "खोज रहे हैं", "Mencari" },
            ["kills"] = new[] { "Kills", "Muertes", "Abates", "हत्याएं", "Pembunuhan" },
            ["score"] = new[] { "Score", "Puntuación", "Pontuação", "स्कोर", "Skor" },
            ["coins"] = new[] { "Coins", "Monedas", "Moedas", "सिक्के", "Koin" },
            ["gems"] = new[] { "Gems", "Gemas", "Gemas", "रत्न", "Permata" },
            ["level"] = new[] { "Level", "Nivel", "Nível", "स्तर", "Level" },
            ["cancel"] = new[] { "Cancel", "Cancelar", "Cancelar", "रद्द करें", "Batal" },
            ["back"] = new[] { "Back", "Volver", "Voltar", "वापस", "Kembali" },
            ["equip"] = new[] { "Equip", "Equipar", "Equipar", "सुसज्जित", "Pakai" },
            ["purchase"] = new[] { "Purchase", "Comprar", "Comprar", "खरीदें", "Beli" },
            ["free"] = new[] { "Free", "Gratis", "Grátis", "मुफ्त", "Gratis" },
            ["premium"] = new[] { "Premium", "Premium", "Premium", "प्रीमियम", "Premium" },
            ["death_reason"] = new[] { "You were eliminated", "Has sido eliminado", "Você foi eliminado", "आप हटा दिए गए", "Anda tersingkir" },
            ["rematch"] = new[] { "Rematch", "Revancha", "Revanche", "पुनः मैच", "Pertandingan Ulang" },
            ["home"] = new[] { "Home", "Inicio", "Início", "होम", "Beranda" },
            ["boost"] = new[] { "Boost", "Impulso", "Impulso", "बूस्ट", "Dorongan" },
            ["loading"] = new[] { "Loading...", "Cargando...", "Carregando...", "लोड हो रहा है...", "Memuat..." },
            ["connecting"] = new[] { "Connecting...", "Conectando...", "Conectando...", "कनेक्ट हो रहा है...", "Menghubungkan..." },
            ["quests"] = new[] { "Quests", "Misiones", "Missões", "क्वेस्ट", "Misi" },
            ["daily"] = new[] { "Daily", "Diarias", "Diárias", "दैनिक", "Harian" },
            ["weekly"] = new[] { "Weekly", "Semanales", "Semanais", "साप्ताहिक", "Mingguan" },
        };

        public static string Get(string key, params object[] args)
        {
            if (Strings.TryGetValue(key, out var translations))
            {
                int langIndex = (int)CurrentLanguage;
                string text = langIndex >= 0 && langIndex < translations.Length
                    ? translations[langIndex]
                    : translations[0];
                return args.Length > 0 ? string.Format(text, args) : text;
            }
            return key;
        }

        public static void LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var regex = new Regex("\"(?<key>[^\"]+)\"\\s*:\\s*\\[(?<values>[^\\]]+)\\]");
                foreach (Match match in regex.Matches(json))
                {
                    string key = match.Groups["key"].Value;
                    string valuesPart = match.Groups["values"].Value;
                    var parts = new List<string>();
                    int start = 0;
                    for (int i = 0; i < valuesPart.Length; i++)
                    {
                        if (valuesPart[i] == ',' || i == valuesPart.Length - 1)
                        {
                            int end = i == valuesPart.Length - 1 ? i + 1 : i;
                            string val = valuesPart.Substring(start, end - start).Trim().Trim('"');
                            if (val.Length > 0) parts.Add(val);
                            start = i + 1;
                        }
                    }
                    if (parts.Count > 0)
                        Strings[key] = parts.ToArray();
                }
            }
            catch
            {
                // JSON parse failed — keep existing strings
            }
        }

        public static string[] GetSupportedLanguages()
        {
            return new[] { "English", "Spanish", "Portuguese", "Hindi", "Indonesian" };
        }
    }
}

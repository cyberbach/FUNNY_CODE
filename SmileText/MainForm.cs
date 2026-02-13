using System.Text;
using System.Runtime.InteropServices;

namespace SmileText;

public class MainForm : Form
{
    private TextBox inputTextBox = null!;
    private TextBox outputTextBox = null!;
    private Button toSmilesButton = null!;
    private Button toTextButton = null!;
    private Button editorButton = null!;

    // For drag by body
    private const int WM_NCHITTEST = 0x84;
    private const int HTCAPTION = 2;
    private const int HTCLIENT = 1;

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    public static Dictionary<char, string> SmileMap = new()
    {
        ['а'] = "🍉,🍍,🥑,🦈,🦙,🚗,🕊️,🎲",
        ['б'] = "🍌,🐏,🐿️,🦬,🐃,🦋,🐞,🍆,🥪,🥯,🥁,⚾,🥊,🦛,🐝",
        ['в'] = "🍇,🐺,🐫,🚁,🐦‍⬛,🌋,⚖️,🍒,💧,🎈,🧹,🛞",
        ['г'] = "🍐,🦢,🍄,🚚,🎸,🌍,🏔️,🐛,💐,🦍",
        ['д'] = "🐬,🍈,🌳,🏠,🛣️,☔,🐉,🐦,💰,🛋️,🚪",
        ['е'] = "🦝,🦄,🌲,🍽️,🗺️,🦌",
        ['ё'] = "🦔,🎄",
        ['ж'] = "🐞,🦒,🌰,🍮,🐸,💎,🍲,🪲",
        ['з'] = "🦓,🐍,🏰,⭐,☂️,🪞,🐇,🦷,🌽",
        ['и'] = "🦃,🧸,🏵️,✨,🌿,🦬",
        ['й'] = "🥛,🧘,💊,👣,🧶",
        ['к'] = "🐨,🐱,🐮,🦘,🐇,🐐,🐎,🐊,🐋,🦀,🐔,🌵,🍓,🥬,🥔,✏️,📚,🛞,🛏️,💍,🔑,🪨",
        ['л'] = "🦊,🦁,🐎,🐸,🦌,🏹,🛶,💡,🥄,🐆,🦙,🐦,🦭",
        ['м'] = "🐻,🐭,🥕,🚗,⚽,🌉,🌊,⚡,🍦,🍄,🍯,🐜,🧹,🎤",
        ['н'] = "🦏,✂️,🦶,👃,🔪,🧦,☁️,🌙,🧵,🎵,📰",
        ['о'] = "🐵,🐑,🦅,🐟,🦌,🥒,🫒,👓,🔥,🪟,🏝️,🔫,🕳️",
        ['п'] = "🐧,🐼,🦚,🦜,🐷,🕷️,🐝,🐓,🍕,🍪,🥧,🍅,🌻,🚂,⛵,✉️,🌴,🪐",
        ['р'] = "🐟,🦞,🌹,🌈,🚀,✋,✒️,🤖,🥂,🏞️,🧵,🍚,🐚",
        ['с'] = "🐘,🐶,🦉,🐷,🦩,🐿️,🦂,🐟,🧀,❄️,☀️,❤️,✈️,🏹,👢,🪑,🕯️,🧂,🔥",
        ['т'] = "🐯,🦭,🎂,📱,📺,🍽️,🪓,🌿,🚜,☁️,👠,📓,🕶️,👡,🛞",
        ['у'] = "🦆,🐌,👂,🎣,🖤,😊,🦔",
        ['ф'] = "🦩,🦉,🏮,🍎,🏁,⚽,🔥,📷,🎶",
        ['х'] = "🐹,🍞,🧊,🏒,🐕,🛕,🧪,🍑",
        ['ц'] = "🐤,🌸,🎪,📐,🌊,🎯",
        ['ч'] = "🐢,🫖,⌚,👤,🧳,🪱,🍒,👿,☕,🧄",
        ['ш'] = "🎩,🧣,🧢,🍫,🎈,♟️,🛞,🌲,🧥,🔊,🪶",
        ['щ'] = "🛐,🐶,🪥,🐟,🥢,🌿",
        ['ъ'] = "🪨,🧱,💎,🪓,⚒️,🏔️,🧊,🛡️",
        ['ы'] = "🧀,🧼,💨,👦,🐭",
        ['ь'] = "🛏️,🧸,☁️,🧶,🕊️,🍦,🪶",
        ['э'] = "🍦,🦩,🧝,⚡,🚜,📺,🗣️,🌊",
        ['ю'] = "🪀,👗,🧭,🏕️,😂,🪐",
        ['я'] = "🍎,🍓,🥚,🦎,⚓,🕳️,🏷️,🌍"
    };

    public MainForm()
    {
        Text = "SmileText";
        Width = 600;
        Height = 400;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;

        var inputLabel = new Label { Text = "Ввод:", Top = 20, Left = 20, Width = 50 };
        inputTextBox = new TextBox { Top = 20, Left = 80, Width = 480, Height = 100, Multiline = true };

        var outputLabel = new Label { Text = "Вывод:", Top = 140, Left = 20, Width = 50 };
        outputTextBox = new TextBox { Top = 140, Left = 80, Width = 480, Height = 100, Multiline = true };

        toSmilesButton = new Button { Text = "В смайлы", Top = 260, Left = 20, Width = 150 };
        toSmilesButton.Click += ToSmilesClick;

        toTextButton = new Button { Text = "В текст", Top = 260, Left = 190, Width = 150 };
        toTextButton.Click += ToTextClick;

        editorButton = new Button { Text = "Редактор смайлов", Top = 260, Left = 360, Width = 200 };
        editorButton.Click += EditorClick;

        Controls.AddRange(new Control[] { inputLabel, inputTextBox, outputLabel, outputTextBox, toSmilesButton, toTextButton, editorButton });
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_NCHITTEST)
        {
            base.WndProc(ref m);
            if ((int)m.Result == HTCLIENT)
            {
                m.Result = (IntPtr)HTCAPTION;
            }
            return;
        }
        base.WndProc(ref m);
    }

    private Random random = new Random();

    private void ToSmilesClick(object? sender, EventArgs e)
    {
        var input = inputTextBox.Text.ToLower();
        var sb = new StringBuilder();
        foreach (var c in input)
        {
            if (SmileMap.TryGetValue(c, out var smiles))
            {
                var smileList = smiles.Split(',').Select(s => s.Trim()).ToList();
                if (smileList.Count > 0)
                {
                    if (sb.Length > 0) sb.Append(' ');
                    var randomSmile = smileList[random.Next(smileList.Count)];
                    sb.Append(randomSmile);
                }
            }
            else if (c == ' ' || c == '\n' || c == '\r' || c == '\t')
            {
                sb.Append(' ');
            }
        }
        outputTextBox.Text = sb.ToString();
    }

    private void ToTextClick(object? sender, EventArgs e)
    {
        var input = outputTextBox.Text.ToLower();
        var words = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (var word in words)
        {
            foreach (var kvp in SmileMap)
            {
                var smileList = kvp.Value.Split(',').Select(s => s.Trim().ToLower()).ToList();
                if (smileList.Contains(word))
                {
                    sb.Append(kvp.Key);
                    break;
                }
            }
        }
        inputTextBox.Text = sb.ToString();
    }

    private void EditorClick(object? sender, EventArgs e)
    {
        var editor = new EditorForm();
        editor.ShowDialog();
    }
}

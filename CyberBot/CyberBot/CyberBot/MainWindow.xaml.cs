using System;
using System.Collections.Generic;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CyberBot
{
    public partial class MainWindow : Window
    {
        private string _userName = "";
        private string _lastTopic = "";
        private string _favouriteTopic = "";
        private bool _chatStarted = false;
        private readonly Random _random = new Random();
        private readonly HashSet<int> _usedIndexes = new HashSet<int>();
        private readonly SpeechSynthesizer _speechSynthesizer = new SpeechSynthesizer();

        // All keyword-based cybersecurity responses
        private readonly Dictionary<string, List<string>> _responses = new Dictionary<string, List<string>>
        {
            ["password"] = new List<string>
            {
                "Use at least 12 characters with uppercase, lowercase, numbers and symbols.",
                "Never reuse passwords across accounts — use a unique password for every site.",
                "Avoid using personal info like birthdays or pet names in passwords.",
                "Consider using a password manager like Bitwarden to store them safely.",
                "A passphrase like 'PurpleCat$Runs@Night' is strong and easier to remember.",
                "Change your passwords immediately if you suspect a breach.",
                "Never share your password with anyone, even someone claiming to be IT support."
            },
            ["phishing"] = new List<string>
            {
                "Be cautious of emails asking for personal info — legitimate companies rarely do this.",
                "Always check the sender's full email address, not just the display name.",
                "If an email feels urgent or threatening, slow down — that is a manipulation tactic.",
                "Hover over links before clicking to see where they actually lead.",
                "Look out for spelling mistakes and poor grammar — common signs of phishing.",
                "Never download attachments from unknown senders.",
                "Phishing can also happen via SMS (smishing) or phone calls (vishing)."
            },
            ["privacy"] = new List<string>
            {
                "Review your privacy settings on all apps and social media regularly.",
                "Limit the personal information you share publicly online.",
                "Use private browsing mode when on shared or public computers.",
                "Read app permissions carefully — does a flashlight app really need your contacts?",
                "Turn off location tracking on apps that do not actually need it.",
                "Use a private search engine like DuckDuckGo to reduce data tracking.",
                "Regularly audit which third-party apps have access to your accounts."
            },
            ["scam"] = new List<string>
            {
                "If something sounds too good to be true, it almost always is.",
                "Never send money or gift cards to someone you have only met online.",
                "Report scams to your country's cybercrime authority immediately.",
                "Be wary of unsolicited calls claiming to be from your bank or Microsoft.",
                "Online shopping scams often use fake websites that look like real stores.",
                "Always verify charity organisations before donating, especially after disasters."
            },
            ["vpn"] = new List<string>
            {
                "A VPN encrypts your internet connection, keeping your data private.",
                "Always use a VPN when connecting to public Wi-Fi in cafes or airports.",
                "Choose a reputable paid VPN — free ones often sell your data to advertisers.",
                "VPNs hide your IP address, making it harder for sites to track your location.",
                "A VPN does not make you fully anonymous — combine it with safe browsing habits.",
                "Check if your VPN has a no-logs policy, meaning they do not store your activity."
            },
            ["antivirus"] = new List<string>
            {
                "Keep your antivirus software updated to detect the latest threats.",
                "Run full system scans regularly, not just the quick scan.",
                "Antivirus alone is not enough — pair it with a firewall and safe habits.",
                "Be careful with free antivirus tools — some are actually malware themselves.",
                "Real-time protection is a key feature to look for in antivirus software.",
                "Antivirus cannot protect you from clicking on a phishing link — stay alert."
            },
            ["public wifi"] = new List<string>
            {
                "Avoid logging into sensitive accounts like banking on public Wi-Fi.",
                "Use a VPN on public Wi-Fi to encrypt your traffic.",
                "Turn off auto-connect to open Wi-Fi networks on your device.",
                "Hackers can set up fake hotspots like 'Free Airport WiFi' to steal your data.",
                "Stick to HTTPS websites when using public Wi-Fi for extra protection.",
                "Log out of accounts when done, especially on shared or public networks."
            },
            ["two factor"] = new List<string>
            {
                "Two-factor authentication adds a second verification step beyond your password.",
                "Even if a hacker gets your password, 2FA stops them from logging in.",
                "Use an authenticator app like Google Authenticator instead of SMS when possible.",
                "Enable 2FA on your email first — it is the key to all your other accounts.",
                "Never share your 2FA code with anyone, even someone claiming to be support."
            },
            ["backup"] = new List<string>
            {
                "Follow the 3-2-1 rule: 3 copies, 2 different media types, 1 offsite backup.",
                "Back up your data regularly — ransomware can encrypt all your files instantly.",
                "Test your backups occasionally to make sure they actually restore correctly.",
                "Keep at least one backup disconnected from the internet to protect from ransomware.",
                "Automate your backups so you never forget to do them manually."
            },
            ["safe browsing"] = new List<string>
            {
                "Always look for HTTPS in the address bar before entering any information.",
                "Avoid clicking on pop-up ads — they can lead to malicious sites.",
                "Keep your browser and extensions updated to patch security vulnerabilities.",
                "Use a browser extension like uBlock Origin to block malicious ads.",
                "Clear your cookies and cache regularly to reduce tracking.",
                "Be cautious of browser extensions — some can read everything you type."
            },
            ["hack"] = new List<string>
            {
                "If you think you have been hacked, change all your passwords immediately.",
                "Enable two-factor authentication on all accounts after a suspected hack.",
                "Check haveibeenpwned.com to see if your email appeared in a data breach.",
                "Review your accounts for suspicious activity like unknown logins.",
                "Inform your bank immediately if you suspect financial accounts are compromised.",
                "Run a full antivirus scan to check for malware left behind by attackers."
            }
        };

        // Sentiment keywords mapped to empathetic response prefixes
        private readonly Dictionary<string, string> _sentiments = new Dictionary<string, string>
        {
            ["worried"] = "It is completely understandable to feel that way. Let me help ease your concerns. ",
            ["scared"] = "Do not worry — knowledge is your best defence. Here is what you need to know: ",
            ["frustrated"] = "I hear you. Cybersecurity can feel overwhelming. Let us break it down simply. ",
            ["curious"] = "Great curiosity — that is the first step to staying safe online. ",
            ["confused"] = "No problem at all — let me explain that more clearly. ",
            ["angry"] = "I understand your frustration. Let us work through this together. ",
            ["nervous"] = "It is okay to feel nervous — being cautious online is actually a good thing. ",
            ["unsure"] = "No worries — that is exactly what I am here for. Let me clarify. "
        };

        // Phrases that trigger a follow-up tip on the last discussed topic
        private readonly List<string> _followUpPhrases = new List<string>
        {
            "tell me more", "explain more", "another tip", "more info",
            "give me another", "what else", "keep going", "go on",
            "more please", "continue", "elaborate", "anything else",
            "more details", "give me more", "and then"
        };

        public MainWindow()
        {
            InitializeComponent();
            ShowWelcomeBanner();
            SpeakGreeting();
        }

        // Speaks a voice greeting when the app launches
        private void SpeakGreeting()
        {
            _speechSynthesizer.Rate = 0;    // Speed: -10 (slow) to 10 (fast)
            _speechSynthesizer.Volume = 100;  // Volume: 0 to 100
            _speechSynthesizer.SpeakAsync(
                "Welcome to CyberBot, your personal cybersecurity assistant. " +
                "Please enter your name to get started."
            );
        }

        // Displays ASCII art welcome message as a bot bubble on startup
        private void ShowWelcomeBanner()
        {
            string banner =
                "  _____      ____   ____ _______ \n" +
                " / ____|    |  _ \\ / __ \\__   __|\n" +
                "| |   ______| |_) | |  | | | |  \n" +
                "| |  |______|  _ <| |  | | | |  \n" +
                "| |____     | |_) | |__| | | |  \n" +
                " \\_____|    |____/ \\____/  |_|  \n\n" +
                "----------------------------------------------------------\n" +
                "  Welcome to CyberBot - Your Cybersecurity Assistant\n" +
                "----------------------------------------------------------\n" +
                "  Enter your name above and click 'Start Chat' to begin.";

            AddBubble(banner, isUser: false);
        }

        // Handles the Start Chat button — validates name and opens the session
        private void StartChat_Click(object sender, RoutedEventArgs e)
        {
            string nameInput = NameBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(nameInput))
            {
                AddBubble("Please enter your name to begin.", isUser: false);
                return;
            }

            if (_chatStarted)
            {
                AddBubble("A session is already active. Type 'bye' to end it first.", isUser: false);
                return;
            }

            _userName = nameInput;
            _chatStarted = true;

            // Speak a personalised greeting when the session starts
            _speechSynthesizer.SpeakAsync($"Hello {_userName}, welcome to CyberBot. How can I help you stay safe online today?");

            AddBubble(
                $"Hello, {_userName}! Welcome to CyberBot.\n" +
                "I am here to help you stay safe online.\n\n" +
                "You can ask me about:\n" +
                "  Passwords, Phishing, Privacy, Scams, VPN,\n" +
                "  Antivirus, Public WiFi, Two-Factor Authentication,\n" +
                "  Backups, Safe Browsing, or Hacking.\n\n" +
                "Type 'help' at any time to see this list again.",
                isUser: false
            );

            // Lock the name box once the session has started
            NameBox.IsEnabled = false;
            UserInput.Focus();
        }

        // Handles the Send button click
        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            ProcessMessage();
        }

        // Allows the Enter key to send a message
        private void UserInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                ProcessMessage();
        }

        // Validates input, displays user bubble, gets bot response, displays bot bubble
        private void ProcessMessage()
        {
            if (!_chatStarted)
            {
                AddBubble("Please enter your name and click 'Start Chat' first.", isUser: false);
                return;
            }

            string userMessage = UserInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(userMessage))
                return;

            // Show user message on the right
            AddBubble(userMessage, isUser: true);

            // Get and show bot response on the left
            string response = GetResponse(userMessage);
            AddBubble(response, isUser: false);

            // End session if user said goodbye
            string lower = userMessage.ToLower();
            if (lower.Contains("bye") || lower.Contains("exit") || lower.Contains("quit"))
                EndSession();

            UserInput.Text = "";
        }

        // Creates and adds a styled chat bubble to the ChatPanel
        private void AddBubble(string message, bool isUser)
        {
            // Red for user, dark blue for bot
            var bubbleColor = isUser
                ? new SolidColorBrush(Color.FromRgb(233, 69, 96))
                : new SolidColorBrush(Color.FromRgb(15, 52, 96));

            // User bubbles align right, bot bubbles align left
            var alignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;

            var textBlock = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                FontSize = 13,
                FontFamily = new FontFamily("Consolas"),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 520,
                Padding = new Thickness(10, 8, 10, 8)
            };

            var bubble = new Border
            {
                Background = bubbleColor,
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(5, 4, 5, 4),
                HorizontalAlignment = alignment,
                Child = textBlock
            };

            ChatPanel.Children.Add(bubble);

            // Auto-scroll to the latest bubble
            ChatScroller.ScrollToEnd();
        }

        // Core response logic: sentiment, greetings, follow-ups, keywords, fallback
        private string GetResponse(string input)
        {
            string lower = input.ToLower().Trim();
            string prefix = DetectSentiment(lower);

            // Greetings
            if (ContainsWord(lower, "hello") || ContainsWord(lower, "hi") || ContainsWord(lower, "hey"))
                return $"{prefix}Hello {_userName}! How can I help you stay safe online today?";

            if (lower.Contains("how are you") || lower.Contains("how r u"))
                return $"I am running at full security, thanks for asking, {_userName}! How can I help?";

            if (lower.Contains("good morning"))
                return $"Good morning, {_userName}! Stay cyber-safe today. What would you like to know?";

            if (lower.Contains("good evening") || lower.Contains("good night"))
                return $"Good evening, {_userName}! Always lock your accounts before bed. Anything I can help with?";

            // Follow-up: deliver another tip on the last discussed topic
            foreach (var phrase in _followUpPhrases)
            {
                if (lower.Contains(phrase))
                {
                    if (!string.IsNullOrEmpty(_lastTopic) && _responses.ContainsKey(_lastTopic))
                    {
                        string tip = GetFreshTip(_lastTopic);
                        return $"{prefix}Here is another tip on {_lastTopic}:\n{tip}\n\nSay 'tell me more' for another tip, or ask about a different topic!";
                    }
                    return "What topic would you like more info on? Try asking about passwords, phishing, privacy, or scams!";
                }
            }

            // Memory: user expresses interest in a specific topic
            if (lower.Contains("i'm interested in") || lower.Contains("i am interested in")
                || lower.Contains("i want to know about"))
            {
                foreach (var key in _responses.Keys)
                {
                    if (lower.Contains(key))
                    {
                        _favouriteTopic = key;
                        _lastTopic = key;
                        _usedIndexes.Clear();
                        return $"Great! I will remember that you are interested in {key}.\n\n{GetFreshTip(key)}\n\nSay 'tell me more' for another {key} tip anytime!";
                    }
                }
            }

            // Keyword matching against the response dictionary
            foreach (var key in _responses.Keys)
            {
                if (lower.Contains(key))
                {
                    if (_lastTopic != key)
                    {
                        _usedIndexes.Clear();
                        _lastTopic = key;
                    }

                    string tip = GetFreshTip(key);

                    if (!string.IsNullOrEmpty(_favouriteTopic) && key == _favouriteTopic)
                        return $"{prefix}As someone interested in {_favouriteTopic}, here is a tip:\n{tip}\n\nSay 'tell me more' for another tip!";

                    return $"{prefix}{tip}\n\nSay 'tell me more' if you would like another tip on {key}!";
                }
            }

            // Help / topic list
            if (lower.Contains("what can i ask") || ContainsWord(lower, "help")
                || lower.Contains("topics") || lower.Contains("menu"))
                return "You can ask me about:\n- Passwords\n- Phishing\n- Privacy\n- Scams\n- VPN\n- Antivirus\n- Public WiFi\n- Two Factor Authentication\n- Backups\n- Safe Browsing\n- Hacking";

            if (lower.Contains("thank"))
                return $"You are welcome, {_userName}! Stay safe and stay smart online.";

            if (ContainsWord(lower, "bye") || lower.Contains("exit") || lower.Contains("quit"))
                return $"Goodbye {_userName}! Remember — stay alert, stay secure. See you next time!";

            // Favourite topic reminder
            if (!string.IsNullOrEmpty(_favouriteTopic) && (lower.Contains("remind") || lower.Contains("my interest")))
                return $"You told me earlier that you are interested in {_favouriteTopic}. Would you like another tip on that?";

            // Default fallback
            return $"I am not sure I understand that, {_userName}. Could you try rephrasing?\nType 'help' to see all the topics I can assist with!";
        }

        // Returns a tip not recently shown; resets the cycle when all tips have been used
        private string GetFreshTip(string topic)
        {
            var list = _responses[topic];

            if (_usedIndexes.Count >= list.Count)
                _usedIndexes.Clear();

            int index;
            do { index = _random.Next(list.Count); }
            while (_usedIndexes.Contains(index));

            _usedIndexes.Add(index);
            return list[index];
        }

        // Returns a sentiment prefix if an emotion keyword is found in input
        private string DetectSentiment(string input)
        {
            foreach (var sentiment in _sentiments)
            {
                if (input.Contains(sentiment.Key))
                    return sentiment.Value;
            }
            return "";
        }

        // Checks for a whole-word match to avoid false positives (e.g. "hi" inside "this")
        private bool ContainsWord(string input, string word)
        {
            int index = input.IndexOf(word);
            while (index >= 0)
            {
                bool beforeOk = index == 0 || !char.IsLetter(input[index - 1]);
                bool afterOk = index + word.Length == input.Length || !char.IsLetter(input[index + word.Length]);
                if (beforeOk && afterOk) return true;
                index = input.IndexOf(word, index + 1);
            }
            return false;
        }

        // Ends the session, shows favourite topic summary, and disables input
        private void EndSession()
        {
            _chatStarted = false;
            UserInput.IsEnabled = false;

            if (!string.IsNullOrEmpty(_favouriteTopic))
            {
                AddBubble(
                    $"Your favourite topic this session was: {_favouriteTopic}.\nKeep learning about it to stay protected.",
                    isUser: false
                );
            }

            AddBubble($"Session ended. Stay safe online, {_userName}.", isUser: false);

            // Speak a farewell message when the session ends
            _speechSynthesizer.SpeakAsync($"Goodbye {_userName}. Stay safe online. See you next time!");
        }
    }
}

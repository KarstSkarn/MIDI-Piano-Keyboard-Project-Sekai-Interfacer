using System;
using System.Xml.Serialization;
using NAudio.Midi;
using WindowsInput;
using WindowsInput.Native;

public static class Keyboard2SekaiInterfacer
{
    private static readonly InputSimulator inputSimulator = new InputSimulator();

    public static VirtualKeyCode[] keyCodes = new VirtualKeyCode[13];
    public static byte[] keyCounter = new byte[12];
    public static byte[] oldKeyCounter = new byte[12];

    public static int pedalControllerIndex = 64;
    public static int keyboardCutPoint = 64;
    public static byte layoutMode = 2;

    public static bool pedalStatus = false;
    public static DateTime lastPedal = DateTime.UtcNow;
    public static int pedalValue = 0;
    public static int oldPedalValue = 0;
    public static bool pedalTriggerOnPush = true;
    public static bool pedalTriggerOnRelease = true;

    public static object statusLock = new object();
    public static object keysLock = new object();
    public static object midiLock = new object();

    public class ConfigurationData
    {
        public byte layoutMode { get; set; } = 2;
        public int pedalControllerIndex { get; set; } = 64;
        public int keyboardCutPoint { get; set; } = 64;
        public bool triggerPedalOnPush { get; set; } = true;
        public bool triggerPedalOnRelease { get; set; } = true;

        public VirtualKeyCode[] keyCodes = new VirtualKeyCode[13];

        public ConfigurationData()
        {
            keyCodes[0] = VirtualKeyCode.VK_A; // W
            keyCodes[1] = VirtualKeyCode.VK_S; // B
            keyCodes[2] = VirtualKeyCode.VK_D; // W
            keyCodes[3] = VirtualKeyCode.VK_F; // B
            keyCodes[4] = VirtualKeyCode.VK_G; // W
            keyCodes[5] = VirtualKeyCode.VK_H; // W
            keyCodes[6] = VirtualKeyCode.VK_J; // B
            keyCodes[7] = VirtualKeyCode.VK_K; // W
            keyCodes[8] = VirtualKeyCode.VK_L; // B
            keyCodes[9] = VirtualKeyCode.OEM_3; // W
            keyCodes[10] = VirtualKeyCode.OEM_7; // B
            keyCodes[11] = VirtualKeyCode.OEM_2; // W
            keyCodes[12] = VirtualKeyCode.SPACE; // PEDAL
        }
    }

    public static void Main(string[] args)
    {
        for (int i = 0; i < 12; i++)
        {
            keyCounter[i] = 0;
        }

        keyCodes[0] = VirtualKeyCode.VK_A; // W
        keyCodes[1] = VirtualKeyCode.VK_S; // B
        keyCodes[2] = VirtualKeyCode.VK_D; // W
        keyCodes[3] = VirtualKeyCode.VK_F; // B
        keyCodes[4] = VirtualKeyCode.VK_G; // W
        keyCodes[5] = VirtualKeyCode.VK_H; // W
        keyCodes[6] = VirtualKeyCode.VK_J; // B
        keyCodes[7] = VirtualKeyCode.VK_K; // W
        keyCodes[8] = VirtualKeyCode.VK_L; // B
        keyCodes[9] = VirtualKeyCode.OEM_3; // W
        keyCodes[10] = VirtualKeyCode.OEM_7; // B
        keyCodes[11] = VirtualKeyCode.OEM_2; // W
        keyCodes[12] = VirtualKeyCode.SPACE; // PEDAL

        if (File.Exists("Configuration.xml"))
        {
            ConfigurationData loadedData = LoadObjectFromFile<ConfigurationData>("Configuration.xml");
            keyCodes = loadedData.keyCodes;
            layoutMode = loadedData.layoutMode;
            pedalControllerIndex = loadedData.pedalControllerIndex;
            keyboardCutPoint = loadedData.keyboardCutPoint;
            pedalTriggerOnPush = loadedData.triggerPedalOnPush;
            pedalTriggerOnRelease = loadedData.triggerPedalOnRelease;
        }
        else
        {
            ConfigurationData cfgData = new ConfigurationData();
            SaveObjectToFile<ConfigurationData>(cfgData, "Configuration.xml");
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("MIDI Keyboard to Project Sekai Interfacer");
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("   Version 1.0 - 01/2025");
        Console.WriteLine("");

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("Made with ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("<3");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(" by KarstSkarn   ");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("https://karstskarn.carrd.co");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("If you enjoy this program consider ");
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Write("donating");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(" @ ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("https://ko-fi.com/karstskarn");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("It helps me to keep this one and many other projects working!");
        Console.WriteLine("");

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("NOTE: ");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("This program is intended to work in tandem with the following Project Sekai BlueStacks keyboard layout");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("https://github.com/KarstSkarn/Karst-PSekai-PC-Control-Scheme");
        Console.WriteLine("");
        Console.WriteLine("");

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Searching for MIDI devices...");

        if (MidiIn.NumberOfDevices > 0)
        {
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($"[");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(i);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"] {MidiIn.DeviceInfo(i).ProductName}");
            }
        }
        else
        {
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No MIDI devices found! Ensure your devices are properly connected and restart the program!");
                Console.ReadKey();
            }
        }

        Console.WriteLine("");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("Select the MIDI device index to connect to: ");
        if (!int.TryParse(Console.ReadLine(), out int deviceIndex) || deviceIndex < 0 || deviceIndex >= MidiIn.NumberOfDevices)
        {
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid device index! Restart the program and ensure your devices are properly connected and you are inputting a valid MIDI device index!");
                Console.ReadKey();
            }
        }

        DateTime lastMessageTime = DateTime.UtcNow;
        TimeSpan timeout = TimeSpan.FromMilliseconds(100); // Timeout for no MIDI input
        bool reconnecting = false;

        while (true)
        {
            try
            {
                using (var midiIn = new MidiIn(deviceIndex))
                {
                    midiIn.MessageReceived += (sender, e) =>
                    {
                        lock (midiLock)
                        {
                            MidiIn_MessageReceived(sender, e);
                            lastMessageTime = DateTime.UtcNow; // Update last message time
                        }
                    };

                    midiIn.ErrorReceived += MidiIn_ErrorReceived;

                    Console.WriteLine("");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($"Connected to ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(MidiIn.DeviceInfo(deviceIndex).ProductName);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("Listening for MIDI events (Press Ctrl+C to stop)...");
                    Console.WriteLine("");

                    midiIn.Start();
                    Task.Run(() => CheckKeyStatus());

                    // Monitor health in a separate task
                    Task.Run(() =>
                    {
                        while (!reconnecting)
                        {
                            if ((DateTime.UtcNow - lastMessageTime) > timeout)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("MIDI input seems to have stopped. Attempting to reconnect...");
                                reconnecting = true;
                                midiIn.Stop();
                                break;
                            }
                            Thread.Sleep(3000);
                        }
                    });

                    Console.CancelKeyPress += (s, e) =>
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Stopping MIDI input...");
                        midiIn.Stop();
                        reconnecting = true; // Break the outer loop on manual stop
                    };

                    while (!reconnecting)
                    {
                        Thread.Sleep(1000); // Keep the main thread alive
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}. Retrying in 3 seconds...");
            }

            reconnecting = false;
            lock (statusLock)
            {
                for (int i = 0; i < 12; i++)
                {
                    keyCounter[i] = 0;
                }
            }
            Thread.Sleep(0); // Wait before reconnecting
        }
    }

    private static void MidiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
    {
        try
        {
            lock (midiLock)
            {
                // Parse the MIDI message
                var midiEvent = e.MidiEvent;
                // Console.WriteLine($"MIDI Event: {midiEvent}");

                if (midiEvent is NoteEvent noteEvent)
                {
                    //Console.WriteLine($"MIDI Event: {midiEvent}");

                    int noteNumber = noteEvent.NoteNumber;

                    if (midiEvent is not NoteOnEvent)
                    {
                        if (layoutMode == 1)
                        {
                            int noteModule = noteNumber % 12;
                            lock (statusLock)
                            {
                                keyCounter[noteModule]--;
                                if (keyCounter[noteModule] < 0) { keyCounter[noteModule] = 0; }
                            }

                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.Write($"> NOTE [");
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("OFF");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.Write("] (");
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write(noteNumber);
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.Write(") VKey [");
                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                            Console.Write(keyCodes[noteModule].ToString());
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine("]");
                        }
                        else
                        {
                            if (noteNumber < keyboardCutPoint)
                            {
                                int noteModule = (noteNumber % 12) / 2;
                                lock (statusLock)
                                {
                                    keyCounter[noteModule]--;
                                    if (keyCounter[noteModule] < 0) { keyCounter[noteModule] = 0; }
                                }

                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.Write($"> NOTE [");
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("OFF");
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.Write("] (");
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.Write(noteNumber);
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.Write(") VKey [");
                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                Console.Write(keyCodes[noteModule].ToString());
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.WriteLine("]");
                            }
                            else
                            {
                                int noteModule = 6 + ((noteNumber % 12) / 2);
                                lock (statusLock)
                                {
                                    keyCounter[noteModule]--;
                                    if (keyCounter[noteModule] < 0) { keyCounter[noteModule] = 0; }
                                }

                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.Write($"> NOTE [");
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("OFF");
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.Write("] (");
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.Write(noteNumber);
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.Write(") VKey [");
                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                Console.Write(keyCodes[noteModule].ToString());
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.WriteLine("]");
                            }
                        }
                    }

                    if (midiEvent is NoteOnEvent noteOnEvent)
                    {
                        if (layoutMode == 1)
                        {
                            int noteModule = noteNumber % 12;
                            lock (statusLock)
                            {
                                keyCounter[noteModule]++;
                                if (keyCounter[noteModule] < 0) { keyCounter[noteModule] = 0; }
                            }

                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.Write($"> NOTE [");
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("ON");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.Write("] (");
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write(noteOnEvent.NoteNumber);
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.Write(" - ");
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.Write(noteOnEvent.Velocity);
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.Write(") VKey [");
                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                            Console.Write(keyCodes[noteModule].ToString());
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine("]");
                        }
                        else
                        {
                            if (noteNumber < keyboardCutPoint)
                            {
                                int noteModule = (noteNumber % 12) / 2;
                                lock (statusLock)
                                {
                                    keyCounter[noteModule]++;
                                    if (keyCounter[noteModule] < 0) { keyCounter[noteModule] = 0; }
                                }

                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.Write($"> NOTE [");
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.Write("ON");
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.Write("] (");
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.Write(noteOnEvent.NoteNumber);
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.Write(" - ");
                                Console.ForegroundColor = ConsoleColor.Blue;
                                Console.Write(noteOnEvent.Velocity);
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.Write(") VKey [");
                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                Console.Write(keyCodes[noteModule].ToString());
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.WriteLine("]");
                            }
                            else
                            {
                                int noteModule = 6 + ((noteNumber % 12) / 2);
                                lock (statusLock)
                                {
                                    keyCounter[noteModule]++;
                                    if (keyCounter[noteModule] < 0) { keyCounter[noteModule] = 0; }
                                }

                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.Write($"> NOTE [");
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.Write("ON");
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.Write("] (");
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.Write(noteOnEvent.NoteNumber);
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.Write(" - ");
                                Console.ForegroundColor = ConsoleColor.Blue;
                                Console.Write(noteOnEvent.Velocity);
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.Write(") VKey [");
                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                Console.Write(keyCodes[noteModule].ToString());
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.WriteLine("]");
                            }
                        }
                    }
                }

                // Handle Control Change events (e.g., damper pedal, sustain pedal)
                else if (midiEvent is ControlChangeEvent controlChangeEvent)
                {
                    if ((int)controlChangeEvent.Controller == pedalControllerIndex)
                    {
                        pedalValue = controlChangeEvent.ControllerValue;
                        if (pedalTriggerOnRelease && pedalTriggerOnPush)
                        {
                            if (oldPedalValue != pedalValue)
                            {
                                lock (statusLock)
                                {
                                    if (!pedalStatus)
                                    {
                                        if ((DateTime.UtcNow - lastPedal) > TimeSpan.FromMilliseconds(50))
                                        {
                                            pedalStatus = true; // Mark the key as released

                                            lastPedal = DateTime.UtcNow;

                                            Console.ForegroundColor = ConsoleColor.Gray;
                                            Console.Write($"> PEDAL [");
                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.Write("TRIGGERED");
                                            Console.ForegroundColor = ConsoleColor.Gray;
                                            Console.Write("] (");
                                            Console.ForegroundColor = ConsoleColor.Cyan;
                                            Console.Write($"Controller Index [{pedalControllerIndex}]");
                                            Console.ForegroundColor = ConsoleColor.Gray;
                                            Console.Write(" - ");
                                            Console.ForegroundColor = ConsoleColor.Blue;
                                            Console.Write(pedalValue);
                                            Console.ForegroundColor = ConsoleColor.Gray;
                                            Console.WriteLine(")");
                                        }
                                    }
                                }
                            }
                        }
                        else if (pedalTriggerOnPush && !pedalTriggerOnRelease)
                        {
                            if (oldPedalValue < pedalValue)
                            {
                                lock (statusLock)
                                {
                                    if (!pedalStatus)
                                    {
                                        if ((DateTime.UtcNow - lastPedal) > TimeSpan.FromMilliseconds(50))
                                        {
                                            pedalStatus = true; // Mark the key as released

                                            lastPedal = DateTime.UtcNow;

                                            Console.ForegroundColor = ConsoleColor.Gray;
                                            Console.Write($"> PEDAL [");
                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.Write("TRIGGERED");
                                            Console.ForegroundColor = ConsoleColor.Gray;
                                            Console.Write("] (");
                                            Console.ForegroundColor = ConsoleColor.Cyan;
                                            Console.Write($"Controller Index [{pedalControllerIndex}]");
                                            Console.ForegroundColor = ConsoleColor.Gray;
                                            Console.Write(" - ");
                                            Console.ForegroundColor = ConsoleColor.Blue;
                                            Console.Write(pedalValue);
                                            Console.ForegroundColor = ConsoleColor.Gray;
                                            Console.WriteLine(")");
                                        }
                                    }
                                }
                            }
                        }
                        else if (!pedalTriggerOnPush && pedalTriggerOnRelease)
                        {
                            if (oldPedalValue > pedalValue)
                            {
                                lock (statusLock)
                                {
                                    if (!pedalStatus)
                                    {
                                        if ((DateTime.UtcNow - lastPedal) > TimeSpan.FromMilliseconds(50))
                                        {
                                            pedalStatus = true; // Mark the key as released

                                            lastPedal = DateTime.UtcNow;

                                            Console.ForegroundColor = ConsoleColor.Gray;
                                            Console.Write($"> PEDAL [");
                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.Write("TRIGGERED");
                                            Console.ForegroundColor = ConsoleColor.Gray;
                                            Console.Write("] (");
                                            Console.ForegroundColor = ConsoleColor.Cyan;
                                            Console.Write($"Controller Index [{pedalControllerIndex}]");
                                            Console.ForegroundColor = ConsoleColor.Gray;
                                            Console.Write(" - ");
                                            Console.ForegroundColor = ConsoleColor.Blue;
                                            Console.Write(pedalValue);
                                            Console.ForegroundColor = ConsoleColor.Gray;
                                            Console.WriteLine(")");
                                        }
                                    }
                                }
                            }
                        }
                        oldPedalValue = pedalValue;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("MIDI EVENT ERROR: " + ex.ToString());
        }
    }

    public static async void CheckKeyStatus()
    {
        while (true)
        {
            lock (statusLock)
            {
                for (int i = 0; i < 12; i++)
                {
                    if (keyCounter[i] > 0)
                    {
                        HoldKey(keyCodes[i]);
                    }
                    if (keyCounter[i] != oldKeyCounter[i])
                    {
                        if (keyCounter[i] <= 0)
                        {
                            ReleaseKey(keyCodes[i]);
                        }
                        oldKeyCounter[i] = keyCounter[i];
                    }
                }
                if (pedalStatus)
                {
                    HoldKey(keyCodes[12]);
                    Task.Delay(3);
                    ReleaseKey(keyCodes[12]);
                    pedalStatus = false;
                }
            }
            await Task.Delay(5);
        }
    }

    private static void MidiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"MIDI Error: {e.RawMessage}");
    }

    public static void HoldKey(VirtualKeyCode virtualKeyCode)
    {
        lock (keysLock)
        {
            inputSimulator.Keyboard.KeyDown(virtualKeyCode);
            //Console.WriteLine($"Key {virtualKeyCode} is being held down.");
        }
    }
    public static void ReleaseKey(VirtualKeyCode virtualKeyCode)
    {
        lock (keysLock)
        {
            inputSimulator.Keyboard.KeyUp(virtualKeyCode);
            //Console.WriteLine($"Key {virtualKeyCode} has been released.");
        }
    }

    public static void SaveObjectToFile<T>(T objectToSave, string filePath)
    {
        const int maxRetries = 100;
        const int minDelayMilliseconds = 20;
        const int maxDelayMilliseconds = 40;

        int retries = 0;
        Random random = new Random();

        while (retries < maxRetries)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (TextWriter writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, objectToSave);
                }

                // Save successful, exit the loop
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                retries++;
                int delayMilliseconds = random.Next(minDelayMilliseconds, maxDelayMilliseconds + 1);
                System.Threading.Thread.Sleep(delayMilliseconds);
                AppendToFile("ErrorLog.txt", ex.ToString());
            }
        }
    }
    public static T LoadObjectFromFile<T>(string filePath)
    {
        const int maxRetries = 100;
        const int minDelayMilliseconds = 20;
        const int maxDelayMilliseconds = 40;

        int retries = 0;
        Random random = new Random();

        while (retries < maxRetries)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (TextReader reader = new StreamReader(filePath))
                {
                    return (T)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                retries++;
                int delayMilliseconds = random.Next(minDelayMilliseconds, maxDelayMilliseconds + 1);
                System.Threading.Thread.Sleep(delayMilliseconds);
                AppendToFile("ErrorLog.txt", ex.ToString());
            }
        }
        return default(T);
    }

    public static void AppendToFile(string filePath, string content)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(filePath, append: true))
            {
                writer.WriteLine(content);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while writing the error log: {ex.Message}");
        }
    }
}

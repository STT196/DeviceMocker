using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PosHardwareTestApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            RefreshSerialPorts();
            UpdateTransportPanels();
        }

        private string TransportMode => ((TransportModeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "TCP").ToUpperInvariant();

        private async void PrintReceiptButton_Click(object sender, RoutedEventArgs e)
        {
            await SendPayloadAsync(BuildReceiptPayload(includeDrawerKick: false, includeCut: false), "Print receipt");
        }

        private async void OpenDrawerButton_Click(object sender, RoutedEventArgs e)
        {
            await SendPayloadAsync(BuildDrawerKickPayload(), "Open drawer");
        }

        private async void PrintAndOpenDrawerButton_Click(object sender, RoutedEventArgs e)
        {
            await SendPayloadAsync(BuildReceiptPayload(includeDrawerKick: true, includeCut: true), "Print receipt and open drawer");
        }

        private async void ResetDrawerStateButton_Click(object sender, RoutedEventArgs e)
        {
            await SendPayloadAsync(BuildResetDrawerStatePayload(), "Reset drawer state");
        }

        private async void CutPaperButton_Click(object sender, RoutedEventArgs e)
        {
            await SendPayloadAsync(BuildCutPayload(), "Cut paper");
        }

        private async void SendRawHexButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var payload = ParseHex(RawHexTextBox.Text);
                await SendPayloadAsync(payload, "Send raw ESC/POS bytes");
            }
            catch (Exception ex)
            {
                SetStatus($"Raw hex parse error: {ex.Message}", isError: true);
            }
        }

        private async void SampleNoSaleButton_Click(object sender, RoutedEventArgs e)
        {
            await SendPayloadAsync(BuildSampleNoSalePayload(), "Sample no-sale flow");
        }

        private void RefreshPortsButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshSerialPorts();
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            ActivityLogTextBox.Clear();
        }

        private void TransportModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateTransportPanels();
        }

        private async Task SendPayloadAsync(byte[] payload, string actionLabel)
        {
            try
            {
                LastPayloadTextBox.Text = ToHex(payload);
                await SendToTargetAsync(payload);
                AppendLog($"[{DateTime.Now:HH:mm:ss}] OK    {actionLabel}");
                SetStatus($"{actionLabel} sent successfully.", isError: false);
            }
            catch (Exception ex)
            {
                AppendLog($"[{DateTime.Now:HH:mm:ss}] FAIL  {actionLabel} -> {ex.Message}");
                SetStatus($"{actionLabel} failed: {ex.Message}", isError: true);
            }
        }

        private async Task SendToTargetAsync(byte[] payload)
        {
            if (TransportMode == "SERIAL")
            {
                var portName = SerialPortComboBox.SelectedItem?.ToString();
                if (string.IsNullOrWhiteSpace(portName))
                    throw new InvalidOperationException("Select a COM port first.");

                if (!int.TryParse(BaudRateTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var baudRate))
                    throw new InvalidOperationException("Invalid baud rate.");

                await Task.Run(() =>
                {
                    using var serialPort = new SerialPort(portName, baudRate);
                    serialPort.Open();
                    serialPort.Write(payload, 0, payload.Length);
                    serialPort.BaseStream.Flush();
                });
            }
            else
            {
                if (!int.TryParse(PortTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
                    throw new InvalidOperationException("Invalid TCP port.");

                using var client = new TcpClient();
                await client.ConnectAsync(HostTextBox.Text.Trim(), port);
                await using var stream = client.GetStream();
                await stream.WriteAsync(payload);
                await stream.FlushAsync();
            }
        }

        private byte[] BuildReceiptPayload(bool includeDrawerKick, bool includeCut)
        {
            var bytes = new List<byte>();
            bytes.AddRange(new byte[] { 0x1B, 0x40 });
            bytes.AddRange(new byte[] { 0x1B, 0x61, 0x01 });
            bytes.AddRange(new byte[] { 0x1B, 0x45, 0x01 });
            bytes.AddRange(ToAsciiBytes(StoreNameTextBox.Text));
            bytes.Add(0x0A);
            bytes.AddRange(new byte[] { 0x1B, 0x45, 0x00 });
            bytes.AddRange(ToAsciiBytes(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)));
            bytes.Add(0x0A);
            bytes.AddRange(new byte[] { 0x1B, 0x61, 0x00 });
            bytes.AddRange(ToAsciiBytes("------------------------------"));
            bytes.Add(0x0A);

            foreach (var line in SplitLines(ReceiptBodyTextBox.Text))
            {
                bytes.AddRange(ToAsciiBytes(line));
                bytes.Add(0x0A);
            }

            bytes.AddRange(ToAsciiBytes("------------------------------"));
            bytes.Add(0x0A);
            bytes.AddRange(new byte[] { 0x1B, 0x61, 0x01 });
            bytes.AddRange(ToAsciiBytes(FooterTextBox.Text));
            bytes.Add(0x0A);
            bytes.Add(0x0A);

            if (includeDrawerKick)
                bytes.AddRange(BuildDrawerKickPayload(includeInitialize: false));

            if (includeCut)
                bytes.AddRange(BuildCutPayload(includeInitialize: false));

            return bytes.ToArray();
        }

        private byte[] BuildDrawerKickPayload(bool includeInitialize = true)
        {
            var bytes = new List<byte>();
            if (includeInitialize)
                bytes.AddRange(new byte[] { 0x1B, 0x40 });
            bytes.AddRange(new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA });
            return bytes.ToArray();
        }

        private byte[] BuildCutPayload(bool includeInitialize = true)
        {
            var bytes = new List<byte>();
            if (includeInitialize)
                bytes.AddRange(new byte[] { 0x1B, 0x40 });
            bytes.AddRange(new byte[] { 0x1D, 0x56, 0x00 });
            return bytes.ToArray();
        }

        private byte[] BuildResetDrawerStatePayload()
        {
            var bytes = new List<byte>();
            bytes.AddRange(new byte[] { 0x1B, 0x40 });
            bytes.AddRange(ToAsciiBytes("<<DM_RESET_DRAWER>>"));
            bytes.Add(0x0A);
            return bytes.ToArray();
        }

        private byte[] BuildSampleNoSalePayload()
        {
            var bytes = new List<byte>();
            bytes.AddRange(new byte[] { 0x1B, 0x40, 0x1B, 0x61, 0x01 });
            bytes.AddRange(ToAsciiBytes("NO SALE"));
            bytes.Add(0x0A);
            bytes.AddRange(new byte[] { 0x1B, 0x61, 0x00 });
            bytes.AddRange(ToAsciiBytes("Drawer test from sample app"));
            bytes.Add(0x0A);
            bytes.AddRange(BuildDrawerKickPayload(includeInitialize: false));
            bytes.AddRange(BuildCutPayload(includeInitialize: false));
            return bytes.ToArray();
        }

        private static byte[] ParseHex(string input)
        {
            var tokens = input
                .Split(new[] { ' ', '\r', '\n', '\t', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToArray();

            if (tokens.Length == 0)
                throw new InvalidOperationException("Enter at least one hex byte.");

            var bytes = new byte[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
                bytes[i] = Convert.ToByte(tokens[i], 16);

            return bytes;
        }

        private static IEnumerable<string> SplitLines(string text)
        {
            return (text ?? string.Empty).Replace("\r\n", "\n").Split('\n');
        }

        private static byte[] ToAsciiBytes(string value)
        {
            return Encoding.ASCII.GetBytes(value ?? string.Empty);
        }

        private static string ToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", " ", StringComparison.Ordinal);
        }

        private void RefreshSerialPorts()
        {
            var current = SerialPortComboBox.SelectedItem?.ToString();
            SerialPortComboBox.Items.Clear();

            foreach (var port in SerialPort.GetPortNames().OrderBy(x => x))
                SerialPortComboBox.Items.Add(port);

            if (SerialPortComboBox.Items.Count > 0)
            {
                var match = SerialPortComboBox.Items.Cast<object>().FirstOrDefault(x => string.Equals(x.ToString(), current, StringComparison.OrdinalIgnoreCase));
                SerialPortComboBox.SelectedItem = match ?? SerialPortComboBox.Items[0];
            }
        }

        private void UpdateTransportPanels()
        {
            if (TcpPanel == null || SerialPanel == null)
                return;

            var isSerial = TransportMode == "SERIAL";
            TcpPanel.Visibility = isSerial ? Visibility.Collapsed : Visibility.Visible;
            SerialPanel.Visibility = isSerial ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AppendLog(string message)
        {
            ActivityLogTextBox.AppendText(message + Environment.NewLine);
            ActivityLogTextBox.ScrollToEnd();
        }

        private void SetStatus(string message, bool isError)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = isError
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F45151"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1DBE72"));
        }
    }
}

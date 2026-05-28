# Quick Start Guide

## Your First Test: Scanner → Notepad

This test proves the core concept — DeviceMocker sends keystrokes to another application, just like a real barcode scanner.

### Steps

1. **Open Notepad** (or any text editor)
2. **Open DeviceMocker**
3. Go to **Devices** → click **SCAN** (Barcode / QR Scanner)
4. On the right panel, click **"Coca-Cola 330ml"** (`5449000000996`)
5. You get **3 seconds** — quickly click inside **Notepad**
6. Watch: `5449000000996` appears in Notepad, followed by Enter

### What just happened?

DeviceMocker simulated a barcode scanner:
- It typed the barcode digits one by one using the Windows SendInput API
- It pressed Enter at the end (the "suffix")
- Notepad received the input as if you typed it on a physical keyboard
- Your POS/inventory app would receive it the same way

### Next Steps

- Try the **Virtual Keyboard** — send individual keys and shortcuts
- Try the **Weighing Scale** — send weight values to your app
- Try the **Test Sequence Builder** — automate multi-step inputs
- Try **Batch Scan Mode** — fire 10 barcodes in a row to stress-test your app

## Understanding the 3-Second Countdown

Most device simulators have a **"Send After 3s"** button. This is because:

1. DeviceMocker sends keystrokes to the **currently focused window**
2. When you click a button in DeviceMocker, DeviceMocker has focus
3. The 3-second countdown gives you time to click your target app
4. After 3 seconds, the keystrokes go to whatever window is focused

You can change the countdown duration in **Settings → Countdown Seconds**.

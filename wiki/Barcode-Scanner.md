# Barcode / QR Scanner Simulator

Simulates a keyboard wedge barcode scanner — the most common type used in POS and inventory systems.

## How Real Scanners Work

Most barcode scanners act as a keyboard:
1. You scan a barcode
2. The scanner types the barcode digits as keystrokes
3. It presses Enter (or Tab) at the end
4. Your app receives the input in the focused text field

DeviceMocker does exactly this — without the physical scanner.

## Features

- **20 sample barcodes** — EAN-13, UPC-A, Code128, QR codes, EAN-8
- **Random barcode generator** — generates valid barcodes with correct check digits
- **Barcode type selector** — EAN-13, UPC-A, Code128, QR Code, EAN-8, Custom
- **Prefix / Suffix** — add characters before/after the barcode
- **Character delay** — simulate realistic typing speed
- **Batch scan mode** — fire N random barcodes with configurable interval
- **Scan history** — click to re-scan previous barcodes

## Usage

1. Go to **Devices → SCAN**
2. Enter a barcode value or click a sample barcode
3. Select suffix: **Enter** (most common), Tab, None, CR, LF, CRLF
4. Click **Send After 3s** → switch to your target app
5. The barcode appears in your app's focused input field

## Batch Scan Mode

For stress testing:
1. Enable **Batch Scan Mode**
2. Set **Count** (e.g., 10) and **Interval** (e.g., 1000ms)
3. Click **Start Batch Scan** → switch to your app
4. 10 random EAN-13 barcodes fire one after another

## Common Suffix Settings

| Scanner Type | Suffix |
|-------------|--------|
| Most POS scanners | Enter |
| Tab-to-next-field | Tab |
| Serial scanners | CR or CRLF |
| Raw data | None |
